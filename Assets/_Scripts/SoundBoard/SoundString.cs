using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SoundString : MonoBehaviour
{
    private static readonly List<SoundString> All = new();

    [Header("Resolution")]
    [Tooltip("Target spacing between points; smaller = more points on long strings")]
    [SerializeField] private float segmentLength = 0.01f;
    [SerializeField] private int minPoints = 8;
    [SerializeField] private int maxPoints = 128;


    public Pin pinA { get; private set; }
    public Pin pinB { get; private set; }


    private LineRenderer line;
    private int linePointCount;


    public static SoundString Create(Pin a, Pin b, Material material, float width)
    {
        var gameObject = new GameObject($"SoundString{a.name}_{b.name}");

        var s = gameObject.AddComponent<SoundString>();
        s.Init(a, b, material, width);

        Debug.Log("cONNECTION");
        return s;
    }

    private void Init(Pin a, Pin b, Material mat, float width)
    {
        pinA = a;
        pinB = b;
        //pinA.Attach(this);
        //pinB.Attach(this);

        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        //line.positionCount = Mathf.Max(2, 3); !!
        line.startWidth = line.endWidth = width;
        if (mat != null) line.material = mat;

        //_sampleRate = AudioSettings.outputSampleRate;
        //RecalcEnvelope();

        //_audio = GetComponent<AudioSource>();
        //_audio.spatialBlend = 1f;
        //_audio.dopplerLevel = 0f;              // strings move; avoid doppler pitch artifacts
        //_audio.loop = true;
        //_audio.playOnAwake = false;
        //_audio.clip = AudioClip.Create("silence", _sampleRate, 1, _sampleRate, false); // keeps the DSP chain running
        //_audio.Play();

        //RebuildLine(0f);
    }

    public static bool Exists(Pin a, Pin b)
    {
        foreach (var s in All)
            if ((s.pinA == a && s.pinB == b) || (s.pinB == a && s.pinA == b)) return true;
        return false;
    }


    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() => All.Remove(this);

    private void OnDestroy()
    {
        //if (pinA != null) pinA.Detach(this);
        //if (pinB != null) pinB.Detach(this);
    }

    private void RebuildLine(float amp)
    {
        Vector3 a = pinA.Position, b = pinB.Position;
        int n = line.positionCount;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            Vector3 p = Vector3.Lerp(a, b, t);
            //if (amp > 0f) p += _vibPerp * (amp * Mathf.Sin(Mathf.PI * t) * Mathf.Cos(_vibPhase));
            line.SetPosition(i, p);
        }
    }

    public void Cut() => Destroy(gameObject);
}
