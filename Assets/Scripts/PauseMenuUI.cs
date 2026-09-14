// Developer map: Runtime-built in-game pause/options overlay using PaperBattleHUD's canonical time-scale controls and GamePersistence save route.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuUI : MonoBehaviour
{
    static readonly Color Dimmer = new(0f, 0f, 0f, .72f);
    static readonly Color Panel = new(.025f, .03f, .04f, .985f);
    static readonly Color ButtonColor = new(.105f, .13f, .16f, 1f);

    PaperBattleHUD hud;
    GameObject root;
    GameObject menuPanel;
    GameObject optionsPanel;
    TMP_Text pausePassiveTreeLabel;
    Func<bool> saveAction;
    Action<string> loadSceneAction;
    Action quitAction;

    public bool IsOpen => root != null && root.activeSelf;
    public bool IsOptionsOpen => IsOpen && optionsPanel != null && optionsPanel.activeSelf;
    public bool QuitRequested { get; private set; }
    public GameObject Root => root;
    public GameObject OptionsPanel => optionsPanel;
    public Button ResumeButton { get; private set; }
    public Button OptionsButton { get; private set; }
    public Button SaveAndMainMenuButton { get; private set; }
    public Button SaveAndQuitButton { get; private set; }
    public Button OptionsBackButton { get; private set; }
    public Button PausePassiveTreeButton { get; private set; }

    public void Initialize(PaperBattleHUD owner)
    {
        if (root != null || owner == null) return;
        hud = owner;
        saveAction = GamePersistence.TrySave;
        loadSceneAction = scene => SceneManager.LoadScene(scene);
        quitAction = () => Application.Quit();
        Build();
    }

    public void ConfigureActions(Func<bool> save, Action<string> loadScene, Action quit)
    {
        saveAction = save ?? GamePersistence.TrySave;
        loadSceneAction = loadScene ?? (scene => SceneManager.LoadScene(scene));
        quitAction = quit ?? (() => Application.Quit());
    }

    public void Open()
    {
        if (root == null) return;
        hud?.CloseGameplayPanels();
        menuPanel.SetActive(true);
        optionsPanel.SetActive(false);
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void Resume() => hud?.PlayGameplay();

    public void OpenOptions()
    {
        if (!IsOpen) return;
        RefreshOptions();
        menuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ReturnFromOptions()
    {
        if (!IsOpen) return;
        optionsPanel.SetActive(false);
        menuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void SaveAndMainMenu()
    {
        if (!TrySave()) return;
        Close();
        Time.timeScale = 1f;
        loadSceneAction(GameSceneNames.MainMenu);
    }

    public void SaveAndQuit()
    {
        if (!TrySave()) return;
        QuitRequested = true;
        quitAction();
    }

    bool TrySave()
    {
        bool saved;
        try { saved = saveAction != null && saveAction(); }
        catch (Exception exception)
        {
            Debug.LogError($"PauseMenuUI: save failed; remaining in gameplay. {exception}", this);
            return false;
        }
        if (!saved) Debug.LogError("PauseMenuUI: save failed; remaining in gameplay.", this);
        return saved;
    }

    void Build()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("PauseMenuUI requires a parent Canvas.", this); return; }

        root = Box(canvas.transform, "Pause Menu", Vector2.zero, Vector2.one, Dimmer, false);
        var overlayCanvas = root.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 200;
        root.AddComponent<GraphicRaycaster>();

        menuPanel = Card(root.transform, "Pause Menu Panel", new Vector2(560f, 610f));
        Label(menuPanel.transform, "PAUSED", new Vector2(0f, 230f), new Vector2(480f, 70f), 36f);
        ResumeButton = MenuButton(menuPanel.transform, "Resume Button", "RESUME", 130f, Resume);
        OptionsButton = MenuButton(menuPanel.transform, "Options Button", "OPTIONS", 35f, OpenOptions);
        SaveAndMainMenuButton = MenuButton(menuPanel.transform, "Save & Main Menu Button", "SAVE & MAIN MENU", -60f, SaveAndMainMenu);
        SaveAndQuitButton = MenuButton(menuPanel.transform, "Save & Quit Button", "SAVE & QUIT", -155f, SaveAndQuit);

        optionsPanel = Card(root.transform, "Pause Options Panel", new Vector2(760f, 430f));
        Label(optionsPanel.transform, "OPTIONS", new Vector2(0f, 145f), new Vector2(680f, 70f), 34f);
        PausePassiveTreeButton = MenuButton(optionsPanel.transform, "Pause Passive Tree Toggle", string.Empty, 35f, TogglePausePassiveTree, 680f);
        pausePassiveTreeLabel = PausePassiveTreeButton.GetComponentInChildren<TMP_Text>(true);
        OptionsBackButton = MenuButton(optionsPanel.transform, "Options Back Button", "BACK TO PAUSE MENU", -90f, ReturnFromOptions, 420f);

        foreach (Button button in root.GetComponentsInChildren<Button>(true)) CorruptionUIButtonSkin.Ensure(button);
        optionsPanel.SetActive(false);
        root.SetActive(false);
        RefreshOptions();
    }

    void TogglePausePassiveTree()
    {
        GameplayOptions.PausePassiveTree = !GameplayOptions.PausePassiveTree;
        RefreshOptions();
        Time.timeScale = 0f;
    }

    void RefreshOptions()
    {
        if (pausePassiveTreeLabel != null)
            pausePassiveTreeLabel.text = "PAUSE GAMEPLAY WHILE PASSIVE TREE IS OPEN: " + (GameplayOptions.PausePassiveTree ? "ON" : "OFF");
    }

    static GameObject Card(Transform parent, string name, Vector2 size)
    {
        GameObject card = Box(parent, name, Vector2.one * .5f, Vector2.one * .5f, Panel, true);
        RectTransform rect = (RectTransform)card.transform;
        rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = size;
        var outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(.55f, .12f, .1f, .9f);
        outline.effectDistance = new Vector2(2f, -2f);
        return card;
    }

    static GameObject Box(Transform parent, string name, Vector2 min, Vector2 max, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = raycast;
        return go;
    }

    static Button MenuButton(Transform parent, string name, string label, float y, Action action, float width = 440f)
    {
        GameObject go = Box(parent, name, Vector2.one * .5f, Vector2.one * .5f, ButtonColor, true);
        RectTransform rect = (RectTransform)go.transform;
        rect.pivot = Vector2.one * .5f; rect.anchoredPosition = new Vector2(0f, y); rect.sizeDelta = new Vector2(width, 72f);
        Button button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(() => action());
        Label(go.transform, label, Vector2.zero, rect.sizeDelta, 20f);
        return button;
    }

    static TMP_Text Label(Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(value + " Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        TMP_Text text = go.GetComponent<TMP_Text>(); text.text = value; text.fontSize = fontSize; text.enableAutoSizing = true;
        text.fontSizeMin = 12f; text.fontSizeMax = fontSize; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }

    public void Release()
    {
        if (IsOpen) Time.timeScale = 1f;
        if (root != null)
        {
            GameObject ownedRoot = root;
            root = null;
            if (Application.isPlaying) Destroy(ownedRoot); else DestroyImmediate(ownedRoot);
        }
    }

    void OnDestroy() => Release();
}
