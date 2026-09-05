using UnityEngine;

public class Test : MonoBehaviour
{
    public GameObject canvas;

    public void CloseCanvas()
    {
        canvas.SetActive(false);
    }
}