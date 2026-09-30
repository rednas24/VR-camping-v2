using UnityEngine;

public class PanCooking : MonoBehaviour
{
    [Header("Can")]
    public GameObject closedCan;
    public GameObject openCan;

    [Header("Cooking")]
    public float cookingTime = 5f;

    [Header("Pop Effect")]
    public float popForce = 2f;
    public float popTorque = 1.5f;
    public float randomSideForce = 0.2f;

    [Header("Optional Effects")]
    public AudioSource popSound;
    public ParticleSystem popVFX;

    [Header("Cooking Smoke")]
    public ParticleSystem cookingSmoke;

    private bool canPlaced = false;
    private bool cooking = false;
    private bool cookingFinished = false;

    private float cookingTimer = 0f;

    public void CanPlaced()
    {
        // Don't allow another can to be placed after cooking is finished
        if (cookingFinished)
            return;

        canPlaced = true;

        Debug.Log("Can placed in pan!");
    }

    public void StartCooking()
    {
        // No can in the pan
        if (!canPlaced)
            return;

        // Already finished
        if (cookingFinished)
            return;

        // Already cooking
        if (cooking)
            return;

        cooking = true;

        // Start cooking smoke
        if (cookingSmoke != null)
        {
            cookingSmoke.Play();
        }

        Debug.Log("Can is cooking!");
    }

    public void StopCooking()
    {
        if (!cooking)
            return;

        cooking = false;

        // Stop cooking smoke
        if (cookingSmoke != null)
        {
            cookingSmoke.Stop();
        }

        Debug.Log("Can stopped cooking!");
    }

    private void Update()
    {
        if (!cooking)
            return;

        cookingTimer += Time.deltaTime;

        if (cookingTimer >= cookingTime)
        {
            FinishCooking();
        }
    }

    private void FinishCooking()
    {
        // Make sure this can can never cook/pop again
        cooking = false;
        cookingFinished = true;
        canPlaced = false;

        // Stop cooking smoke
        if (cookingSmoke != null)
        {
            cookingSmoke.Stop();
        }

        Debug.Log("Can finished cooking!");

        // Save the closed can's exact world position and rotation
        Vector3 spawnPosition = closedCan.transform.position;
        Quaternion spawnRotation = closedCan.transform.rotation;

        // Hide the closed can
        closedCan.SetActive(false);

        // Make sure the open can isn't parented to the pan
        openCan.transform.SetParent(null);

        // Put the open can where the closed can was
        openCan.transform.position = spawnPosition;
        openCan.transform.rotation = spawnRotation;

        // Activate the open can
        openCan.SetActive(true);

        // Optional sound
        if (popSound != null)
        {
            popSound.Play();
        }

        // Optional VFX
        if (popVFX != null)
        {
            popVFX.Play();
        }

        // Get the open can's Rigidbody
        Rigidbody rb = openCan.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Pop out of the pan
            Vector3 popDirection = transform.up;

            // Add a little random sideways movement
            popDirection += transform.right *
                            Random.Range(-randomSideForce, randomSideForce);

            popDirection += transform.forward *
                            Random.Range(-randomSideForce, randomSideForce);

            popDirection.Normalize();

            rb.AddForce(
                popDirection * popForce,
                ForceMode.Impulse
            );

            // Add some rotation
            rb.AddTorque(
                Random.insideUnitSphere * popTorque,
                ForceMode.Impulse
            );
        }
    }
}