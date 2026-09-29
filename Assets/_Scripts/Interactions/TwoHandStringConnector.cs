using _Scripts.SoundBoard;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace _Scripts.Interactions
{
    public class TwoHandStringConnector : MonoBehaviour
    {
        [Header("Hands")] [SerializeField] private Hand leftHand;
        [SerializeField] private Hand rightHand;

        [Header("Poses")] [SerializeField] private ActiveStateSelector leftFullPinchSelector;
        [SerializeField] private ActiveStateSelector rightFullPinchSelector;

        [Header("String")] [SerializeField] private Material stringMaterial;
        [SerializeField] private float stringRadius = 0.004f;

        [Header("Interaction")] [SerializeField]
        private float pinFindRadius = 0.05f;
        [SerializeField] private float minSeparationForPreview = 0.05f;
        [SerializeField] private float maxTipToThumbDistance = 0.025f;
        [SerializeField] private float sessionStartMaxDistance = 0.05f; 

        private TubeRenderer stringPreview;
        private Renderer stringPreviewRenderer;

        private struct HandState
        {
            public bool isPinching;
            public Vector3 pinchPos;
        }

        private HandState left;
        private HandState right;

        private bool leftAttached;
        private bool rightAttached;
        private Pin leftPin;
        private Pin rightPin;
        private Vector3 leftAttachPos;
        private Vector3 rightAttachPos;

        public bool SessionActive { get; private set; }

        private void Awake()
        {
            GameObject go = new("StringPreview");
            go.transform.SetParent(transform, false);
            stringPreview = go.AddComponent<TubeRenderer>();
            stringPreview.points = new Vector3[2];
            stringPreviewRenderer = stringPreview.GetComponent<Renderer>();
            stringPreviewRenderer.material = stringMaterial;
            stringPreview.radius = stringRadius;
            stringPreview.MarkDynamic();
            stringPreviewRenderer.enabled = false;
        }

        private void Start()
        {
            if (!leftHand || !rightHand)
            {
                foreach (Hand h in FindObjectsByType<Hand>())
                {
                    if (!leftHand && h.Handedness == Handedness.Left) leftHand = h;
                    if (!rightHand && h.Handedness == Handedness.Right) rightHand = h;
                }
            }

            if (leftFullPinchSelector)
            {
                leftFullPinchSelector.WhenSelected += OnLeftPinchSelected;
                leftFullPinchSelector.WhenUnselected += OnLeftPinchUnselected;
            }

            if (rightFullPinchSelector)
            {
                rightFullPinchSelector.WhenSelected += OnRightPinchSelected;
                rightFullPinchSelector.WhenUnselected += OnRightPinchUnselected;
            }
        }

        private void OnDestroy()
        {
            if (leftFullPinchSelector)
            {
                leftFullPinchSelector.WhenSelected -= OnLeftPinchSelected;
                leftFullPinchSelector.WhenUnselected -= OnLeftPinchUnselected;
            }

            if (rightFullPinchSelector)
            {
                rightFullPinchSelector.WhenSelected -= OnRightPinchSelected;
                rightFullPinchSelector.WhenUnselected -= OnRightPinchUnselected;
            }
        }

        private void OnLeftPinchSelected()
        {
            left.isPinching = true;
            TryStartSession();
        }

        private void OnLeftPinchUnselected()
        {
            left.isPinching = false;
            if (!SessionActive) return;
            HandleRelease(left.pinchPos, right.pinchPos, ref leftAttached, ref leftPin, ref leftAttachPos);
        }

        private void OnRightPinchSelected()
        {
            right.isPinching = true;
            TryStartSession();
        }

        private void OnRightPinchUnselected()
        {
            right.isPinching = false;
            if (!SessionActive) return;
            HandleRelease(right.pinchPos, left.pinchPos, ref rightAttached, ref rightPin, ref rightAttachPos);
        }

        private void TryStartSession()
        {
            if (SessionActive) return;
            if (!left.isPinching || !right.isPinching) return;
            if (Vector3.Distance(left.pinchPos, right.pinchPos) > sessionStartMaxDistance) return;
 
            SessionActive = true;
        }


        private void Update()
        {
            UpdatePinchPos(leftHand, ref left);
            UpdatePinchPos(rightHand, ref right);

            if (SessionActive)
            {
                bool leftLost = left.isPinching && (!leftHand || !leftHand.IsConnected);
                bool rightLost = right.isPinching && (!rightHand || !rightHand.IsConnected);
                if (leftLost || rightLost)
                {
                    Cancel();
                    return;
                }
            }

            if (!SessionActive) return;
            UpdatePreview();
        }

        private void UpdatePinchPos(Hand hand, ref HandState state)
        {
            if (!hand || !hand.IsConnected) return;
            if (!hand.GetJointPose(HandJointId.HandThumbTip, out Pose thumb)) return;
            if (!hand.GetJointPose(HandJointId.HandIndexTip, out Pose index)) return;
            state.pinchPos = (thumb.position + index.position) * 0.5f;
        }
        
        private void HandleRelease(Vector3 releasePos, Vector3 otherLivePos, ref bool attached, ref Pin pin,
            ref Vector3 attachPos)
        {
            if (attached) return; // this end is already locked, nothing to do until finalize/cancel

            Pin found = FindPinNear(releasePos, pinFindRadius);

            if (found && found.IsOnBoard)
            {
                attached = true;
                pin = found;
                attachPos = found.ConnectionPoint(otherLivePos);
                
                pin.PlayStringAttachSound();

                if (leftAttached && rightAttached)
                {
                    TryFinalizeConnection();
                }
            }
            else
            {
                Cancel();
            }
        }

        private void UpdatePreview()
        {
            Vector3 p0 = leftAttached ? leftAttachPos : left.pinchPos;
            Vector3 p1 = rightAttached ? rightAttachPos : right.pinchPos;
 
            float sep = Vector3.Distance(p0, p1);
            stringPreviewRenderer.enabled = sep >= minSeparationForPreview || leftAttached || rightAttached;
 
            if (!stringPreviewRenderer.enabled ) return;

            Vector3 local0 = stringPreview.transform.InverseTransformPoint(p0);
            Vector3 local1 = stringPreview.transform.InverseTransformPoint(p1);
 
            stringPreview.points[0].Set(local0.x, local0.y, local0.z);
            stringPreview.points[1].Set(local1.x, local1.y, local1.z);
 
            stringPreview.points = stringPreview.points;
        }

        private void TryFinalizeConnection()
        {
            if (leftPin == rightPin || SoundString.Exists(leftPin, rightPin))
            {
                Cancel();
                return;
            }

            SoundString.Create(leftPin, rightPin, stringMaterial, stringRadius);
            Reset();
        }

        private Pin FindPinNear(Vector3 pos, float radius)
        {
            Pin best = null;
            float bestSqr = radius * radius;
            foreach (Pin pin in Pin.All)
            {
                if (!pin) continue;
                float d = (pin.Position - pos).sqrMagnitude;
                if (!(d <= bestSqr)) continue;
                bestSqr = d;
                best = pin;
            }

            return best;
        }

        private void Cancel()
        {
            Reset();
        }

        private void Reset()
        {
            SessionActive = false;
            leftAttached = false;
            rightAttached = false;
            leftPin = null;
            rightPin = null;
            stringPreviewRenderer.enabled = false;
        }
    }
}