// Developer map: Menu button wiring; New Game loads SampleScene and Options persists the passive-tree pause preference.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private MainMenuView authoredView;
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
    private GameObject slotSelectionPanel;
    private readonly Button[] slotButtons=new Button[GamePersistence.CharacterSlotCount];
    private int selectedSlot=1;
    private bool selectingForLoad;
    public bool NewGameConfirmationVisible => newGameConfirmation != null && newGameConfirmation.activeSelf;
    public bool ClassSelectionVisible => classSelectionPanel != null && classSelectionPanel.activeSelf;
    public bool SlotSelectionVisible=>slotSelectionPanel!=null&&slotSelectionPanel.activeSelf;
    public int VisibleSlotCount=>slotButtons.Length;

    private void Awake()
    {
        if(authoredView==null)authoredView=GetComponent<MainMenuView>();
        if(authoredView==null){Debug.LogError("MainMenuUI requires an authored MainMenuView. Run the Main Menu authoring builder.",this);return;}
        BindAuthoredView();
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

        WireAuthoredControls();

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
        OpenSlotSelection(false);
    }

    void BindAuthoredView()
    {
        root=authoredView.authoredRoot;achievementsButton=authoredView.achievementsButton;newGameButton=authoredView.newGameButton;loadGameButton=authoredView.loadGameButton;optionsButton=authoredView.optionsButton;optionsPanel=authoredView.optionsPanel;pausePassiveTreeLabel=authoredView.pausePassiveTreeLabel;newGameConfirmation=authoredView.overwriteConfirmation;classSelectionPanel=authoredView.classSelectionPanel;classSelectionLabel=authoredView.classSelectionLabel;beginSelectedClassButton=authoredView.beginSelectedClassButton;slotSelectionPanel=authoredView.slotSelectionPanel;
        for(int i=0;i<slotButtons.Length&&i<authoredView.slotButtons.Count;i++)slotButtons[i]=authoredView.slotButtons[i];
    }
    void WireAuthoredControls()
    {
        Wire(optionsButton,OpenOptions);Wire(authoredView.pausePassiveTreeButton,TogglePausePassiveTree);Wire(authoredView.optionsBackButton,CloseOptions);Wire(authoredView.confirmOverwriteButton,ConfirmNewGame);Wire(authoredView.cancelOverwriteButton,CancelNewGame);Wire(beginSelectedClassButton,StartSelectedClass);Wire(authoredView.cancelClassButton,CancelNewGame);Wire(authoredView.cancelSlotsButton,CancelNewGame);
        for(int i=0;i<authoredView.classButtons.Count&&i<PlayerClassCatalog.All.Count;i++){string id=PlayerClassCatalog.All[i].Id;Wire(authoredView.classButtons[i],()=>SelectClass(id));}
        for(int i=0;i<slotButtons.Length;i++){int slot=i+1;Wire(slotButtons[i],()=>SelectSlot(slot));}
    }
    static void Wire(Button button,UnityEngine.Events.UnityAction action){if(button==null)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}

#if UNITY_EDITOR
    public void BuildAuthoring()
    {
        EnsureOptionsMenu();EnsureNewGameConfirmation();EnsureClassSelection();EnsureSlotSelection();
        authoredView=GetComponent<MainMenuView>()??gameObject.AddComponent<MainMenuView>();authoredView.authoredRoot=root;authoredView.achievementsButton=achievementsButton;authoredView.newGameButton=newGameButton;authoredView.loadGameButton=loadGameButton;authoredView.optionsButton=optionsButton;authoredView.optionsPanel=optionsPanel;authoredView.pausePassiveTreeLabel=pausePassiveTreeLabel;authoredView.overwriteConfirmation=newGameConfirmation;authoredView.classSelectionPanel=classSelectionPanel;authoredView.classSelectionLabel=classSelectionLabel;authoredView.beginSelectedClassButton=beginSelectedClassButton;authoredView.slotSelectionPanel=slotSelectionPanel;
        authoredView.pausePassiveTreeButton=FindButton("Pause Passive Tree Toggle");authoredView.optionsBackButton=FindButton("Options Back Button");authoredView.confirmOverwriteButton=FindButton("Confirm Start New Game");authoredView.cancelOverwriteButton=FindButton("Cancel New Game");authoredView.cancelClassButton=FindButton("Cancel Class Selection");authoredView.cancelSlotsButton=FindButton("Cancel Slot Selection");
        authoredView.classButtons.Clear();foreach(var definition in PlayerClassCatalog.All)authoredView.classButtons.Add(FindButton("Choose "+definition.DisplayName));authoredView.slotButtons.Clear();for(int i=0;i<slotButtons.Length;i++)authoredView.slotButtons.Add(slotButtons[i]);
        UnityEditor.EditorUtility.SetDirty(authoredView);UnityEditor.EditorUtility.SetDirty(this);
    }
    Button FindButton(string objectName){foreach(Button button in root.GetComponentsInChildren<Button>(true))if(button.name==objectName)return button;return null;}
#endif

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
        GamePersistence.RequestConfirmedNewGame(selectedSlot);
        SceneManager.LoadScene(GameSceneNames.Gameplay);
    }

    public void CancelNewGame()
    {
        GamePersistence.RequestNewGame();
        if (newGameConfirmation != null) newGameConfirmation.SetActive(false);
        if (classSelectionPanel != null) classSelectionPanel.SetActive(false);
        if(slotSelectionPanel!=null)slotSelectionPanel.SetActive(false);
        selectedClassId=null;GameLaunchSelection.Clear();
    }

    public void OnLoadGameClicked()
    {
        OpenSlotSelection(true);
    }

    public void SelectSlot(int slot)
    {
        selectedSlot=slot;var summary=GamePersistence.GetSlotSummary(slot);
        if(selectingForLoad){if(!summary.occupied)return;if(!GamePersistence.RequestLoad(slot))return;SceneManager.LoadScene(GameSceneNames.Gameplay);return;}
        if(summary.occupied){newGameConfirmation.SetActive(true);newGameConfirmation.transform.SetAsLastSibling();return;}
        OpenClassSelection();
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

    private void EnsureSlotSelection()
    {
        if(root==null||newGameButton==null||slotSelectionPanel!=null)return;
        slotSelectionPanel=new GameObject("Character Slot Selection",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));slotSelectionPanel.transform.SetParent(root.transform,false);
        var rect=(RectTransform)slotSelectionPanel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.sizeDelta=new Vector2(1040,760);slotSelectionPanel.GetComponent<Image>().color=new Color(.025f,.03f,.04f,.99f);
        CreateLabel(slotSelectionPanel.transform,"CHARACTER SLOTS",new Vector2(0,320),new Vector2(920,60),34);
        for(int i=0;i<slotButtons.Length;i++){int slot=i+1;var b=CloneMenuButton(newGameButton,slotSelectionPanel.transform,"Character Slot "+slot,"SLOT "+slot);((RectTransform)b.transform).anchoredPosition=new Vector2(0,225-i*92);((RectTransform)b.transform).sizeDelta=new Vector2(850,72);b.onClick.AddListener(()=>SelectSlot(slot));slotButtons[i]=b;}
        var cancel=CloneMenuButton(newGameButton,slotSelectionPanel.transform,"Cancel Slot Selection","CANCEL");((RectTransform)cancel.transform).anchoredPosition=new Vector2(0,-325);((RectTransform)cancel.transform).sizeDelta=new Vector2(280,65);cancel.onClick.AddListener(CancelNewGame);slotSelectionPanel.SetActive(false);
    }

    private void OpenSlotSelection(bool load)
    {
        selectingForLoad=load;RefreshSlots();if(slotSelectionPanel!=null){slotSelectionPanel.SetActive(true);slotSelectionPanel.transform.SetAsLastSibling();}
    }

    private void RefreshSlots()
    {
        for(int i=0;i<slotButtons.Length;i++){var summary=GamePersistence.GetSlotSummary(i+1);var text=slotButtons[i].GetComponentInChildren<TMP_Text>(true);string cls=summary.occupied&&PlayerClassCatalog.TryGet(summary.baseClassId,out var definition)?definition.DisplayName.ToUpperInvariant():"EMPTY";string sub=string.IsNullOrEmpty(summary.selectedSubclassId)?"":$" / {summary.selectedSubclassId}";text.text=summary.occupied?$"SLOT {i+1}  /  {cls}{sub}  /  LEVEL {summary.playerLevel}  /  COMBAT {summary.combatLevel}\n<size=12>LAST PLAYED {summary.lastPlayedUtc}</size>":$"SLOT {i+1}  /  EMPTY";slotButtons[i].interactable=!selectingForLoad||summary.occupied;}
    }

    private void OpenClassSelection()
    {
        if(slotSelectionPanel!=null)slotSelectionPanel.SetActive(false);
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
