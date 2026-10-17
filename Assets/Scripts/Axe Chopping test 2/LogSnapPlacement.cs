using System.Collections.Generic;
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

    [Tooltip("Seconds a snapped log gets to settle onto the snap pose. " +
             "After that it is placed anyway, so it can't end up locked but unchoppable.")]
    public float maxSettleTime = 1f;

    private bool placementTriggered = false;
    private float _snapTime;

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

    // Every way to pick the log up: hand grab, distance grab, ray and so on.
    private void CacheGrabInteractables()
    {
        _grabInteractables.Clear();

        foreach (IInteractable interactable in GetComponentsInChildren<IInteractable>(true))
        {
            if (interactable is SnapInteractable)
                continue;

            if (interactable is MonoBehaviour behaviour)
                _grabInteractables.Add(behaviour);
        }
    }

 
    public void LockLog()
    {
        foreach (MonoBehaviour grab in _grabInteractables)
        {
            if (grab != null)
                grab.enabled = false;
        }
    }

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

        if (_claimedTarget != snapTarget)
        {
            ReleaseTarget();
            _claimedTarget = snapTarget;
            Occupants[snapTarget] = this;
            _snapTime = Time.time;

            LockLog();
        }

        bool hasPose = snapTarget.PoseForInteractor(
            snapInteractor,
            out Pose targetPose);

        bool timedOut = Time.time - _snapTime >= maxSettleTime;

        if (!hasPose && !timedOut)
            return;

        // Compare the SnapPose with the target pose
        bool settled = false;

        if (hasPose)
        {
            float positionDistance =
                Vector3.Distance(
                    snapPose.position,
                    targetPose.position);

            float rotationDifference =
                Quaternion.Angle(
                    snapPose.rotation,
                    targetPose.rotation);

            settled =
                positionDistance <= positionTolerance &&
                rotationDifference <= rotationTolerance;
        }

        if (!settled && !timedOut)
            return;

        placementTriggered = true;

        if (hasPose)
        {
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
        }

        // Tell WoodLog that the log has been placed
        woodLog.PlaceLog();

        Debug.Log("SNAP COMPLETE - LOG IS NOW LOCKED!");
    }
}