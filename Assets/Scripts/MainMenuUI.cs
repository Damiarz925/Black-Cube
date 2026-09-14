// Developer map: Menu button wiring; New Game loads SampleScene and Options persists the passive-tree pause preference.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Buttons")]
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;

    private Button optionsButton;
    private GameObject optionsPanel;
    private TMP_Text pausePassiveTreeLabel;

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
        {
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
            loadGameButton.interactable = GamePersistence.HasSave;
        }
        else
            Debug.LogWarning("MainMenuUI: loadGameButton not assigned.");

        EnsureOptionsMenu();

        if (root != null)
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                CorruptionUIButtonSkin.Ensure(button);

        Debug.Log($"MainMenuUI: Awake. root={(root != null ? root.name : "null")}, load={(loadGameButton != null ? loadGameButton.name : "null")}, loadInteractable={(loadGameButton != null && loadGameButton.interactable)}");

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
        GamePersistence.RequestNewGame();
        SceneManager.LoadScene(GameSceneNames.Gameplay);
    }

    public void OnLoadGameClicked()
    {
        if (!GamePersistence.RequestLoad()) { Debug.LogWarning("MainMenuUI: no saved game exists."); return; }
        SceneManager.LoadScene(GameSceneNames.Gameplay);
    }

    private void EnsureOptionsMenu()
    {
        if (root == null || newGameButton == null) return;

        optionsButton = CloneMenuButton(newGameButton, root.transform, "Options Button", "OPTIONS");
        RectTransform optionsRect = (RectTransform)optionsButton.transform;
        optionsRect.anchoredPosition = ((RectTransform)newGameButton.transform).anchoredPosition + Vector2.down * 220f;
        optionsButton.onClick.AddListener(OpenOptions);

        optionsPanel = new GameObject("Options Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        optionsPanel.transform.SetParent(root.transform, false);
        RectTransform panelRect = (RectTransform)optionsPanel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = Vector2.one * .5f;
        panelRect.sizeDelta = new Vector2(760f, 400f);
        optionsPanel.GetComponent<Image>().color = new Color(.025f, .03f, .04f, .98f);

        CreateLabel(optionsPanel.transform, "OPTIONS", new Vector2(0f, 125f), new Vector2(680f, 70f), 34f);
        Button pauseToggle = CloneMenuButton(newGameButton, optionsPanel.transform,
            "Pause Passive Tree Toggle", string.Empty);
        ((RectTransform)pauseToggle.transform).anchoredPosition = new Vector2(0f, 25f);
        ((RectTransform)pauseToggle.transform).sizeDelta = new Vector2(680f, 80f);
        pausePassiveTreeLabel = pauseToggle.GetComponentInChildren<TMP_Text>(true);
        pauseToggle.onClick.AddListener(TogglePausePassiveTree);

        Button back = CloneMenuButton(newGameButton, optionsPanel.transform, "Options Back Button", "BACK");
        ((RectTransform)back.transform).anchoredPosition = new Vector2(0f, -110f);
        ((RectTransform)back.transform).sizeDelta = new Vector2(300f, 70f);
        back.onClick.AddListener(CloseOptions);

        optionsPanel.SetActive(false);
        RefreshOptions();
    }

    private static Button CloneMenuButton(Button template, Transform parent, string objectName, string label)
    {
        Button result = Instantiate(template, parent);
        result.name = objectName;
        result.onClick.RemoveAllListeners();
        TMP_Text text = result.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
        return result;
    }

    private static TMP_Text CreateLabel(Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(value + " Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private void OpenOptions()
    {
        if (optionsPanel == null) return;
        RefreshOptions();
        optionsPanel.SetActive(true);
        optionsPanel.transform.SetAsLastSibling();
    }

    private void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    private void TogglePausePassiveTree()
    {
        GameplayOptions.PausePassiveTree = !GameplayOptions.PausePassiveTree;
        RefreshOptions();
    }

    private void RefreshOptions()
    {
        if (pausePassiveTreeLabel != null)
            pausePassiveTreeLabel.text = "PAUSE GAMEPLAY WHILE PASSIVE TREE IS OPEN: "
                + (GameplayOptions.PausePassiveTree ? "ON" : "OFF");
    }
}
