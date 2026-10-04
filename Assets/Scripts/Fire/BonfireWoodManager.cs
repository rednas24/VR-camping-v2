using UnityEngine;
using Oculus.Interaction.HandGrab;

public class BonfireWoodManager : MonoBehaviour
{
    public GameObject[] woodSlots;

    private int currentSlot = 0;

    public bool IsComplete { get; private set; }

    private void Start()
    {
        for (int i = 0; i < woodSlots.Length; i++)
        {
            woodSlots[i].SetActive(i == 0);
        }

        IsComplete = false;
    }

    public void WoodPlaced()
    {
        // Get the wood that was just placed
        GameObject placedWood = woodSlots[currentSlot];

        // Disable Distance Hand Grab
        DistanceHandGrabInteractable distanceGrab =
            placedWood.GetComponent<DistanceHandGrabInteractable>();

        if (distanceGrab != null)
        {
            distanceGrab.enabled = false;
        }

        // Move to the next slot
        currentSlot++;

        if (currentSlot < woodSlots.Length)
        {
            woodSlots[currentSlot].SetActive(true);
        }
        else
        {
            IsComplete = true;
            Debug.Log("Bonfire wood is complete!");
        }
    }
}