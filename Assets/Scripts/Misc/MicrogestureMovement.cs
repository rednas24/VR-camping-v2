using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;

public class MicrogestureMovement : MonoBehaviour
{
    [Header("Tracked Hands")]
    public OVRHand leftHand;
    public OVRHand rightHand;

    public bool allowLeftHand = true;

    [Tooltip("Off by default so chopping with the right hand can't move you.")]
    public bool allowRightHand = false;

    [Header("Movement")]
    public FirstPersonLocomotor locomotor;
    public Transform headTransform;
    public float moveSpeed = 1.5f;
    public float backwardSpeed = 0.8f;

    [Tooltip("Seconds to reach full speed and to stop again. " +
             "Gentle starts and stops are more comfortable in VR.")]
    public float accelerationTime = 0.25f;

    [Tooltip("Stops walking if no allowed hand has been tracked for this many seconds, " +
             "so you don't keep walking after lowering your hands out of view.")]
    public float stopAfterHandsLost = 1f;

    [Header("Turning")]
    public bool allowSnapTurn = true;
    public float snapTurnAngle = 45f;

    [Header("Gesture Detection")]
    [Tooltip("Seconds after a gesture during which the same hand is ignored, " +
             "so one swipe can't count twice.")]
    public float gestureCooldown = 0.3f;

    [Tooltip("Turns off the camera rig's own microgesture locomotion (teleport, " +
             "turn and step), which reacts to the same gestures.")]
    public bool disableBuiltInMicrogestureLocomotion = true;

    private const string BuiltInLocomotionName = "MicroGesturesLocomotionHandInteractorGroup";

    private HandGrabTracker grabTracker;

    // Marks the turn events as coming from this script.
    private UniqueIdentifier identifier;

    // -1 = backward, 0 = standing, 1 = forward.
    private int walkDirection;
    private float currentSpeed;
    private bool wasMoving;

    private float leftCooldownEnd;
    private float rightCooldownEnd;
    private float handsLostTime;

    private void Awake()
    {
        identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);
    }

    private void Start()
    {
        grabTracker = new HandGrabTracker();

        if (disableBuiltInMicrogestureLocomotion)
            DisableBuiltInLocomotion();
    }

    private void DisableBuiltInLocomotion()
    {
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (candidate.name == BuiltInLocomotionName && candidate.gameObject.activeSelf)
            {
                candidate.gameObject.SetActive(false);
                Debug.Log($"MicrogestureMovement turned off {BuiltInLocomotionName} so its gestures don't clash.", candidate);
            }
        }
    }

    private void OnDestroy()
    {
        UniqueIdentifier.Release(identifier);
    }

    private void Update()
    {
        bool anyHandTracked = false;

        if (allowLeftHand)
            ReadHand(leftHand, Handedness.Left, ref leftCooldownEnd, ref anyHandTracked);

        if (allowRightHand)
            ReadHand(rightHand, Handedness.Right, ref rightCooldownEnd, ref anyHandTracked);

        handsLostTime = anyHandTracked ? 0f : handsLostTime + Time.deltaTime;

        if (handsLostTime >= stopAfterHandsLost)
            walkDirection = 0;
    }

    private void ReadHand(OVRHand hand, Handedness handedness, ref float cooldownEnd, ref bool anyHandTracked)
    {
        if (hand == null || !hand.IsTracked)
            return;

        anyHandTracked = true;

        // Also tells the runtime that microgestures are in use.
        OVRHand.MicrogestureType gesture = hand.GetMicrogestureType();

        if (gesture == OVRHand.MicrogestureType.NoGesture ||
            gesture == OVRHand.MicrogestureType.Invalid)
            return;

        if (Time.time < cooldownEnd || grabTracker.IsHolding(handedness))
            return;

        cooldownEnd = Time.time + gestureCooldown;

        switch (gesture)
        {
            case OVRHand.MicrogestureType.SwipeForward:
                walkDirection = Mathf.Min(walkDirection + 1, 1);
                break;

            case OVRHand.MicrogestureType.SwipeBackward:
                walkDirection = Mathf.Max(walkDirection - 1, -1);
                break;

            case OVRHand.MicrogestureType.ThumbTap:
                walkDirection = 0;
                break;

            case OVRHand.MicrogestureType.SwipeLeft:
                SnapTurn(-snapTurnAngle);
                break;

            case OVRHand.MicrogestureType.SwipeRight:
                SnapTurn(snapTurnAngle);
                break;
        }
    }

    private void SnapTurn(float angle)
    {
        if (!allowSnapTurn || locomotor == null)
            return;

        locomotor.HandleLocomotionEvent(new LocomotionEvent(
            identifier.ID,
            Quaternion.Euler(0f, angle, 0f),
            LocomotionEvent.RotationType.Relative));
    }

    // Runs after the locomotor's Update, so the velocity is used next frame.
    private void LateUpdate()
    {
        if (locomotor == null || headTransform == null)
            return;

        float targetSpeed =
            walkDirection > 0 ? moveSpeed :
            walkDirection < 0 ? -backwardSpeed :
            0f;

        float acceleration = accelerationTime > 0f ? moveSpeed / accelerationTime : float.MaxValue;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        if (!Mathf.Approximately(currentSpeed, 0f))
        {
            Move(currentSpeed);
            wasMoving = true;
        }
        else if (wasMoving)
        {
            // Only stop once, so other locomotion isn't overridden while standing still.
            StopHorizontalMovement();
            wasMoving = false;
        }
    }

    // Negative speed moves backward.
    private void Move(float speed)
    {
        // Use the headset's forward direction, ignoring head tilt.
        Vector3 direction = headTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            direction.Normalize();

        // Compensate for the locomotor's horizontal velocity damping.
        float damping = locomotor.IsGrounded
            ? locomotor.GroundDamping
            : locomotor.AirDamping;

        Vector3 velocity = direction * speed
            * (1f + damping * Time.deltaTime);

        velocity.y = locomotor.Velocity.y;
        locomotor.Velocity = velocity;
    }

    private void StopHorizontalMovement()
    {
        Vector3 velocity = locomotor.Velocity;
        velocity.x = 0f;
        velocity.z = 0f;
        locomotor.Velocity = velocity;
    }
}
