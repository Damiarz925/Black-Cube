using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Static authored controls, dynamic content/state only. No per-frame layout changes.
public sealed class AdvancedLootFilterUI : MonoBehaviour
{
    [Serializable] public sealed class ModRow
    {
        public StatTypes stat;
        public GameObject root;
        public Button selected, tier, implicitOnly, implicitTier;
        public Slider tierSlider;
        public TMP_Text tierLabel;
    }
    [SerializeField] GameObject panel;
    [SerializeField] Button open, master, scope, logic, minimum;
    [SerializeField] List<Button> rarities = new(), weapons = new(), elements = new();
    [SerializeField] List<ModRow> rows = new();
    [SerializeField] TMP_Dropdown itemTypeDropdown;
    [SerializeField] Button autoDismantle,help;
    [SerializeField] Button legacyRules;
    [SerializeField] GameObject helpPanel;
    [SerializeField] Button clearSlot,clearAll;
    bool confirmClearAll;
    int scopeIndex;
    bool refreshing;
    static readonly Element[] Elements = {Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Void};
    static readonly LootManager.GearType[] Slots = {LootManager.GearType.Helmets,LootManager.GearType.BodyArmours,LootManager.GearType.Gloves,LootManager.GearType.Boots,LootManager.GearType.Rings,LootManager.GearType.Amulets,LootManager.GearType.Belts};
    AdvancedLootFilter Policy => Inventory.Instance?.AdvancedFilter;
    ItemTypeLootFilter Current => scopeIndex < Slots.Length ? Policy.For(Slots[scopeIndex]) : Policy.For(LootManager.GearType.Weapons,AdvancedLootFilter.Weapons[scopeIndex-Slots.Length]);
    void Start()
    {
        if(helpPanel!=null)
        {
            var legend=helpPanel.GetComponentInChildren<TMP_Text>(true);
            if(legend!=null)legend.text=legend.text.Replace("Highlighted buttons retain that rarity/type/element.",
                "Selected buttons retain that rarity/type/element. KEEP never highlights items; only selected modifier matches highlight inventory items.")
                .Replace("Explicit matches ordinary affixes; Implicit requires the permanent implicit.",
                "EITHER accepts an explicit OR permanent implicit roll. IMPLICIT ONLY requires the permanent implicit. SELECTED means the modifier is required; NOT SELECTED means it is ignored.");
        }
        if(helpPanel!=null){var legend=helpPanel.GetComponentInChildren<TMP_Text>(true);if(legend!=null)legend.text+="\n\nUniques always survive automatic filtering. CLEAR THIS SLOT clears only this item type's modifiers. CLEAR ALL MODIFIER FILTERS requires confirmation; rarity, type, element and auto-dismantle settings remain."; }
        // One authored row template controls every generated legal-mod row.
        // Clone once on opening, never rebuild geometry during normal refresh.
        if(rows.Count==1)
        {
            var template=rows[0];
            foreach(var stat in InventoryModFilter.SelectableStats.Where(s=>s!=template.stat))
            {
                var clone=Instantiate(template.root,template.root.transform.parent);clone.name=stat.ToString();var controls=clone.GetComponentsInChildren<Button>(true);
                rows.Add(new ModRow{stat=stat,root=clone,selected=controls[0],implicitOnly=controls[1],tierSlider=clone.GetComponentInChildren<Slider>(true),tierLabel=clone.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="Minimum Tier")});
            }
        }
        Wire(open,()=>{panel.SetActive(!panel.activeSelf);Refresh();});
        Wire(clearSlot,()=>{Current.requirements.Clear();confirmClearAll=false;Changed();});
        Wire(clearAll,()=>{if(!confirmClearAll){confirmClearAll=true;Label(clearAll,"CONFIRM CLEAR ALL MODIFIERS");return;}Policy.ClearAllModifiers();confirmClearAll=false;Changed();});
        Wire(master,()=>{Policy.enabled=!Policy.enabled;Changed();});
        if(itemTypeDropdown!=null)
        {
            itemTypeDropdown.ClearOptions();itemTypeDropdown.AddOptions(Slots.Select(ItemSlotUI.DisplayType).Concat(AdvancedLootFilter.Weapons.Select(w=>WeaponTypeCatalog.Get(w).DisplayName)).ToList());
            itemTypeDropdown.onValueChanged.AddListener(i=>{scopeIndex=i;confirmClearAll=false;Refresh();});
        }
        Wire(autoDismantle,()=>{Policy.autoDismantleFilteredItems=!Policy.autoDismantleFilteredItems;Changed();});
        Wire(help,()=>helpPanel.SetActive(!helpPanel.activeSelf));
        Wire(legacyRules,()=>{var inv=Inventory.Instance;inv.FilterLevelEnabled=false;inv.FilterRarityEnabled=false;inv.FilterModMismatchEnabled=false;Changed();});
        Wire(logic,()=>{Current.all=!Current.all;Current.enabled=true;Changed();});
        Wire(minimum,()=>{Current.minimumMatches=Current.minimumMatches%Math.Max(1,Current.requirements.Count)+1;Changed();});
        for(int i=0;i<rarities.Count;i++){int k=i;Wire(rarities[i],()=>{Policy.keptRarities^=1<<k;Changed();});}
        for(int i=0;i<weapons.Count;i++){int k=i;Wire(weapons[i],()=>{Policy.keptWeapons^=1<<k;Changed();});}
        for(int i=0;i<elements.Count;i++){int k=(int)Elements[i];Wire(elements[i],()=>{Policy.keptElements^=1<<k;Changed();});}
        foreach(var row in rows)
        {
            var r=row;
            Wire(r.selected,()=>{var requirement=Current.requirements.Find(x=>x.stat==r.stat);if(requirement==null)Current.requirements.Add(new LootModRequirement{stat=r.stat});else Current.requirements.Remove(requirement);Current.enabled=true;Changed();});

            Wire(r.implicitOnly,()=>{var requirement=Require(r.stat);requirement.requireImplicit=!requirement.requireImplicit;Changed();});
            if(r.tierSlider!=null)r.tierSlider.onValueChanged.AddListener(value=>
            {
                if(refreshing)return;
                var requirement=Require(r.stat);var tiers=LegalTiers(r.stat);int index=Mathf.Clamp(Mathf.RoundToInt(value),0,tiers.Length-1);
                if(requirement.requireImplicit)requirement.minimumImplicitTier=tiers[index];else requirement.minimumTier=tiers[index];Changed();
            });
        }
        if(panel!=null)panel.SetActive(false);
    }
    static int NextTier(int tier) => tier >= 99 ? 1 : tier >= 15 ? 99 : tier+1;
    LootModRequirement Require(StatTypes stat){var r=Current.requirements.Find(x=>x.stat==stat);if(r==null){r=new LootModRequirement{stat=stat};Current.requirements.Add(r);}Current.enabled=true;return r;}
    void Changed(){Policy.Save();GetComponent<InventoryUI>()?.RefreshModHighlights();Refresh();}
    static void Wire(Button b,UnityEngine.Events.UnityAction action){if(b==null)return;b.onClick.RemoveAllListeners();b.onClick.AddListener(action);}
    static void Label(Button button,string value){if(button!=null)button.GetComponentInChildren<TMP_Text>().text=value;}
    void Refresh()
    {
        if(Policy==null||panel==null)return;
        refreshing=true;
        var inventory=Inventory.Instance;
        if(legacyRules!=null)
        {
            bool legacy=inventory.FilterLevelEnabled||inventory.FilterRarityEnabled||inventory.FilterModMismatchEnabled;
            legacyRules.gameObject.SetActive(legacy);
            Label(legacyRules,"CLEAR SAVED LEGACY RULES: "+(inventory.FilterLevelEnabled?$"LEVEL ≤{inventory.FilterLevel} ":"")+(inventory.FilterRarityEnabled?$"RARITY ≤{inventory.FilterRarity} ":"")+(inventory.FilterModMismatchEnabled?"HIGHLIGHT MISMATCH":""));
        }
        Label(clearAll,confirmClearAll?"CONFIRM CLEAR ALL MODIFIERS":"CLEAR ALL MODIFIER FILTERS");
        Label(autoDismantle,"AUTO-DISMANTLE FILTERED ITEMS: "+(Policy.autoDismantleFilteredItems?"ON":"OFF"));
        Label(master,"ADVANCED PICKUP POLICY: "+(Policy.enabled?"ON":"OFF"));
        Label(scope,"ITEM TYPE: "+(scopeIndex<Slots.Length?ItemSlotUI.DisplayType(Slots[scopeIndex]):WeaponTypeCatalog.Get(Current.weaponTypeId).DisplayName)+"  ›");
        Label(logic,"MOD LOGIC: "+(Current.all?"ALL":"ANY"));Label(minimum,"MINIMUM MATCHES: "+Current.minimumMatches);
        for(int i=0;i<rarities.Count;i++)Label(rarities[i],((Policy.keptRarities&(1<<i))!=0?"KEEP ":"DISCARD ")+((LootManager.GearRarity)i));
        for(int i=0;i<weapons.Count;i++)Select(weapons[i],WeaponTypeCatalog.Get(AdvancedLootFilter.Weapons[i]).DisplayName,(Policy.keptWeapons&(1<<i))!=0);
        for(int i=0;i<elements.Count;i++)Select(elements[i],ItemTooltipUI.ElementName(Elements[i]),(Policy.keptElements&(1<<(int)Elements[i]))!=0);
        var legal=AdvancedLootFilter.LegalStats(Current.itemType,Current.weaponTypeId);
        foreach(var r in rows)
        {
            r.root.SetActive(legal.Contains(r.stat));
            var requirement=Current.requirements.Find(x=>x.stat==r.stat);
            Select(r.selected,(requirement!=null?"SELECTED: ":"NOT SELECTED: ")+StatDisplayFormatting.ToFriendlyName(r.stat),requirement!=null);

            Label(r.implicitOnly,requirement?.requireImplicit==true?"IMPLICIT ONLY":"EITHER");
            var tiers=LegalTiers(r.stat);int value=requirement?.requireImplicit==true?requirement.minimumImplicitTier:requirement?.minimumTier??99;
            if(r.tierSlider!=null){r.tierSlider.minValue=0;r.tierSlider.maxValue=tiers.Length-1;r.tierSlider.wholeNumbers=true;r.tierSlider.SetValueWithoutNotify(Mathf.Max(0,Array.IndexOf(tiers,value)));}
            if(r.tierLabel!=null)r.tierLabel.text="Minimum Tier: "+Tier(value);
        }
        refreshing=false;
    }
    static void Select(Button button,string name,bool selected)
    {Label(button,name);CorruptionUIButtonSkin.Ensure(button)?.SetSelected(selected);}
    int[] LegalTiers(StatTypes stat)
    {
        var tiers=ModManager.ApplicableTiers(ModManager.Instance?.Database?.GetDefinition(stat),Current.itemType,Current.weaponTypeId);
        return new[]{99}.Concat(tiers.Select(t=>t.tierIndex).Distinct().OrderByDescending(t=>t)).ToArray();
    }
    static string Tier(int value)=>value>=99?"ANY":$"T{value}";
#if UNITY_EDITOR
    public void UpgradeAuthoring()
    {
        if(panel==null)BuildAuthoring();
        if(rarities.Count<5){rarities.Add(MakeButton(panel.transform,"KEEP UNIQUE",new(.8f,.84f),new(.98f,.90f)));for(int i=0;i<rarities.Count;i++)Place((RectTransform)rarities[i].transform,new(.02f+i*.192f,.84f),new(.20f+i*.192f,.90f));}
        if(legacyRules==null)legacyRules=MakeButton(panel.transform,"CLEAR SAVED LEGACY RULES",new(.02f,.405f),new(.98f,.45f));
        var existingViewport=panel.transform.Find("Legal modifier viewport") as RectTransform;
        if(existingViewport!=null)Place(existingViewport,new(.02f,.02f),new(.98f,.34f));
        if(legacyRules!=null)Place((RectTransform)legacyRules.transform,new(.02f,.35f),new(.98f,.395f));
        if(clearSlot==null){clearSlot=MakeButton(panel.transform,"CLEAR THIS SLOT",new(.02f,.405f),new(.40f,.45f));clearAll=MakeButton(panel.transform,"CLEAR ALL MODIFIER FILTERS",new(.42f,.405f),new(.98f,.45f));if(legacyRules!=null)legacyRules.gameObject.SetActive(false);}
        if(itemTypeDropdown!=null)return;
        CompactAuthoring();
        if(scope!=null)scope.gameObject.SetActive(false);
        var dropdown=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        dropdown.name="Item Type Dropdown";dropdown.transform.SetParent(panel.transform,false);
        Place((RectTransform)dropdown.transform,new(.02f,.60f),new(.98f,.66f));itemTypeDropdown=dropdown.GetComponent<TMP_Dropdown>();
        foreach(var text in dropdown.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=13;text.raycastTarget=false;}
        autoDismantle=MakeButton(panel.transform,"AUTO-DISMANTLE FILTERED ITEMS",new(.02f,.46f),new(.78f,.51f));
        help=MakeButton(panel.transform,"HELP",new(.80f,.46f),new(.98f,.51f));
        var viewport=panel.transform.Find("Legal modifier viewport") as RectTransform;Place(viewport,new(.02f,.02f),new(.98f,.40f));
        var content=viewport.Find("Legal modifier rows");var layout=content.GetComponent<VerticalLayoutGroup>();layout.childControlWidth=true;layout.childForceExpandWidth=true;
        var row=rows[0];foreach(var control in new[]{row.selected,row.tier,row.implicitOnly,row.implicitTier})if(control!=null)DestroyImmediate(control.gameObject);
        row.root.GetComponent<LayoutElement>().preferredHeight=46;
        row.selected=MakeButton(row.root.transform,"Modifier Name",new(.18f,0),new(.65f,1));
        row.implicitOnly=MakeButton(row.root.transform,"Explicit",new(0,0),new(.17f,1));row.tier=row.implicitTier=null;
        var slider=DefaultControls.CreateSlider(new DefaultControls.Resources());slider.name="Minimum Tier Slider";slider.transform.SetParent(row.root.transform,false);
        Place((RectTransform)slider.transform,new(.68f,.05f),new(.98f,.45f));row.tierSlider=slider.GetComponent<Slider>();row.tierSlider.wholeNumbers=true;
        var label=new GameObject("Minimum Tier",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(row.root.transform,false);
        Place((RectTransform)label.transform,new(.65f,.50f),new(1,1));row.tierLabel=label.GetComponent<TextMeshProUGUI>();row.tierLabel.fontSize=12;row.tierLabel.alignment=TextAlignmentOptions.Center;row.tierLabel.raycastTarget=false;
        helpPanel=Box(panel.transform,"Advanced Loot Help");Place((RectTransform)helpPanel.transform,new(.04f,.06f),new(.96f,.88f));
        var body=new GameObject("Legend",typeof(RectTransform),typeof(TextMeshProUGUI));body.transform.SetParent(helpPanel.transform,false);
        Place((RectTransform)body.transform,new(.04f,.10f),new(.96f,.96f));var textBody=body.GetComponent<TextMeshProUGUI>();textBody.fontSize=15;textBody.enableAutoSizing=true;textBody.fontSizeMin=11;textBody.fontSizeMax=15;textBody.raycastTarget=false;
        textBody.text="ADVANCED LOOT\\n\\nRarity always takes precedence. Highlighted buttons retain that rarity/type/element.\\n\\nWeapon Type means the actual weapon class. Weapon Element means its BASE/PRIMARY element, not an affix. These only restrict weapons.\\n\\nItem Type selects a separate legal modifier policy. Click a modifier to require it. Explicit matches ordinary affixes; Implicit requires the permanent implicit. Minimum Tier accepts that tier or better; Any accepts every tier.\\n\\nALL requires every selected modifier. If more explicit mods are selected than the item's capacity, its full explicit set must belong to the selected pool. ANY requires Minimum Matches.\\n\\nAuto-Dismantle ON destroys rejected pickups for normal fragments. OFF retains them. Locked items always survive. Manual right-click dismantling remains available.";
        var close=MakeButton(helpPanel.transform,"CLOSE HELP",new(.3f,.01f),new(.7f,.08f));UnityEditor.Events.UnityEventTools.AddPersistentListener(close.onClick,CloseHelp);helpPanel.SetActive(false);
    }
    public void CompactAuthoring()
    {
        for(int i=rows.Count-1;i>0;i--){DestroyImmediate(rows[i].root);rows.RemoveAt(i);}
    }
    public void BuildAuthoring()
    {
        if(panel!=null)return;
        open=MakeButton(transform,"ADVANCED LOOT",new Vector2(.4f,.91f),new Vector2(.65f,.96f));
        panel=Box(transform,"Advanced Loot Filter");Place((RectTransform)panel.transform,new Vector2(.02f,.08f),new Vector2(.98f,.88f));
        master=MakeButton(panel.transform,"",new(.02f,.92f),new(.72f,.98f));
        var close=MakeButton(panel.transform,"CLOSE",new(.75f,.92f),new(.98f,.98f));
        // Persistent UnityEvent is editor-owned, all remaining controls are wired at Start.
        UnityEditor.Events.UnityEventTools.AddPersistentListener(close.onClick,Close);
        for(int i=0;i<4;i++)rarities.Add(MakeButton(panel.transform,"",new(.02f+i*.24f,.84f),new(.24f+i*.24f,.90f)));
        for(int i=0;i<6;i++)weapons.Add(MakeButton(panel.transform,"",new(.02f+i*.16f,.76f),new(.17f+i*.16f,.82f)));
        for(int i=0;i<5;i++)elements.Add(MakeButton(panel.transform,"",new(.02f+i*.192f,.68f),new(.20f+i*.192f,.74f)));
        scope=MakeButton(panel.transform,"",new(.02f,.60f),new(.98f,.66f));
        logic=MakeButton(panel.transform,"",new(.02f,.52f),new(.47f,.58f));minimum=MakeButton(panel.transform,"",new(.5f,.52f),new(.98f,.58f));
        var viewport=Box(panel.transform,"Legal modifier viewport");Place((RectTransform)viewport.transform,new(.02f,.02f),new(.98f,.50f));viewport.AddComponent<RectMask2D>();
        var content=Box(viewport.transform,"Legal modifier rows");var rect=(RectTransform)content.transform;rect.anchorMin=new(0,1);rect.anchorMax=Vector2.one;rect.pivot=new(.5f,1);rect.sizeDelta=Vector2.zero;
        content.GetComponent<Image>().color=Color.clear;
        var layout=content.AddComponent<VerticalLayoutGroup>();layout.spacing=5;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        content.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.AddComponent<ScrollRect>();scroll.content=rect;scroll.viewport=(RectTransform)viewport.transform;scroll.horizontal=false;scroll.scrollSensitivity=35;
        foreach(var stat in InventoryModFilter.SelectableStats.Take(1))
        {
            var row=new ModRow{stat=stat,root=Box(content.transform,stat.ToString())};row.root.AddComponent<LayoutElement>().preferredHeight=56;
            row.selected=MakeButton(row.root.transform,"",new(0,.5f),new(.55f,1));row.tier=MakeButton(row.root.transform,"",new(.56f,.5f),new(1,1));
            row.implicitOnly=MakeButton(row.root.transform,"",new(0,0),new(.55f,.48f));row.implicitTier=MakeButton(row.root.transform,"",new(.56f,0),new(1,.48f));rows.Add(row);
        }
        panel.SetActive(false);
    }
    static GameObject Box(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=new(.025f,.035f,.045f,.99f);return go;}
    static void Place(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
    static Button MakeButton(Transform parent,string label,Vector2 min,Vector2 max)
    {
        var go=Box(parent,label);Place((RectTransform)go.transform,min,max);var b=go.AddComponent<Button>();b.targetGraphic=go.GetComponent<Image>();
        var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);Place((RectTransform)text.transform,Vector2.zero,Vector2.one);
        var t=text.GetComponent<TextMeshProUGUI>();t.text=label;t.fontSize=12;t.enableAutoSizing=true;t.fontSizeMin=9;t.fontSizeMax=12;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return b;
    }
#endif
    public void CloseHelp(){if(helpPanel!=null)helpPanel.SetActive(false);}
    public void Close(){CloseHelp();if(panel!=null)panel.SetActive(false);}
    void OnDisable()=>Close();
}
