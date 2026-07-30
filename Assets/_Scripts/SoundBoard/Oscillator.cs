//using UnityEngine;

//public class Oscillator1 : MonoBehaviour
//{
//	public Point point1;

//	public Point point2;

//	public double frequency = 440.0;

//	private double increment;

//	private double phase;

//	private double samplingFrequency = 48000.0;

//	public float gain;

//	private float attack_level;

//	private float sustain_level;

//	private bool triggered;

//	private bool playing;

//	private bool attacking;

//	private bool decaying;

//	private bool sustaining;

//	private bool releasing;

//	public float releaseStep;

//	public float attackStep;

//	public float decayStep;

//	public float attackMultiplier;

//	public float decayMultiplier;

//	public float releaseMultiplier;

//	public float t1m;

//	public float t2m;

//	public float t3m;

//	public float t4m;

//	private float sustain_duration_ms;

//	private float sustainMultiplier;

//	private float current_time_ms;

//	public bool staticSound;

//	public float stringVibration;

//	public float vibrationStep;

//	public float vibrationDirection = 1f;

//	private void Start()
//	{
//		sustain_duration_ms = 400f;
//	}

//	private void FixedUpdate()
//	{
//		if (triggered && !playing)
//		{
//			triggered = false;
//			playing = true;
//			attacking = true;
//			releasing = false;
//			decaying = false;
//			sustaining = false;
//			current_time_ms = 0f;
//			if (staticSound)
//			{
//				gain = 0.2f;
//			}
//		}
//		if (triggered && playing)
//		{
//			triggered = false;
//			attacking = true;
//			releasing = false;
//			decaying = false;
//			sustaining = false;
//		}
//		if (playing)
//		{
//			current_time_ms += Time.deltaTime * 1000f;
//			if (attacking && gain > attack_level)
//			{
//				attacking = false;
//				decaying = true;
//				current_time_ms = 0f;
//			}
//			else if (decaying && gain < sustain_level)
//			{
//				decaying = false;
//				sustaining = true;
//				current_time_ms = 0f;
//			}
//			else if (sustaining && current_time_ms >= sustain_duration_ms)
//			{
//				sustaining = false;
//				releasing = true;
//				current_time_ms = 0f;
//			}
//			else if (releasing && gain < 0f)
//			{
//				releasing = false;
//				playing = false;
//				gain = 0f;
//			}
//			if (attacking && !staticSound)
//			{
//				gain += attackStep;
//			}
//			if (releasing && !staticSound)
//			{
//				gain -= releaseStep;
//			}
//			if (decaying && !staticSound)
//			{
//				gain -= decayStep;
//			}
//		}
//	}

//	private void OnAudioFilterRead(float[] data, int channels)
//	{
//		if (!playing || !(point1 != null) || !(point2 != null))
//		{
//			return;
//		}
//		increment = frequency * 2.0 * 3.1415927410125732 / samplingFrequency;
//		for (int i = 0; i < data.Length; i += channels)
//		{
//			phase += increment;
//			if (point1.getType() == point2.getType())
//			{
//				data[i] = getSimpleOutput();
//			}
//			else
//			{
//				data[i] = getMixedOutput();
//			}
//			if (channels == 2)
//			{
//				data[i + 1] = data[i];
//			}
//			if (phase > 6.2831854820251465)
//			{
//				phase = 0.0;
//			}
//		}
//	}

//	private float getType1_output(float m)
//	{
//		return gain * Mathf.Sin((float)phase * m);
//	}

//	private float getType2_output(float m)
//	{
//		return gain * Mathf.Sin((float)phase * m) * Mathf.Sin((float)phase * t2m * m);
//	}

//	private float getType3_output(float m)
//	{
//		if (gain * Mathf.Sin((float)phase * m) > 0f)
//		{
//			return gain;
//		}
//		return 0f - gain;
//	}

//	private float getType4_output(float m)
//	{
//		return gain * Mathf.Asin(Mathf.Sin((float)phase * m));
//	}

//	private float getMixedOutput()
//	{
//		float num = 0f;
//		float num2 = 0f;
//		switch (point1.getType())
//		{
//		case 1:
//			num = getType1_output(point1.getWaveMultiplier());
//			break;
//		case 2:
//			num = getType2_output(point1.getWaveMultiplier());
//			break;
//		case 3:
//			num = getType3_output(point1.getWaveMultiplier());
//			break;
//		case 4:
//			num = getType4_output(point1.getWaveMultiplier());
//			break;
//		}
//		switch (point2.getType())
//		{
//		case 1:
//			num2 = getType1_output(point2.getWaveMultiplier());
//			break;
//		case 2:
//			num2 = getType2_output(point2.getWaveMultiplier());
//			break;
//		case 3:
//			num2 = getType3_output(point2.getWaveMultiplier());
//			break;
//		case 4:
//			num2 = getType4_output(point2.getWaveMultiplier());
//			break;
//		}
//		return 2f * num * num2;
//	}

//	private float getSimpleOutput()
//	{
//		float m = (point1.getWaveMultiplier() + point2.getWaveMultiplier()) / 2f;
//		float num = 0f;
//		switch (point1.getType())
//		{
//		case 1:
//			num = getType1_output(m);
//			break;
//		case 2:
//			num = getType2_output(m);
//			break;
//		case 3:
//			num = getType3_output(m);
//			break;
//		case 4:
//			num = getType4_output(m);
//			break;
//		}
//		return 0.1f * num;
//	}

//	private float getType1_graph(float x, float m)
//	{
//		return gain * Mathf.Sin(x * m);
//	}

//	private float getType2_graph(float x, float m)
//	{
//		return gain * Mathf.Sin(x * m) * Mathf.Sin(x * m * 1.5f);
//	}

//	private float getType3_graph(float x, float m)
//	{
//		if (gain * Mathf.Sin(x * m) < 0f)
//		{
//			return gain;
//		}
//		return 0f - gain;
//	}

//	private float getType4_graph(float x, float m)
//	{
//		return gain * Mathf.Asin(Mathf.Sin(x * m));
//	}

//	private float getVibration()
//	{
//		float num = 1f;
//		float num2 = 0f - num;
//		vibrationStep = (float)((double)(1000f * Time.deltaTime) / frequency) * vibrationDirection;
//		stringVibration += vibrationStep;
//		if (vibrationDirection > 0f && stringVibration >= num)
//		{
//			vibrationDirection = -1f;
//		}
//		else if (vibrationDirection < 0f && stringVibration <= num2)
//		{
//			vibrationDirection = 1f;
//		}
//		return stringVibration;
//	}

//	public float getMixedGraph(float x)
//	{
//		float num = 0f;
//		float num2 = 0f;
//		switch (point1.getType())
//		{
//		case 1:
//			num = 3f * getType1_graph(x, point1.getWaveMultiplier());
//			break;
//		case 2:
//			num = 5f * getType2_graph(x, point1.getWaveMultiplier());
//			break;
//		case 3:
//			num = 5f * getType3_graph(x, point1.getWaveMultiplier());
//			break;
//		case 4:
//			num = 5f * getType4_graph(x, point1.getWaveMultiplier());
//			break;
//		}
//		switch (point2.getType())
//		{
//		case 1:
//			num2 = 3f * getType1_graph(x, point2.getWaveMultiplier());
//			break;
//		case 2:
//			num2 = 5f * getType2_graph(x, point2.getWaveMultiplier());
//			break;
//		case 3:
//			num2 = 5f * getType3_graph(x, point2.getWaveMultiplier());
//			break;
//		case 4:
//			num2 = 5f * getType4_graph(x, point2.getWaveMultiplier());
//			break;
//		}
//		return num * num2 * getVibration();
//	}

//	public float getSimpleGraph(float x)
//	{
//		float m = (point1.getWaveMultiplier() + point2.getWaveMultiplier()) / 2f;
//		float num = 0f;
//		switch (point1.getType())
//		{
//		case 1:
//			num = getType1_graph(x, m);
//			break;
//		case 2:
//			num = getType2_graph(x, m);
//			break;
//		case 3:
//			num = getType3_graph(x, m);
//			break;
//		case 4:
//			num = getType4_graph(x, m);
//			break;
//		}
//		return num * getVibration();
//	}

//	private void calculateADRSValues(float speed)
//	{
//		if (speed > 3f)
//		{
//			speed = 3f;
//		}
//		attack_level = map(speed, 0f, 5f, 0.05f, 0.2f);
//		sustain_level = 0.7f * attack_level;
//		attackStep = (speed + 1f) / (100f * attackMultiplier);
//		decayStep = (speed + 1f) / (100f * decayMultiplier);
//		releaseStep = (speed + 1f) / (100f * releaseMultiplier);
//		sustain_duration_ms = speed * 100f * sustainMultiplier;
//	}

//	public void setMultipliers(float attackMultiplier, float decayMultiplier, float releaseMultiplier, float sustainMutliplier)
//	{
//		this.attackMultiplier = attackMultiplier;
//		this.decayMultiplier = decayMultiplier;
//		this.releaseMultiplier = releaseMultiplier;
//		sustainMultiplier = sustainMutliplier;
//	}

//	public void setTypeMultipliers(float t1, float t2, float t3, float t4)
//	{
//		t1m = t1;
//		t2m = t2;
//		t3m = t3;
//		t4m = t4;
//	}

//	public void setPoints(Point p1, Point p2)
//	{
//		point1 = p1.GetComponent<Point>();
//		point2 = p2.GetComponent<Point>();
//	}

//	public void setValues(float freq)
//	{
//		frequency = freq;
//	}

//	public void playString(float speed)
//	{
//		calculateADRSValues(speed);
//		triggered = true;
//	}

//	public bool isPlaying()
//	{
//		return playing;
//	}

//	private float map(float s, float a1, float a2, float b1, float b2)
//	{
//		return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
//	}
//}
