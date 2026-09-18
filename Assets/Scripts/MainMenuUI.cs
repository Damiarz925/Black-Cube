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
    private GameObject newGameConfirmation;
    private GameObject classSelectionPanel;
    private TMP_Text classSelectionLabel;
    private Button beginSelectedClassButton;
    private string selectedClassId;
    public bool NewGameConfirmationVisible => newGameConfirmation != null && newGameConfirmation.activeSelf;
    public bool ClassSelectionVisible => classSelectionPanel != null && classSelectionPanel.activeSelf;

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
        EnsureNewGameConfirmation();
        EnsureClassSelection();

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
        if (GamePersistence.HasSave)
        {
            newGameConfirmation.SetActive(true);
            newGameConfirmation.transform.SetAsLastSibling();
            return;
        }
        OpenClassSelection();
    }

    public void ConfirmNewGame()
    {
        if (newGameConfirmation != null) newGameConfirmation.SetActive(false);
        OpenClassSelection();
    }

    public bool SelectClass(string classId)
    {
        if(!PlayerClassCatalog.TryGet(classId,out var definition))return false;
        selectedClassId=classId;
        if(classSelectionLabel!=null)classSelectionLabel.text=$"{definition.DisplayName.ToUpperInvariant()}  /  SIGNATURE {WeaponTypeCatalog.Get(definition.SignatureWeaponTypeId).DisplayName.ToUpperInvariant()}";
        if(beginSelectedClassButton!=null)beginSelectedClassButton.interactable=true;
        return true;
    }

    public void StartSelectedClass()
    {
        if(!GameLaunchSelection.SelectNewGameClass(selectedClassId))return;
        if(classSelectionPanel!=null)classSelectionPanel.SetActive(false);
        GamePersistence.RequestConfirmedNewGame();
        SceneManager.LoadScene(GameSceneNames.Gameplay);
    }

    public void CancelNewGame()
    {
        GamePersistence.RequestNewGame();
        if (newGameConfirmation != null) newGameConfirmation.SetActive(false);
        if (classSelectionPanel != null) classSelectionPanel.SetActive(false);
        selectedClassId=null;GameLaunchSelection.Clear();
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

    private void EnsureNewGameConfirmation()
    {
        if (root == null || newGameButton == null || newGameConfirmation != null) return;
        newGameConfirmation = new GameObject("New Game Overwrite Confirmation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        newGameConfirmation.transform.SetParent(root.transform, false);
        RectTransform rect = (RectTransform)newGameConfirmation.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = new Vector2(820f, 390f);
        newGameConfirmation.GetComponent<Image>().color = new Color(.025f, .03f, .04f, .99f);
        CreateLabel(newGameConfirmation.transform, "STARTING A NEW GAME WILL OVERWRITE YOUR CURRENT PROGRESS.",
            new Vector2(0f, 95f), new Vector2(730f, 120f), 25f);
        Button start = CloneMenuButton(newGameButton, newGameConfirmation.transform, "Confirm Start New Game", "START NEW GAME");
        ((RectTransform)start.transform).anchoredPosition = new Vector2(-190f, -95f);
        ((RectTransform)start.transform).sizeDelta = new Vector2(340f, 80f);
        start.onClick.AddListener(ConfirmNewGame);
        Button cancel = CloneMenuButton(newGameButton, newGameConfirmation.transform, "Cancel New Game", "CANCEL");
        ((RectTransform)cancel.transform).anchoredPosition = new Vector2(190f, -95f);
        ((RectTransform)cancel.transform).sizeDelta = new Vector2(280f, 80f);
        cancel.onClick.AddListener(CancelNewGame);
        newGameConfirmation.SetActive(false);
    }

    private void EnsureClassSelection()
    {
        if(root==null||newGameButton==null||classSelectionPanel!=null)return;
        classSelectionPanel=new GameObject("New Game Class Selection",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        classSelectionPanel.transform.SetParent(root.transform,false);var rect=(RectTransform)classSelectionPanel.transform;
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.sizeDelta=new Vector2(980,680);
        classSelectionPanel.GetComponent<Image>().color=new Color(.025f,.03f,.04f,.99f);
        CreateLabel(classSelectionPanel.transform,"CHOOSE YOUR CLASS",new Vector2(0,275),new Vector2(900,60),34);
        for(int i=0;i<PlayerClassCatalog.All.Count;i++)
        {
            var definition=PlayerClassCatalog.All[i];int column=i%2,row=i/2;
            Button button=CloneMenuButton(newGameButton,classSelectionPanel.transform,"Choose "+definition.DisplayName,definition.DisplayName.ToUpperInvariant());
            ((RectTransform)button.transform).anchoredPosition=new Vector2(-230+column*460,170-row*115);
            ((RectTransform)button.transform).sizeDelta=new Vector2(390,80);string id=definition.Id;
            button.onClick.AddListener(()=>SelectClass(id));
        }
        classSelectionLabel=CreateLabel(classSelectionPanel.transform,"SELECT A CLASS",new Vector2(0,-190),new Vector2(900,55),20);
        beginSelectedClassButton=CloneMenuButton(newGameButton,classSelectionPanel.transform,"Begin Selected Class","BEGIN");
        ((RectTransform)beginSelectedClassButton.transform).anchoredPosition=new Vector2(-170,-270);((RectTransform)beginSelectedClassButton.transform).sizeDelta=new Vector2(280,70);
        beginSelectedClassButton.interactable=false;beginSelectedClassButton.onClick.AddListener(StartSelectedClass);
        Button cancel=CloneMenuButton(newGameButton,classSelectionPanel.transform,"Cancel Class Selection","CANCEL");
        ((RectTransform)cancel.transform).anchoredPosition=new Vector2(170,-270);((RectTransform)cancel.transform).sizeDelta=new Vector2(280,70);cancel.onClick.AddListener(CancelNewGame);
        classSelectionPanel.SetActive(false);
    }

    private void OpenClassSelection()
    {
        selectedClassId=null;if(beginSelectedClassButton!=null)beginSelectedClassButton.interactable=false;
        if(classSelectionLabel!=null)classSelectionLabel.text="SELECT A CLASS";
        if(classSelectionPanel!=null){classSelectionPanel.SetActive(true);classSelectionPanel.transform.SetAsLastSibling();}
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
