using UnityEngine;

public class handleInventory : MonoBehaviour
{
	//public bool active;

	//public bool opened;

	//public bool closed = true;

	//private GameObject[] pins;

	//private GameObject[] hands;

	//private SoundBoardManager connectionHandler;

	//private bool placingPoint;

	//private void Start()
	//{
	//	base.transform.localScale = Vector2.zero;
	//	pins = GameObject.FindGameObjectsWithTag("inventoryPin");
	//	connectionHandler = GameObject.Find("Points").GetComponent<SoundBoardManager>();
	//}

	//private void Update()
	//{
	//	hands = connectionHandler.hands;
	//	placingPoint = false;
	//	if (active && !opened)
	//	{
	//		Open();
	//		opened = true;
	//		closed = false;
	//	}
	//	if (!active && !closed)
	//	{
	//		Close();
	//		opened = false;
	//		closed = true;
	//	}
	//	for (int i = 0; i < hands.Length; i++)
	//	{
	//		Hand component = hands[i].GetComponent<Hand>();
	//		if (component.grabbingPoint)
	//		{
	//			placingPoint = true;
	//		}
	//		if (component.gesture == 4)
	//		{
	//			active = true;
	//		}
	//		if ((component.gesture != 4 && component.lastGesture == 4) || (component.gesture == 4 && !component.active))
	//		{
	//			active = false;
	//		}
	//		if (!active || placingPoint)
	//		{
	//			continue;
	//		}
	//		Vector2 a = component.handPosition;
	//		for (int j = 0; j < pins.Length; j++)
	//		{
	//			if (component.gesture == 2 && Vector2.Distance(a, pins[j].transform.position) < pins[j].transform.localScale.x * 0.6f)
	//			{
	//				Point grabbedPoint = connectionHandler.createPoint(component.handPosition.x, component.handPosition.y, j + 1);
	//				component.grabbingPoint = true;
	//				component.grabbedPoint = grabbedPoint;
	//				component.grabbedPoint.isBeingGrabbed = true;
	//				placingPoint = true;
	//				pins[j].transform.localScale = Vector2.zero;
	//				ExpandPin(j + 1);
	//			}
	//		}
	//	}
	//}

	//private void Open()
	//{
	//	base.transform.LeanScale(new Vector2(0.2f, 0.2f), 0.2f);
	//}

	//private void Close()
	//{
	//	base.transform.LeanScale(Vector2.zero, 0.1f);
	//}

	//private void ExpandPin(int type)
	//{
	//	pins[type - 1].transform.LeanScale(new Vector2(0.6f, 0.6f), 0.4f);
	//}
}
