using System;
using _Scripts.SoundBoard;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace _Scripts.Interactions
{
    public class ScissorsCutString : MonoBehaviour
    {
        [SerializeField] private ActiveStateSelector pose;
        [SerializeField] private HandRef handRef;
        [SerializeField] private Handedness handedness;

        [Header("Cut detection")]
        [SerializeField]private float closedAngleThreshold = 15f;

        [SerializeField] private float openAngleThreshold = 35f;
        [SerializeField] private float minCutSpeed = 40f;

        [Header("String proximity")] 
        [SerializeField] private float hoverRadius = 0.03f;

        [Header("Cut Visualization")]
        [SerializeField] private float cutIndicatorLength = 0.01f;
        [SerializeField] private float cutIndicatorSmoothness = 0.01f;
        [SerializeField] private Color hoverColor = Color.darkRed;

        private bool isPoseActive;
        private bool isClosed;
        private float lastAngle;
        private bool hasLastAngle;

        private SoundString hoveredString;

        private void Start()
        {
            pose.WhenSelected += OnScissors;
            pose.WhenUnselected += OnScissorsRelease;
        }

        private void Update()
        {
            if (!isPoseActive) return;

            // Find closest hovered string at the midpoint between
            // the tips of the index and middle finger
            UpdateStringHover();
            if (!hoveredString) return;

            // Track finger cut micro gesture
            TrackCut();
        }

        private void UpdateStringHover()
        {
            IHand hand = handRef.Hand;
            if (!hand.GetJointPose(HandJointId.HandIndexTip, out Pose indexTip)) return;
            if (!hand.GetJointPose(HandJointId.HandMiddleTip, out Pose middleTip)) return;
            Vector3 cutPoint = (indexTip.position + middleTip.position) / 2;

            SoundString closest = null;
            float closestDist = hoverRadius;
            float closestT = -1;

            foreach (SoundString s in SoundString.All)
            {
                float dist = DistancePointToSegment(cutPoint, s.PinA.transform.position, s.PinB.transform.position,
                    out float t);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = s;
                    closestT = t;
                }
            }


            if (closest != hoveredString)
            {
                if (hoveredString) hoveredString.ClearColor();
                hoveredString = closest;
            }

            if (hoveredString)
            {
                hoveredString.SetLocalGradientColor(hoverColor, closestT, cutIndicatorLength, cutIndicatorSmoothness);
            }
        }

        private void TrackCut()
        {
            if (!hoveredString)
            {
                hasLastAngle = false;
                return;
            }

            // Get joint positions
            IHand hand = handRef.Hand;
            if (!hand.GetJointPose(HandJointId.HandIndexTip, out Pose indexTip)) return;
            if (!hand.GetJointPose(HandJointId.HandIndex1, out Pose indexBase)) return;
            if (!hand.GetJointPose(HandJointId.HandMiddleTip, out Pose middleTip)) return;
            if (!hand.GetJointPose(HandJointId.HandMiddle1, out Pose middleBase)) return;

            // Get angle between the middle and index finger
            Vector3 indexDir = (indexTip.position - indexBase.position).normalized;
            Vector3 middleDir = (middleTip.position - middleBase.position).normalized;
            float angle = Vector3.Angle(indexDir, middleDir);

            if (!hasLastAngle)
            {
                lastAngle = angle;
                hasLastAngle = true;
                return;
            }

            float angularSpeed = (lastAngle - angle) / Time.deltaTime;

            // Detect intentional cut micro gesture
            if (!isClosed && angle < closedAngleThreshold && angularSpeed > minCutSpeed)
            {
                isClosed = true;
                hoveredString.Cut();
            }
            else if (isClosed && angle > openAngleThreshold)
            {
                isClosed = false;
            }

            lastAngle = angle;
        }

        private void OnScissors()
        {
            isPoseActive = true;
            hasLastAngle = false;
            isClosed = false;
            Debug.Log("scissors pose");
        }

        private void OnScissorsRelease()
        {
            isPoseActive = false;
            Debug.Log("scissors released");
        }

        private static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b, out float t)
        {
            Vector3 ab = b - a;
            t = Vector3.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f);
            t = Mathf.Clamp01(t);
            Vector3 closest = a + ab * t;
            return Vector3.Distance(point, closest);
        }

        private void OnDestroy()
        {
            pose.WhenSelected -= OnScissors;
            pose.WhenUnselected -= OnScissorsRelease;
        }
    }
}