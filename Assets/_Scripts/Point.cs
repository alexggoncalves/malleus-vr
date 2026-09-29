using System;
using _Scripts.SoundBoard;
using UnityEngine;

namespace _Scripts
{
    public class Point : MonoBehaviour
    {
        [Header("Pitch mapping")]
        [Tooltip("Wave multiplier at the left edge of the board.")]
        [SerializeField] private float minWaveMultiplier = 0.5f;

        [Tooltip("Wave multiplier at the right edge of the board.")]
        [SerializeField] private float maxWaveMultiplier = 2f;

        private SoundBoardSurface board;
        private bool isConnected;
        private bool isBeingConnected;
        private bool isPinned;

        public event Action<Point> VisualStateChanged;
        public event Action<Point> RemoveRequested;

        public Vector3 WorldPosition
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Vector2 BoardPosition
        {
            get => board.WorldToLocal(transform.position);
            set => transform.position = board.LocalToWorld(value);
        }

        public bool IsPinned
        {
            get => isPinned;
            set => SetFlag(ref isPinned, value);
        }
        public bool IsConnected
        {
            get => isConnected;
            set => SetFlag(ref isConnected, value);
        }


        public void Initialize(SoundBoardSurface board, int typeIndex, Vector2 boardPosition)
        {
            this.board = board;
            //TypeIndex = typeIndex;
            BoardPosition = boardPosition;
            VisualStateChanged?.Invoke(this);
        }

        public void MoveTo(Vector3 worldPosition)
        {
            transform.position = board != null ? board.ProjectOntoPlane(worldPosition) : worldPosition;
        }

        public void Nudge(Vector2 boardOffset)
        {
            BoardPosition += boardOffset;
        }

        public void RequestRemoval() => RemoveRequested?.Invoke(this);

        private void SetFlag(ref bool field, bool value)
        {
            if (field == value) return;
            field = value;
            VisualStateChanged?.Invoke(this);
        }

    }
}
