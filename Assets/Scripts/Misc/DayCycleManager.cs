using UnityEngine;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    [Header("Timing")]
    [Tooltip("Seconds it takes to move from one time of day to the next.")]
    [SerializeField, Min(0.1f)] private float transitionDuration = 12f;

    [Tooltip("Shape of each individual change (0 = start of change, 1 = end). " +
             "EaseInOut gives a slow start and a gentle landing.")]
    [SerializeField] private AnimationCurve transitionEase =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Sun")]
    [SerializeField] private Light sun;

    [Tooltip("Sun pitch offset (degrees) applied at the start and end of the full cycle.")]
    [SerializeField] private float startAngle = 0f;
    [SerializeField] private float endAngle = 180f;

    [Tooltip("Sun intensity multiplier over the whole day (0 = morning, 1 = night).")]
    [SerializeField] private AnimationCurve sunIntensityCurve = CreateDefaultIntensityCurve();

    [Header("Ambient Light")]
    [Tooltip("Ambient colour over the whole day (left = morning, right = night).")]
    [SerializeField] private Gradient ambientGradient = CreateDefaultAmbientGradient();

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
        ApplyLighting(GetDisplayProgress(currentProgress));
    }

    private void Update()
    {
        if (Mathf.Approximately(currentProgress, targetProgress))
            return;

        // Each objective is worth 1/3 of the cycle, and every step takes
        // exactly transitionDuration seconds regardless of distance.
        float speed = (1f / 3f) / transitionDuration;

        currentProgress = Mathf.MoveTowards(
            currentProgress,
            targetProgress,
            speed * Time.deltaTime
        );

        ApplyLighting(GetDisplayProgress(currentProgress));
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

    // Applies the ease curve within each third of the day, so every change
    // starts gently and settles softly into the next time of day.
    private float GetDisplayProgress(float linearProgress)
    {
        float scaled = linearProgress * 3f;
        float segment = Mathf.Floor(scaled);
        float t = scaled - segment;

        return (segment + transitionEase.Evaluate(t)) / 3f;
    }

    private void ApplyLighting(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (sun != null)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, progress);

            sun.transform.rotation =
                initialSunRotation * Quaternion.Euler(angle, 0f, 0f);

            sun.intensity =
                initialSunIntensity * sunIntensityCurve.Evaluate(progress);
        }

        RenderSettings.ambientLight = ambientGradient.Evaluate(progress);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ---- Default values (editable in the Inspector afterwards) ----

    private static AnimationCurve CreateDefaultIntensityCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.8f),          // morning
            new Keyframe(1f / 3f, 1f),       // midday
            new Keyframe(2f / 3f, 0.4f),     // evening
            new Keyframe(1f, 0f)             // night
        );

        for (int i = 0; i < curve.length; i++)
            curve.SmoothTangents(i, 0f);

        return curve;
    }

    private static Gradient CreateDefaultAmbientGradient()
    {
        var gradient = new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 0.50f, 0.45f), 0f),        // morning
                new GradientColorKey(new Color(0.80f, 0.80f, 0.80f), 1f / 3f),   // midday
                new GradientColorKey(new Color(0.45f, 0.28f, 0.22f), 2f / 3f),   // evening
                new GradientColorKey(new Color(0.025f, 0.035f, 0.09f), 1f)       // night
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

        return gradient;
    }
}