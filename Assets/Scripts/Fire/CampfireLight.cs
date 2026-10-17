using UnityEngine;

// Makes the campfire light flicker like a real fire and fade in when lit.
// Put it on the object that gets enabled when the fire is lit.
public class CampfireLight : MonoBehaviour
{
    [Tooltip("Optional. If left empty, the first Light in the children is used.")]
    [SerializeField] private Light fireLight;

    [Header("Brightness")]
    [SerializeField, Min(0f)] private float baseIntensity = 3f;

    [SerializeField, Min(0.1f)] private float range = 8f;

    [Tooltip("How far the intensity moves up and down from the base (fraction of the base).")]
    [SerializeField, Range(0f, 1f)] private float flickerAmount = 0.35f;

    [SerializeField, Min(0f)] private float flickerSpeed = 6f;

    [Header("Colour")]
    [Tooltip("Colour at the dim end of a flicker.")]
    [SerializeField] private Color dimColor = new Color(1f, 0.38f, 0.1f);

    [Tooltip("Colour at the bright end of a flicker.")]
    [SerializeField] private Color brightColor = new Color(1f, 0.62f, 0.28f);

    [Header("Movement")]
    [Tooltip("Lifts the light above the logs so it lights the surroundings, " +
             "not just the ground under the fire.")]
    [SerializeField] private float heightOffset = 0.35f;

    [Tooltip("How far (metres) the light wanders, which makes the lighting dance.")]
    [SerializeField, Min(0f)] private float wobbleDistance = 0.04f;

    [Header("Ignition")]
    [SerializeField, Min(0.01f)] private float fadeInDuration = 2.5f;

    [Tooltip("Brightness over the fade in (0 = lit, 1 = fully burning). " +
             "The small overshoot feels like the fire catching.")]
    [SerializeField] private AnimationCurve fadeInCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.3f, 1.15f),
        new Keyframe(1f, 1f)
    );

    private Vector3 baseLocalPosition;
    private float seed;
    private float litTime;

    private void Awake()
    {
        if (fireLight == null)
            fireLight = GetComponentInChildren<Light>(true);

        if (fireLight == null)
        {
            Debug.LogWarning("CampfireLight found no Light to control.", this);
            enabled = false;
            return;
        }

        // The colour set here should be the final colour.
        fireLight.useColorTemperature = false;
        fireLight.range = range;

        baseLocalPosition = fireLight.transform.localPosition;
        seed = Random.Range(0f, 100f);
    }

    private void OnEnable()
    {
        litTime = Time.time;

        if (fireLight != null)
            fireLight.intensity = 0f;
    }

    private void Update()
    {
        float t = Time.time * flickerSpeed;

        // A slow swell plus fast crackle reads as fire rather than noise.
        float slow = Mathf.PerlinNoise(seed, t * 0.3f);
        float fast = Mathf.PerlinNoise(seed + 10f, t);
        float noise = Mathf.Clamp01(slow * 0.6f + fast * 0.4f);

        float flicker = Mathf.Lerp(1f - flickerAmount, 1f + flickerAmount, noise);
        float fade = fadeInCurve.Evaluate(Mathf.Clamp01((Time.time - litTime) / fadeInDuration));

        fireLight.intensity = baseIntensity * flicker * fade;
        fireLight.color = Color.Lerp(dimColor, brightColor, noise);

        Vector3 wobble = new Vector3(
            Mathf.PerlinNoise(seed + 20f, t * 0.5f) - 0.5f,
            (Mathf.PerlinNoise(seed + 30f, t * 0.5f) - 0.5f) * 0.5f,
            Mathf.PerlinNoise(seed + 40f, t * 0.5f) - 0.5f
        ) * (2f * wobbleDistance);

        fireLight.transform.localPosition =
            baseLocalPosition + Vector3.up * heightOffset + wobble;
    }
}
