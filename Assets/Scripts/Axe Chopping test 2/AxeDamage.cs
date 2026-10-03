using UnityEngine;

public class AxeDamage : MonoBehaviour
{
    [Header("Damage")]
    public float minimumSwingSpeed = 1.5f;
    public float maximumSwingSpeed = 7f;

    public float minimumDamage = 5f;
    public float maximumDamage = 35f;

    [Header("Two Handed")]
    public float twoHandMultiplier = 1.4f;

    [Header("Hit Cooldown")]
    public float hitCooldown = 0.25f;

    private AxeSwing axeSwing;
    private AxeGrip axeGrip;

    private float lastHitTime = -999f;

    private void Start()
    {
        axeSwing = GetComponentInParent<AxeSwing>();
        axeGrip = GetComponentInParent<AxeGrip>();

        if (axeSwing == null)
            Debug.LogError("AxeDamage could not find AxeSwing!");

        if (axeGrip == null)
            Debug.LogError("AxeDamage could not find AxeGrip!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastHitTime < hitCooldown)
            return;

        WoodLog log = other.GetComponentInParent<WoodLog>();

        if (log == null)
            return;

        // Do not do anything with a log until it has been placed.
        if (!log.isPlaced)
            return;

        Debug.Log("AxeHead hit placed log: " + other.gameObject.name);

        float speed = axeSwing.swingSpeed;

        Debug.Log("Axe head speed: " + speed);

        if (speed < minimumSwingSpeed)
        {
            Debug.Log("Swing too slow to damage the log.");
            return;
        }

        lastHitTime = Time.time;

        float damage = CalculateDamage(speed);

        log.TakeDamage(damage);

        Debug.Log("AXE DEALT DAMAGE: " + damage);
    }

    private float CalculateDamage(float speed)
    {
        speed = Mathf.Clamp(
            speed,
            minimumSwingSpeed,
            maximumSwingSpeed
        );

        float power = Mathf.InverseLerp(
            minimumSwingSpeed,
            maximumSwingSpeed,
            speed
        );

        float damage = Mathf.Lerp(
            minimumDamage,
            maximumDamage,
            power
        );

        if (axeGrip != null && axeGrip.IsTwoHanded())
        {
            damage *= twoHandMultiplier;

            Debug.Log("Two-handed swing!");
        }

        return damage;
    }
}