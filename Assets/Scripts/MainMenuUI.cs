using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Buttons")]
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;

    private void Awake()
    {
        if (achievementsButton != null)
            achievementsButton.onClick.AddListener(OnAchievementsClicked);
        else
            Debug.LogWarning("MainMenuUI: achievementsButton not assigned.");

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);
        else
            Debug.LogWarning("MainMenuUI: newGameButton not assigned.");

        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
        else
            Debug.LogWarning("MainMenuUI: loadGameButton not assigned.");

        Debug.Log($"MainMenuUI: Awake. root={(root != null ? root.name : "null")}");

        if (root != null)
        {
            root.SetActive(true);
        }
    }

    public void OnAchievementsClicked()
    {
        Debug.Log("MainMenuUI: Achievements clicked (placeholder).");
    }

    public void OnNewGameClicked()
    {
        Debug.Log("MainMenuUI: New Game clicked (placeholder).");
    }

    public void OnLoadGameClicked()
    {
        Debug.Log("MainMenuUI: Load Game clicked (placeholder).");
    }
}
