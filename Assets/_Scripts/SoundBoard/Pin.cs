using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

namespace _Scripts.SoundBoard
{
    public enum PinType
    {
        Sine,
        Sawtooth,
        Square
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Grabbable))]
    [RequireComponent(typeof(GrabFreePhysicsTransformer))]
    public class Pin : MonoBehaviour
    {
        public enum State
        {
            Free,
            Sliding,
            Settled,
            Following
        }

        public static readonly List<Pin> All = new();

        [Header("References")] [SerializeField]
        private Grabbable grabbable;

        [SerializeField] private SoundBoardSurface board;
        [SerializeField] private GrabFreePhysicsTransformer physicsTransformer;

        [Header("Pin")] [Tooltip("Radius used for pin separation on the board")] [SerializeField]
        private float pinRadius = 0.06f;

        [SerializeField] private Transform stringAttachCenter;

        [Tooltip("Piece type, passed through to the graph point")] [SerializeField]
        private PinType type = PinType.Sine;

        [Header("Board Snap")]
        [Tooltip("Distance from the plane at which held pin snaps onto the board")]
        [SerializeField]
        private float snapDistance = 0.1f;

        [Tooltip("Extra distance beyond engageDistance for detaching the pins from the board")] [SerializeField]
        private float releaseMargin = 0.05f;

        [Header("Despawn")] [SerializeField] private float despawnY = 0.1f;

        [Header("Sound")] [SerializeField] 
        private AudioTrigger boardAttachAudio;
        [SerializeField] private AudioTrigger boardDetachAudio;
        [SerializeField] private AudioTrigger stringAttachAudio;

        // Getters
        public PinType Type => type;
        public Vector3 Position => transform.position;
        public Vector3 StringPosition => stringAttachCenter.position;
        public float Radius => pinRadius;
        public bool IsOnBoard { get; private set; }

        private Rigidbody _rigidBody;
        public State state = State.Free;
        private bool _isBeingHeld;
        private Vector3 _grabOffset;

        private Vector3 _followTarget;
        private float _followSpeed;

        public event System.Action<Pin> WhenGrabbed;
        public event System.Action<Pin> WhenDetached;

        private void Awake()
        {
            _rigidBody = GetComponent<Rigidbody>();
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
            if (board == null) board = FindAnyObjectByType<SoundBoardSurface>();
            if (physicsTransformer == null) physicsTransformer = GetComponent<GrabFreePhysicsTransformer>();
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (grabbable != null) grabbable.WhenPointerEventRaised += OnPointerEvent;
        }

        private void OnDisable()
        {
            All.Remove(this);
            if (grabbable != null) grabbable.WhenPointerEventRaised -= OnPointerEvent;
            if (physicsTransformer != null) physicsTransformer.enabled = true;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void OnPointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                {
                    _isBeingHeld = true;
                    if (state == State.Following)
                        ExitFollow();
                    WhenGrabbed?.Invoke(this);
                    if (state == State.Settled)
                        BeginSlide();
                    break;
                }
                case PointerEventType.Unselect when grabbable.GrabPoints.Count == 0:
                {
                    _isBeingHeld = false;
                    if (state == State.Sliding)
                        Settle();
                    else
                    {
                        _rigidBody.isKinematic = false;
                    }

                    break;
                }
            }
        }

        private void FixedUpdate()
        {
            if (transform.position.y <= despawnY)
            {
                Destroy(gameObject);
                return;
            }

            if (_isBeingHeld)
            {
                // Get distance from board
                if (grabbable.GrabPoints.Count == 0) return;
                Vector3 grabPos = grabbable.GrabPoints[0].position;
                float handDistance = Mathf.Abs(board.DistanceFromPlane(grabPos));

                // If Free, attach to board if withing the distance
                // Else detach from board if Sliding and outside the extra distance

                switch (state)
                {
                    case State.Free when handDistance <= snapDistance:
                        BeginSlide();
                        break;
                    case State.Sliding when handDistance >= snapDistance + releaseMargin:
                        Detach();
                        break;
                }

                if (state != State.Sliding) return;

                // Update position when sliding
                _rigidBody.isKinematic = true;

                // Get point in the board
                Vector3 point = grabPos + _grabOffset;
                Vector3 boardPosition = board.ClampToPlayableArea(point);

                _rigidBody.MovePosition(boardPosition);
                _rigidBody.MoveRotation(FlattenRotation());
            }
            else if (state == State.Settled)
            {
                _rigidBody.isKinematic = true;
            }
            else if (state == State.Following)
            {
                _rigidBody.isKinematic = true;
                float t = 1f - Mathf.Exp(-_followSpeed * Time.fixedDeltaTime);
                Vector3 next = Vector3.Lerp(_rigidBody.position, _followTarget, t);
                _rigidBody.MovePosition(next);
            }
        }

        private void BeginSlide()
        {
            if (!IsOnBoard) boardAttachAudio.PlayAudio();
            
            state = State.Sliding;
            SetPhysicsGrabActive(false);
            _rigidBody.isKinematic = true;
            _grabOffset = transform.position - grabbable.GrabPoints[0].position;
            IsOnBoard = true;
        }

        private void Settle()
        {
            state = State.Settled;
            _rigidBody.isKinematic = true;
            SetPhysicsGrabActive(false);
            IsOnBoard = true;
            
        }

        public void Detach()
        {
            state = State.Free;
            _rigidBody.isKinematic = false;
            SetPhysicsGrabActive(true);
            IsOnBoard = false;
            boardDetachAudio.PlayAudio();
            WhenDetached?.Invoke(this);
        }

        public void PlayStringAttachSound()
        {
            stringAttachAudio.PlayAudio();
        }

        private void ExitFollow()
        {
            state = State.Free;
            _rigidBody.isKinematic = false;
            SetPhysicsGrabActive(true);
        }

        public Vector3 ConnectionPoint(Vector3 toward)
        {
            Vector3 dir = toward - stringAttachCenter.position;
            dir -= Vector3.Dot(dir, board.Normal) * board.Normal;
            dir = dir.normalized;
            return stringAttachCenter.position + dir * pinRadius;
        }

        private void SetPhysicsGrabActive(bool active)
        {
            if (physicsTransformer) physicsTransformer.enabled = active;
        }

        private Quaternion FlattenRotation()
        {
            Vector3 n = board.Normal;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n);
            // if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(grabbable.GrabPoints[0].forward, n);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(board.transform.forward, n);
            return Quaternion.LookRotation(fwd.normalized, n);
        }

        public void SetFollowTarget(Vector3 target, float speed)
        {
            _followTarget = target;
            _followSpeed = speed;
        }
    }
}