using UnityEngine;

public class AxeGrip : MonoBehaviour
{
    public int handCount { get; private set; }

    public void AddHand()
    {
        handCount++;

        Debug.Log($"Hands holding axe: {handCount}");
    }

    public void RemoveHand()
    {
        handCount = Mathf.Max(0, handCount - 1);

        Debug.Log($"Hands holding axe: {handCount}");
    }

    public bool IsTwoHanded()
    {
        return handCount >= 2;
    }
}