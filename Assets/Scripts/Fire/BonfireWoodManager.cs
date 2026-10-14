using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class BonfireWoodManager : MonoBehaviour
{
    // Empty placeholder objects marking where each piece of wood goes.
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

    /// <summary>
    /// Call this with the log that was just placed (the real object the
    /// player was holding, NOT the placeholder slot).
    /// </summary>
    public void WoodPlaced(GameObject placedWood)
    {
        if (currentSlot >= woodSlots.Length)
            return;

        // Lock the log that was actually placed so it can't be grabbed again
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
    /// Old signature kept so existing calls still compile, but it can't know
    /// which log was placed, so nothing gets locked. Use WoodPlaced(gameObject).
    /// </summary>
    public void WoodPlaced()
    {
        Debug.LogWarning(
            "BonfireWoodManager.WoodPlaced() was called without the placed log. " +
            "The log will stay grabbable. Call WoodPlaced(gameObject) instead."
        );

        WoodPlaced(null);
    }

    /// <summary>
    /// Disables every interactable on the placed wood (including children),
    /// which releases any hand holding it and removes the grab / distance-grab
    /// indicators. Snap interactables are left alone.
    /// </summary>
    private void LockWood(GameObject wood)
    {
        if (wood == null)
            return;

        List<MonoBehaviour> toDisable = new List<MonoBehaviour>();

        foreach (IInteractable interactable in wood.GetComponentsInChildren<IInteractable>(true))
        {
            if (interactable is SnapInteractable)
                continue;

            if (interactable is MonoBehaviour behaviour)
                toDisable.Add(behaviour);
        }

        foreach (MonoBehaviour behaviour in toDisable)
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }
    }
}