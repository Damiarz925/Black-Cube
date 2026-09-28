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
    }
    [SerializeField] GameObject panel;
    [SerializeField] Button open, master, scope, logic, minimum;
    [SerializeField] List<Button> rarities = new(), weapons = new(), elements = new();
    [SerializeField] List<ModRow> rows = new();
    int scopeIndex;
    static readonly Element[] Elements = {Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Void};
    static readonly LootManager.GearType[] Slots = {LootManager.GearType.Helmets,LootManager.GearType.BodyArmours,LootManager.GearType.Gloves,LootManager.GearType.Boots,LootManager.GearType.Rings,LootManager.GearType.Amulets,LootManager.GearType.Belts};
    AdvancedLootFilter Policy => Inventory.Instance?.AdvancedFilter;
    ItemTypeLootFilter Current => scopeIndex < Slots.Length ? Policy.For(Slots[scopeIndex]) : Policy.For(LootManager.GearType.Weapons,AdvancedLootFilter.Weapons[scopeIndex-Slots.Length]);
    void Start()
    {
        // One authored row template controls every generated legal-mod row.
        // Clone once on opening, never rebuild geometry during normal refresh.
        if(rows.Count==1)
        {
            var template=rows[0];
            foreach(var stat in InventoryModFilter.SelectableStats.Where(s=>s!=template.stat))
            {
                var clone=Instantiate(template.root,template.root.transform.parent);clone.name=stat.ToString();var controls=clone.GetComponentsInChildren<Button>(true);
                rows.Add(new ModRow{stat=stat,root=clone,selected=controls[0],tier=controls[1],implicitOnly=controls[2],implicitTier=controls[3]});
            }
        }
        Wire(open,()=>{panel.SetActive(!panel.activeSelf);Refresh();});
        Wire(master,()=>{Policy.enabled=!Policy.enabled;Changed();});
        Wire(scope,()=>{scopeIndex=(scopeIndex+1)%(Slots.Length+6);Refresh();});
        Wire(logic,()=>{Current.all=!Current.all;Current.enabled=true;Changed();});
        Wire(minimum,()=>{Current.minimumMatches=Current.minimumMatches%Math.Max(1,Current.requirements.Count)+1;Changed();});
        for(int i=0;i<rarities.Count;i++){int k=i;Wire(rarities[i],()=>{Policy.keptRarities^=1<<k;Changed();});}
        for(int i=0;i<weapons.Count;i++){int k=i;Wire(weapons[i],()=>{Policy.keptWeapons^=1<<k;Changed();});}
        for(int i=0;i<elements.Count;i++){int k=(int)Elements[i];Wire(elements[i],()=>{Policy.keptElements^=1<<k;Changed();});}
        foreach(var row in rows)
        {
            var r=row;
            Wire(r.selected,()=>{var requirement=Current.requirements.Find(x=>x.stat==r.stat);if(requirement==null)Current.requirements.Add(new LootModRequirement{stat=r.stat});else Current.requirements.Remove(requirement);Current.enabled=true;Changed();});
            Wire(r.tier,()=>{var requirement=Require(r.stat);requirement.minimumTier=NextTier(requirement.minimumTier);Changed();});
            Wire(r.implicitOnly,()=>{var requirement=Require(r.stat);requirement.requireImplicit=!requirement.requireImplicit;Changed();});
            Wire(r.implicitTier,()=>{var requirement=Require(r.stat);requirement.minimumImplicitTier=NextTier(requirement.minimumImplicitTier);Changed();});
        }
        if(panel!=null)panel.SetActive(false);
    }
    static int NextTier(int tier) => tier >= 99 ? 1 : tier >= 15 ? 99 : tier+1;
    LootModRequirement Require(StatTypes stat){var r=Current.requirements.Find(x=>x.stat==stat);if(r==null){r=new LootModRequirement{stat=stat};Current.requirements.Add(r);}Current.enabled=true;return r;}
    void Changed(){Policy.Save();Refresh();}
    static void Wire(Button b,UnityEngine.Events.UnityAction action){if(b==null)return;b.onClick.RemoveAllListeners();b.onClick.AddListener(action);}
    static void Label(Button button,string value){if(button!=null)button.GetComponentInChildren<TMP_Text>().text=value;}
    void Refresh()
    {
        if(Policy==null||panel==null)return;
        Label(master,"ADVANCED PICKUP POLICY: "+(Policy.enabled?"ON":"OFF"));
        Label(scope,"ITEM TYPE: "+(scopeIndex<Slots.Length?ItemSlotUI.DisplayType(Slots[scopeIndex]):WeaponTypeCatalog.Get(Current.weaponTypeId).DisplayName)+"  ›");
        Label(logic,"MOD LOGIC: "+(Current.all?"ALL":"ANY"));Label(minimum,"MINIMUM MATCHES: "+Current.minimumMatches);
        for(int i=0;i<rarities.Count;i++)Label(rarities[i],((Policy.keptRarities&(1<<i))!=0?"KEEP ":"DISCARD ")+((LootManager.GearRarity)i));
        for(int i=0;i<weapons.Count;i++)Label(weapons[i],((Policy.keptWeapons&(1<<i))!=0?"✓ ":"× ")+WeaponTypeCatalog.Get(AdvancedLootFilter.Weapons[i]).DisplayName);
        for(int i=0;i<elements.Count;i++)Label(elements[i],((Policy.keptElements&(1<<(int)Elements[i]))!=0?"✓ ":"× ")+ItemTooltipUI.ElementName(Elements[i]));
        var legal=AdvancedLootFilter.LegalStats(Current.itemType,Current.weaponTypeId);
        foreach(var r in rows)
        {
            r.root.SetActive(legal.Contains(r.stat));
            var requirement=Current.requirements.Find(x=>x.stat==r.stat);
            Label(r.selected,(requirement!=null?"✓ ":"+ ")+StatDisplayFormatting.ToFriendlyName(r.stat));
            Label(r.tier,"Explicit ≥ "+Tier(requirement?.minimumTier??99));
            Label(r.implicitOnly,requirement?.requireImplicit==true?"IMPLICIT REQUIRED":"EXPLICIT");
            Label(r.implicitTier,"Implicit ≥ "+Tier(requirement?.minimumImplicitTier??99));
        }
    }
    static string Tier(int value)=>value>=99?"ANY":$"T{value}";
#if UNITY_EDITOR
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
    public void Close(){if(panel!=null)panel.SetActive(false);}
    void OnDisable()=>Close();
}
