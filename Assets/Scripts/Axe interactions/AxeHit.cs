using UnityEngine;

public class AxeHit : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Firewood"))
            return;

        float hitForce = collision.relativeVelocity.magnitude;

        Debug.Log("AXE HIT WOOD! Force: " + hitForce);

        Firewood wood = collision.gameObject.GetComponent<Firewood>();

        if (wood != null)
        {
            wood.AddHitForce(hitForce);
        }
    }
}