//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using UnityEngine;

//public class handTracking : MonoBehaviour
//{
//	public UDPReceive udpReceive;

//	public float centerHandPointsZ = -1f;
	
//	public float centerHandPointsScale = 0.3f;

//	private GameObject[] hands;

//	private int handNInfo = 49;

//	public Material[] materials;

//	private int cameraWidth;

//	private int cameraHeight;

//	private float cameraAspectRatio;

//	private Vector2 bottomCorner;

//	private Vector2 topCorner;

//	private int detectionMargin = 4;

//	private int nHands;

//	private void Start()
//	{
//		CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
//		hands = GameObject.FindGameObjectsWithTag("hand");
//		float z = Vector3.Distance(base.transform.position, Camera.main.transform.position);
//		bottomCorner = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, z));
//		topCorner = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, z));
//	}

//	private void Update()
//	{
//		string data = udpReceive.data;
//		if (!string.IsNullOrEmpty(data))
//		{
//			data = data.Remove(0, 1);
//			data = data.Remove(data.Length - 1, 1);
//			nHands = data[data.Length - 1] - 48;
//			data = data.Remove(data.Length - 3, 3);
//			List<string[]> list = new List<string[]>();
//			char[] array = new char[2] { '\'', ' ' };
//			for (int i = 0; i < array.Length; i++)
//			{
//				data = data.Replace(array[i].ToString(), "");
//			}
//			string[] array2 = data.Split(',');
//			cameraWidth = (int)float.Parse(array2[^2]);
//			cameraHeight = (int)float.Parse(array2[^1]);
//			cameraAspectRatio = cameraWidth / cameraHeight;
//			for (int j = 0; j < nHands; j++)
//			{
//				list.Add(data.Split(',').Skip(j * handNInfo).Take((j + 1) * handNInfo)
//					.ToArray());
//			}
//			if (nHands > 0)
//			{
//				runHandTracking(list);
//			}
//		}
//		if (nHands == 0)
//		{
//			for (int k = 0; k < hands.Length; k++)
//			{
//				hands[k].GetComponent<Hand>().disable();
//			}
//		}
//	}

//	private void runHandTracking(List<string[]> handsInfo)
//	{
//		for (int i = 0; i < nHands; i++)
//		{
//			float[] array = new float[42];
//			for (int j = 0; j < 21; j++)
//			{
//				array[j * 2] = float.Parse(handsInfo[i][j * 2]);
//				array[j * 2 + 1] = float.Parse(handsInfo[i][j * 2 + 1]);
//			}
//			float x = map(float.Parse(handsInfo[i][18]), 0f, cameraWidth, topCorner.x + (float)detectionMargin, bottomCorner.x - (float)detectionMargin);
//			float y = map(float.Parse(handsInfo[i][19]), 0f, cameraHeight, bottomCorner.y - (float)detectionMargin, topCorner.y + (float)detectionMargin);
//			bool flag = (object.Equals(handsInfo[i][48], "Right") ? true : false);
//			if (flag)
//			{
//				hands[0].GetComponent<Hand>().SetInfo(new Vector3(x, y, centerHandPointsZ), identifyGesture(i, flag, array), flag);
//				if (nHands == 1)
//				{
//					hands[1].GetComponent<Hand>().disable();
//				}
//			}
//			if (!flag)
//			{
//				hands[1].GetComponent<Hand>().SetInfo(new Vector3(x, y, centerHandPointsZ), identifyGesture(i, flag, array), flag);
//				if (nHands == 1)
//				{
//					hands[0].GetComponent<Hand>().disable();
//				}
//			}
//		}
//	}

//	private int identifyGesture(int nHand, bool handSide, float[] handPoints)
//	{
//		int result = 0;
//		float num = Vector3.Distance(new Vector2(handPoints[8], handPoints[9]), new Vector2(handPoints[16], handPoints[17]));
//		float num2 = Vector3.Distance(new Vector2(handPoints[8], handPoints[9]), new Vector2(handPoints[20], handPoints[21]));
//		float num3 = Vector3.Distance(new Vector2(handPoints[36], handPoints[37]), new Vector2(handPoints[20], handPoints[21]));
//		float num4 = Vector3.Distance(new Vector2(handPoints[4], handPoints[5]), new Vector2(handPoints[6], handPoints[7]));
//		if (num < num4 && handPoints[17] < handPoints[41] && handPoints[17] > handPoints[9] && handPoints[9] > handPoints[5] && handPoints[25] > handPoints[17] && handPoints[33] > handPoints[17] && handPoints[41] > handPoints[17])
//		{
//			result = 1;
//		}
//		else if (handPoints[17] < handPoints[11] && handPoints[25] < handPoints[19] && handPoints[33] < handPoints[27] && handPoints[41] < handPoints[35])
//		{
//			result = 2;
//		}
//		else if (handPoints[33] < handPoints[29] && handPoints[41] < handPoints[37] && handPoints[17] > handPoints[13] && handPoints[25] > handPoints[21])
//		{
//			result = 3;
//		}
//		else if ((double)num2 > 1.2 * (double)num3 && handPoints[17] > handPoints[15] && handPoints[25] < handPoints[19] && handPoints[33] < handPoints[27] && handPoints[41] < handPoints[35] && ((!handSide && handPoints[8] < handPoints[6]) || (handSide && handPoints[8] > handPoints[6])))
//		{
//			result = 4;
//		}
//		return result;
//	}

//	private float map(float s, float a1, float a2, float b1, float b2)
//	{
//		return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
//	}
//}
