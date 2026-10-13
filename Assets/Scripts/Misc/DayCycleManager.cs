
using UnityEngine;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    [Header("Lighting")]
    [SerializeField] private Light sun;
    [SerializeField, Min(0.01f)] private float transitionSpeed = 2f;

    [Header("Wood Objective")]
    [SerializeField, Min(1)] private int requiredWoodPieces = 3;

    private int choppedWoodPieces;

    private bool woodCompleted;
    private bool fireCompleted;
    private bool foodCompleted;

    private float currentProgress;
    private float targetProgress;

    private float initialSunIntensity;
    private Quaternion initialSunRotation;

    public int CompletedObjectives
    {
        get
        {
            int count = 0;

            if (woodCompleted) count++;
            if (fireCompleted) count++;
            if (foodCompleted) count++;

            return count;
        }
    }

    public string CurrentTimeOfDay
    {
        get
        {
            switch (CompletedObjectives)
            {
                case 0: return "Morning";
                case 1: return "Midday";
                case 2: return "Evening";
                default: return "Night";
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one DayCycleManager should exist.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (sun != null)
        {
            initialSunIntensity = sun.intensity;
            initialSunRotation = sun.transform.rotation;
        }
    }

    private void Start()
    {
        UpdateTargetProgress();
        currentProgress = targetProgress;
        ApplyLighting(currentProgress);
    }

    private void Update()
    {
        currentProgress = Mathf.MoveTowards(
            currentProgress,
            targetProgress,
            Time.deltaTime / transitionSpeed
        );

        ApplyLighting(currentProgress);
    }

    public void RegisterWoodChoppedPiece()
    {
        if (woodCompleted)
            return;

        choppedWoodPieces++;

        Debug.Log(
            $"Wood chopped: {choppedWoodPieces}/{requiredWoodPieces}"
        );

        if (choppedWoodPieces >= requiredWoodPieces)
        {
            woodCompleted = true;
            ObjectiveCompleted("Chop enough wood");
        }
    }

    public void CompleteFireObjective()
    {
        if (fireCompleted)
            return;

        fireCompleted = true;
        ObjectiveCompleted("Light the campfire");
    }

    public void CompleteFoodObjective()
    {
        if (foodCompleted)
            return;

        foodCompleted = true;
        ObjectiveCompleted("Cook food");
    }

    private void ObjectiveCompleted(string objectiveName)
    {
        UpdateTargetProgress();

        Debug.Log(
            $"Objective completed: {objectiveName}. " +
            $"Progress: {CompletedObjectives}/3. " +
            $"Time of day: {CurrentTimeOfDay}"
        );
    }

    private void UpdateTargetProgress()
    {
        targetProgress = CompletedObjectives / 3f;
    }

    private void ApplyLighting(float progress)
    {
        if (sun == null)
            return;

        // Rotate the directional light gradually through the day.
        float angle = Mathf.Lerp(0f, 180f, progress);

        sun.transform.rotation =
            initialSunRotation * Quaternion.Euler(angle, 0f, 0f);

        // Fade the sun out towards night.
        float intensityMultiplier;

        if (progress < 1f / 3f)
        {
            intensityMultiplier = Mathf.Lerp(
                0.8f, 1f, progress * 3f
            );
        }
        else if (progress < 2f / 3f)
        {
            intensityMultiplier = Mathf.Lerp(
                1f, 0.4f, (progress - 1f / 3f) * 3f
            );
        }
        else
        {
            intensityMultiplier = Mathf.Lerp(
                0.4f, 0f, (progress - 2f / 3f) * 3f
            );
        }

        sun.intensity = initialSunIntensity * intensityMultiplier;

        Color morning = new Color(0.55f, 0.50f, 0.45f);
        Color midday = new Color(0.80f, 0.80f, 0.80f);
        Color evening = new Color(0.45f, 0.28f, 0.22f);
        Color night = new Color(0.025f, 0.035f, 0.09f);

        Color ambient;

        if (progress < 1f / 3f)
        {
            ambient = Color.Lerp(
                morning, midday, progress * 3f
            );
        }
        else if (progress < 2f / 3f)
        {
            ambient = Color.Lerp(
                midday, evening, (progress - 1f / 3f) * 3f
            );
        }
        else
        {
            ambient = Color.Lerp(
                evening, night, (progress - 2f / 3f) * 3f
            );
        }

        RenderSettings.ambientLight = ambient;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
