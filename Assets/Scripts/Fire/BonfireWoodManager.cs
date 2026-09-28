using UnityEngine;

public class BonfireWoodManager : MonoBehaviour
{
    public GameObject[] woodSlots;

    private int currentSlot = 0;

    private void Start()
    {
        for (int i = 0; i < woodSlots.Length; i++)
        {
            woodSlots[i].SetActive(i == 0);
        }
    }

    public void WoodPlaced()
    {
        // Move to the next slot
        currentSlot++;

        if (currentSlot < woodSlots.Length)
        {
            woodSlots[currentSlot].SetActive(true);
        }
    }
}