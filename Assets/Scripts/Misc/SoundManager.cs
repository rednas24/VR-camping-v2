using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Ambience")]
    [Tooltip("Looping background sound, heard the same everywhere.")]
    [SerializeField] private AudioClip ambientClip;

    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.5f;

    [Tooltip("Seconds the ambience takes to fade in at the start.")]
    [SerializeField, Min(0f)] private float ambientFadeIn = 3f;

    [Header("Bonfire")]
    [Tooltip("One is picked at random each time firewood is placed on the bonfire.")]
    [SerializeField] private AudioClip[] firewoodPlacedClips;

    [SerializeField, Range(0f, 1f)] private float firewoodVolume = 0.9f;

    [Header("Chopping")]
    [Tooltip("One is picked at random each time the axe hits a log.")]
    [SerializeField] private AudioClip[] chopClips;

    [SerializeField, Range(0f, 1f)] private float chopVolume = 1f;

    [Header("3D Sound")]
    [Tooltip("Random pitch change per play, so repeated sounds don't sound identical.")]
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.08f;

    [Tooltip("Full volume within this distance (metres).")]
    [SerializeField, Min(0.1f)] private float minDistance = 1f;

    [Tooltip("Distance (metres) where the sound stops getting quieter.")]
    [SerializeField, Min(1f)] private float maxDistance = 20f;

    [Tooltip("How many effects can play at the same time.")]
    [SerializeField, Range(1, 16)] private int voiceCount = 6;

    private AudioSource ambientSource;
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
            var voiceObject = new GameObject($"Sound Voice {i}");
            voiceObject.transform.SetParent(transform, false);

            AudioSource voice = voiceObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.spatialize = spatialize;
            voice.rolloffMode = AudioRolloffMode.Logarithmic;
            voice.minDistance = minDistance;
            voice.maxDistance = maxDistance;
            voice.dopplerLevel = 0f;

            voices[i] = voice;
        }
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
        if (!ambientSource.isPlaying)
            return;

        float fadeSpeed = ambientFadeIn > 0f ? ambientVolume / ambientFadeIn : float.MaxValue;

        ambientSource.volume = Mathf.MoveTowards(
            ambientSource.volume,
            ambientVolume,
            fadeSpeed * Time.deltaTime
        );
    }

    public void PlayFirewoodPlaced(Vector3 position)
    {
        PlayAt(PickClip(firewoodPlacedClips, ref lastFirewoodClip), position, firewoodVolume);
    }

    // loudness: 0-1, e.g. how hard the swing was.
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
        voice.volume = volume;
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
