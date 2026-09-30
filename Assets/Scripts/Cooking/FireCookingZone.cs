using UnityEngine;

public class FireCookingZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PanCooking cooking =
            other.GetComponentInParent<PanCooking>();

        if (cooking != null)
        {
            cooking.StartCooking();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PanCooking cooking =
            other.GetComponentInParent<PanCooking>();

        if (cooking != null)
        {
            cooking.StopCooking();
        }
    }
}