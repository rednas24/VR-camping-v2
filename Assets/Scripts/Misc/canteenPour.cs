using UnityEngine;

public class CanteenPour : MonoBehaviour
{
    [Header("Water")]
    [SerializeField] private ParticleSystem water;
    [SerializeField, Range(0f, 180f)] private float pourAngle = 80f;

    [Header("Pouring Sound (optional)")]
    [SerializeField] private AudioClip pourSoundClip;
    [SerializeField, Range(0f, 1f)] private float pourSoundVolume = 0.6f;
    [SerializeField, Min(0f)] private float pourSoundFade = 0.2f;

    private AudioSource pourAudio;
    private float pourAudioFade;

    private void Awake()
    {
        if (water == null || pourSoundClip == null)
            return;

        pourAudio = water.gameObject.AddComponent<AudioSource>();
        pourAudio.clip = pourSoundClip;
        pourAudio.loop = true;
        pourAudio.playOnAwake = false;
        pourAudio.spatialBlend = 1f;
        pourAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        pourAudio.minDistance = 1f;
        pourAudio.maxDistance = 15f;
        pourAudio.dopplerLevel = 0f;
        pourAudio.volume = 0f;
    }

    private void Update()
    {
        if (water == null)
            return;

        float limit = water.isEmitting ? pourAngle - 5f : pourAngle;
        bool pouring = Vector3.Angle(water.transform.forward, Vector3.up) >= limit;

        if (pouring && !water.isEmitting)
            water.Play();
        else if (!pouring && water.isEmitting)
            water.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        UpdatePourSound(pouring);
    }

    private void UpdatePourSound(bool pouring)
    {
        if (pourAudio == null)
            return;

        if (pouring && !pourAudio.isPlaying)
            pourAudio.Play();

        if (!pourAudio.isPlaying)
            return;

        float target = pouring ? 1f : 0f;

        pourAudioFade = pourSoundFade > 0f
            ? Mathf.MoveTowards(pourAudioFade, target, Time.deltaTime / pourSoundFade)
            : target;

        float sfxSetting = SoundManager.Instance != null
            ? SoundManager.Instance.SfxVolume
            : 1f;

        pourAudio.volume = pourSoundVolume * sfxSetting * pourAudioFade;

        if (!pouring && pourAudioFade <= 0f)
            pourAudio.Stop();
    }
}