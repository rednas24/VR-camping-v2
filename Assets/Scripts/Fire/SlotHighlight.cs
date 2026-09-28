using UnityEngine;

public class SlotHighlight : MonoBehaviour
{
    public GameObject highlight;

    public void ShowHighlight()
    {
        highlight.SetActive(true);
    }

    public void HideHighlight()
    {
        highlight.SetActive(false);
    }
}