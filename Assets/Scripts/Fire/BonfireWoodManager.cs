using UnityEngine;

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