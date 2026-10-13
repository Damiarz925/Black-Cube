// Developer map: Maintains Gear-to-slot views and a shared root-canvas tooltip, with a responsive scrolling grid. Subscribes to inventory changes while enabled and preserves unchanged slot objects.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

[Serializable]
public class GearIconEntry
{
    public LootManager.GearType gearType;
    public Sprite icon;
}

public class InventoryUI : MonoBehaviour
{
    public const int GridColumnCount = 10;
    public static readonly Vector2 GridCellSize = new Vector2(54, 54);
    public static readonly Vector2 GridSpacing = new Vector2(2, 2);
    [Header("UI References")]
    [SerializeField] private InventoryView authoredView;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Icons")]
    [SerializeField] private List<GearIconEntry> gearIcons = new List<GearIconEntry>();

    private Dictionary<LootManager.GearType, Sprite> iconLookup;
    private readonly Dictionary<Gear, ItemSlotUI> slots = new();
    private InventoryTypeFilterUI typeFilters;
    private RectTransform gridBackdrop;
    private readonly List<Image> gridCells = new();
    private ZoneManager zone;
    private int displayedCorruptionStage = -1;
    public ItemTooltipUI Tooltip { get; private set; }
    DismantleConfirmationUI dismantleConfirmation;
    public void RequestManualDismantle(Gear item)
    {
        if(Inventory.Instance==null||!Inventory.Instance.CanDismantle(item))return;
        dismantleConfirmation??=GetComponentInChildren<DismantleConfirmationUI>(true);
        if(dismantleConfirmation!=null)dismantleConfirmation.Request(item);
        else if(item.ItemRarity is not (LootManager.GearRarity.Legendary or LootManager.GearRarity.Unique))Inventory.Instance.TryDismantle(item);
        else Debug.LogError("Missing authored dismantle confirmation; protected item was not destroyed.",this);
    }
    private CurrencyInventoryPanel currencyPanel;
    readonly Dictionary<Gear,float> dpsCache = new();
    PlayerController previewPlayer;
    StatsComponent previewStats;
    bool dpsDirty=true;
    float nextDpsRefresh;
    float dpsBaseline;
    int auraFingerprint;
    float nextAuraCheck;
    void ManaPreviewChanged()
    {
        // Regen normally changes Mana every frame; only invalidate if Mana is a damage input.
        if(previewStats!=null&&previewStats.GetRawStat(StatTypes.DmgPerCurrentMana)!=0)InvalidateDpsCache();
    }
    public void InvalidateDpsCache(){dpsCache.Clear();dpsDirty=true;}
    public float? EstimatedUpgrade(Gear item)
    {
        if(item==null||item.IsScrap)return null;
        previewPlayer??=FindFirstObjectByType<PlayerController>();if(previewPlayer==null)return null;
        if(dpsCache.Count==0)dpsBaseline=CharacterDamageEstimate.Calculate(previewPlayer);
        if(!dpsCache.TryGetValue(item,out float estimate))dpsCache[item]=estimate=CharacterDamageEstimate.Replacement(previewPlayer,item);
        return dpsBaseline>0?(estimate/dpsBaseline-1)*100:estimate>0?100:0;
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public Dictionary<RolledMod,float> MarginalUpgradeContributions(Gear item)
    {
        var result=new Dictionary<RolledMod,float>();
        if(item==null||item.IsScrap||!GameplayOptions.UpgradeDiagnostics)return result;
        previewPlayer??=FindFirstObjectByType<PlayerController>();if(previewPlayer==null)return result;
        float full=dpsCache.TryGetValue(item,out float cached)?cached:CharacterDamageEstimate.Replacement(previewPlayer,item);
        for(int i=0;i<item.rolledMods.Count;i++)
        {
            RolledMod mod=item.rolledMods[i];if(mod==null||Gear.IsWeaponBaseStat(mod.statType))continue;
            result[mod]=CharacterDamageEstimate.MarginalContribution(previewPlayer,item,i,full);
        }
        return result;
    }
#endif
    void RefreshDps()
    {
        if(!isActiveAndEnabled||!dpsDirty||Time.unscaledTime<nextDpsRefresh)return;
        nextDpsRefresh=Time.unscaledTime+.02f;
        previewPlayer??=FindFirstObjectByType<PlayerController>();if(previewPlayer==null)return;
        if(dpsCache.Count==0)dpsBaseline=CharacterDamageEstimate.Calculate(previewPlayer);
        int budget=4;
        foreach(var pair in slots)
        {
            if(dpsCache.ContainsKey(pair.Key))continue;
            float estimate=CharacterDamageEstimate.Replacement(previewPlayer,pair.Key);dpsCache[pair.Key]=estimate;
            pair.Value.SetDpsUpgrade(dpsBaseline>0?(estimate/dpsBaseline-1)*100:estimate>0?100:0);
            if(--budget==0)break;
        }
        dpsDirty=dpsCache.Count<slots.Count;
    }
    public void ShowEquipmentView() => currencyPanel?.ShowEquipment();
    public void ShowCurrencyView() => currencyPanel?.ShowCurrency();
    public void ShowRelicView() => currencyPanel?.ShowRelics();
    public void ShowEquipmentViewForCurrency() => currencyPanel?.ShowEquipmentForCurrency();
    public void ShowRelicViewForCurrency() => currencyPanel?.ShowRelicsForCurrency();

    public void ShowTooltip(ItemSlotUI slot)
    {
        if (Tooltip == null) Tooltip = ItemTooltipUI.CreateFromPrefab(GetComponentInParent<Canvas>().rootCanvas.transform,authoredView!=null?authoredView.itemTooltipPrefab:null);
        if(Tooltip==null)return;
        Tooltip.Show(slot);
    }
    public void ShowTooltip(Gear gear, RectTransform anchor, bool equipped, UnityEngine.EventSystems.PointerEventData data,
        bool validatePlayerEquipment = true)
    {
        if (Tooltip == null) Tooltip = ItemTooltipUI.CreateFromPrefab(GetComponentInParent<Canvas>().rootCanvas.transform,authoredView!=null?authoredView.itemTooltipPrefab:null);
        if(Tooltip==null)return;
        Tooltip.Show(gear, anchor, equipped, data, validatePlayerEquipment);
    }

    private void Awake()
    {
        if (authoredView == null) authoredView = GetComponent<InventoryView>();
        if (authoredView == null)
        {
            Debug.LogError("InventoryUI requires an authored InventoryView. Run Black-Cube/UI Authoring/Build Inventory.", this);
            return;
        }
        contentParent = authoredView.itemGridRoot;
        typeFilters = GetComponent<InventoryTypeFilterUI>();
        if (authoredView.itemSlotPrefab != null) itemSlotPrefab = authoredView.itemSlotPrefab.gameObject;
        InventoryEquipmentPanelUI.Ensure(this, contentParent);
        currencyPanel = GetComponent<CurrencyInventoryPanel>();
        if (currencyPanel != null) currencyPanel.Initialize(this, contentParent);
        else Debug.LogError("InventoryView is missing its authored CurrencyInventoryPanel binding.", this);
        iconLookup = new Dictionary<LootManager.GearType, Sprite>();

        foreach (var entry in gearIcons)
        {
            if (entry == null || entry.icon == null) continue;
            iconLookup[entry.gearType] = entry.icon;
        }
    }

#if UNITY_EDITOR
    public void BuildAuthoringView()
    {
        ConfigureGrid();
        authoredView = GetComponent<InventoryView>();
        if (authoredView == null) authoredView = gameObject.AddComponent<InventoryView>();
        authoredView.authoredRoot = (RectTransform)transform;
        authoredView.itemGridRoot = contentParent as RectTransform;
        authoredView.itemSlotPrefab = itemSlotPrefab != null ? itemSlotPrefab.GetComponent<ItemSlotUI>() : null;
    }
#endif

    private void ConfigureGrid()
    {
        if (contentParent == null) return;
        var old = (RectTransform)contentParent;
        var scroll = old.GetComponentInParent<ScrollRect>();
        var go = new GameObject("Compact inventory grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        var rect = (RectTransform)go.transform; rect.SetParent(old.parent, false);
        rect.anchorMin = old.anchorMin; rect.anchorMax = old.anchorMax; rect.pivot = old.pivot;
        rect.offsetMin = old.offsetMin; rect.offsetMax = old.offsetMax;
        var grid = go.GetComponent<GridLayoutGroup>(); grid.cellSize = GridCellSize; grid.spacing = GridSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = GridColumnCount;
        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentParent = rect;
        if (scroll != null) { scroll.content = rect; scroll.horizontal = false; scroll.scrollSensitivity = 35; scroll.onValueChanged.AddListener(_ => Tooltip?.Hide()); }
#if UNITY_EDITOR
        if (scroll != null) UnityEditor.EditorUtility.SetDirty(scroll);
#endif
        old.gameObject.SetActive(false);
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(old.gameObject); else Destroy(old.gameObject);
#else
        Destroy(old.gameObject);
#endif
        ConfigureGridMetrics();
    }

    private void LateUpdate()
    {
        if(Time.unscaledTime>=nextAuraCheck&&previewPlayer!=null)
        {
            nextAuraCheck=Time.unscaledTime+.5f;
            var aura=previewPlayer.GetComponent<SubclassCombatState>();int fingerprint=0;
            if(aura!=null)
            {
                foreach(var element in new[]{Element.Phys,Element.Fire,Element.Cold,Element.Light})fingerprint=unchecked(fingerprint*31+Mathf.RoundToInt(aura.AuraDamageMultiplier(element)*50));
                fingerprint=unchecked(fingerprint*31+Mathf.RoundToInt(aura.AuraSecondary(0,.1f)*50));
                fingerprint=unchecked(fingerprint*31+Mathf.RoundToInt(aura.AuraSecondary(4,.2f)*50));
            }
            if(fingerprint!=auraFingerprint){auraFingerprint=fingerprint;InvalidateDpsCache();}
        }
        RefreshDps();
        if (contentParent == null) return;
        int stage = CurrentCorruptionStage();
        if (stage != displayedCorruptionStage) RefreshAreaCorruptionBorders(stage);
    }

    private void ConfigureGridMetrics()
    {
        if(contentParent==null)return;
        var grid=contentParent.GetComponent<GridLayoutGroup>();if(grid!=null)grid.enabled=false;
        var fitter=contentParent.GetComponent<ContentSizeFitter>();if(fitter!=null)fitter.enabled=false;
    }

    private Sprite GetIconForGear(Gear gear)
    {
        Sprite themed = ItemIconCatalog.Get(gear);
        if (themed != null) return themed;
        if (gear != null && iconLookup != null && iconLookup.TryGetValue(gear.ItemType, out var sprite))
            return sprite;
        return null;
    }

    public void AddItem(Gear gear)
    {
        if (gear == null || contentParent == null || itemSlotPrefab == null)
        {
            Debug.LogWarning("InventoryUI.AddItem: missing gear/contentParent/itemSlotPrefab");
            return;
        }

        GameObject slotGO = Instantiate(itemSlotPrefab, contentParent);
        var slotUI = slotGO.GetComponent<ItemSlotUI>();

        if (slotUI == null)
        {
            Debug.LogError("ItemSlot prefab is missing ItemSlotUI component.");
            return;
        }

        Sprite icon = GetIconForGear(gear);
        slotUI.Setup(gear, icon);
        slotUI.SetAreaCorruption(CurrentCorruptionStage());
    }

    private void OnEnable()
    {
        previewPlayer=FindFirstObjectByType<PlayerController>();previewStats=previewPlayer?.GetComponent<StatsComponent>();
        if(previewStats!=null)previewStats.StatsChanged+=InvalidateDpsCache;
        if(previewPlayer!=null){previewPlayer.AttackChanged+=InvalidateDpsCache;if(previewPlayer.GetComponent<ManaComponent>()!=null)previewPlayer.GetComponent<ManaComponent>().ManaChanged+=ManaPreviewChanged;}
        InvalidateDpsCache();
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if(previewStats!=null)previewStats.StatsChanged-=InvalidateDpsCache;
        if(previewPlayer!=null){previewPlayer.AttackChanged-=InvalidateDpsCache;if(previewPlayer.GetComponent<ManaComponent>()!=null)previewPlayer.GetComponent<ManaComponent>().ManaChanged-=ManaPreviewChanged;}
        if (Tooltip != null) Tooltip.Hide();
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        InvalidateDpsCache();
        if (contentParent == null || itemSlotPrefab == null || Inventory.Instance == null)
            return;

        var removed = new List<Gear>();
        foreach (var pair in slots)
            if (!Contains(pair.Key)) { if(pair.Value != null) { pair.Value.gameObject.SetActive(false); Destroy(pair.Value.gameObject); } removed.Add(pair.Key); }
        foreach (var item in removed) slots.Remove(item);
        foreach (var gear in Inventory.Instance.Items)
        {
            if (gear == null) continue;
            if (!slots.TryGetValue(gear, out var slot) || slot == null)
            {
                slot = Instantiate(itemSlotPrefab, contentParent).GetComponent<ItemSlotUI>();
                slots[gear] = slot;
            }
            Sprite icon = GetIconForGear(gear);
            slot.Setup(gear, icon);
            slot.SetAreaCorruption(CurrentCorruptionStage());
            slot.transform.SetAsLastSibling();
        }
        PackVisibleItems();
        RefreshModHighlights();
    }

    public void RefreshTypeFilters()=>Refresh();

    void PackVisibleItems()
    {
        RectTransform content=contentParent as RectTransform;if(content==null)return;
        ConfigureGridMetrics();
        float width=content.rect.width;if(width<=1f)width=InventoryArtLayout.InventoryBounds.width*InventoryArtLayout.Width;
        float cell=Mathf.Max(18f,(width-GridSpacing.x*(GridColumnCount-1))/GridColumnCount);
        var occupied=new List<bool[]>();int maxRow=1;
        foreach(var gear in Inventory.Instance.Items)
        {
            if(gear==null||!slots.TryGetValue(gear,out var slot)||slot==null)continue;
            bool visible=typeFilters==null||typeFilters.IsVisible(gear.ItemType);slot.gameObject.SetActive(visible);if(!visible)continue;
            Vector2Int size=EquipmentSlotRules.InventorySize(gear.ItemType);int px=0,py=0;bool placed=false;
            for(int y=0;!placed;y++)
            {
                while(occupied.Count<y+size.y)occupied.Add(new bool[GridColumnCount]);
                for(int x=0;x<=GridColumnCount-size.x;x++)
                {
                    bool free=true;for(int yy=y;yy<y+size.y&&free;yy++)for(int xx=x;xx<x+size.x;xx++)if(occupied[yy][xx]){free=false;break;}
                    if(!free)continue;px=x;py=y;placed=true;for(int yy=y;yy<y+size.y;yy++)for(int xx=x;xx<x+size.x;xx++)occupied[yy][xx]=true;break;
                }
            }
            RectTransform rect=(RectTransform)slot.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(px*(cell+GridSpacing.x),-py*(cell+GridSpacing.y));
            rect.sizeDelta=new Vector2(size.x*cell+(size.x-1)*GridSpacing.x,size.y*cell+(size.y-1)*GridSpacing.y);
            maxRow=Mathf.Max(maxRow,py+size.y);slot.transform.SetAsLastSibling();
        }
        float height=maxRow*cell+(maxRow-1)*GridSpacing.y;content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        RefreshGridBackdrop(maxRow,cell);
    }

    void RefreshGridBackdrop(int rows,float cell)
    {
        if(authoredView==null||authoredView.gridCellSprite==null)return;
        if(gridBackdrop==null)
        {
            var go=new GameObject("Inventory grid cells",typeof(RectTransform));go.transform.SetParent(contentParent,false);gridBackdrop=(RectTransform)go.transform;
            gridBackdrop.anchorMin=gridBackdrop.anchorMax=new Vector2(0,1);gridBackdrop.pivot=new Vector2(0,1);gridBackdrop.anchoredPosition=Vector2.zero;gridBackdrop.SetAsFirstSibling();
        }
        int needed=rows*GridColumnCount;
        while(gridCells.Count<needed){var go=new GameObject("Grid cell",typeof(RectTransform),typeof(Image));go.transform.SetParent(gridBackdrop,false);var image=go.GetComponent<Image>();image.raycastTarget=false;gridCells.Add(image);}
        for(int i=0;i<gridCells.Count;i++)
        {
            bool active=i<needed;gridCells[i].gameObject.SetActive(active);if(!active)continue;
            gridCells[i].sprite=authoredView.gridCellSprite;gridCells[i].type=Image.Type.Sliced;RectTransform r=gridCells[i].rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(cell,cell);r.anchoredPosition=new Vector2((i%GridColumnCount)*(cell+GridSpacing.x),-(i/GridColumnCount)*(cell+GridSpacing.y));
        }
    }

    public void RefreshModHighlights()
    {
        AdvancedLootFilter filter = Inventory.Instance != null ? Inventory.Instance.AdvancedFilter : null;
        foreach (var pair in slots)
        {
            if (pair.Value == null) continue;
            pair.Value.SetModHighlight(filter != null && filter.Highlights(pair.Key), 0);
        }
    }

    private int CurrentCorruptionStage()
    {
        if (zone == null) zone = FindAnyObjectByType<ZoneManager>();
        return zone != null ? ZoneManager.ForestBackgroundIndex(zone.zoneLevel) : 0;
    }

    private void RefreshAreaCorruptionBorders(int stage)
    {
        displayedCorruptionStage = stage;
        foreach (var pair in slots)
            if (pair.Value != null) pair.Value.SetAreaCorruption(stage);
    }
    private bool Contains(Gear item)
    { foreach(var gear in Inventory.Instance.Items) if(gear == item) return true; return false; }
    private void OnDestroy() { if(Tooltip != null) Destroy(Tooltip.gameObject); }
}
