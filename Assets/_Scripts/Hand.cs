//using UnityEngine;

//public class Hand : MonoBehaviour
//{
//	private readonly float scaleUI = 1f;

//	public Vector3 handPosition;

//	public Vector3 lastHandPosition;

//	public int gesture;

//	public int lastGesture;

//	private bool side;

//	public bool active;

//	private Sprite leftNormal;

//	private Sprite rightNormal;

//	private Sprite[] cursors;

//	public bool makingConnection;

//	public bool grabbingPoint;

//	public Point grabbedPoint;

//	public int maxPosDistance = 2;

//	private void Start()
//	{
//		base.gameObject.AddComponent<SpriteRenderer>();
//		cursors = new Sprite[3];
//		leftNormal = Resources.Load<Sprite>("Sprites/Cursor/cursorLeftNormal");
//		rightNormal = Resources.Load<Sprite>("Sprites/Cursor/cursorRightNormal");
//		cursors[0] = Resources.Load<Sprite>("Sprites/Cursor/cursorPinch");
//		cursors[1] = Resources.Load<Sprite>("Sprites/Cursor/cursorMove");
//		cursors[2] = Resources.Load<Sprite>("Sprites/Cursor/cursorCut");
//		base.transform.localScale = new Vector3(scaleUI, scaleUI);
//	}

//	public void disable()
//	{
//		active = false;
//	}

//	public void SetInfo(Vector3 pos, int ges, bool handSide)
//	{
//		active = true;
//		handPosition = pos;
//		gesture = ges;
//		side = handSide;
//	}

//	private void Update()
//	{
//		if (active)
//		{
//			base.transform.position = handPosition;
//			if (gesture == 0 || gesture == 4)
//			{
//				if (!side)
//				{
//					GetComponent<SpriteRenderer>().sprite = leftNormal;
//				}
//				else
//				{
//					GetComponent<SpriteRenderer>().sprite = rightNormal;
//				}
//			}
//			else
//			{
//				GetComponent<SpriteRenderer>().sprite = cursors[gesture - 1];
//			}
//		}
//		else
//		{
//			GetComponent<SpriteRenderer>().sprite = null;
//			gesture = 100;
//		}
//		lastHandPosition = handPosition;
//		lastGesture = gesture;
//	}
//}
