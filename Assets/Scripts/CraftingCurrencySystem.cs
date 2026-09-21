// Developer map: Stackable crafting currencies, target validation/consumption authority, and shared gear crafting operations.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public enum CraftingCurrencyType
{
    NormalToMagic,
    MagicToRare,
    RerollMagic,
    AddRareModifier,
    RerollRareModifier,
    RemoveRareModifier,
    AncientNormalToMagic,
    AncientMagicToRare,
    AncientRareToLegendary,
    AncientReroll,
    AncientAddModifier,
    AncientRemoveModifier,
    EmpowermentCatalyst
}

[Serializable]
public struct CurrencyStackData
{
    public CraftingCurrencyType type;
    public int amount;
    public CurrencyStackData(CraftingCurrencyType type, int amount) { this.type = type; this.amount = amount; }
}

public sealed class CurrencyInventory : MonoBehaviour
{
    public static CurrencyInventory Instance { get; private set; }
    [SerializeField] List<CurrencyStackData> serializedStacks = new();
    [SerializeField] int normalToMagicFragments, magicToRareFragments;
    readonly Dictionary<CraftingCurrencyType, int> stacks = new();
    public event Action Changed;
    public static event Action<Gear> GearChanged;
    public CraftingCurrencyType? ArmedCurrency { get; private set; }
    public IReadOnlyList<CurrencyStackData> Stacks => serializedStacks;
    public int NormalToMagicFragments => normalToMagicFragments;
    public int MagicToRareFragments => magicToRareFragments;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        RebuildLookup();
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
    void RebuildLookup()
    {
        stacks.Clear();
        foreach (var stack in serializedStacks) if (stack.amount > 0) stacks[stack.type] = Count(stack.type) + stack.amount;
        SyncSerialized();
    }
    void SyncSerialized()
    {
        serializedStacks.Clear();
        foreach (var pair in stacks) if (pair.Value > 0) serializedStacks.Add(new CurrencyStackData(pair.Key, pair.Value));
        serializedStacks.Sort((a,b) => a.type.CompareTo(b.type));
    }
    public int Count(CraftingCurrencyType type) => stacks.TryGetValue(type, out int amount) ? amount : 0;
    public int FragmentCount(CraftingCurrencyType type) => type switch
    {
        CraftingCurrencyType.NormalToMagic => normalToMagicFragments,
        CraftingCurrencyType.MagicToRare => magicToRareFragments,
        _ => 0
    };
    public bool AddFragments(CraftingCurrencyType type, int amount)
    {
        if (!CanAddFragments(type,amount)) return false;
        int current = FragmentCount(type);
        int full = (current + amount) / 10;
        if (type == CraftingCurrencyType.NormalToMagic) normalToMagicFragments = (current + amount) % 10;
        else magicToRareFragments = (current + amount) % 10;
        if (full > 0) stacks[type] = Count(type) + full;
        SyncSerialized(); Changed?.Invoke(); GamePersistence.MarkDirty();
        return true;
    }
    public bool CanAddFragments(CraftingCurrencyType type,int amount)
    {
        if(amount <= 0 || (type != CraftingCurrencyType.NormalToMagic && type != CraftingCurrencyType.MagicToRare)) return false;
        int current=FragmentCount(type);
        if(current > int.MaxValue-amount) return false;
        int full=(current+amount)/10;
        return Count(type) <= int.MaxValue-full;
    }
    public bool RestoreFragments(int normalToMagic, int magicToRare)
    {
        if (normalToMagic < 0 || magicToRare < 0
            || Count(CraftingCurrencyType.NormalToMagic) > int.MaxValue - normalToMagic / 10
            || Count(CraftingCurrencyType.MagicToRare) > int.MaxValue - magicToRare / 10) return false;
        int normalOrbs=normalToMagic/10,rareOrbs=magicToRare/10;
        normalToMagicFragments = normalToMagic % 10;
        magicToRareFragments = magicToRare % 10;
        if(normalOrbs>0)stacks[CraftingCurrencyType.NormalToMagic]=Count(CraftingCurrencyType.NormalToMagic)+normalOrbs;
        if(rareOrbs>0)stacks[CraftingCurrencyType.MagicToRare]=Count(CraftingCurrencyType.MagicToRare)+rareOrbs;
        SyncSerialized();Changed?.Invoke();GamePersistence.MarkDirty();return true;
    }
    public void ClearFragments()
    {
        normalToMagicFragments = magicToRareFragments = 0;
        Changed?.Invoke(); GamePersistence.MarkDirty();
    }
    public void Add(CraftingCurrencyType type, int amount = 1)
    {
        if (amount <= 0 || Count(type) > int.MaxValue - amount) return;
        stacks[type] = Count(type) + amount; SyncSerialized(); Changed?.Invoke(); GamePersistence.MarkDirty();
    }
    public bool TrySpend(CraftingCurrencyType type, int amount = 1)
    {
        if (amount <= 0 || Count(type) < amount) return false;
        int remaining = Count(type) - amount;
        if (remaining == 0) stacks.Remove(type); else stacks[type] = remaining;
        if (remaining == 0 && ArmedCurrency == type) ArmedCurrency = null;
        SyncSerialized(); Changed?.Invoke(); GamePersistence.MarkDirty(); return true;
    }
    public bool Arm(CraftingCurrencyType type)
    {
        if (Count(type) <= 0) return false;
        ArmedCurrency = type; Changed?.Invoke(); return true;
    }
    public void CancelArmed() { if (ArmedCurrency.HasValue) { ArmedCurrency = null; Changed?.Invoke(); } }
    public bool TryApplyArmedToGear(Gear gear, bool keepArmed = false)
    {
        if (!ArmedCurrency.HasValue) return false;
        var type = ArmedCurrency.Value;
        bool applied=type==CraftingCurrencyType.EmpowermentCatalyst
            ? EmpowermentCrafting.TryApply(gear,GameManager.Instance?.CurrentCombatLevel??1)
            : !IsAncient(type)&&EquipmentCrafting.TryApply(type, gear, ModManager.Instance);
        if (IsAncient(type) || Count(type) <= 0 || !applied)
        {
            CancelArmed();
            return false;
        }
        bool spent = TrySpend(type);
        if (spent && (!keepArmed || Count(type) <= 0)) CancelArmed();
        if (spent) Inventory.Instance?.NotifyItemChanged(gear);
        if (spent) EquipmentManager.Instance?.NotifyItemChanged(gear);
        if (spent) GearChanged?.Invoke(gear);
        if (spent) GamePersistence.Save();
        return spent;
    }
    public bool TryApplyArmedToRelic(RelicData relic)
    {
        if (!ArmedCurrency.HasValue || !IsAncient(ArmedCurrency.Value) || Count(ArmedCurrency.Value) <= 0) return false;
        var type=ArmedCurrency.Value;
        if(!AncientRelicCrafting.TryApply(type,relic,RelicInventory.Instance,false))return false;
        bool spent=TrySpend(type);if(spent){RelicInventory.Instance?.NotifyChanged();GamePersistence.Save();}return spent;
    }
    public void ClearAncient()
    {
        foreach (CraftingCurrencyType type in Enum.GetValues(typeof(CraftingCurrencyType))) if (IsAncient(type)) stacks.Remove(type);
        if (ArmedCurrency.HasValue && IsAncient(ArmedCurrency.Value)) ArmedCurrency = null;
        SyncSerialized(); Changed?.Invoke();
    }
    public void ResetForNewRun()
    {
        foreach (CraftingCurrencyType type in Enum.GetValues(typeof(CraftingCurrencyType)))
            if (!IsAncient(type)) stacks.Remove(type);
        ArmedCurrency = null;
        normalToMagicFragments = magicToRareFragments = 0;
        SyncSerialized(); Changed?.Invoke();
    }
    public void ResetForNewGame()
    {
        stacks.Clear();
        ArmedCurrency = null;
        normalToMagicFragments = magicToRareFragments = 0;
        SyncSerialized(); Changed?.Invoke();
    }
    public static bool IsAncient(CraftingCurrencyType type) => type is >= CraftingCurrencyType.AncientNormalToMagic and <= CraftingCurrencyType.AncientRemoveModifier;
    public static CraftingCurrencyType RandomOrdinary() => (CraftingCurrencyType)UnityEngine.Random.Range(0, 6);
    public void Restore(IEnumerable<CurrencyStackData> data)
    {
        stacks.Clear();
        if (data != null) foreach (var entry in data) if (entry.amount > 0) stacks[entry.type] = Count(entry.type) + entry.amount;
        ArmedCurrency = null;
        normalToMagicFragments = magicToRareFragments = 0;
        SyncSerialized(); Changed?.Invoke();
    }
}

public sealed partial class CurrencyInventoryPanel : MonoBehaviour
{
    InventoryUI owner;
    InventoryEquipmentPanelUI layout;
    [SerializeField] RectTransform ordinaryRoot, ancientRoot, relicRoot;
    [SerializeField] Button gearButton, relicButton;
    CraftingCurrencyCursorUI cursor;
    readonly Dictionary<CraftingCurrencyType, CurrencySlotUI> currencyEntries = new();
    readonly Dictionary<CraftingCurrencyType, TMP_Text> fragmentLabels = new();
    readonly Dictionary<RelicData, RelicSlotUI> relicEntries = new();
    public bool ShowingCurrencies => true;

    public void Initialize(InventoryUI inventoryUI, Transform equipment)
    {
        owner = inventoryUI;
        layout = GetComponent<InventoryEquipmentPanelUI>();
        Canvas canvas=GetComponentInParent<Canvas>(true);
        if(canvas!=null)cursor=CraftingCurrencyCursorUI.Ensure(canvas.rootCanvas);
        InventoryView view=GetComponent<InventoryView>();
        if(view!=null)
        {
            ordinaryRoot=view.ordinaryCurrencyRoot;ancientRoot=view.ancientCurrencyRoot;relicRoot=view.relicRoot;
            gearButton=view.gearToggle;relicButton=view.relicToggle;
            foreach(var slot in view.currencySlots)if(slot!=null){slot.BindOwner(owner);currencyEntries[slot.Type]=slot;}
            CraftingCurrencyType[] fragmentTypes={CraftingCurrencyType.NormalToMagic,CraftingCurrencyType.MagicToRare};
            for(int i=0;i<view.fragmentLabels.Count&&i<fragmentTypes.Length;i++)if(view.fragmentLabels[i]!=null)fragmentLabels[fragmentTypes[i]]=view.fragmentLabels[i];
        }
        if (ordinaryRoot == null) { Debug.LogError("CurrencyInventoryPanel requires authored currency roots.", this); return; }
        WireTabs();
    }
#if UNITY_EDITOR
    public void BuildAuthoring(InventoryUI inventoryUI, Transform equipment)
    {
        owner=inventoryUI;layout=GetComponent<InventoryEquipmentPanelUI>();
        if(ordinaryRoot!=null)return;
        ordinaryRoot = MakeRoot("Ordinary currency");
        ancientRoot = MakeRoot("Ancient relic currency");
        gearButton = MakeButton(transform, "GEAR", null);
        relicButton = MakeButton(transform, "RELICS", null);
        InventoryArtLayout.Apply((RectTransform)gearButton.transform,InventoryArtLayout.P(59,498,139,523));
        InventoryArtLayout.Apply((RectTransform)relicButton.transform,InventoryArtLayout.P(148,498,250,523));
        var relics = new GameObject("Relic inventory", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        relics.transform.SetParent(transform, false); relicRoot = (RectTransform)relics.transform;
        InventoryArtLayout.Apply(relicRoot,InventoryArtLayout.InventoryBounds);
        var relicLayout = relics.GetComponent<VerticalLayoutGroup>(); relicLayout.spacing = 8; relicLayout.childControlWidth = true; relicLayout.childForceExpandWidth = true; relicLayout.childForceExpandHeight = false;
        relics.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        EnsureCurrencyViews(true);
        WireTabs();
        ShowEquipment();
    }
#endif
    void WireTabs(){if(gearButton!=null){gearButton.onClick.RemoveListener(ShowEquipment);gearButton.onClick.AddListener(ShowEquipment);}if(relicButton!=null){relicButton.onClick.RemoveListener(ShowRelics);relicButton.onClick.AddListener(ShowRelics);}}
    void OnEnable() { if (CurrencyInventory.Instance != null) CurrencyInventory.Instance.Changed += RefreshCurrencies; if(RelicInventory.Instance!=null)RelicInventory.Instance.Changed+=RefreshRelics; Refresh(); }
    void OnDisable() { CurrencyTooltipUI.Hide(); RelicTooltipUI.Hide(); if (CurrencyInventory.Instance != null) { CurrencyInventory.Instance.Changed -= RefreshCurrencies; CurrencyInventory.Instance.CancelArmed(); } cursor?.RefreshPresentation(); if(RelicInventory.Instance!=null)RelicInventory.Instance.Changed-=RefreshRelics; }
    public bool IsShowingRelics => relicRoot!=null&&relicRoot.gameObject.activeSelf;
    public Button GearTab=>gearButton;
    public Button RelicTab=>relicButton;
    public void ShowEquipment() => SwitchView(false,true);
    public void ShowEquipmentForCurrency() => SwitchView(false,false);
    public void ShowCurrency() => ShowEquipment();
    public void ShowRelics() => SwitchView(true,true);
    public void ShowRelicsForCurrency() => SwitchView(true,false);
    void SwitchView(bool relicMode,bool manual)
    {
        if(manual)CurrencyInventory.Instance?.CancelArmed();
        layout?.SetRelicMode(relicMode);
        if(relicRoot!=null)relicRoot.gameObject.SetActive(relicMode);
        if(gearButton!=null)gearButton.image.color=relicMode?new Color(.12f,.13f,.17f,.96f):new Color(.34f,.29f,.16f,.98f);
        if(relicButton!=null)relicButton.image.color=relicMode?new Color(.35f,.20f,.42f,.98f):new Color(.13f,.11f,.18f,.96f);
        Refresh();
    }
    void Refresh()
    {
        if(ordinaryRoot==null || CurrencyInventory.Instance==null) return;
        RefreshCurrencies();
        RefreshRelics();
    }
    void RefreshCurrencies()
    {
        if(ordinaryRoot==null || CurrencyInventory.Instance==null) return;
        EnsureCurrencyViews(false);
        foreach(var slot in currencyEntries.Values)if(slot!=null)slot.RefreshPresentation();
        RefreshFragmentLabel(CraftingCurrencyType.NormalToMagic,InventoryArtLayout.P(67,792,174,816));
        RefreshFragmentLabel(CraftingCurrencyType.MagicToRare,InventoryArtLayout.P(355,792,462,816));
        CurrencyTooltipUI.RefreshVisible();
    }
    void EnsureCurrencyViews(bool createMissing)
    {
        if(ordinaryRoot==null||ancientRoot==null)return;
        CraftingCurrencyType[] ordered={CraftingCurrencyType.NormalToMagic,CraftingCurrencyType.RerollMagic,CraftingCurrencyType.MagicToRare,CraftingCurrencyType.RerollRareModifier,CraftingCurrencyType.AddRareModifier,CraftingCurrencyType.RemoveRareModifier,CraftingCurrencyType.AncientNormalToMagic,CraftingCurrencyType.AncientMagicToRare,CraftingCurrencyType.AncientRareToLegendary,CraftingCurrencyType.AncientReroll,CraftingCurrencyType.AncientAddModifier,CraftingCurrencyType.AncientRemoveModifier,CraftingCurrencyType.EmpowermentCatalyst};
        for(int index=0;index<ordered.Length;index++)
        {
            CraftingCurrencyType type=ordered[index];bool ancient=CurrencyInventory.IsAncient(type);bool catalyst=type==CraftingCurrencyType.EmpowermentCatalyst;int rowIndex=index%6;
            Rect slotRect=catalyst?InventoryArtLayout.P(270,498,455,526):(ancient?InventoryArtLayout.AncientCurrencySlots:InventoryArtLayout.OrdinaryCurrencySlots)[rowIndex];
            Rect countRect=catalyst?InventoryArtLayout.P(400,501,450,523):(ancient?InventoryArtLayout.AncientCountBoxes:InventoryArtLayout.OrdinaryCountBoxes)[rowIndex];
            Rect localCountRect=InventoryArtLayout.Relative(countRect,slotRect);
            if(currencyEntries.TryGetValue(type,out var existing)&&existing!=null)continue;
            if(!createMissing){Debug.LogError("Missing authored currency slot: "+type,this);continue;}
            var go = new GameObject(type.ToString(), typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(Outline), typeof(Button), typeof(CurrencySlotUI));
            go.transform.SetParent(ancient?ancientRoot:ordinaryRoot,false);InventoryArtLayout.Apply((RectTransform)go.transform,slotRect);
            var image=go.GetComponent<Image>();image.sprite=ancient||catalyst?InventoryArtCatalog.Currency(type):null;image.preserveAspect=true;image.color=ancient||catalyst?Color.white:new Color(1,1,1,.001f);image.enabled=!ancient&&!catalyst||image.sprite!=null;
            var labelGo=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)); labelGo.transform.SetParent(go.transform,false);
            var rect=(RectTransform)labelGo.transform;InventoryArtLayout.Apply(rect,localCountRect);
            var label=labelGo.GetComponent<TextMeshProUGUI>();label.alignment=TextAlignmentOptions.Center;label.fontSize=11;label.enableAutoSizing=true;label.fontSizeMin=8;label.fontSizeMax=11;label.raycastTarget=false;
            var outline=go.GetComponent<Outline>();outline.effectColor=new Color(1f,.78f,.25f);outline.effectDistance=new Vector2(2,-2);
            var slot=go.GetComponent<CurrencySlotUI>();slot.Initialize(type, owner, label, outline);currencyEntries[type]=slot;
        }
        RefreshFragmentLabel(CraftingCurrencyType.NormalToMagic,InventoryArtLayout.P(67,792,174,816),createMissing);
        RefreshFragmentLabel(CraftingCurrencyType.MagicToRare,InventoryArtLayout.P(355,792,462,816),createMissing);
    }
    void RefreshFragmentLabel(CraftingCurrencyType type,Rect bounds,bool createMissing=false)
    {
        if(!fragmentLabels.TryGetValue(type,out var label)||label==null)
        {
            if(!createMissing){Debug.LogError("Missing authored currency fragment label: "+type,this);return;}
            var go=new GameObject(type+" fragments",typeof(RectTransform),typeof(TextMeshProUGUI));
            go.transform.SetParent(ordinaryRoot,false);
            label=go.GetComponent<TextMeshProUGUI>();label.fontSize=10;label.alignment=TextAlignmentOptions.Center;
            label.color=new Color(.86f,.84f,.73f);label.raycastTarget=false;
            fragmentLabels[type]=label;
        }
        InventoryArtLayout.Apply(label.rectTransform,bounds);
        label.text=$"Fragments {(CurrencyInventory.Instance!=null?CurrencyInventory.Instance.FragmentCount(type):0)}/10";
    }
    void RefreshRelics()
    {
        if(relicRoot==null||RelicInventory.Instance==null)return;
        var removed=new List<RelicData>();
        foreach(var pair in relicEntries)if(!ContainsRelic(pair.Key)){if(pair.Value!=null){pair.Value.gameObject.SetActive(false);Destroy(pair.Value.gameObject);}removed.Add(pair.Key);}
        foreach(var relic in removed)relicEntries.Remove(relic);
        foreach(var relic in RelicInventory.Instance.Relics)
        {
            if(relicEntries.TryGetValue(relic,out var existing)&&existing!=null){existing.RefreshPresentation();continue;}
            var go=new GameObject("Relic "+relic.id,typeof(RectTransform),typeof(Image),typeof(Button),typeof(RelicSlotUI));go.transform.SetParent(relicRoot,false);go.GetComponent<Image>().color=new Color(.18f,.08f,.24f,.96f);
            ((RectTransform)go.transform).sizeDelta=new Vector2(0,74);
            var iconGo=new GameObject("Relic glyph",typeof(RectTransform),typeof(Image));iconGo.transform.SetParent(go.transform,false);
            var iconRect=(RectTransform)iconGo.transform;iconRect.anchorMin=new Vector2(0,.5f);iconRect.anchorMax=new Vector2(0,.5f);iconRect.pivot=new Vector2(0,.5f);iconRect.anchoredPosition=new Vector2(8,0);iconRect.sizeDelta=new Vector2(56,56);
            var glyph=iconGo.GetComponent<Image>();glyph.preserveAspect=true;glyph.raycastTarget=false;
            var labelGo=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));labelGo.transform.SetParent(go.transform,false);
            var rect=(RectTransform)labelGo.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(72,5);rect.offsetMax=new Vector2(-8,-5);
            var label=labelGo.GetComponent<TextMeshProUGUI>();label.fontSize=12;label.raycastTarget=false;var slot=go.GetComponent<RelicSlotUI>();slot.Initialize(relic,label);relicEntries[relic]=slot;
        }
        RelicTooltipUI.RefreshVisible();
    }
    bool ContainsRelic(RelicData target){foreach(var relic in RelicInventory.Instance.Relics)if(ReferenceEquals(relic,target))return true;return false;}
    static Button MakeButton(Transform parent,string textValue,UnityEngine.Events.UnityAction action)
    {
        var go=new GameObject(textValue,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
        var image=go.GetComponent<Image>();image.color=new Color(.15f,.15f,.18f);var button=go.GetComponent<Button>();if(action!=null)button.onClick.AddListener(action);
        var labelGo=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));labelGo.transform.SetParent(go.transform,false);
        var r=(RectTransform)labelGo.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        var label=labelGo.GetComponent<TextMeshProUGUI>();label.text=textValue;label.fontSize=12;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return button;
    }
    RectTransform MakeRoot(string name){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(transform,false);var rect=(RectTransform)go.transform;Place(rect,0,0,1,1);return rect;}
    void MakeCaption(string value,float x0,float y0,float x1,float y1,Color color){var go=new GameObject(value,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(transform,false);Place((RectTransform)go.transform,x0,y0,x1,y1);var label=go.GetComponent<TextMeshProUGUI>();label.text=value;label.fontSize=9;label.color=color;label.raycastTarget=false;}
    static void Place(RectTransform rect,float x0,float y0,float x1,float y1){rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static string RelicText(RelicData relic)=>ItemTooltipFormatter.DescribeRelic(relic);
}

public sealed partial class CurrencySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISubmitHandler
{
    [SerializeField] CraftingCurrencyType type; InventoryUI inventoryUI; [SerializeField] TMP_Text label; [SerializeField] Outline outline; [SerializeField] Image selectionOverlay;
    public CraftingCurrencyType Type=>type;
    public string DisplayText=>label!=null?label.text:string.Empty;
    public Image SelectionOverlay=>selectionOverlay;
    public bool IsSelected=>selectionOverlay!=null&&selectionOverlay.gameObject.activeSelf;
    public void Initialize(CraftingCurrencyType value,InventoryUI owner,TMP_Text target,Outline border){type=value;inventoryUI=owner;label=target;outline=border;EnsureSelectionOverlay();RefreshPresentation();}
    public void BindOwner(InventoryUI owner){inventoryUI=owner;if(selectionOverlay==null)Debug.LogError("Currency slot is missing its authored selection overlay.",this);}
    void EnsureSelectionOverlay()
    {
        if(selectionOverlay!=null)return;
        var go=new GameObject("Selected currency overlay",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);
        var r=(RectTransform)go.transform;r.anchorMin=new Vector2(.06f,.08f);r.anchorMax=new Vector2(.94f,.94f);r.offsetMin=r.offsetMax=Vector2.zero;
        selectionOverlay=go.GetComponent<Image>();selectionOverlay.color=new Color(1f,.78f,.08f,.28f);selectionOverlay.raycastTarget=false;go.transform.SetAsFirstSibling();
    }
    public void RefreshPresentation(){if(label==null)return;bool armed=CurrencyInventory.Instance!=null&&CurrencyInventory.Instance.ArmedCurrency==type;label.text=(CurrencyInventory.Instance!=null?CurrencyInventory.Instance.Count(type):0).ToString();if(outline!=null)outline.enabled=armed;if(selectionOverlay!=null)selectionOverlay.gameObject.SetActive(armed&&!CurrencyInventory.IsAncient(type));}
    public void OnPointerClick(PointerEventData data)
    {
        if((data.button!=PointerEventData.InputButton.Left&&data.button!=PointerEventData.InputButton.Right) || CurrencyInventory.Instance==null) return;
        ToggleArmed();
    }
    public void ToggleArmed()
    {
        if(CurrencyInventory.Instance==null)return;
        if(CurrencyInventory.Instance.ArmedCurrency==type) CurrencyInventory.Instance.CancelArmed();
        else if(CurrencyInventory.Instance.Arm(type)) { if(CurrencyInventory.IsAncient(type))inventoryUI?.ShowRelicViewForCurrency();else inventoryUI?.ShowEquipmentViewForCurrency(); }
    }
    public void OnPointerEnter(PointerEventData data)=>CurrencyTooltipUI.Show(type,(RectTransform)transform);
    public void OnPointerExit(PointerEventData data)=>CurrencyTooltipUI.Hide();
    public void OnSubmit(BaseEventData data)=>ToggleArmed();
}

public sealed class CraftingCurrencyCursorUI : MonoBehaviour
{
    public const float IconSize=54f;
    Canvas canvas;RectTransform rect;Image icon;CanvasGroup group;CurrencyInventory observed;
    readonly List<RaycastResult> raycastResults=new();
    public Image Icon=>icon;
    public bool IsShowing=>icon!=null&&icon.enabled;

    public static CraftingCurrencyCursorUI Ensure(Canvas rootCanvas)
    {
        if(rootCanvas==null)return null;
        var existing=rootCanvas.GetComponentInChildren<CraftingCurrencyCursorUI>(true);
        if(existing!=null){existing.Initialize(rootCanvas);return existing;}
        var go=new GameObject("Held crafting currency",typeof(RectTransform),typeof(CanvasGroup),typeof(Image),typeof(CraftingCurrencyCursorUI));
        go.transform.SetParent(rootCanvas.transform,false);var cursor=go.GetComponent<CraftingCurrencyCursorUI>();cursor.Initialize(rootCanvas);return cursor;
    }
    public void Initialize(Canvas rootCanvas)
    {
        canvas=rootCanvas;rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.15f,.85f);rect.sizeDelta=Vector2.one*IconSize;
        icon=GetComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;group=GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        ObserveInventory();RefreshPresentation();transform.SetAsLastSibling();
    }
    void OnEnable(){ObserveInventory();RefreshPresentation();}
    void OnDisable(){Unobserve();}
    void OnDestroy(){Unobserve();}
    void ObserveInventory()
    {
        if(observed==CurrencyInventory.Instance)return;Unobserve();observed=CurrencyInventory.Instance;if(observed!=null)observed.Changed+=RefreshPresentation;
    }
    void Unobserve(){if(observed!=null)observed.Changed-=RefreshPresentation;observed=null;}
    void Update()
    {
        ObserveInventory();RefreshPresentation();if(!IsShowing)return;
        Mouse mouse=Mouse.current;if(mouse!=null){Vector2 position=mouse.position.ReadValue();SetScreenPosition(position);if(mouse.leftButton.wasPressedThisFrame)HandleClickTarget(RaycastTarget(position));}
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)observed?.CancelArmed();
    }
    GameObject RaycastTarget(Vector2 position)
    {
        EventSystem events=EventSystem.current;if(events==null)return null;raycastResults.Clear();events.RaycastAll(new PointerEventData(events){position=position},raycastResults);return raycastResults.Count>0?raycastResults[0].gameObject:null;
    }
    public void HandleClickTarget(GameObject target)
    {
        if(observed==null||!observed.ArmedCurrency.HasValue)return;
        if(target!=null&&target.GetComponentInParent<CurrencySlotUI>()!=null)return;
        bool ancient=CurrencyInventory.IsAncient(observed.ArmedCurrency.Value);
        if(target!=null&&!ancient&&target.GetComponentInParent<ItemSlotUI>()!=null)return;
        if(target!=null&&ancient&&target.GetComponentInParent<RelicSlotUI>()!=null)return;
        observed.CancelArmed();
    }
    public void SetScreenPosition(Vector2 screenPosition)
    {
        if(canvas==null||rect==null)return;var root=(RectTransform)canvas.transform;Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screenPosition,camera,out Vector2 local))rect.anchoredPosition=local+new Vector2(18f,-18f);
    }
    public void RefreshPresentation()
    {
        if(icon==null)return;CraftingCurrencyType? armed=observed?.ArmedCurrency;bool visible=armed.HasValue&&observed.Count(armed.Value)>0;
        if(!visible){icon.enabled=false;icon.sprite=null;return;}
        Sprite sprite=InventoryArtCatalog.Currency(armed.Value);
        icon.sprite=sprite;
        icon.enabled=sprite!=null;
        icon.color=Color.white;
        transform.SetAsLastSibling();
    }
}

public static class CraftingCurrencyInput
{
    public static bool ShiftHeld=>Keyboard.current!=null&&(Keyboard.current.leftShiftKey.isPressed||Keyboard.current.rightShiftKey.isPressed);
}

public static class CurrencyPresentation
{
    public static string Name(CraftingCurrencyType type)=>type switch
    {
        CraftingCurrencyType.NormalToMagic=>"NORMAL TO MAGIC",CraftingCurrencyType.RerollMagic=>"MAGIC REROLL",CraftingCurrencyType.MagicToRare=>"MAGIC TO RARE",
        CraftingCurrencyType.RerollRareModifier=>"RARE / LEGENDARY REROLL",CraftingCurrencyType.AddRareModifier=>"ADD RARE / LEGENDARY MODIFIER",CraftingCurrencyType.RemoveRareModifier=>"REMOVE EXPLICIT MODIFIER",
        CraftingCurrencyType.AncientNormalToMagic=>"ANCIENT NORMAL TO MAGIC",CraftingCurrencyType.AncientMagicToRare=>"ANCIENT MAGIC TO RARE",CraftingCurrencyType.AncientRareToLegendary=>"ANCIENT RARE TO LEGENDARY",
        CraftingCurrencyType.AncientReroll=>"ANCIENT REROLL",CraftingCurrencyType.AncientAddModifier=>"ANCIENT ADD MODIFIER",CraftingCurrencyType.AncientRemoveModifier=>"ANCIENT REMOVE MODIFIER",_=>"EMPOWERMENT CATALYST"
    };
    public static string Symbol(CraftingCurrencyType type)=>type switch
    {
        CraftingCurrencyType.NormalToMagic=>"N → M", CraftingCurrencyType.RerollMagic=>"M ↻", CraftingCurrencyType.MagicToRare=>"M → R",
        CraftingCurrencyType.RerollRareModifier=>"R/L ↻", CraftingCurrencyType.AddRareModifier=>"R/L +", CraftingCurrencyType.RemoveRareModifier=>"M/R/L −",
        CraftingCurrencyType.AncientNormalToMagic=>"A N→M", CraftingCurrencyType.AncientMagicToRare=>"A M→R", CraftingCurrencyType.AncientRareToLegendary=>"A R→L",
        CraftingCurrencyType.AncientReroll=>"A ↻", CraftingCurrencyType.AncientAddModifier=>"A +", CraftingCurrencyType.AncientRemoveModifier=>"A −", _=>"E"
    };
    public static string Description(CraftingCurrencyType type)=>type switch
    {
        CraftingCurrencyType.NormalToMagic=>"Preserve the implicit; add one Prefix and one Suffix while upgrading to Magic.", CraftingCurrencyType.RerollMagic=>"Replace exactly one explicit Magic modifier on the same side.",
        CraftingCurrencyType.MagicToRare=>"Preserve existing modifiers; add two explicit modifiers while upgrading to Rare.", CraftingCurrencyType.RerollRareModifier=>"Replace exactly one explicit Rare or Legendary modifier on the same side.",
        CraftingCurrencyType.AddRareModifier=>"Add exactly one explicit modifier to a Rare or Legendary item when space permits.", CraftingCurrencyType.RemoveRareModifier=>"Remove exactly one explicit modifier from a Magic, Rare or Legendary item.",
        CraftingCurrencyType.AncientNormalToMagic=>"Ancient: upgrade the current-cycle Normal relic to Magic.", CraftingCurrencyType.AncientMagicToRare=>"Ancient: upgrade the current-cycle Magic relic to Rare.",
        CraftingCurrencyType.AncientRareToLegendary=>"Ancient: upgrade the current-cycle Rare relic to Legendary.", CraftingCurrencyType.AncientReroll=>"Ancient: reroll an unlocked modifier on the current-cycle relic.",
        CraftingCurrencyType.AncientAddModifier=>"Ancient: add a modifier to the current-cycle relic when space permits.", CraftingCurrencyType.AncientRemoveModifier=>"Ancient: remove an unlocked modifier from the current-cycle relic.", _=>"Convert one eligible ordinary T1 explicit modifier into a committed Empowered modifier."
    };
    public static string ValidTarget(CraftingCurrencyType type)=>type switch
    {
        CraftingCurrencyType.NormalToMagic=>"Normal equipment with one implicit and no explicits.",CraftingCurrencyType.RerollMagic=>"Magic equipment with an explicit modifier.",CraftingCurrencyType.MagicToRare=>"Magic equipment; under-filled gear still receives exactly two explicits.",
        CraftingCurrencyType.RerollRareModifier=>"Rare or Legendary equipment with an explicit modifier.",CraftingCurrencyType.AddRareModifier=>"Rare or Legendary equipment below its 4 / 6 explicit cap and with an open side.",CraftingCurrencyType.RemoveRareModifier=>"Magic, Rare or Legendary equipment with an explicit modifier.",CraftingCurrencyType.EmpowermentCatalyst=>"Equipment with an unempowered, ordinary, scalable T1 explicit and an unlocked Empowered slot.",
        _=>"The current-cycle craftable relic at the required rarity and modifier count."
    };
    public static string FailureConditions(CraftingCurrencyType type)=>CurrencyInventory.IsAncient(type)?"Fails on ordinary gear, past-cycle relics, invalid rarity, or a full / empty unlocked pool.":type==CraftingCurrencyType.EmpowermentCatalyst?"Fails below the next progression threshold or when no eligible T1 ordinary explicit remains.":"Fails on relics, scrap, the wrong rarity, insufficient Crafting Potential, a reached side/total cap, or when no eligible explicit modifier can change.";
    public static Color PrimaryColor(CraftingCurrencyType type)=>type switch
    {
        CraftingCurrencyType.NormalToMagic=>new Color(.75f,.86f,1f), CraftingCurrencyType.RerollMagic=>new Color(.18f,.38f,.82f), CraftingCurrencyType.MagicToRare=>new Color(.28f,.47f,.84f),
        CraftingCurrencyType.RerollRareModifier or CraftingCurrencyType.AddRareModifier=>new Color(.82f,.64f,.12f), CraftingCurrencyType.RemoveRareModifier=>new Color(.72f,.08f,.1f),
        CraftingCurrencyType.EmpowermentCatalyst=>new Color(.35f,.78f,.92f), _=>new Color(.28f,.09f,.42f)
    };
    public static Color AccentColor(CraftingCurrencyType type)=>type switch
    {
        CraftingCurrencyType.NormalToMagic=>Color.white, CraftingCurrencyType.MagicToRare=>new Color(.95f,.72f,.16f), CraftingCurrencyType.RemoveRareModifier=>new Color(1f,.32f,.28f),
        _ when CurrencyInventory.IsAncient(type)=>new Color(.72f,.36f,.92f), _=>PrimaryColor(type)*1.18f
    };
}

public sealed partial class CurrencyTooltipUI : MonoBehaviour
{
    static CurrencyTooltipUI instance; [SerializeField] TMP_Text label;CraftingCurrencyType type;RectTransform anchor;
    public static void Show(CraftingCurrencyType type,RectTransform anchor)
    {
        if(anchor==null)return;Canvas canvas=anchor.GetComponentInParent<Canvas>();if(canvas==null)return;
        if(instance==null){CurrencyTooltipUI prefab=Resources.Load<CurrencyTooltipUI>("UI/Tooltips/CurrencyTooltip");if(prefab==null){Debug.LogError("CurrencyTooltip prefab is missing from Resources/UI/Tooltips.");return;}instance=Instantiate(prefab,canvas.rootCanvas.transform);}
        instance.type=type;instance.anchor=anchor;instance.gameObject.SetActive(true);instance.Refresh();
    }
    public static void Hide(){if(instance!=null)instance.gameObject.SetActive(false);}
    public static void RefreshVisible(){if(instance!=null&&instance.gameObject.activeSelf)instance.Refresh();}
    void OnDestroy(){if(instance==this)instance=null;}
#if UNITY_EDITOR
    public static CurrencyTooltipUI BuildAuthoring(Transform parent){var go=new GameObject("Currency tooltip",typeof(RectTransform),typeof(Image),typeof(CurrencyTooltipUI));go.transform.SetParent(parent,false);var tip=go.GetComponent<CurrencyTooltipUI>();go.GetComponent<Image>().color=new Color(.025f,.028f,.035f,.98f);var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);var tr=(RectTransform)text.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(10,8);tr.offsetMax=new Vector2(-10,-8);tip.label=text.GetComponent<TextMeshProUGUI>();tip.label.fontSize=12;tip.label.textWrappingMode=TextWrappingModes.Normal;tip.label.raycastTarget=false;go.SetActive(false);return tip;}
#endif
    void Refresh()
    {
        int best=-1;foreach(var slot in FindObjectsByType<CurrencySlotUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(slot.Type==type&&slot.transform.GetSiblingIndex()>=best){best=slot.transform.GetSiblingIndex();anchor=(RectTransform)slot.transform;}
        if(anchor==null||!anchor.gameObject.activeInHierarchy){Hide();return;}
        int amount=CurrencyInventory.Instance!=null?CurrencyInventory.Instance.Count(type):0;string fragments=type==CraftingCurrencyType.NormalToMagic||type==CraftingCurrencyType.MagicToRare?$"\n<color=#B9C0CA>Fragments: {CurrencyInventory.Instance?.FragmentCount(type)??0}/10 (10 automatically become 1 full currency).</color>":string.Empty;label.text=$"<b>{CurrencyPresentation.Name(type)}  x{amount}</b>\n{CurrencyPresentation.Description(type)}{fragments}\n<color=#B9C0CA>VALID: {CurrencyPresentation.ValidTarget(type)}</color>\n<color=#85898F>{CurrencyPresentation.FailureConditions(type)}\nClick to select or cancel. Hold Shift for repeat use.</color>";
        var r=(RectTransform)transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(0,0);r.sizeDelta=new Vector2(340,136);var corners=new Vector3[4];anchor.GetWorldCorners(corners);r.position=corners[2];transform.SetAsLastSibling();
    }
}

public sealed class RelicSlotUI:MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler,ISubmitHandler
{
    RelicData relic;TMP_Text label;public string LastFeedback{get;private set;} public void Initialize(RelicData value,TMP_Text target=null){relic=value;label=target;RefreshPresentation();}
    public RelicData Item=>relic;
    public void RefreshPresentation()
    {
        if(relic==null)return;
        var glyph=transform.Find("Relic glyph")?.GetComponent<Image>();if(glyph!=null)glyph.sprite=PlaceholderIcon.Relic(relic.rarity,relic.cycle);
        if(label!=null)label.text=$"<b>{relic.rarity.ToString().ToUpperInvariant()} RELIC</b>  •  LEVEL {relic.relicLevel}  •  CYCLE {relic.cycle}\n{relic.ModifierCount} MODIFIERS  •  {(relic.craftableThisCycle?"CRAFTABLE":"PAST CYCLE")}";
    }
    public void OnPointerClick(PointerEventData data){if(data.button==PointerEventData.InputButton.Right)RelicTooltipUI.Show(relic,(RectTransform)transform);else if(data.button==PointerEventData.InputButton.Left)Activate();}
    public void Activate()
    {
        if(CurrencyInventory.Instance?.ArmedCurrency is CraftingCurrencyType currency&&CurrencyInventory.IsAncient(currency))
        {bool applied=CurrencyInventory.Instance.TryApplyArmedToRelic(relic);LastFeedback=applied?"Ancient crafting applied":"Ancient operation invalid";return;}
        string feedback="Relic unavailable";bool changed=RelicInventory.Instance!=null&&RelicInventory.Instance.ToggleEquip(relic,out feedback);LastFeedback=feedback;
        if(!changed&&feedback=="Relic slots full"){if(label!=null)label.text+="\n<color=#FFB85C>RELIC SLOTS FULL</color>";Debug.LogWarning(feedback);}
    }
    public void OnSubmit(BaseEventData data)=>Activate();
    public void OnPointerEnter(PointerEventData data)=>RelicTooltipUI.Show(relic,(RectTransform)transform);
    public void OnPointerExit(PointerEventData data)=>RelicTooltipUI.Hide();
}

public static class EquipmentCrafting
{
    public const int RareCap = 4;
    public static bool CanApply(CraftingCurrencyType currency, Gear gear)
    {
        if (gear == null || gear.IsScrap || CurrencyInventory.IsAncient(currency)
            || currency==CraftingCurrencyType.EmpowermentCatalyst
            || gear.CurrentCraftingPotential<CraftingPotentialProfile.OrdinaryCost(currency)) return false;
        int count = gear.CraftingModCount;
        return currency switch
        {
            CraftingCurrencyType.NormalToMagic => gear.ItemRarity == LootManager.GearRarity.Normal
                && count == 0 && gear.ImplicitMod != null,
            CraftingCurrencyType.MagicToRare => gear.ItemRarity == LootManager.GearRarity.Magic
                && gear.ImplicitMod != null,
            CraftingCurrencyType.RerollMagic => gear.ItemRarity == LootManager.GearRarity.Magic && Rerollable(gear).Count>0,
            CraftingCurrencyType.AddRareModifier => IsRareOrLegendary(gear) && count < Maximum(gear.ItemRarity),
            CraftingCurrencyType.RerollRareModifier => IsRareOrLegendary(gear) && Rerollable(gear).Count>0,
            CraftingCurrencyType.RemoveRareModifier => gear.ItemRarity != LootManager.GearRarity.Normal
                && Removable(gear).Count>0,
            _ => false
        };
    }
    public static bool TryApply(CraftingCurrencyType currency, Gear gear, ModManager mods)
    {
        if (!CanApply(currency, gear) || mods == null) return false;
        switch (currency)
        {
            case CraftingCurrencyType.NormalToMagic:
                if (!AddPair(gear, mods, LootManager.GearRarity.Magic)) return false;
                gear.SetRarity(LootManager.GearRarity.Magic); break;
            case CraftingCurrencyType.MagicToRare:
                if (!AddPair(gear, mods, LootManager.GearRarity.Rare)) return false;
                gear.SetRarity(LootManager.GearRarity.Rare); break;
            case CraftingCurrencyType.AddRareModifier:
                if (!Add(gear, mods, gear.ItemRarity)) return false; break;
            case CraftingCurrencyType.RerollMagic:
            case CraftingCurrencyType.RerollRareModifier:
                if (!RerollRandomUnlocked(gear, mods)) return false; break;
            case CraftingCurrencyType.RemoveRareModifier:
                if (!RemoveRandomUnlocked(gear)) return false; break;
            default: return false;
        }
        if(!gear.TrySpendCraftingPotential(CraftingPotentialProfile.OrdinaryCost(currency)))return false;
        gear.RebuildMods();
        return true;
    }
    static bool AddPair(Gear gear, ModManager mods, LootManager.GearRarity rarity)
    {
        int original = gear.rolledMods.Count;
        for (int index = 0; index < 2; index++)
        {
            int prefixes = 0, suffixes = 0;
            foreach (var mod in gear.rolledMods)
            {
                if (mod == null || mod.lockedOriginal || Gear.IsWeaponBaseStat(mod.statType)) continue;
                if (AffixPolicy.Side(mod) == AffixSide.Prefix) prefixes++; else suffixes++;
            }
            AffixSide side = prefixes <= suffixes ? AffixSide.Prefix : AffixSide.Suffix;
            if (!Add(gear, mods, rarity, side))
            {
                gear.rolledMods.RemoveRange(original, gear.rolledMods.Count - original);
                return false;
            }
        }
        return true;
    }
    static bool Add(Gear gear, ModManager mods, LootManager.GearRarity rarity,
        AffixSide? side = null)
    {
        RolledMod added = mods.RollAdditionalMod(gear, rarity, side);
        if (added == null) return false;
        added.lockedOriginal = false; gear.rolledMods.Add(added); return true;
    }
    static bool RerollRandomUnlocked(Gear gear, ModManager mods)
    {
        var candidates = Rerollable(gear);
        if (candidates.Count == 0) return false;
        RolledMod old = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        RolledMod replacement = mods.RerollModifier(gear, old, gear.ItemRarity);
        if (replacement == null) return false;
        replacement.lockedOriginal = false;
        gear.rolledMods[gear.rolledMods.IndexOf(old)] = replacement; return true;
    }
    static bool RemoveRandomUnlocked(Gear gear)
    {
        var candidates = Removable(gear);
        return candidates.Count > 0 && gear.rolledMods.Remove(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
    }
    static bool IsRareOrLegendary(Gear gear)=>gear.ItemRarity is LootManager.GearRarity.Rare or LootManager.GearRarity.Legendary;
    static int Maximum(LootManager.GearRarity rarity)=>rarity==LootManager.GearRarity.Legendary?6:RareCap;
    static List<RolledMod> Rerollable(Gear gear)
    {
        var result = new List<RolledMod>();
        foreach (var mod in gear.rolledMods) if (mod != null && !Gear.IsWeaponBaseStat(mod.statType)
            && !mod.lockedOriginal && !mod.isEmpowered && !mod.isBossSpecial) result.Add(mod);
        return result;
    }
    static List<RolledMod> Removable(Gear gear)
    {
        var result = new List<RolledMod>();
        foreach (var mod in gear.rolledMods) if (mod != null && !Gear.IsWeaponBaseStat(mod.statType)
            && !mod.lockedOriginal && !mod.isEmpowered && !mod.isBossSpecial) result.Add(mod);
        return result;
    }
    public static IReadOnlyList<RolledMod> RemovableForTests(Gear gear)=>Removable(gear);
}
