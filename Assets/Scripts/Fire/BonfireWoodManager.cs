using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class BonfireWoodManager : MonoBehaviour
{
    // Empty placeholder objects marking where each piece of wood goes.
    public GameObject[] woodSlots;

    private int currentSlot = 0;

    // Wood that has already been counted, so the same piece never counts twice.
    private readonly HashSet<GameObject> countedWood = new HashSet<GameObject>();

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

        if (placedWood != null && !countedWood.Add(placedWood))
            return;

        // Lock the log that was actually placed so it can't be grabbed again
        LockWood(placedWood);

        if (SoundManager.Instance != null)
        {
            Vector3 soundPosition = placedWood != null
                ? placedWood.transform.position
                : woodSlots[currentSlot].transform.position;

            SoundManager.Instance.PlayFirewoodPlaced(soundPosition);
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

    /// <summary>
    /// Called by each slot's InteractableUnityEventWrapper (When Select). That
    /// event can't pass the wood along, so the newly snapped piece is looked up
    /// from what the slots are holding.
    /// </summary>
    public void WoodPlaced()
    {
        foreach (GameObject slot in woodSlots)
        {
            if (slot == null || !slot.TryGetComponent(out SnapInteractable snapSlot))
                continue;

            foreach (SnapInteractor interactor in snapSlot.SelectingInteractors)
            {
                Rigidbody body = interactor.Rigidbody != null
                    ? interactor.Rigidbody
                    : interactor.GetComponentInParent<Rigidbody>();

                GameObject wood = body != null ? body.gameObject : interactor.gameObject;

                if (!countedWood.Contains(wood))
                {
                    WoodPlaced(wood);
                    return;
                }
            }
        }

        Debug.LogWarning("BonfireWoodManager.WoodPlaced() found no newly placed wood in the slots.");
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