
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

    [Header("Day Cycle")]
    [Tooltip("Enable on logs that should count towards the wood objective.")]
    public bool countsTowardsWoodObjective = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (splitSpawnPoint == null)
        {
            GameObject spawnObject = GameObject.Find("SplitSpawn");

            if (spawnObject != null)
            {
                splitSpawnPoint = spawnObject.transform;
                Debug.Log("WoodLog found SplitSpawn automatically.");
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

        if (currentHealth <= 0f)
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

        // Count this successful chop only once.
        if (countsTowardsWoodObjective &&
            DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.RegisterWoodChoppedPiece();
        }

        SpawnSplitLogs();

        Destroy(gameObject);
    }

    private void SpawnSplitLogs()
    {
        if (halfLogsPrefab == null)
        {
            Debug.LogError(
                "WoodLog: Half Logs Prefab is not assigned!"
            );
            return;
        }

        if (splitSpawnPoint == null)
        {
            Debug.LogError(
                "WoodLog: Split Spawn Point is not assigned!"
            );
            return;
        }

        GameObject splitObject = Instantiate(
            halfLogsPrefab,
            splitSpawnPoint.position,
            splitSpawnPoint.rotation
        );

        splitObject.transform.SetParent(null);

        if (splitObject.transform.childCount < 2)
        {
            Debug.LogError(
                "Half Logs prefab needs at least TWO child objects!"
            );

            Destroy(splitObject);
            return;
        }

        Transform firstHalf = splitObject.transform.GetChild(0);
        Transform secondHalf = splitObject.transform.GetChild(1);

        // Preserve world positions while detaching the halves.
        firstHalf.SetParent(null, true);
        secondHalf.SetParent(null, true);

        Rigidbody firstRb = firstHalf.GetComponent<Rigidbody>();
        Rigidbody secondRb = secondHalf.GetComponent<Rigidbody>();

        Vector3 splitDirection = splitSpawnPoint.right;

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
