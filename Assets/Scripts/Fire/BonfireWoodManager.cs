using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
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
        if (currentSlot >= woodSlots.Length)
            return;

        // Get the wood that was just placed
        GameObject placedWood = woodSlots[currentSlot];

        // Lock the placed wood so the player can't pick it up again
        LockWood(placedWood);

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

    /// <summary>
    /// Disables every grab interactable on the placed wood (including children),
    /// which also releases any hand currently holding it.
    /// </summary>
    private void LockWood(GameObject wood)
    {
        if (wood == null)
            return;

        List<MonoBehaviour> grabInteractables = new List<MonoBehaviour>();

        grabInteractables.AddRange(wood.GetComponentsInChildren<HandGrabInteractable>(true));
        grabInteractables.AddRange(wood.GetComponentsInChildren<DistanceHandGrabInteractable>(true));
        grabInteractables.AddRange(wood.GetComponentsInChildren<GrabInteractable>(true));
        grabInteractables.AddRange(wood.GetComponentsInChildren<DistanceGrabInteractable>(true));

        foreach (MonoBehaviour grab in grabInteractables)
        {
            if (grab != null)
                grab.enabled = false;
        }
    }
}