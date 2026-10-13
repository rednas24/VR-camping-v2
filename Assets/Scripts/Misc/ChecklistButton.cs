
using UnityEngine;
using UnityEngine.UI;

public class ChecklistButton : MonoBehaviour
{
    [SerializeField]
    private GameObject buttonCheckmark;

    private Button button;
    private bool isCompleted;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (buttonCheckmark != null)
        {
            buttonCheckmark.SetActive(false);
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError(
                "ChecklistButton needs a Button component.",
                this
            );
        }
    }

    private void OnButtonClicked()
    {
        if (buttonCheckmark == null || isCompleted)
            return;

        isCompleted = true;
        buttonCheckmark.SetActive(true);

        Debug.Log("Checklist task completed!");
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
