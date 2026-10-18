using UnityEngine;
using UnityEngine.UI;

public class SettingsControls : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField]
    private Slider ambienceSlider;

    [SerializeField]
    private Slider sfxSlider;

    [Header("Reset Items")]
    [SerializeField]
    private Button resetButton;

    [SerializeField]
    private Transform[] items;

    private struct StartPose
    {
        public Transform parent;
        public Vector3 position;
        public Quaternion rotation;
    }

    private StartPose[] startPoses;

    private void Start()
    {
        SoundManager soundManager = SoundManager.Instance;

        if (soundManager != null)
        {
            if (ambienceSlider != null)
            {
                ambienceSlider.SetValueWithoutNotify(soundManager.AmbienceVolume);
                ambienceSlider.onValueChanged.AddListener(OnAmbienceChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(soundManager.SfxVolume);
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }
        }
        else
        {
            Debug.LogError(
                "SettingsControls needs a SoundManager in the scene.",
                this
            );
        }

        SaveStartPoses();

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetItems);
        }
    }

    private void OnAmbienceChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.AmbienceVolume = value;
    }

    private void OnSfxChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SfxVolume = value;
    }

    private void SaveStartPoses()
    {
        startPoses = new StartPose[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            startPoses[i] = new StartPose
            {
                parent = items[i].parent,
                position = items[i].position,
                rotation = items[i].rotation
            };
        }
    }

    public void ResetItems()
    {
        for (int i = 0; i < items.Length; i++)
        {
            Transform item = items[i];
            
            if (item == null)
                continue;

            StartPose start = startPoses[i];

            if (item.parent != start.parent)
                item.SetParent(start.parent, true);

            item.SetPositionAndRotation(start.position, start.rotation);

            if (item.TryGetComponent(out Rigidbody rb))
            {
                rb.position = start.position;
                rb.rotation = start.rotation;

                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (ambienceSlider != null)
            ambienceSlider.onValueChanged.RemoveListener(OnAmbienceChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetItems);
    }
}
