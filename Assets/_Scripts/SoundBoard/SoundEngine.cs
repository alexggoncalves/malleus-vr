using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.SoundBoard
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEngine : Singleton<SoundEngine>
    {
        [SerializeField] private int maxVoices = 16;

        [Header("Envelope")] [SerializeField] private float attackTime = 0.01f;
        [SerializeField] private float decayTime = 0.15f;
        [SerializeField] private float releaseTime = 0.6f;
        [SerializeField, Range(0f, 1f)] private float sustainLevel = 0.4f;
        [SerializeField] private float sustainDuration = 0.4f;

        [Header("Audio Generation")] [SerializeField]
        private AudioSource audioSource;

        [SerializeField] private float secondaryWaveMultiplier = 1f;
        [SerializeField] private float pitchGlideTime = 0.03f;
        [SerializeField, Range(0f, 2f)] private float masterGain = 0.8f;

        [Header("Tuning")]
        [SerializeField] private float lengthAtLowFreq = 0.6f;   // meters — longest string
        [SerializeField] private float lowFrequency = 110f;      // Hz at lengthAtLowFreq
        [SerializeField] private float lengthAtHighFreq = 0.05f; // meters — shortest string
        [SerializeField] private float highFrequency = 880f;     // Hz at lengthAtHighFreq
        [SerializeField] private float minFrequency = 20f;
        [SerializeField] private float maxFrequency = 1200f;
        [SerializeField] private bool snapToChromaticScale;

        private readonly List<Voice> _voices = new();
        private double _sampleRate;

        protected override void Awake()
        {
            base.Awake();

            _sampleRate = AudioSettings.outputSampleRate;

            _voices.Clear();
            for (int i = 0; i < maxVoices; i++)
            {
                _voices.Add(new Voice());
            }

            if (!audioSource) audioSource = GetComponent<AudioSource>();
            audioSource.clip = AudioClip.Create("silence", (int)_sampleRate, 1, (int)_sampleRate, false);
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.Play();
        }

        public VoiceHandle PlayOrRetriggerString(VoiceHandle handle, PinType typeA, PinType typeB, float frequency,
            float velocity)
        {
            velocity = Mathf.Clamp01(velocity);

            if (handle.IsValid && _voices[handle.Index].Generation == handle.Generation)
            {
                return RetriggerString(handle, typeA, typeB, frequency, velocity);
            }

            return PlayString(typeA, typeB, frequency, velocity);
        }

        private VoiceHandle RetriggerString(VoiceHandle handle, PinType typeA, PinType typeB, float frequency,
            float velocity)
        {
            _voices[handle.Index].Retrigger(typeA, typeB, secondaryWaveMultiplier,
                frequency, velocity, _sampleRate,
                attackTime, decayTime, sustainLevel, sustainDuration, releaseTime,
                pitchGlideTime);

            return handle;
        }

        public VoiceHandle PlayString(PinType typeA, PinType typeB, float frequency, float velocity)
        {
            int index = GetFreeVoiceIndex();

            Voice voice = _voices[index];

            voice.Trigger(
                typeA, typeB, secondaryWaveMultiplier,
                frequency, velocity, _sampleRate,
                attackTime, decayTime, sustainLevel, sustainDuration, releaseTime,
                pitchGlideTime);

            return new VoiceHandle(index, voice.Generation);
        }

        public float LengthToFrequency(float length)
        {
            length = Mathf.Clamp(length, lengthAtHighFreq, lengthAtLowFreq);

            float totalSemitones = 12f * Mathf.Log(highFrequency / lowFrequency, 2f);
            float t = Mathf.InverseLerp(lengthAtLowFreq, lengthAtHighFreq, length); // 0 at low end, 1 at high end
            float semitones = t * totalSemitones;

            float frequency = lowFrequency * Mathf.Pow(2f, semitones / 12f);
            frequency = Mathf.Clamp(frequency, minFrequency, maxFrequency);

            if (snapToChromaticScale)
                frequency = QuantizeToChromaticScale(frequency);

            return frequency;
        }

        private float QuantizeToChromaticScale(float frequency)
        {
            float semitonesFromRef = 12f * Mathf.Log(frequency / lowFrequency, 2f);
            float nearestSemitone = Mathf.Round(semitonesFromRef);
            return lowFrequency * Mathf.Pow(2f, nearestSemitone / 12f);
        }

        public void SetFrequency(VoiceHandle handle, float frequency)
        {
            if (!handle.IsValid) return;
            Voice voice = _voices[handle.Index];
            if (voice.Generation != handle.Generation) return;

            if (snapToChromaticScale)
                frequency = QuantizeToChromaticScale(frequency);

            voice.SetFrequency(frequency);
        }

        private int GetFreeVoiceIndex()
        {
            int quietestIndex = -1;

            for (int i = 0; i < _voices.Count; i++)
            {
                if (!_voices[i].IsActive) return i;
                if (quietestIndex < 0 || _voices[i].CurrentGain < _voices[quietestIndex].CurrentGain)
                    quietestIndex = i;
            }

            return quietestIndex;
        }


        private void OnAudioFilterRead(float[] data, int channels)
        {
            bool useGlide = snapToChromaticScale;

            for (int i = 0; i < data.Length; i += channels)
            {
                float mixed = 0f;

                foreach (Voice voice in _voices)
                {
                    if (voice.IsActive)
                        mixed += voice.NextSample(useGlide);
                }

                mixed *= masterGain;
                mixed = mixed / (1f + Mathf.Abs(mixed));

                data[i] = mixed;
                if (channels == 2) data[i + 1] = mixed;
            }
        }

        public void SetChromaticSnap(bool on) => snapToChromaticScale = on;
    }
}