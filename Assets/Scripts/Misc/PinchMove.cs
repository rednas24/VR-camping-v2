
using UnityEngine;
using Oculus.Interaction.Locomotion;

public class PinchMove : MonoBehaviour
{
    [Header("Tracked Hands")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;

    [Header("Hand Skeletons")]
    [SerializeField] private OVRSkeleton leftSkeleton;
    [SerializeField] private OVRSkeleton rightSkeleton;

    [Header("Existing Locomotion")]
    [SerializeField] private FirstPersonLocomotor locomotor;
    [SerializeField] private Transform headTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private bool allowLeftHand = true;
    [SerializeField] private bool allowRightHand = true;

    [Header("Thumbs-Up Detection")]
    [Tooltip("How far above the wrist the thumb tip must be.")]
    [SerializeField] private float thumbHeight = 0.06f;

    [Tooltip("Maximum wrist-to-fingertip distance for curled fingers.")]
    [SerializeField] private float curledFingerMaxDistance = 0.14f;

    private bool IsThumbsUp(
        OVRHand hand,
        OVRSkeleton skeleton)
    {
        if (hand == null || skeleton == null)
            return false;

        if (!hand.IsTracked ||
            !hand.IsDataValid ||
            !skeleton.IsInitialized ||
            skeleton.Bones == null)
        {
            return false;
        }

        Transform wrist = FindBone(skeleton, "WristRoot", "Wrist");
        Transform thumbTip = FindBone(skeleton, "ThumbTip");
        Transform indexTip = FindBone(skeleton, "IndexTip");
        Transform middleTip = FindBone(skeleton, "MiddleTip");
        Transform ringTip = FindBone(skeleton, "RingTip");
        Transform pinkyTip = FindBone(skeleton, "PinkyTip", "LittleTip");

        if (wrist == null || thumbTip == null ||
            indexTip == null || middleTip == null ||
            ringTip == null || pinkyTip == null)
        {
            return false;
        }

        // Thumb must be raised above the wrist.
        bool thumbRaised =
            thumbTip.position.y >
            wrist.position.y + thumbHeight;

        // Other four fingers must be curled toward the palm.
        bool fingersCurled =
            IsCurled(wrist, indexTip) &&
            IsCurled(wrist, middleTip) &&
            IsCurled(wrist, ringTip) &&
            IsCurled(wrist, pinkyTip);

        return thumbRaised && fingersCurled;
    }

    private bool IsCurled(Transform wrist, Transform fingertip)
    {
        return Vector3.Distance(
            wrist.position,
            fingertip.position
        ) < curledFingerMaxDistance;
    }

    private Transform FindBone(
        OVRSkeleton skeleton,
        params string[] possibleNames)
    {
        foreach (var bone in skeleton.Bones)
        {
            if (bone == null || bone.Transform == null)
                continue;

            string boneName = bone.Id.ToString();

            foreach (string possibleName in possibleNames)
            {
                if (boneName.EndsWith(
                    possibleName,
                    System.StringComparison.Ordinal))
                {
                    return bone.Transform;
                }
            }
        }

        return null;
    }

    private void LateUpdate()
    {
        if (locomotor == null || headTransform == null)
            return;

        bool moving =
            (allowLeftHand && IsThumbsUp(leftHand, leftSkeleton)) ||
            (allowRightHand && IsThumbsUp(rightHand, rightSkeleton));

        Vector3 velocity = locomotor.Velocity;

        if (moving)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                headTransform.forward,
                Vector3.up
            ).normalized;

            float damping = locomotor.IsGrounded
                ? locomotor.GroundDamping
                : locomotor.AirDamping;

            float compensation = 1f + damping * Time.deltaTime;

            velocity.x = forward.x * moveSpeed * compensation;
            velocity.z = forward.z * moveSpeed * compensation;
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        // Preserve the locomotor's vertical velocity and gravity.
        locomotor.Velocity = velocity;
    }
}
