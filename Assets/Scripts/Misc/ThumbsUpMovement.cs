using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;

// Walks the player forward (where the head is looking) while a hand makes a
// thumbs-up. A hand that is holding something never counts, so gripping the
// axe with the thumb on top doesn't make you walk.
public class ThumbsUpMovement : MonoBehaviour
{
    [Header("Tracked Hands")]
    public OVRHand leftHand;
    public OVRHand rightHand;

    public bool allowLeftHand = true;
    public bool allowRightHand = true;

    [Header("Left Hand Bones")]
    public Transform leftWrist;
    public Transform leftThumbTip;
    public Transform leftPalm;
    public Transform leftIndexTip;
    public Transform leftMiddleTip;
    public Transform leftRingTip;
    public Transform leftPinkyTip;

    [Header("Right Hand Bones")]
    public Transform rightWrist;
    public Transform rightThumbTip;
    public Transform rightPalm;
    public Transform rightIndexTip;
    public Transform rightMiddleTip;
    public Transform rightRingTip;
    public Transform rightPinkyTip;

    [Header("Movement")]
    public FirstPersonLocomotor locomotor;
    public Transform headTransform;
    public float moveSpeed = 1.5f;

    [Tooltip("Seconds to reach full speed and to stop again. " +
             "Gentle starts and stops are more comfortable in VR.")]
    public float accelerationTime = 0.25f;

    [Header("Gesture Detection")]
    public float thumbHeight = 0.05f;
    public float curledFingerDistance = 0.09f;

    [Tooltip("Seconds the thumbs-up must be held before walking starts, " +
             "so a hand passing through the pose doesn't move you.")]
    public float holdTime = 0.2f;

    [Tooltip("Seconds the gesture can drop out (e.g. a tracking glitch) " +
             "before walking stops.")]
    public float releaseTime = 0.15f;

    private HandGrabTracker grabTracker;

    private float gestureTime;
    private float lostGestureTime;
    private bool isWalking;

    private float currentSpeed;
    private bool wasMoving;

    private void Start()
    {
        grabTracker = new HandGrabTracker();
    }

    private void Update()
    {
        bool leftGesture =
            allowLeftHand &&
            !grabTracker.IsHolding(Handedness.Left) &&
            IsThumbsUp(
                leftHand, leftWrist, leftThumbTip, leftPalm,
                leftIndexTip, leftMiddleTip, leftRingTip, leftPinkyTip);

        bool rightGesture =
            allowRightHand &&
            !grabTracker.IsHolding(Handedness.Right) &&
            IsThumbsUp(
                rightHand, rightWrist, rightThumbTip, rightPalm,
                rightIndexTip, rightMiddleTip, rightRingTip, rightPinkyTip);

        if (leftGesture || rightGesture)
        {
            gestureTime += Time.deltaTime;
            lostGestureTime = 0f;

            if (gestureTime >= holdTime)
                isWalking = true;
        }
        else
        {
            gestureTime = 0f;

            if (isWalking)
            {
                lostGestureTime += Time.deltaTime;

                if (lostGestureTime >= releaseTime)
                    isWalking = false;
            }
        }
    }

    // Runs after the locomotor's Update, so the velocity is used next frame.
    private void LateUpdate()
    {
        if (locomotor == null || headTransform == null)
            return;

        float targetSpeed = isWalking ? moveSpeed : 0f;
        float acceleration = accelerationTime > 0f ? moveSpeed / accelerationTime : float.MaxValue;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        if (currentSpeed > 0f)
        {
            MoveForward(currentSpeed);
            wasMoving = true;
        }
        else if (wasMoving)
        {
            // Only stop once, so other locomotion isn't overridden while standing still.
            StopHorizontalMovement();
            wasMoving = false;
        }
    }

    private bool IsThumbsUp(
        OVRHand hand,
        Transform wrist,
        Transform thumbTip,
        Transform palm,
        Transform indexTip,
        Transform middleTip,
        Transform ringTip,
        Transform pinkyTip)
    {
        if (hand == null || !hand.IsTracked || !hand.IsDataValid)
            return false;

        if (wrist == null || thumbTip == null || palm == null ||
            indexTip == null || middleTip == null ||
            ringTip == null || pinkyTip == null)
            return false;

        // The thumb must be raised above the wrist.
        bool thumbRaised =
            thumbTip.position.y > wrist.position.y + thumbHeight;

        // The other four fingers must be curled toward the palm.
        bool fingersCurled =
            Vector3.Distance(indexTip.position, palm.position)
                < curledFingerDistance &&
            Vector3.Distance(middleTip.position, palm.position)
                < curledFingerDistance &&
            Vector3.Distance(ringTip.position, palm.position)
                < curledFingerDistance &&
            Vector3.Distance(pinkyTip.position, palm.position)
                < curledFingerDistance;

        return thumbRaised && fingersCurled;
    }

    private void MoveForward(float speed)
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
