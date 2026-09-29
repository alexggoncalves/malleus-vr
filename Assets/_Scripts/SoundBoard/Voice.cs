using _Scripts.SoundBoard;
using UnityEngine;


public readonly struct VoiceHandle
{
    public static readonly VoiceHandle Invalid = new(-1, -1);

    internal readonly int Index;
    internal readonly int Generation;
    public bool IsValid => Index >= 0;

    internal VoiceHandle(int index, int generation)
    {
        this.Index = index;
        this.Generation = generation;
    }
}

public class Voice
{
    private enum Stage
    {
        Idle,
        Attack,
        Decay,
        Sustain,
        Release
    }

    private const double TwoPi = 2.0 * System.Math.PI;

    // Perceived-loudness weights. A square at 1.0 is far louder than a sine at 1.0.
    private const float SineGain = 1.00f;
    private const float SawGain = 0.45f;
    private const float SquareGain = 0.32f;

    // Each type sits at its own interval, so the pair is commutative.
    private const float SineRatio = 1.0f; // fundamental
    private const float SawRatio = 0.5f; // octave below
    private const float SquareRatio = 1.5f; // a fifth above

    // --- Per-type filter character ---------------------------------------------
    // This is what actually separates the combinations. Cutoff is a multiple of the
    // note's own frequency, so it tracks pitch instead of going muddy in the bass.

    private const float SineCutoff = 3f; // dark and pure
    private const float SquareCutoff = 6f; // mid, hollow
    private const float SawCutoff = 12f; // wide open

    private const float SineResonance = 0.0f;
    private const float SquareResonance = 0.55f; // the ringy one
    private const float SawResonance = 0.2f;

    // How much of the brightness follows the amplitude envelope. At 0 the filter
    // is static; at 1 the note darkens completely as it decays, like a real pluck.
    private const float BrightnessDecay = 0.7f;

    private PinType _typeA;
    private PinType _typeB;
    private float _multiplierB;

    private double _phaseA;
    private double _phaseB;
    private double _increment;
    private double _targetIncrement;
    private double _sampleRate;
    private float _glideAlpha;

    private Stage _stage = Stage.Idle;
    private float _velocity;

    private float _attackTime, _decayTime, _sustainLevel, _sustainDuration, _releaseTime;
    private float _stageTime;
    private float _releaseCoeff;

    private float _attackStartGain;
    private float _cutoffRatio;
    private float _resonance;
    private float _filterLow;
    private float _filterBand;


    public bool IsActive => _stage != Stage.Idle;
    public float CurrentGain { get; private set; }
    public int Generation { get; private set; }

    public void Trigger(
        PinType a, PinType b, float bMultiplier,
        float frequency, float vel, double sr,
        float attack, float decay, float sustain, float sustainDur, float release,
        float pitchGlideTime)
    {
        ApplyCommon(a, b, bMultiplier, frequency, vel, sr,
            attack, decay, sustain, sustainDur, release, pitchGlideTime);

        if (CurrentGain <= 0f)
        {
            _filterLow = 0f;
            _filterBand = 0f;
            _phaseA = 0.0;
            _phaseB = 0.0;
        }

        Generation++;
    }

    public void Retrigger(PinType a, PinType b, float bMultiplier,
        float frequency, float vel, double sr,
        float attack, float decay, float sustain, float sustainDur, float release,
        float pitchGlideTime)
    {
        ApplyCommon(a, b, bMultiplier, frequency, vel, sr,
            attack, decay, sustain, sustainDur, release, pitchGlideTime);
    }

    private void ApplyCommon(
        PinType a, PinType b, float bMultiplier,
        float frequency, float vel, double sr,
        float attack, float decay, float sustain, float sustainDur, float release,
        float pitchGlideTime)
    {
        _typeA = a;
        _typeB = b;
        _multiplierB = bMultiplier;
        _sampleRate = sr;

        _increment = _targetIncrement = FrequencyToIncrement(frequency);

        float dt = 1f / (float)_sampleRate;
        _glideAlpha = 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, pitchGlideTime));

        // Geometric mean of the two cutoffs, so a dark pin genuinely damps a bright
        // one instead of the average landing in the middle every time.
        _cutoffRatio = Mathf.Sqrt(CutoffFor(a) * CutoffFor(b));
        _resonance = Mathf.Max(ResonanceFor(a), ResonanceFor(b));

        _velocity = vel;

        // Ramp from wherever we currently are rather than from silence, so a retrigger
        // or a stolen voice doesn't step the amplitude in a single sample.
        _attackStartGain = CurrentGain;

        _attackTime = Mathf.Max(0.001f, attack);
        _decayTime = Mathf.Max(0.001f, decay);
        _sustainLevel = sustain;
        _sustainDuration = sustainDur;
        _releaseTime = Mathf.Max(0.001f, release);
        _releaseCoeff = Mathf.Exp(-6.9f / (_releaseTime * (float)_sampleRate));

        _stageTime = 0f;
        _stage = Stage.Attack;
    }

    public void SetFrequency(float frequency) => _targetIncrement = FrequencyToIncrement(frequency);

    private double FrequencyToIncrement(float frequency) => frequency * TwoPi / _sampleRate;

    public void Reset()
    {
        _stage = Stage.Idle;
        CurrentGain = 0f;
        _filterLow = 0f;
        _filterBand = 0f;
        _phaseA = 0.0;
        _phaseB = 0.0;
        Generation++;
    }

    public float NextSample(bool useGlide)
    {
        float dt = 1f / (float)_sampleRate;
        AdvanceEnvelope(dt);

        if (_stage == Stage.Idle) return 0f;

        // if (useGlide)
        //     _increment += (_targetIncrement - _increment) * _glideAlpha;
        // else
        _increment = _targetIncrement;

        double baseStep = _increment / TwoPi; // cycles per sample at the note's pitch
        double stepA = baseStep * RatioFor(_typeA);
        double stepB = baseStep * RatioFor(_typeB) * _multiplierB;

        _phaseA += stepA;
        if (_phaseA >= 1.0) _phaseA -= 1.0;
        _phaseB += stepB;
        if (_phaseB >= 1.0) _phaseB -= 1.0;

        float outA = Oscillator(_typeA, (float)_phaseA, (float)stepA);
        float outB = Oscillator(_typeB, (float)_phaseB, (float)stepB);
        float raw = 0.5f * (outA + outB);

        // Brightness tracks the envelope, so the note darkens as it dies away
        float noteHz = (float)(_increment * _sampleRate / TwoPi);

        // Envelope shape normalised back to 0..1, so a soft strum isn't permanently dark
        float envelope = CurrentGain / Mathf.Max(_velocity, 0.0001f);
        float brightness = Mathf.Lerp(1f - BrightnessDecay, 1f, envelope);

        return CurrentGain * Filter(raw, noteHz * _cutoffRatio * brightness);
    }

    /// <summary>
    /// Chamberlin state variable filter, lowpass output. Cheap, and the resonance is
    /// what gives each pin pairing an identifiable color.
    /// </summary>
    private float Filter(float input, float cutoffHz)
    {
        float q = Mathf.Max(0.06f, 1f - _resonance);

        // Keep the cutoff well below Nyquist; the Chamberlin topology gets inaccurate up there
        float maxHz = (float)_sampleRate * 0.24f;
        float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(cutoffHz, 20f, maxHz) / (float)_sampleRate);

        // The filter is only stable while f² + 2qf < 4, so cap f just under that bound.
        // Low resonance (large q) lowers the ceiling, which the 0.24 clamp alone doesn't cover.
        float fMax = 0.9f * (Mathf.Sqrt(q * q + 4f) - q);
        f = Mathf.Min(f, fMax);

        _filterLow += f * _filterBand;
        float high = input - _filterLow - q * _filterBand;
        _filterBand += f * high;

        return _filterLow;
    }


    private static float Oscillator(PinType type, float phase, float step)
        => Shape(type, phase, step) * GainFor(type);

    private static float GainFor(PinType type)
    {
        switch (type)
        {
            case PinType.Sawtooth: return SawGain;
            case PinType.Square: return SquareGain;
            default: return SineGain;
        }
    }

    private static float RatioFor(PinType type)
    {
        switch (type)
        {
            case PinType.Sawtooth: return SawRatio;
            case PinType.Square: return SquareRatio;
            default: return SineRatio;
        }
    }

    private static float CutoffFor(PinType type)
    {
        switch (type)
        {
            case PinType.Sawtooth: return SawCutoff;
            case PinType.Square: return SquareCutoff;
            default: return SineCutoff;
        }
    }

    private static float ResonanceFor(PinType type)
    {
        switch (type)
        {
            case PinType.Sawtooth: return SawResonance;
            case PinType.Square: return SquareResonance;
            default: return SineResonance;
        }
    }

    private static float Shape(PinType type, float t, float step)
    {
        switch (type)
        {
            case PinType.Sine:
                return Mathf.Sin(t * 2f * Mathf.PI);

            case PinType.Sawtooth:
                return (2f * t - 1f) - PolyBlep(t, step);

            case PinType.Square:
            {
                float value = t < 0.5f ? 1f : -1f;
                value += PolyBlep(t, step); // rising edge at t = 0
                value -= PolyBlep(Frac(t + 0.5f), step); // falling edge at t = 0.5
                return value;
            }

            default:
                return 0f;
        }
    }

    /// <summary>
    /// Polynomial band-limited step. Rounds off the discontinuity over one sample
    /// either side of the edge, which stops the harmonics folding back down as hash.
    /// </summary>
    private static float PolyBlep(float t, float step)
    {
        if (step <= 0f) return 0f;

        if (t < step)
        {
            t /= step;
            return t + t - t * t - 1f;
        }

        if (!(t > 1f - step)) return 0f;

        t = (t - 1f) / step;
        return t * t + t + t + 1f;
    }

    private static float Frac(float value) => value - Mathf.Floor(value);

    private void AdvanceEnvelope(float delta)
    {
        _stageTime += delta;

        switch (_stage)
        {
            case Stage.Attack:
                CurrentGain = Mathf.Lerp(_attackStartGain, _velocity, _stageTime / _attackTime);
                if (_stageTime >= _attackTime)
                {
                    _stage = Stage.Decay;
                    _stageTime = 0f;
                }

                break;

            case Stage.Decay:
                CurrentGain = Mathf.Lerp(_velocity, _velocity * _sustainLevel, _stageTime / _decayTime);
                if (_stageTime >= _decayTime)
                {
                    _stage = Stage.Sustain;
                    _stageTime = 0f;
                    CurrentGain = _velocity * _sustainLevel;
                }

                break;

            case Stage.Sustain:
                CurrentGain = _velocity * _sustainLevel;
                if (_stageTime >= _sustainDuration)
                {
                    _stage = Stage.Release;
                    _stageTime = 0f;
                }

                break;

            case Stage.Release:
                CurrentGain *= _releaseCoeff;
                if (CurrentGain < 0.0001f)
                {
                    _stage = Stage.Idle;
                    CurrentGain = 0f;
                }

                break;
            case Stage.Idle:
                break;
        }
    }
}