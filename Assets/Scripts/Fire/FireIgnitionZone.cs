
using UnityEngine;

public class FireIgnitionZone : MonoBehaviour
{
    public GameObject fireParticles;
    public BonfireWoodManager woodManager;

    [Header("Cooking")]
    public GameObject cookingZone;

    [Header("Putting Out")]
    [Min(1)] public int dropsToPutOut = 45;
    public ParticleSystem steam;

    public float ignitionTime = 1f;

    private float timer = 0f;
    private bool lighterInside = false;
    private bool fireLit = false;
    private int dropsReceived = 0;
    public bool IsLit => fireLit;

    private void Start()
    {
        if (cookingZone != null)
        {
            cookingZone.SetActive(false);
        }

        if (fireParticles != null)
        {
            fireParticles.SetActive(false);
        }
    }

    private void Update()
    {
        if (fireLit)
            return;

        if (!lighterInside)
        {
            timer = 0f;
            return;
        }

        if (woodManager == null || !woodManager.IsComplete)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= ignitionTime)
        {
            IgniteFire();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Lighter"))
        {
            lighterInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Lighter"))
        {
            lighterInside = false;
            timer = 0f;
        }
    }

    private void IgniteFire()
    {
        if (fireLit)
            return;

        fireLit = true;

        if (fireParticles != null)
        {
            fireParticles.SetActive(true);
        }

        if (cookingZone != null)
        {
            cookingZone.SetActive(true);
        }

        if (SoundManager.Instance != null)
        {
            Vector3 soundPosition = fireParticles != null
                ? fireParticles.transform.position
                : transform.position;

            SoundManager.Instance.StartFireLoop(soundPosition);
        }

        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.CompleteFireObjective();
        }

        Debug.Log("Bonfire ignited!");
    }

    public void AddWater(int drops)
    {
        if (!fireLit)
            return;

        if (steam != null)
            steam.Emit(drops*2);

        dropsReceived += drops;

        if (dropsReceived >= dropsToPutOut)
            PutOutFire();
    }

    private void PutOutFire()
    {
        fireLit = false;
        timer = 0f;
        dropsReceived = 0;

        if (fireParticles != null)
            fireParticles.SetActive(false);

        PanCooking pan = FindAnyObjectByType<PanCooking>();

        if (pan != null)
            pan.StopCooking();

        if (cookingZone != null)
            cookingZone.SetActive(false);

        if (SoundManager.Instance != null)
            SoundManager.Instance.StopFireLoop();

        if (steam != null)
            steam.Emit(40);

        Debug.Log("Bonfire put out!");
    }
}
