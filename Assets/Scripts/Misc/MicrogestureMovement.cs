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
    public bool allowRightHand = false;

    [Header("Movement")]
    public FirstPersonLocomotor locomotor;
    public Transform headTransform;
    public float moveSpeed = 1.5f;
    public float backwardSpeed = 0.8f;
    public float accelerationTime = 0.25f;
    public float stopAfterHandsLost = 1f;

    [Header("Turning")]
    public bool allowSnapTurn = true;
    public float snapTurnAngle = 45f;

    [Header("Gesture Detection")]
    public float gestureCooldown = 0.3f;
    public bool disableBuiltInMicrogestureLocomotion = true;
    private const string BuiltInLocomotionName = "MicroGesturesLocomotionHandInteractorGroup";
    private HandGrabTracker grabTracker;
    private UniqueIdentifier identifier;
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
            StopHorizontalMovement();
            wasMoving = false;
        }
    }
    private void Move(float speed)
    {
        Vector3 direction = headTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            direction.Normalize();

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
