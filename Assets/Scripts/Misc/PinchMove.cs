
using UnityEngine;
using Oculus.Interaction.Locomotion;

public class PinchMove : MonoBehaviour
{
    [Header("Hand Tracking")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;

    [Header("Existing Locomotion")]
    [SerializeField] private FirstPersonLocomotor locomotor;
    [SerializeField] private Transform headTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private bool allowLeftHand = true;
    [SerializeField] private bool allowRightHand = true;

    private bool isPinching;

    private void Update()
    {
        bool leftPinch =
            allowLeftHand &&
            leftHand != null &&
            leftHand.IsTracked &&
            leftHand.GetFingerIsPinching(
                OVRHand.HandFinger.Index);

        bool rightPinch =
            allowRightHand &&
            rightHand != null &&
            rightHand.IsTracked &&
            rightHand.GetFingerIsPinching(
                OVRHand.HandFinger.Index);

        isPinching = leftPinch || rightPinch;
    }

    private void LateUpdate()
    {
        if (locomotor == null || headTransform == null)
            return;

        Vector3 velocity = locomotor.Velocity;

        if (isPinching)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                headTransform.forward,
                Vector3.up
            ).normalized;

            float damping = locomotor.IsGrounded
                ? locomotor.GroundDamping
                : locomotor.AirDamping;

            // Compensate for the locomotor's velocity damping.
            float compensation = 1f + damping * Time.deltaTime;

            velocity.x = forward.x * moveSpeed * compensation;
            velocity.z = forward.z * moveSpeed * compensation;
        }
        else
        {
            // Stop horizontal movement without overriding gravity.
            velocity.x = 0f;
            velocity.z = 0f;
        }

        locomotor.Velocity = velocity;
    }
}
