
using UnityEngine;
using Oculus.Interaction.Locomotion;

public class ThumbsUpMovement : MonoBehaviour
{
    [Header("Tracked Hands")]
    public OVRHand leftHand;
    public OVRHand rightHand;

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

    [Header("Gesture Detection")]
    public float thumbHeight = 0.05f;
    public float curledFingerDistance = 0.09f;

    private void Update()
    {
        bool leftGesture = IsThumbsUp(
            leftHand, leftWrist, leftThumbTip, leftPalm,
            leftIndexTip, leftMiddleTip, leftRingTip, leftPinkyTip);

        bool rightGesture = IsThumbsUp(
            rightHand, rightWrist, rightThumbTip, rightPalm,
            rightIndexTip, rightMiddleTip, rightRingTip, rightPinkyTip);

        if (leftGesture || rightGesture)
        {
            MoveForward();
        }
        else
        {
            StopHorizontalMovement();
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

    private void MoveForward()
    {
        if (locomotor == null || headTransform == null)
            return;

        // Use the headset's forward direction, ignoring head tilt.
        Vector3 direction = headTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            direction.Normalize();

        // Compensate for the locomotor's horizontal velocity damping.
        float damping = locomotor.IsGrounded
            ? locomotor.GroundDamping
            : locomotor.AirDamping;

        Vector3 velocity = direction * moveSpeed
            * (1f + damping * Time.deltaTime);

        velocity.y = locomotor.Velocity.y;
        locomotor.Velocity = velocity;
    }

    private void StopHorizontalMovement()
    {
        if (locomotor == null)
            return;

        Vector3 velocity = locomotor.Velocity;
        velocity.x = 0f;
        velocity.z = 0f;
        locomotor.Velocity = velocity;
    }
}
