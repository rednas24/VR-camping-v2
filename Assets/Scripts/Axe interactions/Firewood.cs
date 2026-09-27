using UnityEngine;

public class Firewood : MonoBehaviour
{
    [Header("Splitting")]
    [SerializeField] private GameObject splitPiecePrefab;
    [SerializeField] private float requiredForce = 10f;

    private float accumulatedForce = 0f;
    private bool hasSplit = false;

    public void AddHitForce(float hitForce)
    {
        if (hasSplit)
            return;

        accumulatedForce += hitForce;

        Debug.Log(
            "Wood force: " +
            accumulatedForce +
            " / " +
            requiredForce
        );

        if (accumulatedForce >= requiredForce)
        {
            Split();
        }
    }

    private void Split()
    {
        if (hasSplit)
            return;

        hasSplit = true;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        Instantiate(
            splitPiecePrefab,
            position + transform.right * 0.25f,
            rotation
        );

        Instantiate(
            splitPiecePrefab,
            position - transform.right * 0.25f,
            rotation
        );

        Destroy(gameObject);
    }
}