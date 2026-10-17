using UnityEngine;
using Oculus.Interaction;
//
// Runs before the hand visuals' LateUpdate, so the hand stays on the handle
// while the axe is stuck.
[DefaultExecutionOrder(-50)]
public class AxeImpact : MonoBehaviour
{
    private enum State { Free, Stuck, Returning }

    [Header("References")]
    [Tooltip("Optional. Found on this object if left empty.")]
    [SerializeField] private Grabbable grabbable;

    [Tooltip("Optional. The trigger on the axe head (next to AxeDamage) if left empty.")]
    [SerializeField] private Collider headTrigger;

    [Header("Impact")]
    [Tooltip("How deep (metres) the blade bites into the wood on a chopping hit.")]
    [SerializeField, Min(0f)] private float biteDepth = 0.03f;

    [Tooltip("Seconds the axe can stay stuck before it comes loose by itself.")]
    [SerializeField, Min(0.1f)] private float maxStuckTime = 1.5f;

    [Tooltip("The axe comes loose if the hand gets this far (metres) from it.")]
    [SerializeField, Min(0.05f)] private float maxHandDistance = 0.35f;

    [Tooltip("Seconds the axe takes to slide back into the hand after coming loose.")]
    [SerializeField, Min(0.01f)] private float returnTime = 0.12f;

    private Collider[] solidColliders;
    private bool collidersEnabled = true;

    private Rigidbody body;
    private RigidbodyInterpolation droppedInterpolation;

    private State state = State.Free;
    private Pose stuckPose;
    private Collider stuckCollider;
    private float stuckSince;
    private float returnStart;

    // Where the hand puts the axe this frame, before any correction here.
    private Pose desiredPose;
    private Pose lastWritten;
    private bool wroteLastFrame;

    // The axe pose in the latest physics step and the one before it.
    private Pose physicsPose;
    private Pose previousPhysicsPose;

    public bool IsHeld => grabbable != null && grabbable.SelectingPointsCount > 0;

    public bool IsStuck => state == State.Stuck;

    private void Awake()
    {
        if (grabbable == null)
            grabbable = GetComponent<Grabbable>();

        if (headTrigger == null)
        {
            AxeDamage damage = GetComponentInChildren<AxeDamage>(true);

            if (damage != null)
            {
                foreach (Collider candidate in damage.GetComponents<Collider>())
                {
                    if (candidate.isTrigger)
                        headTrigger = candidate;
                }
            }
        }

        var solids = new System.Collections.Generic.List<Collider>();

        foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
        {
            if (!candidate.isTrigger)
                solids.Add(candidate);
        }

        solidColliders = solids.ToArray();
        body = GetComponent<Rigidbody>();

        physicsPose = previousPhysicsPose = desiredPose = CurrentPose();
    }

    private void OnDisable()
    {
        SetHeldPhysics(false);
        state = State.Free;
    }

    private void FixedUpdate()
    {
        previousPhysicsPose = physicsPose;
        physicsPose = CurrentPose();
    }

    /// <summary>
    /// Called by AxeDamage when the head touches a placed log. bite: the hit
    /// cut into the wood (a hit that splits the log should not stick).
    /// </summary>
    public void RegisterHit(Collider logCollider, bool bite)
    {
        if (!IsHeld || state != State.Free || headTrigger == null || logCollider == null)
            return;

        Pose contact = FindContactPose(logCollider);

        if (bite)
        {
            Vector3 headTravel =
                HeadPosition(physicsPose) - HeadPosition(previousPhysicsPose);

            if (headTravel.sqrMagnitude > 1e-8f)
                contact.position += headTravel.normalized * biteDepth;
        }

        stuckPose = contact;
        stuckCollider = logCollider;
        stuckSince = Time.time;
        state = State.Stuck;
    }

    private void LateUpdate()
    {
        bool held = IsHeld;
        SetHeldPhysics(held);

        // If the grab didn't move the axe this frame, the transform still holds
        // what was written here last frame, so keep the previous desired pose.
        Pose current = CurrentPose();

        if (!wroteLastFrame || !SamePose(current, lastWritten))
            desiredPose = current;

        wroteLastFrame = false;

        if (state == State.Stuck && ShouldComeLoose(held))
        {
            state = held ? State.Returning : State.Free;
            returnStart = Time.time;
        }

        if (state == State.Stuck)
        {
            Write(stuckPose);
        }
        else if (state == State.Returning)
        {
            float t = (Time.time - returnStart) / returnTime;

            if (t >= 1f || !held)
            {
                state = State.Free;
            }
            else
            {
                t = t * t * (3f - 2f * t);
                Write(new Pose(
                    Vector3.Lerp(stuckPose.position, desiredPose.position, t),
                    Quaternion.Slerp(stuckPose.rotation, desiredPose.rotation, t)));
            }
        }
    }

    private bool ShouldComeLoose(bool held)
    {
        if (!held)
            return true;

        // The log was chopped or removed.
        if (stuckCollider == null || !stuckCollider.enabled || !stuckCollider.gameObject.activeInHierarchy)
            return true;

        if (Time.time - stuckSince > maxStuckTime)
            return true;

        if (Vector3.Distance(desiredPose.position, stuckPose.position) > maxHandDistance)
            return true;

        // The hand has pulled the axe back out of the wood.
        return !Overlaps(desiredPose, stuckCollider);
    }

    // The first pose between the last two physics steps where the head touches the log.
    private Pose FindContactPose(Collider logCollider)
    {
        Pose from = previousPhysicsPose;
        Pose to = physicsPose;

        if (Overlaps(from, logCollider))
            return to;

        float outside = 0f;
        float inside = 1f;

        for (int i = 0; i < 10; i++)
        {
            float middle = (outside + inside) * 0.5f;

            if (Overlaps(Lerp(from, to, middle), logCollider))
                inside = middle;
            else
                outside = middle;
        }

        return Lerp(from, to, inside);
    }

    private bool Overlaps(Pose axePose, Collider other)
    {
        Pose head = HeadPose(axePose);

        return Physics.ComputePenetration(
            headTrigger, head.position, head.rotation,
            other, other.transform.position, other.transform.rotation,
            out _, out _);
    }

    // Where the head trigger would be if the axe root had the given pose.
    private Pose HeadPose(Pose axePose)
    {
        Transform head = headTrigger.transform;
        Quaternion toLocal = Quaternion.Inverse(transform.rotation);

        return new Pose(
            axePose.position + axePose.rotation * (toLocal * (head.position - transform.position)),
            axePose.rotation * (toLocal * head.rotation));
    }

    private Vector3 HeadPosition(Pose axePose)
    {
        return HeadPose(axePose).position;
    }

    private void Write(Pose pose)
    {
        transform.SetPositionAndRotation(pose.position, pose.rotation);
        lastWritten = pose;
        wroteLastFrame = true;
    }

    // While held: solid colliders off, so the axe can't shove anything, and no
    // rigidbody interpolation, which would fight the hand moving the transform.
    private void SetHeldPhysics(bool held)
    {
        if (collidersEnabled == !held || solidColliders == null)
            return;

        collidersEnabled = !held;

        foreach (Collider solid in solidColliders)
        {
            if (solid != null)
                solid.enabled = !held;
        }

        if (body != null)
        {
            if (held)
            {
                droppedInterpolation = body.interpolation;
                body.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                body.interpolation = droppedInterpolation;
            }
        }
    }

    private Pose CurrentPose()
    {
        return new Pose(transform.position, transform.rotation);
    }

    private static bool SamePose(Pose a, Pose b)
    {
        return a.position == b.position && a.rotation == b.rotation;
    }

    private static Pose Lerp(Pose a, Pose b, float t)
    {
        return new Pose(Vector3.Lerp(a.position, b.position, t), Quaternion.Slerp(a.rotation, b.rotation, t));
    }
}
