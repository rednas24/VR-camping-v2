using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneButton : MonoBehaviour
{
    [SerializeField]
    private string sceneName = "MainScene";

    private Button button;
    private bool isLoading;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError(
                "LoadSceneButton needs a Button component.",
                this
            );
        }
    }

    private void OnButtonClicked()
    {
        if (isLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"Scene '{sceneName}' is not in File > Build Profiles > Scene List.",
                this
            );
            return;
        }

        isLoading = true;
        button.interactable = false;

        // Async so the headset keeps rendering while the camp loads.
        SceneManager.LoadSceneAsync(sceneName);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
