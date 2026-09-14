// Developer map: Permanent rebirth relics, four active slots, Ancient crafting and atomic rebirth transaction.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public enum RelicModifierType { MoreDamage, IncreasedExperience, MoreAttackSpeed }

[Serializable]
public sealed class RelicModifier
{
    public RelicModifierType type;
    public float value;
    public bool lockedOriginal;
    public RelicModifier(RelicModifierType type, float value, bool locked) { this.type=type; this.value=value; lockedOriginal=locked; }
}

[Serializable]
public sealed class RelicData
{
    public string id;
    public int cycle;
    public LootManager.GearRarity rarity;
    public bool craftableThisCycle;
    public List<RelicModifier> modifiers = new();
    public int ModifierCount => modifiers?.Count ?? 0;
}

public static class RelicRolls
{
    // Conservative initial ranges; keep all tuning centralized here.
    public static RelicModifier Roll(bool locked, ISet<RelicModifierType> excluded = null)
    {
        var choices=new List<RelicModifierType>();
        foreach(RelicModifierType type in Enum.GetValues(typeof(RelicModifierType))) if(excluded==null||!excluded.Contains(type)) choices.Add(type);
        if(choices.Count==0) foreach(RelicModifierType type in Enum.GetValues(typeof(RelicModifierType))) choices.Add(type);
        var chosen=choices[UnityEngine.Random.Range(0,choices.Count)];
        float value=chosen switch
        {
            RelicModifierType.MoreDamage => UnityEngine.Random.Range(5f,10f),
            RelicModifierType.IncreasedExperience => UnityEngine.Random.Range(5f,10f),
            _ => UnityEngine.Random.Range(3f,6f)
        };
        return new RelicModifier(chosen,value,locked);
    }
}

public sealed class RelicInventory : MonoBehaviour
{
    public static RelicInventory Instance { get; private set; }
    [SerializeField] List<RelicData> relics=new();
    [SerializeField] int[] activeIndices={-1,-1,-1,-1};
    [SerializeField] int currentCycle;
    public event Action Changed;
    public IReadOnlyList<RelicData> Relics=>relics;
    public int CurrentCycle=>currentCycle;
    public const int ActiveSlotCount=4;

    void Awake(){if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;DontDestroyOnLoad(gameObject);NormalizeSlots();}
    void OnDestroy(){if(Instance==this)Instance=null;}
    void NormalizeSlots(){var migrated=new int[ActiveSlotCount];for(int i=0;i<migrated.Length;i++)migrated[i]=-1;if(activeIndices!=null)Array.Copy(activeIndices,migrated,Mathf.Min(activeIndices.Length,migrated.Length));activeIndices=migrated;}
    public RelicData Active(int slot)=>slot>=0&&slot<ActiveSlotCount&&activeIndices[slot]>=0&&activeIndices[slot]<relics.Count?relics[activeIndices[slot]]:null;
    public bool Equip(RelicData relic,int slot)
    {
        if(relic==null||slot<0||slot>=ActiveSlotCount)return false;int index=relics.IndexOf(relic);if(index<0)return false;
        for(int i=0;i<ActiveSlotCount;i++)if(activeIndices[i]==index)activeIndices[i]=-1;
        activeIndices[slot]=index;PublishChanged();return true;
    }
    public void Unequip(int slot){if(slot>=0&&slot<ActiveSlotCount&&activeIndices[slot]!=-1){activeIndices[slot]=-1;PublishChanged();}}
    public RelicData BeginNewCycle()
    {
        foreach(var relic in relics) relic.craftableThisCycle=false;
        currentCycle++;
        var created=new RelicData{id=Guid.NewGuid().ToString("N"),cycle=currentCycle,rarity=LootManager.GearRarity.Normal,craftableThisCycle=true};
        created.modifiers.Add(RelicRolls.Roll(true));relics.Add(created);PublishChanged();return created;
    }
    public bool IsCurrentCraftable(RelicData relic)=>relic!=null&&relic.craftableThisCycle&&relic.cycle==currentCycle&&relics.Contains(relic);
    public float DamageMultiplier=>Product(RelicModifierType.MoreDamage);
    public float AttackSpeedMultiplier=>Product(RelicModifierType.MoreAttackSpeed);
    public float ExperienceMultiplier=>1f+Sum(RelicModifierType.IncreasedExperience)/100f;
    float Product(RelicModifierType type){float value=1f;foreach(var relic in ActiveRelics())foreach(var mod in relic.modifiers)if(mod.type==type)value*=1f+mod.value/100f;return value;}
    float Sum(RelicModifierType type){float value=0f;foreach(var relic in ActiveRelics())foreach(var mod in relic.modifiers)if(mod.type==type)value+=mod.value;return value;}
    IEnumerable<RelicData> ActiveRelics(){for(int i=0;i<ActiveSlotCount;i++){var relic=Active(i);if(relic!=null)yield return relic;}}
    public void Restore(List<RelicData> saved,int cycle,int[] slots)
    {
        relics=saved??new List<RelicData>();currentCycle=Mathf.Max(0,cycle);activeIndices=slots??new[]{-1,-1};NormalizeSlots();PublishChanged();
    }
    public void ResetForNewGame()=>Restore(new List<RelicData>(),0,new[]{-1,-1,-1,-1});
    public int[] CopyActiveIndices()=>(int[])activeIndices.Clone();
    public void NotifyChanged()=>PublishChanged();
    void PublishChanged(){Changed?.Invoke();FindAnyObjectByType<PlayerController>()?.NotifyRelicChanged();GamePersistence.MarkDirty();}
}

public static class AncientRelicCrafting
{
    public static int Minimum(LootManager.GearRarity rarity)=>rarity switch{LootManager.GearRarity.Normal=>1,LootManager.GearRarity.Magic=>2,LootManager.GearRarity.Rare=>3,_=>5};
    public static int Maximum(LootManager.GearRarity rarity)=>rarity switch{LootManager.GearRarity.Normal=>1,LootManager.GearRarity.Magic=>2,LootManager.GearRarity.Rare=>4,_=>6};
    public static bool CanApply(CraftingCurrencyType currency,RelicData relic,RelicInventory inventory)
    {
        if(inventory==null||!CurrencyInventory.IsAncient(currency)||!inventory.IsCurrentCraftable(relic))return false;
        int count=relic.ModifierCount;
        return currency switch
        {
            CraftingCurrencyType.AncientNormalToMagic=>relic.rarity==LootManager.GearRarity.Normal&&count==1,
            CraftingCurrencyType.AncientMagicToRare=>relic.rarity==LootManager.GearRarity.Magic&&count==2,
            CraftingCurrencyType.AncientRareToLegendary=>relic.rarity==LootManager.GearRarity.Rare&&count>=3&&count<=4,
            CraftingCurrencyType.AncientReroll=>HasUnlocked(relic),
            CraftingCurrencyType.AncientAddModifier=>count<Maximum(relic.rarity),
            CraftingCurrencyType.AncientRemoveModifier=>count>Minimum(relic.rarity)&&HasUnlocked(relic),
            _=>false
        };
    }
    public static bool TryApply(CraftingCurrencyType currency,RelicData relic,RelicInventory inventory,bool notify=true)
    {
        if(!CanApply(currency,relic,inventory))return false;
        switch(currency)
        {
            case CraftingCurrencyType.AncientNormalToMagic: relic.rarity=LootManager.GearRarity.Magic;FillToMinimum(relic);break;
            case CraftingCurrencyType.AncientMagicToRare: relic.rarity=LootManager.GearRarity.Rare;FillToMinimum(relic);break;
            case CraftingCurrencyType.AncientRareToLegendary: relic.rarity=LootManager.GearRarity.Legendary;FillToMinimum(relic);break;
            case CraftingCurrencyType.AncientAddModifier:Add(relic);break;
            case CraftingCurrencyType.AncientReroll:
                var reroll=Unlocked(relic);int ri=UnityEngine.Random.Range(0,reroll.Count);int index=relic.modifiers.IndexOf(reroll[ri]);relic.modifiers[index]=RollFor(relic,false);break;
            case CraftingCurrencyType.AncientRemoveModifier:
                var remove=Unlocked(relic);relic.modifiers.Remove(remove[UnityEngine.Random.Range(0,remove.Count)]);break;
            default:return false;
        }
        if(notify)inventory.NotifyChanged();return true;
    }
    static void FillToMinimum(RelicData relic){while(relic.ModifierCount<Minimum(relic.rarity))Add(relic);}
    static void Add(RelicData relic)=>relic.modifiers.Add(RollFor(relic,false));
    static RelicModifier RollFor(RelicData relic,bool locked){var used=new HashSet<RelicModifierType>();foreach(var mod in relic.modifiers)used.Add(mod.type);return RelicRolls.Roll(locked,used);}
    static bool HasUnlocked(RelicData relic)=>Unlocked(relic).Count>0;
    static List<RelicModifier> Unlocked(RelicData relic){var result=new List<RelicModifier>();foreach(var mod in relic.modifiers)if(mod!=null&&!mod.lockedOriginal)result.Add(mod);return result;}
}

public sealed class RebirthManager : MonoBehaviour
{
    public const int RequiredLevel=50;
    public static RebirthManager Instance{get;private set;}
    public bool ConfirmationPending{get;private set;}
    public bool Eligible=>GetComponent<PlayerProgression>()?.Level>=RequiredLevel;
    void Awake(){if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;}
    void OnDestroy(){if(Instance==this)Instance=null;}
    public bool RequestRebirth(){if(!Eligible)return false;ConfirmationPending=true;return true;}
    public void Cancel(){ConfirmationPending=false;}
    public bool ConfirmRebirth()
    {
        if(!ConfirmationPending||!Eligible)return false;ConfirmationPending=false;
        EquipmentManager.Instance?.ResetForRebirth();
        Inventory.Instance?.ResetForRebirth();
        CurrencyInventory.Instance?.Restore(Array.Empty<CurrencyStackData>());
        var relics=GetComponent<RelicInventory>();if(relics==null)relics=gameObject.AddComponent<RelicInventory>();
        relics.BeginNewCycle();
        foreach(CraftingCurrencyType type in Enum.GetValues(typeof(CraftingCurrencyType)))if(CurrencyInventory.IsAncient(type))CurrencyInventory.Instance?.Add(type);
        var player=FindAnyObjectByType<PlayerController>();
        if(player!=null){player.GetComponent<StatusController>()?.ClearStatuses();player.GetComponent<HealthComponent>()?.ReviveToFullLife();player.GetComponent<ManaComponent>()?.RestoreFull();player.ResetToStarterWeapon();}
        GetComponent<GameManager>()?.StartNewRun();
        GamePersistence.Save();
        return true;
    }
}

/// <summary>Runtime-built four-slot relic strip for the player equipment/stats screen.</summary>
public sealed class RelicEquipmentUI : MonoBehaviour
{
    readonly List<ActiveRelicSlotUI> slots=new();
    public void Build()
    {
        if(slots.Count>0)return;
        var root=new GameObject("Active Relics",typeof(RectTransform));root.transform.SetParent(transform,false);
        var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        for(int i=0;i<RelicInventory.ActiveSlotCount;i++)
        {
            var go=new GameObject("Active Relic Slot "+(i+1),typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(Button),typeof(ActiveRelicSlotUI));go.transform.SetParent(root.transform,false);go.GetComponent<Image>().color=new Color(1,1,1,.001f);
            InventoryArtLayout.Apply((RectTransform)go.transform,InventoryArtLayout.RelicSlots[i]);
            var labelGo=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));labelGo.transform.SetParent(go.transform,false);var lr=(RectTransform)labelGo.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(5,5);lr.offsetMax=new Vector2(-5,-5);
            var label=labelGo.GetComponent<TextMeshProUGUI>();label.alignment=TextAlignmentOptions.Center;label.fontSize=11;label.raycastTarget=false;
            var slot=go.GetComponent<ActiveRelicSlotUI>();slot.Initialize(i,label);go.GetComponent<Button>().onClick.AddListener(slot.Activate);slots.Add(slot);
        }
    }
    void OnEnable(){Build();if(RelicInventory.Instance!=null)RelicInventory.Instance.Changed+=Refresh;Refresh();}
    void OnDisable(){if(RelicInventory.Instance!=null)RelicInventory.Instance.Changed-=Refresh;}
    void Refresh(){foreach(var slot in slots)slot.Refresh();}
}

public sealed class ActiveRelicSlotUI:MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler
{
    int slot;TMP_Text label;
    public void Initialize(int index,TMP_Text target){slot=index;label=target;Refresh();}
    public RelicData Item=>RelicInventory.Instance?.Active(slot);
    public string DisplayText=>label!=null?label.text:string.Empty;
    public void Refresh()
    {
        if(label==null)return;var relic=RelicInventory.Instance?.Active(slot);
        label.text=relic==null?"Empty":$"{relic.rarity}\n{relic.ModifierCount} MODS";
        var surface=GetComponent<Image>();if(surface!=null)surface.color=relic==null?new Color(1,1,1,.001f):new Color(.055f,.04f,.065f,1f);
    }
    public void OnPointerClick(PointerEventData data)
    {
        var inventory=RelicInventory.Instance;if(inventory==null)return;
        if(data.button==PointerEventData.InputButton.Right){inventory.Unequip(slot);return;}
        if(data.button!=PointerEventData.InputButton.Left||inventory.Relics.Count==0)return;
        var current=inventory.Active(slot);int index=current==null?-1:IndexOf(inventory,current);inventory.Equip(inventory.Relics[(index+1)%inventory.Relics.Count],slot);
    }
    public void Activate(){var inventory=RelicInventory.Instance;if(inventory==null||inventory.Relics.Count==0)return;var current=inventory.Active(slot);int index=current==null?-1:IndexOf(inventory,current);inventory.Equip(inventory.Relics[(index+1)%inventory.Relics.Count],slot);}
    public void OnPointerEnter(PointerEventData data){RelicTooltipUI.Show(RelicInventory.Instance?.Active(slot),(RectTransform)transform);}
    public void OnPointerExit(PointerEventData data){RelicTooltipUI.Hide();}
    static int IndexOf(RelicInventory inventory,RelicData relic){for(int i=0;i<inventory.Relics.Count;i++)if(ReferenceEquals(inventory.Relics[i],relic))return i;return -1;}
}

public sealed class RelicTooltipUI:MonoBehaviour
{
    static RelicTooltipUI instance;RelicData relic;RectTransform anchor;TMP_Text label;
    public static void Show(RelicData value,RectTransform source)
    {
        if(value==null||source==null)return;Canvas canvas=source.GetComponentInParent<Canvas>();if(canvas==null)return;
        if(instance==null){var go=new GameObject("Relic tooltip",typeof(RectTransform),typeof(Image),typeof(RelicTooltipUI));go.transform.SetParent(canvas.rootCanvas.transform,false);go.GetComponent<Image>().color=new Color(.025f,.02f,.035f,.99f);instance=go.GetComponent<RelicTooltipUI>();var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);var tr=(RectTransform)text.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(12,10);tr.offsetMax=new Vector2(-12,-10);instance.label=text.GetComponent<TextMeshProUGUI>();instance.label.fontSize=12;instance.label.textWrappingMode=TextWrappingModes.Normal;instance.label.raycastTarget=false;}
        instance.relic=value;instance.anchor=source;instance.gameObject.SetActive(true);instance.Refresh();var r=(RectTransform)instance.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(0,0);var corners=new Vector3[4];source.GetWorldCorners(corners);r.position=corners[2];instance.transform.SetAsLastSibling();
    }
    public static void Hide(){if(instance!=null)instance.gameObject.SetActive(false);}
    public static void RefreshVisible(){if(instance!=null&&instance.gameObject.activeSelf)instance.Refresh();}
    void OnEnable(){if(RelicInventory.Instance!=null)RelicInventory.Instance.Changed+=Refresh;}
    void OnDisable(){if(RelicInventory.Instance!=null)RelicInventory.Instance.Changed-=Refresh;}
    void OnDestroy(){if(instance==this)instance=null;}
    void Refresh(){if(relic==null||!ContainsRelic(relic)){Hide();return;}var inventorySlot=anchor!=null?anchor.GetComponent<RelicSlotUI>():null;var activeSlot=anchor!=null?anchor.GetComponent<ActiveRelicSlotUI>():null;if((inventorySlot!=null&&!ReferenceEquals(inventorySlot.Item,relic))||(activeSlot!=null&&!ReferenceEquals(activeSlot.Item,relic))){Hide();return;}if(anchor==null||!anchor.gameObject.activeInHierarchy){Hide();return;}label.text=ItemTooltipFormatter.DescribeRelic(relic);float height=Mathf.Clamp(label.GetPreferredValues(label.text,296,0).y+24,90,360);((RectTransform)transform).sizeDelta=new Vector2(320,height);LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);}
    static bool ContainsRelic(RelicData target){if(RelicInventory.Instance==null)return false;foreach(var item in RelicInventory.Instance.Relics)if(ReferenceEquals(item,target))return true;return false;}
}

public sealed class RebirthConfirmationUI:MonoBehaviour
{
    Button openButton;GameObject confirmation;TMP_Text openLabel;
    public void Build()
    {
        if(openButton!=null)return;
        openButton=Button(transform,"REBIRTH",new Vector2(.015f,.015f),new Vector2(.13f,.075f),Open,out openLabel);
        confirmation=new GameObject("Rebirth confirmation",typeof(RectTransform),typeof(Image));confirmation.transform.SetParent(transform,false);var rect=(RectTransform)confirmation.transform;rect.anchorMin=new Vector2(.3f,.3f);rect.anchorMax=new Vector2(.7f,.7f);rect.offsetMin=rect.offsetMax=Vector2.zero;confirmation.GetComponent<Image>().color=new Color(.03f,.025f,.04f,.98f);
        var message=Label(confirmation.transform,"REBIRTH?\nReset all run progress and ordinary items. Relics remain permanent.",15);Place(message.rectTransform,.06f,.42f,.94f,.94f);
        Button(confirmation.transform,"CONFIRM",new Vector2(.08f,.08f),new Vector2(.47f,.34f),Confirm,out _);
        Button(confirmation.transform,"CANCEL",new Vector2(.53f,.08f),new Vector2(.92f,.34f),Cancel,out _);confirmation.SetActive(false);
    }
    void Update(){Build();bool eligible=RebirthManager.Instance!=null&&RebirthManager.Instance.Eligible;openButton.gameObject.SetActive(eligible);if(openLabel!=null)openLabel.text=eligible?"REBIRTH / LEVEL 50+":"REBIRTH LOCKED";}
    void Open(){if(RebirthManager.Instance!=null&&RebirthManager.Instance.RequestRebirth())confirmation.SetActive(true);}
    void Confirm(){if(RebirthManager.Instance!=null&&RebirthManager.Instance.ConfirmRebirth())confirmation.SetActive(false);}
    void Cancel(){RebirthManager.Instance?.Cancel();confirmation.SetActive(false);}
    static Button Button(Transform parent,string name,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action,out TMP_Text label)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);Place((RectTransform)go.transform,min.x,min.y,max.x,max.y);go.GetComponent<Image>().color=new Color(.28f,.13f,.35f,.96f);var button=go.GetComponent<Button>();button.onClick.AddListener(action);label=Label(go.transform,name,12);Place(label.rectTransform,0,0,1,1);return button;}
    static TMP_Text Label(Transform parent,string value,int size){var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;}
    static void Place(RectTransform rect,float x0,float y0,float x1,float y1){rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;}
}
