using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

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

    // All grab interactables on this log, cached so we can lock/unlock them.
    private readonly List<MonoBehaviour> _grabInteractables = new List<MonoBehaviour>();

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

        CacheGrabInteractables();
    }

    private void CacheGrabInteractables()
    {
        _grabInteractables.Clear();
        _grabInteractables.AddRange(GetComponentsInChildren<HandGrabInteractable>(true));
        _grabInteractables.AddRange(GetComponentsInChildren<DistanceHandGrabInteractable>(true));
        _grabInteractables.AddRange(GetComponentsInChildren<GrabInteractable>(true));
        _grabInteractables.AddRange(GetComponentsInChildren<DistanceGrabInteractable>(true));
    }

    /// <summary>
    /// Prevents the player from picking the log up again.
    /// Disabling an interactable also releases any hand currently holding it.
    /// </summary>
    public void LockLog()
    {
        foreach (MonoBehaviour grab in _grabInteractables)
        {
            if (grab != null)
                grab.enabled = false;
        }
    }

    /// <summary>
    /// Makes the log grabbable again and re-arms snap detection.
    /// Only needed if the same object is reused after splitting.
    /// </summary>
    public void UnlockLog()
    {
        foreach (MonoBehaviour grab in _grabInteractables)
        {
            if (grab != null)
                grab.enabled = true;
        }

        placementTriggered = false;
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

            // Tell WoodLog that the log has been placed
            woodLog.PlaceLog();

            // Lock every grab interactable so the player can't pick it up again
            LockLog();

            Debug.Log("SNAP COMPLETE - LOG IS NOW LOCKED!");
        }
    }
}