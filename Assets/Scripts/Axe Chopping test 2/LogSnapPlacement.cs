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

    // Which log currently occupies each snap target.
    // Shared between all LogSnapPlacement instances, so only one log
    // can be placed on a given SnapInteractable at a time.
    private static readonly Dictionary<SnapInteractable, LogSnapPlacement> Occupants =
        new Dictionary<SnapInteractable, LogSnapPlacement>();

    // The snap target this log has claimed (null if it hasn't placed yet).
    private SnapInteractable _claimedTarget;

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

    private void OnDestroy()
    {
        // Free the slot if this log is destroyed (e.g. after being chopped).
        ReleaseTarget();
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
    /// Makes the log grabbable again, frees the snap slot and re-arms
    /// snap detection. Only needed if the same object is reused after splitting.
    /// </summary>
    public void UnlockLog()
    {
        foreach (MonoBehaviour grab in _grabInteractables)
        {
            if (grab != null)
                grab.enabled = true;
        }

        ReleaseTarget();
        placementTriggered = false;
    }

    /// <summary>
    /// Call this when the log leaves the snap spot (chopped, removed, etc.)
    /// so another log can be placed there.
    /// </summary>
    public void ReleaseTarget()
    {
        if (_claimedTarget == null)
            return;

        if (Occupants.TryGetValue(_claimedTarget, out LogSnapPlacement occupant) &&
            occupant == this)
        {
            Occupants.Remove(_claimedTarget);
        }

        _claimedTarget = null;
    }

    private bool IsTargetOccupiedByAnotherLog(SnapInteractable target)
    {
        if (!Occupants.TryGetValue(target, out LogSnapPlacement occupant))
            return false;

        // Occupant was destroyed without cleaning up, treat the slot as free.
        if (occupant == null)
        {
            Occupants.Remove(target);
            return false;
        }

        return occupant != this;
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

        // Another log is already placed here, so this one can't complete.
        if (IsTargetOccupiedByAnotherLog(snapTarget))
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

            // Claim the snap target so no other log can be placed on it.
            _claimedTarget = snapTarget;
            Occupants[snapTarget] = this;

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