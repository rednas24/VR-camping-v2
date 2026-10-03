using UnityEngine;

public class WoodLog : MonoBehaviour
{
    [Header("Wood Health")]
    public float maxHealth = 100f;

    private float currentHealth;
    private bool isChopped = false;

    [Header("Placement")]
    public bool isPlaced = false;

    [Header("Split")]
    public GameObject halfLogsPrefab;

    [Tooltip("Scene object used as the spawn position for split logs.")]
    public Transform splitSpawnPoint;

    public float splitForce = 2f;
    public float splitTorque = 1f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        // Automatically find SplitSpawn in the scene
        // if it has not been assigned manually.
        if (splitSpawnPoint == null)
        {
            GameObject spawnObject =
                GameObject.Find("SplitSpawn");

            if (spawnObject != null)
            {
                splitSpawnPoint = spawnObject.transform;

                Debug.Log(
                    "WoodLog found SplitSpawn automatically."
                );
            }
            else
            {
                Debug.LogError(
                    "WoodLog: Could not find SplitSpawn in the scene!"
                );
            }
        }
    }

    public void PlaceLog()
    {
        if (isPlaced)
            return;

        isPlaced = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = false;
            rb.useGravity = false;

            rb.constraints =
                RigidbodyConstraints.FreezePosition |
                RigidbodyConstraints.FreezeRotation;
        }

        Debug.Log("LOG PLACED AND PHYSICALLY LOCKED!");
    }

    public void TakeDamage(float damage)
    {
        if (isChopped || !isPlaced)
            return;

        currentHealth -= damage;

        Debug.Log(
            $"Wood damage: {damage:F1} | " +
            $"Remaining health: {currentHealth:F1}"
        );

        if (currentHealth <= 0)
        {
            ChopLog();
        }
    }

    private void ChopLog()
    {
        if (isChopped)
            return;

        isChopped = true;

        Debug.Log("WOOD CHOPPED!");

        SpawnSplitLogs();

        // Remove the original log
        Destroy(gameObject);
    }

private void SpawnSplitLogs()
{
    if (halfLogsPrefab == null)
    {
        Debug.LogError("WoodLog: Half Logs Prefab is not assigned!");
        return;
    }

    if (splitSpawnPoint == null)
    {
        Debug.LogError("WoodLog: Split Spawn Point is not assigned!");
        return;
    }

    Debug.Log(
        "SPLIT SPAWN POSITION: " +
        splitSpawnPoint.position
    );

    // Spawn the prefab DIRECTLY at the spawn point.
    GameObject splitObject = Instantiate(
        halfLogsPrefab,
        splitSpawnPoint.position,
        splitSpawnPoint.rotation
    );

    Debug.Log(
        "SPLIT PARENT ACTUAL POSITION: " +
        splitObject.transform.position
    );

    // Make sure the parent is independent.
    splitObject.transform.SetParent(null);

    if (splitObject.transform.childCount < 2)
    {
        Debug.LogError(
            "Half Logs prefab needs at least TWO child objects!"
        );

        Destroy(splitObject);
        return;
    }

    // Get the two halves.
    Transform firstHalf = splitObject.transform.GetChild(0);
    Transform secondHalf = splitObject.transform.GetChild(1);

    Debug.Log(
        "FIRST HALF POSITION: " +
        firstHalf.position
    );

    Debug.Log(
        "SECOND HALF POSITION: " +
        secondHalf.position
    );

    // Detach them while keeping their WORLD positions.
    firstHalf.SetParent(null, true);
    secondHalf.SetParent(null, true);

    Rigidbody firstRb =
        firstHalf.GetComponent<Rigidbody>();

    Rigidbody secondRb =
        secondHalf.GetComponent<Rigidbody>();

    Vector3 splitDirection =
        splitSpawnPoint.right;

    // -------------------------
    // FIRST HALF
    // -------------------------

    if (firstRb != null)
    {
        firstRb.isKinematic = false;
        firstRb.useGravity = true;

        firstRb.linearVelocity = Vector3.zero;
        firstRb.angularVelocity = Vector3.zero;

        firstRb.AddForce(
            splitDirection * splitForce,
            ForceMode.Impulse
        );

        firstRb.AddTorque(
            splitSpawnPoint.forward *
            Random.Range(-splitTorque, splitTorque),
            ForceMode.Impulse
        );
    }

    // -------------------------
    // SECOND HALF
    // -------------------------

    if (secondRb != null)
    {
        secondRb.isKinematic = false;
        secondRb.useGravity = true;

        secondRb.linearVelocity = Vector3.zero;
        secondRb.angularVelocity = Vector3.zero;

        secondRb.AddForce(
            -splitDirection * splitForce,
            ForceMode.Impulse
        );

        secondRb.AddTorque(
            splitSpawnPoint.forward *
            Random.Range(-splitTorque, splitTorque),
            ForceMode.Impulse
        );
    }

    // Destroy only the temporary parent.
    Destroy(splitObject);
}
}