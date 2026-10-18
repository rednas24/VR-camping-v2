using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Ambience")]
    [SerializeField] private AudioClip ambientClip;

    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.5f;

    [SerializeField, Min(0f)] private float ambientFadeIn = 3f;

    [Header("Bonfire")]
    [SerializeField] private AudioClip[] firewoodPlacedClips;

    [SerializeField, Range(0f, 1f)] private float firewoodVolume = 0.9f;

    [SerializeField] private AudioClip fireLoopClip;

    [SerializeField, Range(0f, 1f)] private float fireLoopVolume = 0.8f;

    [SerializeField, Min(0f)] private float fireFadeIn = 2.5f;

    [Header("Chopping")]
    [SerializeField] private AudioClip[] chopClips;

    [SerializeField, Range(0f, 1f)] private float chopVolume = 1f;

    [Header("3D Sound")]
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.08f;

    [SerializeField, Min(0.1f)] private float minDistance = 1f;

    [SerializeField, Min(1f)] private float maxDistance = 20f;

    [SerializeField, Range(1, 16)] private int voiceCount = 6;

    // Set by the settings sign, applied on top of the volumes above.
    [Header("Player Settings")]
    [SerializeField, Range(0f, 1f)] private float ambienceSetting = 1f;

    [SerializeField, Range(0f, 1f)] private float sfxSetting = 1f;

    public float AmbienceVolume
    {
        get => ambienceSetting;
        set => ambienceSetting = Mathf.Clamp01(value);
    }

    public float SfxVolume
    {
        get => sfxSetting;
        set => sfxSetting = Mathf.Clamp01(value);
    }

    private AudioSource ambientSource;
    private float ambientFade;
    private AudioSource fireSource;
    private float fireFade;
    private AudioSource[] voices;
    private int nextVoice;

    private int lastFirewoodClip = -1;
    private int lastChopClip = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one SoundManager should exist.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;
        ambientSource.priority = 0;
        ambientSource.volume = 0f;

        // Uses the project's spatializer (Meta XR Audio) when one is set up.
        bool spatialize = !string.IsNullOrEmpty(AudioSettings.GetSpatializerPluginName());

        voices = new AudioSource[voiceCount];

        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = Create3DSource($"Sound Voice {i}", spatialize);
        }

        fireSource = Create3DSource("Fire Loop", spatialize);
        fireSource.loop = true;
        fireSource.volume = 0f;
    }

    private AudioSource Create3DSource(string sourceName, bool spatialize)
    {
        var sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.spatialize = spatialize;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;

        return source;
    }

    private void Start()
    {
        if (ambientClip != null)
        {
            ambientSource.clip = ambientClip;
            ambientSource.Play();
        }
    }

    private void Update()
    {
        if (ambientSource.isPlaying)
        {
            ambientFade = FadeIn(ambientFade, ambientFadeIn);
            ambientSource.volume = ambientVolume * ambienceSetting * ambientFade;
        }

        // The fire is part of the ambience, so it follows the ambience setting.
        if (fireSource.isPlaying)
        {
            fireFade = FadeIn(fireFade, fireFadeIn);
            fireSource.volume = fireLoopVolume * ambienceSetting * fireFade;
        }
    }

    // Fades in once; after that, setting changes are heard straight away.
    private static float FadeIn(float fade, float duration)
    {
        return duration > 0f ? Mathf.MoveTowards(fade, 1f, Time.deltaTime / duration) : 1f;
    }

    public void PlayFirewoodPlaced(Vector3 position)
    {
        PlayAt(PickClip(firewoodPlacedClips, ref lastFirewoodClip), position, firewoodVolume);
    }

    public void StartFireLoop(Vector3 position)
    {
        if (fireLoopClip == null || fireSource.isPlaying)
            return;

        fireSource.transform.position = position;
        fireSource.clip = fireLoopClip;
        fireSource.Play();
    }

    public void PlayChop(Vector3 position, float loudness = 1f)
    {
        PlayAt(PickClip(chopClips, ref lastChopClip), position, chopVolume * Mathf.Clamp01(loudness));
    }

    private void PlayAt(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null)
            return;

        AudioSource voice = GetFreeVoice();

        voice.transform.position = position;
        voice.clip = clip;
        voice.volume = volume * sfxSetting;
        voice.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        voice.Play();
    }

    // A voice that isn't playing, or else the one that started longest ago.
    private AudioSource GetFreeVoice()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            int index = (nextVoice + i) % voices.Length;

            if (!voices[index].isPlaying)
            {
                nextVoice = (index + 1) % voices.Length;
                return voices[index];
            }
        }

        AudioSource oldest = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        return oldest;
    }

    // Random clip, never the same one twice in a row.
    private static AudioClip PickClip(AudioClip[] clips, ref int lastIndex)
    {
        if (clips == null || clips.Length == 0)
            return null;

        int index = Random.Range(0, clips.Length);

        if (clips.Length > 1 && index == lastIndex)
            index = (index + Random.Range(1, clips.Length)) % clips.Length;

        lastIndex = index;
        return clips[index];
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
