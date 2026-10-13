
using UnityEngine;

public class FireIgnitionZone : MonoBehaviour
{
    public GameObject fireParticles;
    public BonfireWoodManager woodManager;

    [Header("Cooking")]
    public GameObject cookingZone;

    public float ignitionTime = 1f;

    private float timer = 0f;
    private bool lighterInside = false;
    private bool fireLit = false;

    private void Start()
    {
        if (cookingZone != null)
        {
            cookingZone.SetActive(false);
        }

        if (fireParticles != null)
        {
            fireParticles.SetActive(false);
        }
    }

    private void Update()
    {
        if (fireLit)
            return;

        if (!lighterInside)
        {
            timer = 0f;
            return;
        }

        // Wait until all required wood has been placed.
        if (woodManager == null || !woodManager.IsComplete)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= ignitionTime)
        {
            IgniteFire();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Lighter"))
        {
            lighterInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Lighter"))
        {
            lighterInside = false;
            timer = 0f;
        }
    }

    private void IgniteFire()
    {
        if (fireLit)
            return;

        fireLit = true;

        if (fireParticles != null)
        {
            fireParticles.SetActive(true);
        }

        if (cookingZone != null)
        {
            cookingZone.SetActive(true);
        }

        // Advance the day cycle only when ignition succeeds.
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.CompleteFireObjective();
        }

        Debug.Log("Bonfire ignited!");
    }
}
