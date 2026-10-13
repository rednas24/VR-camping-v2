
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
        if (cookingFinished)
            return;

        canPlaced = true;

        Debug.Log("Can placed in pan!");
    }

    public void StartCooking()
    {
        if (!canPlaced || cookingFinished || cooking)
            return;

        cooking = true;

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
        if (cookingFinished)
            return;

        if (closedCan == null || openCan == null)
        {
            Debug.LogError(
                "PanCooking: Assign both Closed Can and Open Can."
            );

            cooking = false;

            if (cookingSmoke != null)
            {
                cookingSmoke.Stop();
            }

            return;
        }

        // Prevent cooking and completion from running twice.
        cooking = false;
        cookingFinished = true;
        canPlaced = false;

        if (cookingSmoke != null)
        {
            cookingSmoke.Stop();
        }

        Debug.Log("Can finished cooking!");

        // Preserve the original can's world position and rotation.
        Vector3 spawnPosition = closedCan.transform.position;
        Quaternion spawnRotation = closedCan.transform.rotation;

        closedCan.SetActive(false);

        // Detach and position the cooked can.
        openCan.transform.SetParent(null);
        openCan.transform.SetPositionAndRotation(
            spawnPosition,
            spawnRotation
        );

        openCan.SetActive(true);

        // Advance the day cycle on successful cooking completion.
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.CompleteFoodObjective();
        }

        if (popSound != null)
        {
            popSound.Play();
        }

        if (popVFX != null)
        {
            popVFX.Play();
        }

        Rigidbody rb = openCan.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 popDirection = transform.up;

            popDirection += transform.right *
                Random.Range(-randomSideForce, randomSideForce);

            popDirection += transform.forward *
                Random.Range(-randomSideForce, randomSideForce);

            popDirection.Normalize();

            rb.AddForce(
                popDirection * popForce,
                ForceMode.Impulse
            );

            rb.AddTorque(
                Random.insideUnitSphere * popTorque,
                ForceMode.Impulse
            );
        }
    }
}
