using UnityEngine;

public class PanCooking : MonoBehaviour
{
    public GameObject closedCan;
    public GameObject openCan;

    public float cookingTime = 5f;

    private bool canPlaced = false;
    private bool cooking = false;
    private float cookingTimer = 0f;

    public void CanPlaced()
    {
        canPlaced = true;

        Debug.Log("Can placed in pan!");
    }

    public void StartCooking()
    {
        if (!canPlaced)
            return;

        if (cooking)
            return;

        cooking = true;
        cookingTimer = 0f;

        Debug.Log("Can is cooking!");
    }

    private void Update()
    {
        if (!cooking)
            return;

        cookingTimer += Time.deltaTime;

        if (cookingTimer >= cookingTime)
        {
            FinishCooking();
        }
    }

    private void FinishCooking()
    {
        cooking = false;

        Debug.Log("Can finished cooking!");

        closedCan.SetActive(false);
        openCan.SetActive(true);
    }
}