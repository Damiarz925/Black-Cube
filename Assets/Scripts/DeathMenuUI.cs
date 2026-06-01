using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathMenuUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;

    [Header("Buttons")]
    [SerializeField] private Button restartLevelButton;
    [SerializeField] private Button returnToMainMenuButton;
    [SerializeField] private Button quitGameButton;

    [Header("Scene Routing")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (restartLevelButton != null)
            restartLevelButton.onClick.AddListener(OnRestartLevelClicked);
        else
            Debug.LogWarning("DeathMenuUI: restartLevelButton not assigned.");

        if (returnToMainMenuButton != null)
            returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
        else
            Debug.LogWarning("DeathMenuUI: returnToMainMenuButton not assigned.");

        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(OnQuitGameClicked);
        else
            Debug.LogWarning("DeathMenuUI: quitGameButton not assigned.");

        Debug.Log($"DeathMenuUI: Awake. root={(root != null ? root.name : "self")}, mainMenuSceneName={mainMenuSceneName}");
        Hide();
    }

    public void Show(int enemyLevel, EnemyAI.EnemyRarity enemyRarity, Element weaponElement)
    {
        if (root != null)
        {
            root.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = "You Died";
        }

        if (detailsText != null)
        {
            detailsText.text = $"Killed by Level {enemyLevel} {enemyRarity}\nWeapon Element: {weaponElement}";
        }

        Debug.Log($"DeathMenuUI: Show -> enemyLevel={enemyLevel}, enemyRarity={enemyRarity}, weaponElement={weaponElement}");
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        Debug.Log("DeathMenuUI: Hide");
        if (root != null)
        {
            root.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void OnRestartLevelClicked()
    {
        Debug.Log("DeathMenuUI: Restart Level clicked.");
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartCurrentLevelAfterDeath();
        }
    }

    public void OnReturnToMainMenuClicked()
    {
        Debug.Log($"DeathMenuUI: Return to Main Menu clicked. Loading scene={mainMenuSceneName}");
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnQuitGameClicked()
    {
        Debug.Log("DeathMenuUI: Quit Game clicked.");
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        Debug.Log("DeathMenuUI: Quit requested (Application.Quit has no effect in editor).");
#endif
    }
}
