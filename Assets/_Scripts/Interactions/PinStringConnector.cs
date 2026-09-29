using _Scripts.SoundBoard;
using Oculus.Interaction.Input;
using UnityEngine;

namespace _Scripts.Interactions
{
    public class PinStringConnector : MonoBehaviour
    {
        [SerializeField] private Handedness handedness;
        [SerializeField] private Hand hand;

        [Header("String")] [SerializeField] private Material stringMaterial;
        [SerializeField] private float stringWidth = 0.004f;

        [Header("Interaction")] [SerializeField]
        private float pinchRadius = 0.03f;

        [SerializeField] private float pinchCloseDistance = 0.02f; // fingers touching
        [SerializeField] private float pinchOpenDistance = 0.04f; // pinch release

        private Pin startPin;
        private float pinchStrength;
        private bool wasPinching = false;
        private LineRenderer stringPreview;

        private void Awake()
        {
            // Create preview line GameObject
            GameObject stringObject = new GameObject("StringPreview");
            stringObject.transform.SetParent(transform, false);
            stringPreview = stringObject.AddComponent<LineRenderer>();
            stringPreview.useWorldSpace = true;
            stringPreview.positionCount = 2;
            stringPreview.startWidth = stringWidth;
            stringPreview.endWidth = stringWidth;
            if (stringMaterial != null) stringPreview.material = stringMaterial;
            stringPreview.enabled = false;
        }

        private void Start()
        {
            if (hand) return;

            foreach (Hand h in FindObjectsByType<Hand>())
                if (h.Handedness == handedness)
                {
                    hand = h;
                    break;
                }
        }

        private void Update()
        {
            if (!hand || !hand.IsConnected || !TryGetPinchPoint(out Vector3 pinchPos, out bool isPinching))
            {
                CancelPreview();
                wasPinching = false;
                return;
            }

            // Find near pin when pinch gesture is detected
            if (isPinching && !wasPinching)
            {
                Pin pin = FindPinNear(pinchPos, pinchRadius);

                if (pin && pin.IsOnBoard)
                {
                    startPin = pin;
                    stringPreview.enabled = true;
                    
                    startPin.PlayStringAttachSound();
                }
            }

            // If a connection is currently being made, update string preview points
            if (isPinching && startPin)
            {
                stringPreview.SetPosition(0, startPin.ConnectionPoint(pinchPos));
                stringPreview.SetPosition(1, pinchPos);
            }

            // If pinch gesture ends over another pin try connecting start and end pin
            if (!isPinching && wasPinching && startPin)
            {
                Pin endPin = FindPinNear(pinchPos, pinchRadius, exclude: startPin);

                if (endPin && endPin.IsOnBoard)
                {
                    TryConnect(startPin, endPin);
                }

                CancelPreview();
            }

            wasPinching = isPinching;
        }

        private bool TryGetPinchPoint(out Vector3 point, out bool isPinching)
        {
            point = default;
            isPinching = false;

            if (!hand.GetJointPose(HandJointId.HandThumbTip, out Pose thumb)) return false;
            if (!hand.GetJointPose(HandJointId.HandIndexTip, out Pose index)) return false;

            point = (thumb.position + index.position) * 0.5f;

            float dist = Vector3.Distance(thumb.position, index.position);
            isPinching = wasPinching ? dist < pinchOpenDistance : dist < pinchCloseDistance;

            return true;
        }

        private Pin FindPinNear(Vector3 pos, float radius, Pin exclude = null)
        {
            Pin best = null;
            float bestSqr = radius * radius;
            foreach (Pin pin in Pin.All)
            {
                if (!pin || pin == exclude) continue;
                float d = (pin.Position - pos).sqrMagnitude;
                if (!(d <= bestSqr)) continue;
                bestSqr = d;
                best = pin;
            }

            return best;
        }
        
        private void TryConnect(Pin a, Pin b)
        {
            // Prevent duplicate connections
            if (SoundString.Exists(a, b)) return;

            // Create string
            SoundString.Create(a, b, stringMaterial, stringWidth);
            b.PlayStringAttachSound();
        } 

        private void CancelPreview()
        {
            startPin = null;
            if (stringPreview) stringPreview.enabled = false;
        }
    }
}