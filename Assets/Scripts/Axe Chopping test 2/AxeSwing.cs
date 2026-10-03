using UnityEngine;

public class AxeSwing : MonoBehaviour
{
    [Header("Axe Head")]
    public Transform axeHead;

    [Header("Physics")]
    public Transform centerOfMass;
    public float axeMass = 2.0f;
    public float angularDrag = 0.3f;

    public float swingSpeed { get; private set; }

    private Vector3 previousPosition;
    private Rigidbody rb;

    private void Start()
    {
        if (axeHead == null)
        {
            axeHead = transform.Find("AxeHead");
        }

        if (axeHead == null)
        {
            Debug.LogError("AxeSwing could not find AxeHead!");
            return;
        }

        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.mass = axeMass;
        rb.useGravity = true;
        rb.isKinematic = false;

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.angularDamping = angularDrag;

        // Use the manually positioned CenterOfMass object
        if (centerOfMass != null)
        {
            rb.centerOfMass =
                transform.InverseTransformPoint(centerOfMass.position);
        }

        previousPosition = axeHead.position;
    }

    private void FixedUpdate()
    {
        if (axeHead == null)
            return;

        Vector3 movement =
            axeHead.position - previousPosition;

        swingSpeed =
            movement.magnitude / Time.fixedDeltaTime;

        previousPosition = axeHead.position;
    }
}