using System;
using _Scripts.SoundBoard;
using Oculus.Interaction.Input;
using UnityEngine;

namespace _Scripts.Interactions
{
    public class FingerStrummer : MonoBehaviour
    {
        [SerializeField] private Handedness handedness;
        [SerializeField] private Hand hand;

        [Header("Finger Segment")] [SerializeField]
        private HandJointId knuckleJoint = HandJointId.HandIndex2;

        [SerializeField] private HandJointId fingerTip = HandJointId.HandIndexTip;

        [SerializeField] private float hitRadius = 0.015f;
        [SerializeField] private float minStrumSpeed = 0.1f;
        [SerializeField] private float maxStrumSpeed = 1f;
        [SerializeField] private float strumCooldown = 0.1f;

        private Vector3 lastKnuckle;
        private Vector3 lastTip;
        private bool hasLastFinger;

        private void Update()
        {
            if (!hand ||
                !hand.GetJointPose(knuckleJoint, out Pose knucklePose) ||
                !hand.GetJointPose(fingerTip, out Pose tipPose))
            {
                hasLastFinger = false;
                return;
            }

            Vector3 currentKnuckle = knucklePose.position;
            Vector3 currentTip = tipPose.position;

            if (hasLastFinger)
            {
                CheckStrings(lastKnuckle, lastTip, currentKnuckle, currentTip, Time.deltaTime);
            }

            lastKnuckle = currentKnuckle;
            lastTip = currentTip;
            hasLastFinger = true;
        }

        private void CheckStrings(Vector3 lastKnuckle, Vector3 currentKnuckle, Vector3 lastTip, Vector3 currentTip, float dt)
        {
            foreach (SoundString s in SoundString.All)
            {
                Vector3 stringStart = s.PinA.StringPosition;
                Vector3 stringEnd = s.PinB.StringPosition;

                // Detect intersection with the quad as two triangles.
                // Triangle A: P = lastNuckle, Q = currentKnuckle, R = currentTip
                bool hit = SegmentIntersectsTriangle(stringStart, stringEnd, lastKnuckle, currentKnuckle, currentTip,
                    out _, out float segT, out float baryV, out float baryW);
                
                float fingerU;
                if (hit)
                {
                    fingerU = baryV + baryW;
                }
                else
                {
                    // Triangle B: P = lastKnuckle, Q = currentTip, R = lastTip
                    hit = SegmentIntersectsTriangle(stringStart, stringEnd, lastKnuckle, currentTip, lastTip,
                        out _, out segT, out baryV, out baryW);
                    fingerU = baryV;
                }
                
                if (!hit)
                {
                    s.IsIntersecting = false;
                    continue;
                }

                if (s.IsIntersecting) continue;
                s.IsIntersecting = true;

                Vector3 contactPrevious = Vector3.Lerp(lastKnuckle, currentKnuckle, fingerU);
                Vector3 contactCurrent = Vector3.Lerp(lastTip, currentTip, fingerU);
                Vector3 velocity = (contactCurrent - contactPrevious) / dt;

                Vector3 stringDir = (stringEnd - stringStart).normalized;
                Vector3 transverse = velocity - Vector3.Dot(velocity, stringDir) * stringDir;
                float transverseSpeed = transverse.magnitude;

                if (transverseSpeed < minStrumSpeed) continue;

                float normalizedVelocity = Mathf.InverseLerp(minStrumSpeed, maxStrumSpeed, transverseSpeed);
                s.Strum(segT, normalizedVelocity);
                
            }
        }

        /// <summary>
        /// Möller–Trumbore segment/triangle intersection, clamped to segT in [0,1].
        /// baryV/baryW are barycentric weights on edge1 (q-p) and edge2 (r-p);
        /// the implicit weight on p is (1 - baryV - baryW).
        /// </summary>
        private static bool SegmentIntersectsTriangle(
            Vector3 segStart, Vector3 segEnd,
            Vector3 p, Vector3 q, Vector3 r,
            out Vector3 hitPoint, out float segT, out float baryV, out float baryW)
        {
            hitPoint = default;
            segT = 0f;
            baryV = 0f;
            baryW = 0f;

            Vector3 edge1 = q - p;
            Vector3 edge2 = r - p;
            Vector3 dir = segEnd - segStart;

            Vector3 pVec = Vector3.Cross(dir, edge2);
            float det = Vector3.Dot(edge1, pVec);

            if (Mathf.Abs(det) < 1e-9f) return false; // segment parallel to triangle, or degenerate triangle

            float invDet = 1f / det;
            Vector3 tVec = segStart - p;

            baryV = Vector3.Dot(tVec, pVec) * invDet;
            if (baryV < 0f || baryV > 1f) return false;

            Vector3 qVec = Vector3.Cross(tVec, edge1);
            baryW = Vector3.Dot(dir, qVec) * invDet;
            if (baryW < 0f || baryV + baryW > 1f) return false;

            segT = Vector3.Dot(edge2, qVec) * invDet;
            if (segT < 0f || segT > 1f) return false;

            hitPoint = segStart + dir * segT;
            return true;
        }
    }
}