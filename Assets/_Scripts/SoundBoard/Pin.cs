using Oculus.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PinType { Sine, Sawtooth, Square }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Grabbable))]
[RequireComponent(typeof(GrabFreePhysicsTransformer))]
public class Pin : MonoBehaviour
{
    private enum State { Free, Sliding, Settled }

    public static readonly List<Pin> All = new();

    [Header("References")]
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private SoundBoardSurface board;
    [SerializeField] private GrabFreePhysicsTransformer physicsTransformer;

    [Header("Piece")]
    [Tooltip("Radius used for pin separation on the board")]
    [SerializeField] private float pieceRadius = 0.03f;

    [Tooltip("Piece type, passed through to the graph point")]
    [SerializeField] private PinType type = PinType.Sine;

    [Header("Board Snap")]
    [Tooltip("Distance from the plane at which held pin snaps onto the board")]
    [SerializeField] private float snapDistance = 0.1f;
    [Tooltip("Extra distance beyond engageDistance for detaching the pins from the board")]
    [SerializeField] private float releaseMargin = 0.05f;

    // Getters
    public PinType Type => type;
    public Vector3 Position => transform.position;
    public bool IsOnBoard => isPlaced;


    private Rigidbody rigidBody;
    private State state = State.Free;
    private bool isBeingHeld;
    private bool isPlaced = false;
    private Vector3 grabOffset;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
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

    private void OnPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            isBeingHeld = true;
            if (state == State.Settled)
                BeginSlide();
        }
        else if (evt.Type == PointerEventType.Unselect && grabbable.GrabPoints.Count == 0)
        {
            isBeingHeld = false;
            if (state == State.Sliding)
                Settle();
            else
            {
                rigidBody.isKinematic = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isBeingHeld)
        {
            // Get distance from board
            if (grabbable.GrabPoints.Count == 0) return;
            Vector3 grabPos = grabbable.GrabPoints[0].position;
            float handDistance = Mathf.Abs(board.DistanceFromPlane(grabPos));

            // If Free attach to board if withing the distance
            // Else detach from board if Sliding and outside the extra distance
            if (state == State.Free && handDistance <= snapDistance)
            {
                BeginSlide();
            }
            else if (state == State.Sliding && handDistance >= snapDistance + releaseMargin)
            {
                Detach();
            }

            if (state == State.Sliding)
            {
                rigidBody.isKinematic = true;
                Vector3 point = grabPos + grabOffset;
                Vector3 boardPosition = board.ClampToPlayableArea(point);
                rigidBody.MovePosition(boardPosition);
                rigidBody.MoveRotation(FlattenRotation()); //ROTATION
            }
        }
        else if (state == State.Settled)
        {
            rigidBody.isKinematic = true;
        }
    }

    private void BeginSlide()
    {
        state = State.Sliding;
        SetPhysicsGrabActive(false);
        rigidBody.isKinematic = true;
        grabOffset = transform.position - grabbable.GrabPoints[0].position;

        isPlaced = true;
    }

    private void Settle()
    {
        state = State.Settled;
        rigidBody.isKinematic = true;
        SetPhysicsGrabActive(false);

        isPlaced = true;
    }

    private void Detach()
    {
        state = State.Free;
        rigidBody.isKinematic = false;
        SetPhysicsGrabActive(true);
        isPlaced = false;
    }

    private void SetPhysicsGrabActive(bool active)
    {
        if (physicsTransformer != null) physicsTransformer.enabled = active;
    }

    private Quaternion FlattenRotation()
    {
        Vector3 n = board.Normal;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(grabbable.GrabPoints[0].forward, n);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(board.transform.forward, n);
        return Quaternion.LookRotation(fwd.normalized, n);
    }
}
