using UnityEngine;

public class AxeImpact : MonoBehaviour
{
    [Header("References")]
    public Transform axeHead;

    [Header("Impact")]
    public float impactOffset = 0.005f;

    private bool isImpacted = false;

    private Vector3 previousAxePosition;
    private Quaternion previousAxeRotation;

    private void Start()
    {
        if (axeHead == null)
        {
            Debug.LogError("AxeImpact: Axe Head is not assigned!");
            return;
        }

        previousAxePosition = axeHead.position;
        previousAxeRotation = axeHead.rotation;
    }

    private void LateUpdate()
    {
        if (axeHead == null)
            return;

        // If the axe has not hit anything,
        // remember its previous pose.
        if (!isImpacted)
        {
            previousAxePosition = axeHead.position;
            previousAxeRotation = axeHead.rotation;
        }
    }

    public void RegisterHit()
    {
        if (isImpacted)
            return;

        isImpacted = true;

        // Move the entire axe back to the pose
        // it had immediately before the impact.
        Vector3 positionDifference =
            previousAxePosition - axeHead.position;

        transform.position += positionDifference;

        Quaternion rotationDifference =
            previousAxeRotation * Quaternion.Inverse(
                axeHead.rotation
            );

        transform.rotation =
            rotationDifference * transform.rotation;

        Debug.Log("AXE IMPACT");
    }

    public void ReleaseImpact()
    {
        isImpacted = false;

        previousAxePosition = axeHead.position;
        previousAxeRotation = axeHead.rotation;

        Debug.Log("AXE RELEASED");
    }
}