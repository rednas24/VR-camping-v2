using UnityEngine;

public class FireIgnitionZone : MonoBehaviour
{
    public GameObject fireParticles;
    public BonfireWoodManager woodManager;

    public float ignitionTime = 1f;

    private float timer = 0f;
    private bool lighterInside = false;
    private bool fireLit = false;

    private void Update()
    {
        if (fireLit)
            return;

        if (!lighterInside)
        {
            timer = 0f;
            return;
        }

        // Don't allow ignition until all wood is placed
        if (!woodManager.IsComplete)
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
        fireLit = true;

        fireParticles.SetActive(true);

        Debug.Log("Bonfire ignited!");
    }
}