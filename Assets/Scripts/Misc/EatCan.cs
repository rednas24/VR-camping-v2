using UnityEngine;

public class EatCan : MonoBehaviour
{
    [Header("Player Head")]
    public Transform playerHead;

    [Header("Eating")]
    public float eatingTime = 3f;
    public float minimumHeightAboveHead = 0.15f;

    [Header("Tilt")]
    public float requiredTiltAngle = 60f;

    private float eatingTimer = 0f;

    private void Update()
    {
        if (playerHead == null)
            return;

        // Check if the can is above the player's head
        bool aboveHead =
            transform.position.y >
            playerHead.position.y + minimumHeightAboveHead;

        // Check if the can is tilted enough
        float tiltAngle = Vector3.Angle(transform.up, Vector3.down);

        bool tilted = tiltAngle < (180f - requiredTiltAngle);

        if (aboveHead && tilted)
        {
            eatingTimer += Time.deltaTime;

            if (eatingTimer >= eatingTime)
            {
                Eat();
            }
        }
        else
        {
            // Reset if the player moves the can away
            eatingTimer = 0f;
        }
    }

    private void Eat()
    {
        Debug.Log("Player ate the food!");

        Destroy(gameObject);
    }
}