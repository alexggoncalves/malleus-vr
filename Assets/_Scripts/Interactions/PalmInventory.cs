using System.Collections.Generic;
using System.Linq;
using _Scripts.SoundBoard;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace _Scripts.Interactions
{
    public class PalmInventory : MonoBehaviour
    {
        [SerializeField] private ActiveStateSelector palmUpPose;
        [SerializeField] private HandRef handRef;

        [Header("Spawn layout")] [SerializeField]
        private Pin[] pinPrefabs = new Pin[3];

        [SerializeField] private float circleRadius = 0.08f;
        [SerializeField] private float heightAbovePalm = 0.08f;

        [Header("Follow smoothing")] [SerializeField]
        private float followLerpSpeed = 20f;

        private class SpawnedPin
        {
            public Pin pin;
            public int slotIndex;
        }

        private readonly List<SpawnedPin> spawned = new();

        public bool IsOpen { get; private set; }

        private void Start()
        {
            palmUpPose.WhenSelected += OnPalmUp;
            palmUpPose.WhenUnselected += OnPalmUpRelease;
        }

        private void OnDestroy()
        {
            palmUpPose.WhenSelected -= OnPalmUp;
            palmUpPose.WhenUnselected -= OnPalmUpRelease;
        }

        private void OnPalmUp()
        {
            IsOpen = true;
            SpawnPins();
        }

        private void OnPalmUpRelease()
        {
            IsOpen = false;

            // Remove any pins that were never grabbed — they were just previews
            foreach (SpawnedPin entry in spawned)
            {
                if (!entry.pin) continue;
                entry.pin.WhenGrabbed -= OnPinGrabbed;
                Destroy(entry.pin.gameObject);
            }

            spawned.Clear();
        }

        private void SpawnPins()
        {
            for (int i = 0; i < pinPrefabs.Length; i++)
            {
                if (!pinPrefabs[i]) continue;
                SpawnPinAt(i);
            }
        }

        private void SpawnPinAt(int slotIndex)
        {
            handRef.GetJointPose(HandJointId.HandPalm, out Pose palmPose);
            Vector3 center = palmPose.position + Vector3.up * heightAbovePalm;

            float angle = (360f / pinPrefabs.Length) * slotIndex;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * circleRadius);

            Pin instance = Instantiate(pinPrefabs[slotIndex], center + offset, Quaternion.identity);
            instance.state = Pin.State.Following;

            SpawnedPin entry = new SpawnedPin
            {
                pin = instance,
                slotIndex = slotIndex
            };

            instance.WhenGrabbed += OnPinGrabbed;
            spawned.Add(entry);
        }

        private void OnPinGrabbed(Pin pin)
        {
            SpawnedPin entry = spawned.FirstOrDefault(e => e.pin == pin);
            if (entry == null) return;
            entry.pin.WhenGrabbed -= OnPinGrabbed;
            spawned.Remove(entry);
            SpawnPinAt(entry.slotIndex);
        }

        private void Update()
        {
            if (!IsOpen) return;
            
            handRef.GetJointPose(HandJointId.HandPalm, out Pose palmPose);
            Vector3 center = palmPose.position + Vector3.up * heightAbovePalm;

            foreach (SpawnedPin entry in spawned)
            {
                if (!entry.pin) continue;

                float angle = (360f / pinPrefabs.Length) * entry.slotIndex;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * circleRadius);
                Vector3 targetPos = center + offset;

                entry.pin.SetFollowTarget(targetPos, followLerpSpeed);
            }
        }
    }
}