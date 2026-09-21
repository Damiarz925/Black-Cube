// Developer map: Builds the automatic-dismantle controls. Enabled level, rarity,
// and mod-mismatch rules combine with OR; existing inventory is never retroactively scrapped.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Session-only, independently enabled inclusive pickup filters.</summary>
public class InventoryFilterUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text levelLabel, rarityLabel, modMismatchLabel;
    [SerializeField] Button filterButton, levelToggle, levelDown, levelUp, rarityToggle, rarityDown, rarityUp, modMismatchToggle;
    void Start()
    {
        if(panel==null){Debug.LogError("InventoryFilterUI requires an authored filter panel.",this);return;}
        WireControls();
        Refresh(); panel.SetActive(false);
    }
#if UNITY_EDITOR
    public void BuildAuthoring()
    {
        if(panel!=null)return;
        foreach(var text in GetComponentsInChildren<TMP_Text>())
        {
            if(text.text=="RECOVERED EQUIPMENT") text.text="EQUIPMENT";
            if(text.text=="SELECT AN ITEM TO EQUIP")
            {
                text.text="LEFT: EQUIP / RIGHT: SCRAP   •   HOVER: DETAILS";
                text.fontSize=11;text.enableAutoSizing=true;text.fontSizeMin=8;text.fontSizeMax=11;
                text.alignment=TextAlignmentOptions.MidlineLeft;text.textWrappingMode=TextWrappingModes.NoWrap;
                text.rectTransform.anchorMin=new Vector2(1.025f,.04f);text.rectTransform.anchorMax=new Vector2(1.48f,.095f);
                text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
            }
        }
        filterButton = Button(transform,"AUTO-DISMANTLE",new Vector2(.59f,.86f),new Vector2(.755f,.905f),null);
        BakedInventoryButton.Configure(filterButton,InventoryArtLayout.DismantleButton);
        panel = Box(transform,"Auto-dismantle filter",new Color(.04f,.055f,.065f,1));
        Place((RectTransform)panel.transform,new Vector2(.035f,.25f),new Vector2(.965f,.82f));
        Text(panel.transform,"AUTO-DISMANTLE / NEW LOOT ONLY",.86f,.98f,16);
        Text(panel.transform,"Any enabled rule can match. Existing gear and equipment returns are kept.",.72f,.86f,11);
        levelToggle = Button(panel.transform,"",new Vector2(.04f,.56f),new Vector2(.96f,.70f),null);
        levelLabel = levelToggle.GetComponentInChildren<TMP_Text>();
        levelDown=Button(panel.transform,"−",new Vector2(.05f,.43f),new Vector2(.27f,.55f),null);
        levelUp=Button(panel.transform,"+",new Vector2(.73f,.43f),new Vector2(.95f,.55f),null);
        rarityToggle = Button(panel.transform,"",new Vector2(.04f,.28f),new Vector2(.96f,.42f),null);
        rarityLabel = rarityToggle.GetComponentInChildren<TMP_Text>();
        rarityDown=Button(panel.transform,"<",new Vector2(.05f,.15f),new Vector2(.27f,.27f),null);
        rarityUp=Button(panel.transform,">",new Vector2(.73f,.15f),new Vector2(.95f,.27f),null);
        modMismatchToggle = Button(panel.transform,"",new Vector2(.04f,.01f),new Vector2(.96f,.14f),null);
        modMismatchLabel = modMismatchToggle.GetComponentInChildren<TMP_Text>();
        modMismatchLabel.enableAutoSizing=true;modMismatchLabel.fontSizeMin=8;modMismatchLabel.fontSizeMax=13;
        panel.SetActive(false);
    }
#endif
    void WireControls()
    {
        Wire(filterButton,TogglePanel);Wire(levelToggle,()=>{Inventory.Instance.FilterLevelEnabled=!Inventory.Instance.FilterLevelEnabled;Refresh();});
        Wire(levelDown,()=>{Inventory.Instance.FilterLevel=Mathf.Max(1,Inventory.Instance.FilterLevel-1);Refresh();});Wire(levelUp,()=>{Inventory.Instance.FilterLevel=Mathf.Min(int.MaxValue-1,Inventory.Instance.FilterLevel)+1;Refresh();});
        Wire(rarityToggle,()=>{Inventory.Instance.FilterRarityEnabled=!Inventory.Instance.FilterRarityEnabled;Refresh();});Wire(rarityDown,()=>{Inventory.Instance.FilterRarity=(LootManager.GearRarity)Mathf.Max(0,(int)Inventory.Instance.FilterRarity-1);Refresh();});Wire(rarityUp,()=>{Inventory.Instance.FilterRarity=(LootManager.GearRarity)Mathf.Min(3,(int)Inventory.Instance.FilterRarity+1);Refresh();});
        Wire(modMismatchToggle,()=>{Inventory.Instance.FilterModMismatchEnabled=!Inventory.Instance.FilterModMismatchEnabled;Refresh();});
    }
    static void Wire(Button button,UnityEngine.Events.UnityAction action){if(button==null)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}

    void TogglePanel()
    {
        GetComponent<InventoryUI>().Tooltip?.Hide();
        bool open = !panel.activeSelf;
        GetComponent<InventoryModHighlightUI>()?.Close();
        panel.SetActive(open);
        Refresh();
    }

    public void Close()
    {
        if(panel != null) panel.SetActive(false);
        BakedInventoryButton.SetEngaged(filterButton,false);
    }
    void OnDisable()
    {
        if(panel != null) panel.SetActive(false);
        BakedInventoryButton.SetEngaged(filterButton,false);
    }
    void Refresh()
    {
        var inv=Inventory.Instance;if(inv==null)return;
        levelLabel.text=$"{(inv.FilterLevelEnabled?"ON":"OFF")} / Level {inv.FilterLevel} and below";
        rarityLabel.text=$"{(inv.FilterRarityEnabled?"ON":"OFF")} / {inv.FilterRarity} and below";
        string readiness = inv.ModHighlightFilter.HasSelection ? "" : "  (set a mod filter first)";
        modMismatchLabel.text=$"{(inv.FilterModMismatchEnabled?"ON":"OFF")} / Dismantle items that miss Mod Highlight{readiness}";
        BakedInventoryButton.SetEngaged(filterButton,panel != null && panel.activeSelf);
        CorruptionUIButtonSkin.Ensure(levelToggle)?.SetSelected(inv.FilterLevelEnabled);
        CorruptionUIButtonSkin.Ensure(rarityToggle)?.SetSelected(inv.FilterRarityEnabled);
        CorruptionUIButtonSkin.Ensure(modMismatchToggle)?.SetSelected(inv.FilterModMismatchEnabled);
    }
    static GameObject Box(Transform parent,string name,Color color)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go;}
    static void Place(RectTransform r,Vector2 min,Vector2 max)
    {r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
    static TMP_Text Text(Transform parent,string text,float bottom,float top,int size)
    {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();t.text=text;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;Place(t.rectTransform,new Vector2(.04f,bottom),new Vector2(.96f,top));return t;}
    static Button Button(Transform parent,string label,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {var go=Box(parent,label,new Color(.14f,.19f,.22f));Place((RectTransform)go.transform,min,max);var b=go.AddComponent<Button>();b.targetGraphic=go.GetComponent<Image>();if(action!=null)b.onClick.AddListener(action);Text(go.transform,label,0,1,13);return b;}
}
