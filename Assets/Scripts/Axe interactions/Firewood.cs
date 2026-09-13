using UnityEngine;

public class Firewood : MonoBehaviour
{
    public GameObject splitPiecePrefab;

    private bool hasSplit = false;

    public void Split()
    {
        if (hasSplit)
            return;

        hasSplit = true;

        Vector3 position = transform.position;

        Destroy(gameObject);

        Instantiate(
            splitPiecePrefab,
            position + transform.right * 0.25f,
            transform.rotation
        );

        Instantiate(
            splitPiecePrefab,
            position - transform.right * 0.25f,
            transform.rotation
        );
    }
}