using System;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace _Scripts.Interactions
{
    public class ObjectOutlineHandler : Singleton<ObjectOutlineHandler>
    {
        private static readonly List<OutlineTarget> Targets = new List<OutlineTarget>();

        [Serializable]
        private class Hand
        {
            public HandGrabInteractor interactor;
            public Transform measureFrom;
            public PalmInventory inventory;

            [Tooltip("Optional. Outlines does not show while this pose is active. Leave empty to always allow.")]
            [Interface(typeof(IActiveState))]
            public UnityEngine.Object outlineDisablePose;
            
            public bool DisableOutline => outlineDisablePose is IActiveState state && state.Active;
        }

        [SerializeField] private List<Hand> hands = new List<Hand>();
        [SerializeField] private float highlightRadius = 0.15f;
        [SerializeField] private int frameInterval = 3;

        private static readonly List<OutlineTarget> TargetsToOutline = new List<OutlineTarget>();
        private static readonly List<OutlineTarget> CurrentlyOutlined = new List<OutlineTarget>();
        private int frameCounter;

        public static void Register(OutlineTarget target)
        {
            Targets.Add(target);
        }

        public static void Unregister(OutlineTarget target)
        {
            Targets.Remove(target);
            TargetsToOutline.Remove(target);
            CurrentlyOutlined.Remove(target);
        }

        private void Update()
        {
            frameCounter++;
            if (frameInterval > 1 && frameCounter % frameInterval != 0)
            {
                return;
            }

            TargetsToOutline.Clear();
            float maxDistanceSqr = highlightRadius * highlightRadius;

            foreach (Hand hand in hands)
            {
                if (!hand.interactor || !hand.measureFrom) continue;
                if (!hand.interactor.gameObject.activeInHierarchy) continue;

                // A hand that's already holding something or has inventory open highlights nothing
                if (hand.interactor.State == InteractorState.Select) continue;
                if (hand.inventory && hand.inventory.IsOpen) continue;
                if (hand.DisableOutline) continue;
                
                // Find closest outline target to each hand
                OutlineTarget closestTarget = null;
                float closestDistanceSqr = maxDistanceSqr;
                foreach (OutlineTarget target in Targets)
                {
                    if (target.IsSelected()) continue;

                    float distanceSqr = (target.Position - hand.measureFrom.position).sqrMagnitude;
                    if (!(distanceSqr < closestDistanceSqr)) continue;

                    closestDistanceSqr = distanceSqr;
                    closestTarget = target;
                }

                // Mark target for outline if it's not already set to be outlined
                if (closestTarget && !TargetsToOutline.Contains(closestTarget))
                {
                    TargetsToOutline.Add(closestTarget);
                }
            }

            // Disable outline on targets that leave the highlight radius
            foreach (OutlineTarget target in CurrentlyOutlined)
            {
                if (!TargetsToOutline.Contains(target)) target.SetOutline(false);
            }

            CurrentlyOutlined.Clear();

            // Enable outline on marked targets
            foreach (OutlineTarget target in TargetsToOutline)
            {
                target.SetOutline(true);
                CurrentlyOutlined.Add(target);
            }
        }
    }
}