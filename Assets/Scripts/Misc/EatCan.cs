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

    [Header("Eating Sound")]
    public AudioClip eatingSoundClip;
    [Range(0f, 1f)] public float eatingSoundVolume = 0.8f;
    [Min(0f)] public float eatingSoundFade = 0.25f;
    [Min(0f)] public float eatingSoundEndFade = 1f;

    private float eatingTimer = 0f;

    private AudioSource eatingAudio;
    private float eatingAudioFade = 0f;

    private void Start()
    {
        if (eatingSoundClip == null)
            return;

        // Kept on its own object so it can fade out after the can is destroyed.
        var soundObject = new GameObject("Eating Sound");

        eatingAudio = soundObject.AddComponent<AudioSource>();
        eatingAudio.clip = eatingSoundClip;
        eatingAudio.loop = true;
        eatingAudio.playOnAwake = false;
        eatingAudio.spatialBlend = 0f; // Heard "inside" the player's head.
        eatingAudio.volume = 0f;
    }

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

        bool eating = aboveHead && tilted;

        UpdateEatingSound(eating);

        if (eating)
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

    // Chewing plays while the can is held to the mouth, and fades out when moved away.
    private void UpdateEatingSound(bool eating)
    {
        if (eatingAudio == null)
            return;

        if (eating && !eatingAudio.isPlaying)
        {
            eatingAudio.Play();
        }

        if (!eatingAudio.isPlaying)
            return;

        float target = eating ? 1f : 0f;

        eatingAudioFade = eatingSoundFade > 0f
            ? Mathf.MoveTowards(eatingAudioFade, target, Time.deltaTime / eatingSoundFade)
            : target;

        float sfxSetting = SoundManager.Instance != null
            ? SoundManager.Instance.SfxVolume
            : 1f;

        eatingAudio.volume = eatingSoundVolume * sfxSetting * eatingAudioFade;

        // Stopping rewinds the clip, so the next attempt starts from the beginning.
        if (!eating && eatingAudioFade <= 0f)
        {
            eatingAudio.Stop();
        }
    }

    private void Eat()
    {
        Debug.Log("Player ate the food!");

        // Let the chewing trail off instead of cutting when the can disappears.
        if (eatingAudio != null)
        {
            SoundManager.FadeOutAndDestroy(eatingAudio, eatingSoundEndFade);
            eatingAudio = null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Destroyed some other way (e.g. scene unload): clean up the sound object.
        if (eatingAudio != null)
        {
            Destroy(eatingAudio.gameObject);
        }
    }
}
