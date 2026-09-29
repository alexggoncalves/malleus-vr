using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Scripts.SoundBoard
{
    public class SoundString : MonoBehaviour
    {
        public static readonly List<SoundString> All = new();

        [Header("Line Resolution")] [SerializeField]
        private float segmentLength = 0.01f;
        [SerializeField] private int minPoints = 8;
        [SerializeField] private int maxPoints = 128;
        
        [Header("Vibration")]
        [SerializeField] private float maxAmplitude = 0.012f;
        [SerializeField] private float vibrationDamping = 3.5f;
        [SerializeField] private float minVisualFrequency = 4f;
        [SerializeField] private float maxVisualFrequency = 16f;
        [SerializeField] private float referenceLength = 0.3f;

        public Pin PinA { get; private set; }
        public Pin PinB { get; private set; }


        private TubeRenderer line;
        private Renderer lineRenderer;
        private Vector3[] rest;
        private Vector3[] offset;
        private int pointCount;
        private float frequency;
        private const int Modes = 4;

        private float[,] modeShape;
        private readonly float[] modeAmplitude = new float[Modes];
        private readonly float[] modePhase = new float[Modes];

        private Vector3 vibrationAxis;
        private float vibrationAmplitude;
        private float vibrationTime;
        private float visualFrequency;

        public bool IsIntersecting { get; set; }

        private VoiceHandle voiceHandle = VoiceHandle.Invalid;

        // ReSharper disable Unity.PerformanceAnalysis
        public static SoundString Create(Pin a, Pin b, Material material, float radius)
        {
            GameObject gameObject = new GameObject($"SoundString{a.name}_{b.name}");

            SoundString s = gameObject.AddComponent<SoundString>();
            s.Init(a, b, material, radius);

            All.Add(s);
            return s;
        }

        private void Init(Pin a, Pin b, Material mat, float radius)
        {
            PinA = a;
            PinB = b;

            PinA.WhenDetached += OnPinDetached;
            PinB.WhenDetached += OnPinDetached;
            
            line = gameObject.AddComponent<TubeRenderer>();
            line.radius = radius;
            lineRenderer = line.GetComponent<Renderer>();
            lineRenderer.material = mat;

            RebuildLine();
        }

        private void LateUpdate()
        {
            if (!PinA || !PinB)
            {
                Cut();
                return;
            }
            
            vibrationTime += Time.deltaTime;
            
            UpdateLine();

            if (!SoundEngine.Instance) return;
            
            frequency = SoundEngine.Instance.LengthToFrequency(Length);

            if (voiceHandle.IsValid)
            {
                SoundEngine.Instance.SetFrequency(voiceHandle, frequency);
            }
        }

        private void UpdateLine()
        {
            Vector3 a = PinA.ConnectionPoint(PinB.Position);
            Vector3 b = PinB.ConnectionPoint(PinA.Position);

            Vector3 localA = line.transform.InverseTransformPoint(a);
            Vector3 localB = line.transform.InverseTransformPoint(b);

            float amplitude = vibrationAmplitude * Mathf.Exp(-vibrationDamping * vibrationTime);
            bool vibrating = amplitude > 0.00005f;

            if (vibrating)
            {
                for (int n = 0; n < Modes; n++)
                {
                    modePhase[n] = Mathf.Cos(2f * Mathf.PI * visualFrequency * (n + 1) * vibrationTime);
                }
            }

            Vector3 localAxis = line.transform.InverseTransformDirection(vibrationAxis);

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                rest[i] = Vector3.Lerp(localA, localB, t);

                Vector3 point = rest[i];

                if (vibrating)
                {
                    float displacement = 0f;
                    for (int n = 0; n < Modes; n++)
                    {
                        displacement += modeAmplitude[n] * modeShape[n, i] * modePhase[n];
                    }

                    point += localAxis * (displacement * amplitude);
                }

                line.points[i].Set(point.x, point.y, point.z);
            }

            line.points = line.points;
        }

        // Update line resolution
        private void RebuildLine()
        {
            Vector3 a = PinA.ConnectionPoint(PinB.Position);
            Vector3 b = PinB.ConnectionPoint(PinA.Position);
            float len = Vector3.Distance(a, b);

            pointCount = Mathf.Clamp(
                Mathf.CeilToInt(len / Mathf.Max(0.0001f, segmentLength)) + 1,
                minPoints, maxPoints);
            line.points = new Vector3[pointCount];
            rest = new Vector3[pointCount];
            offset = new Vector3[pointCount];
            
            modeShape = new float[Modes, pointCount];
            for (int n = 0; n < Modes; n++)
            {
                for (int i = 0; i < pointCount; i++)
                {
                    float t = i / (float)(pointCount - 1);
                    modeShape[n, i] = Mathf.Sin((n + 1) * Mathf.PI * t);
                }
            }

            line.MarkDynamic();
        }

        public static bool Exists(Pin a, Pin b)
        {
            return All.Any(s => (s.PinA == a && s.PinB == b) || (s.PinB == a && s.PinA == b));
        }

        private void OnPinDetached(Pin pin) => Cut();
        
        public void Cut()
        {
            if (PinA) PinA.WhenDetached -= OnPinDetached;
            if (PinB) PinB.WhenDetached -= OnPinDetached;
            
            All.Remove(this);
            Destroy(gameObject);
        }


        public void ClearColor() => SetSolidWhite();

        private void SetSolidWhite()
        {
            // Gradient gradient = new Gradient();
            // gradient.SetKeys(
            //     new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            //     new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            // );
            // line.colorGradient = gradient;
        }

        public void SetLocalGradientColor(Color color, float t, float marginLength, float smoothness)
        {
            // Gradient gradient = new Gradient();
            // float marginHalfT = (marginLength/2) / Length;
            // float smoothnessT = smoothness / Length;
            //
            // gradient.SetKeys(
            //     new GradientColorKey[]
            //     {
            //         new GradientColorKey(Color.white, 0.0f),
            //         new GradientColorKey(Color.white, t - marginHalfT - smoothnessT),
            //         new GradientColorKey(color, t - marginHalfT),
            //         new GradientColorKey(color, t + marginHalfT),
            //         new GradientColorKey(Color.white, t + marginHalfT + smoothnessT),
            //         new GradientColorKey(Color.white, 1.0f),
            //     },
            //     new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            // );
            //
            // line.colorGradient = gradient;
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable() => All.Remove(this);

        private void OnDestroy()
        {
            if (PinA) PinA.WhenDetached -= OnPinDetached;
            if (PinB) PinB.WhenDetached -= OnPinDetached;
        }

        private float Length => Vector3.Distance(
            PinA.ConnectionPoint(PinB.Position),
            PinB.ConnectionPoint(PinA.Position));

        public void Strum(float normalizedPosition, float normalizedVelocity, Vector3 direction = default)
        {
            StartVibration(normalizedPosition, normalizedVelocity, direction);

            if (!SoundEngine.Instance) return;

            voiceHandle = SoundEngine.Instance.PlayOrRetriggerString(
                voiceHandle, PinA.Type, PinB.Type, frequency, normalizedVelocity);
        }
        
        private void StartVibration(float normalizedPosition, float normalizedVelocity, Vector3 direction)
        {
            float pluck = Mathf.Clamp(normalizedPosition, 0.05f, 0.95f);
            float velocity = Mathf.Clamp01(normalizedVelocity);

            float peak = 0f;
            for (int n = 0; n < Modes; n++)
            {
                int mode = n + 1;
                modeAmplitude[n] = Mathf.Sin(mode * Mathf.PI * pluck) / (mode * mode);
                peak += Mathf.Abs(modeAmplitude[n]);
            }

            if (peak > 0.0001f)
            {
                for (int n = 0; n < Modes; n++) modeAmplitude[n] /= peak;
            }

            Vector3 axis = (PinB.Position - PinA.Position).normalized;

            Vector3 push = direction.sqrMagnitude > 0.000001f ? direction : Vector3.up;
            push = Vector3.ProjectOnPlane(push, axis);
            if (push.sqrMagnitude < 0.000001f) push = Vector3.ProjectOnPlane(Vector3.forward, axis);
            vibrationAxis = push.normalized;

            float lengthScale = Mathf.Clamp01(Length / Mathf.Max(0.0001f, referenceLength));

            vibrationAmplitude = maxAmplitude * velocity * lengthScale;
            vibrationTime = 0f;

            float k = Mathf.InverseLerp(Mathf.Log(20f), Mathf.Log(1200f), Mathf.Log(Mathf.Max(frequency, 20f)));
            visualFrequency = Mathf.Lerp(minVisualFrequency, maxVisualFrequency, k);
        }
    }
}