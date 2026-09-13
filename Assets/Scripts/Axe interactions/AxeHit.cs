using UnityEngine;

public class AxeHit : MonoBehaviour
{
    [SerializeField] private float minimumHitForce = 2.5f;

    private void OnCollisionEnter(Collision collision)
    {
        // Only react to the firewood
        if (!collision.gameObject.CompareTag("Firewood"))
            return;

        // Measure how fast the axe and wood hit each other
        float hitForce = collision.relativeVelocity.magnitude;

        Debug.Log("AXE HIT WOOD! Force: " + hitForce);

        // Only split if the hit was strong enough
        if (hitForce >= minimumHitForce)
        {
            Firewood wood = collision.gameObject.GetComponent<Firewood>();

            if (wood != null)
            {
                Debug.Log("WOOD SPLIT!");
                wood.Split();
            }
            else
            {
                Debug.LogWarning("Firewood was hit, but no Firewood script was found!");
            }
        }
    }
}