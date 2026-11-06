using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SleepInTent : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform head;

    [Header("Sleep Zone")]
    [SerializeField] private Transform sleepPoint;
    [SerializeField] private float sleepRadius = 1.2f;
    [SerializeField] private float promptDelay = 0.5f;

    [Header("Prompt")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject promptButtons;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Fire")]
    [SerializeField] private FireIgnitionZone fire;

    [Header("Messages")]
    [SerializeField, TextArea] private string notNightMessage = "You're not tired yet";
    [SerializeField, TextArea] private string sleepQuestion = "Go to sleep?";
    [SerializeField, TextArea] private string fireStillLitMessage = "You can't sleep yet.\nPut out the fire with the canteen.";

    [Header("Ending")]
    [SerializeField] private OVRScreenFade screenFade;
    [SerializeField] private float fadeDuration = 3f;
    [SerializeField] private string endSceneName = "Main Menu";

    private float timeInZone;
    private bool dismissed;
    private bool isSleeping;

    private bool IsNight =>
        DayCycleManager.Instance != null && DayCycleManager.Instance.IsNight;
    
    private bool IsFireLit => fire != null && fire.IsLit;

    private void Start()
    {
        if (fire == null)
            Debug.LogWarning("SleepInTent: Fire is not assigned, so the player can sleep with the fire lit.", this);

        HidePrompt();

        if (yesButton != null) yesButton.onClick.AddListener(OnYes);
        if (noButton != null) noButton.onClick.AddListener(OnNo);
    }

    private void Update()
    {
        if (isSleeping || head == null || sleepPoint == null)
            return;

        Vector3 offset = head.position - sleepPoint.position;
        offset.y = 0f;
        bool inZone = offset.sqrMagnitude <= sleepRadius * sleepRadius;

        if (!inZone)
        {
            timeInZone = 0f;
            dismissed = false;
            HidePrompt();
            return;
        }

        timeInZone += Time.deltaTime;

        if (dismissed || timeInZone < promptDelay)
            return;

        if (!IsNight)
            ShowPrompt(notNightMessage, false);
        else if (IsFireLit)
            ShowPrompt(fireStillLitMessage, false);
        else
            ShowPrompt(sleepQuestion, true);
    }

    private void ShowPrompt(string message, bool canSleep)
    {
        if (promptPanel != null && !promptPanel.activeSelf)
            promptPanel.SetActive(true);

        if (promptText != null && promptText.text != message)
            promptText.text = message;

        if (promptButtons != null && promptButtons.activeSelf != canSleep)
            promptButtons.SetActive(canSleep);
    }

    private void HidePrompt()
    {
        if (promptPanel != null && promptPanel.activeSelf)
            promptPanel.SetActive(false);
    }

    private void OnYes()
    {
        if (isSleeping || !IsNight || IsFireLit)
            return;

        HidePrompt();
        StartCoroutine(Sleep());
    }

    private void OnNo()
    {
        dismissed = true;
        HidePrompt();
    }

    private IEnumerator Sleep()
    {
        isSleeping = true;

        if (screenFade != null)
        {
            screenFade.fadeTime = fadeDuration;
            screenFade.FadeOut();
        }

        yield return new WaitForSeconds(fadeDuration);

        if (!Application.CanStreamedLevelBeLoaded(endSceneName))
        {
            Debug.LogError($"Scene '{endSceneName}' is not in the Build Profiles scene list.", this);
            yield break;
        }

        SceneManager.LoadSceneAsync(endSceneName);
    }

    private void OnDestroy()
    {
        if (yesButton != null) yesButton.onClick.RemoveListener(OnYes);
        if (noButton != null) noButton.onClick.RemoveListener(OnNo);
    }

    private void OnDrawGizmosSelected()
    {
        if (sleepPoint == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(sleepPoint.position, sleepRadius);
    }
}