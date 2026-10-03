using UnityEngine;
using Oculus.Interaction;

public class LogSnapPlacement : MonoBehaviour
{
    [Header("References")]
    public SnapInteractor snapInteractor;
    public WoodLog woodLog;
    public Transform snapPose;

    [Header("Snap Completion")]
    public float positionTolerance = 0.05f;
    public float rotationTolerance = 5f;

    private bool placementTriggered = false;

    private void Awake()
    {
        if (snapInteractor == null)
            snapInteractor = GetComponentInChildren<SnapInteractor>();

        if (woodLog == null)
            woodLog = GetComponent<WoodLog>();

        // Automatically find the SnapPose child
        if (snapPose == null)
        {
            Transform foundSnapPose = transform.Find("SnapPose");

            if (foundSnapPose != null)
            {
                snapPose = foundSnapPose;
            }
        }
    }

    private void Update()
    {
        if (placementTriggered)
            return;

        if (snapInteractor == null)
            return;

        if (woodLog == null)
            return;

        if (snapPose == null)
        {
            Debug.LogWarning(
                "LogSnapPlacement: SnapPose Transform is missing!"
            );
            return;
        }

        SnapInteractable snapTarget =
            snapInteractor.SelectedInteractable;

        if (snapTarget == null)
            return;

        if (!snapTarget.PoseForInteractor(
                snapInteractor,
                out Pose targetPose))
        {
            return;
        }

        // Compare the SnapPose with the target pose
        float positionDistance =
            Vector3.Distance(
                snapPose.position,
                targetPose.position);

        float rotationDifference =
            Quaternion.Angle(
                snapPose.rotation,
                targetPose.rotation);

        Debug.Log(
            $"Log snapping | Distance: {positionDistance:F3}m | " +
            $"Rotation: {rotationDifference:F2}°"
        );

        if (positionDistance <= positionTolerance &&
            rotationDifference <= rotationTolerance)
        {
            placementTriggered = true;

            // Calculate where the LOG ROOT needs to be
            // so that the SnapPose ends up exactly on target.
            Vector3 localSnapPosition =
                transform.InverseTransformPoint(
                    snapPose.position
                );

            Quaternion localSnapRotation =
                Quaternion.Inverse(transform.rotation) *
                snapPose.rotation;

            Quaternion targetRootRotation =
                targetPose.rotation *
                Quaternion.Inverse(localSnapRotation);

            Vector3 targetRootPosition =
                targetPose.position -
                targetRootRotation * localSnapPosition;

            transform.SetPositionAndRotation(
                targetRootPosition,
                targetRootRotation
            );

            woodLog.PlaceLog();

            Debug.Log("================================");
            Debug.Log("SNAP COMPLETE - LOG IS NOW LOCKED!");
            Debug.Log("================================");
        }
    }
}