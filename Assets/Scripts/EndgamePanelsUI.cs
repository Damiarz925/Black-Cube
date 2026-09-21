// Functional production access for challenge encounters and the three endgame crafts.
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

static class EndgameUIFactory
{
    public static GameObject Panel(Transform parent,string name)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=new Vector2(.12f,.08f);r.anchorMax=new Vector2(.88f,.92f);r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<Image>().color=new Color(.025f,.03f,.04f,.985f);return go;}
    public static TMP_Text Text(Transform parent,string value,Vector2 min,Vector2 max,float size=14)
    {var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=new Vector2(12,8);r.offsetMax=new Vector2(-12,-8);var t=go.GetComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.color=Color.white;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;return t;}
    public static Button Button(Transform parent,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {var go=new GameObject(value,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=new Vector2(4,4);r.offsetMax=new Vector2(-4,-4);go.GetComponent<Image>().color=new Color(.16f,.20f,.24f,.98f);var b=go.GetComponent<Button>();b.onClick.AddListener(action);var t=Text(go.transform,value,Vector2.zero,Vector2.one,12);t.alignment=TextAlignmentOptions.Center;CorruptionUIButtonSkin.Ensure(b);return b;}
}

public sealed partial class ChallengeLauncherUI:MonoBehaviour
{
    [SerializeField] ChallengeView authoredView;GameObject root;TMP_Text body;Button open,closeButton;readonly List<Button> entries=new();bool bound;
    public bool IsOpen=>root!=null&&root.activeSelf;
    public void Build()
    {
        if(bound)return;if(authoredView==null)authoredView=GetComponent<ChallengeView>();if(authoredView==null){Debug.LogError("ChallengeLauncherUI requires an authored ChallengeView.",this);return;}root=authoredView.panel;body=authoredView.body;open=authoredView.openButton;closeButton=authoredView.closeButton;entries.Clear();entries.AddRange(authoredView.entries);Wire(open,Open);Wire(closeButton,Close);for(int i=0;i<entries.Count;i++){int index=i;Wire(entries[i],()=>Enter(index));}bound=true;
    }
    static void Wire(Button button,UnityEngine.Events.UnityAction action){if(button==null)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}
#if UNITY_EDITOR
    public void BuildAuthoring()
    {
        if(root!=null)return;open=EndgameUIFactory.Button(transform,"CHALLENGES",new Vector2(.135f,.015f),new Vector2(.255f,.075f),Open);BottomActionBarLayout.Attach(open,11,190);
        Canvas canvas=GetComponentInParent<Canvas>();root=EndgameUIFactory.Panel(canvas.rootCanvas.transform,"Challenge Launcher");EndgameUIFactory.Text(root.transform,"CHALLENGE BOSSES",new Vector2(.05f,.89f),new Vector2(.95f,.98f),28).alignment=TextAlignmentOptions.Center;
        body=EndgameUIFactory.Text(root.transform,string.Empty,new Vector2(.05f,.18f),new Vector2(.95f,.87f),13);
        for(int i=0;i<6;i++){int index=i;float x=.05f+i*.15f;entries.Add(EndgameUIFactory.Button(root.transform,$"ENTER {i+1}",new Vector2(x,.09f),new Vector2(x+.14f,.16f),()=>Enter(index)));}
        closeButton=EndgameUIFactory.Button(root.transform,"CLOSE",new Vector2(.40f,.01f),new Vector2(.60f,.075f),Close);root.SetActive(false);authoredView=GetComponent<ChallengeView>()??gameObject.AddComponent<ChallengeView>();authoredView.panel=root;authoredView.body=body;authoredView.openButton=open;authoredView.closeButton=closeButton;authoredView.entries.Clear();authoredView.entries.AddRange(entries);
    }
#endif
    void Update(){Build();var identity=GameManager.Instance?.GetComponent<PlayerIdentityState>();if(open!=null)open.interactable=identity!=null&&identity.SubclassChoiceUnlocked;if(IsOpen)Refresh();}
    public void Open(){Build();Refresh();root.SetActive(true);root.transform.SetAsLastSibling();Time.timeScale=0;}
    public void Close(){if(root!=null)root.SetActive(false);if(ChallengeRuntimeService.Instance?.IsActive!=true)Time.timeScale=1;}
    void Enter(int index){var list=WorldContentCatalog.Reference?.challengeEncounters;if(list==null||index>=list.Count)return;if(ChallengeRuntimeService.Instance?.TryLaunch(list[index])==true)Close();else Refresh();}
    void Refresh()
    {
        var db=WorldContentCatalog.Reference;var ledger=EndgameResourceLedger.Instance;var text=new StringBuilder();
        if(db?.challengeEncounters==null)return;for(int i=0;i<db.challengeEncounters.Count;i++)
        {var c=db.challengeEncounters[i];bool unlocked=ChallengeRuntimeService.Instance?.IsUnlocked(c)==true;int keys=ledger?.Count(c.entryResourceId)??0;var boss=db.Boss(c.bossId);
            text.AppendLine($"<b>{i+1}. {c.displayName}</b> — {ProductionWorldContent.BiomeIds[i]} — {(unlocked?"UNLOCKED":"LOCKED")}");
            text.AppendLine($"Combat {c.minimumCombatLevel}+ • Keys {keys} • Cost {c.entryResourceAmount} • Essence {c.rewardResourceId}\nTheme: {c.specialAffixPoolId}\n");
            entries[i].interactable=unlocked&&keys>=c.entryResourceAmount&&!ChallengeRuntimeService.Instance.IsActive;}
        body.text=text.ToString();
    }
}

public sealed partial class EndgameItemizationUI:MonoBehaviour
{
    enum Mode{Empowerment,Infusion,Implicit}
    [SerializeField] EndgameCraftingView authoredView;GameObject root;TMP_Text body;Button open,apply;Mode mode;int itemIndex,modIndex,poolIndex;Gear selected;RolledMod target;bool bound;
    public bool IsOpen=>root!=null&&root.activeSelf;
    public void Build()
    {
        if(bound)return;if(authoredView==null)authoredView=GetComponent<EndgameCraftingView>();if(authoredView==null){Debug.LogError("EndgameItemizationUI requires an authored EndgameCraftingView.",this);return;}root=authoredView.panel;body=authoredView.body;open=authoredView.openButton;apply=authoredView.applyButton;Wire(open,Open);Wire(apply,Apply);Wire(authoredView.nextItemButton,()=>{itemIndex++;modIndex=0;Refresh();});Wire(authoredView.nextModButton,()=>{modIndex++;Refresh();});Wire(authoredView.nextPoolButton,()=>{poolIndex++;Refresh();});Wire(authoredView.closeButton,Close);for(int i=0;i<authoredView.modeButtons.Count&&i<3;i++){Mode value=(Mode)i;Wire(authoredView.modeButtons[i],()=>SetMode(value));}bound=true;
    }
    static void Wire(Button button,UnityEngine.Events.UnityAction action){if(button==null)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}
#if UNITY_EDITOR
    public void BuildAuthoring()
    {
        if(root!=null)return;open=EndgameUIFactory.Button(transform,"ENDGAME CRAFTING",new Vector2(.26f,.015f),new Vector2(.405f,.075f),Open);BottomActionBarLayout.Attach(open,12,230);
        Canvas canvas=GetComponentInParent<Canvas>();root=EndgameUIFactory.Panel(canvas.rootCanvas.transform,"Endgame Crafting");EndgameUIFactory.Text(root.transform,"ENDGAME ITEMIZATION",new Vector2(.05f,.89f),new Vector2(.95f,.98f),28).alignment=TextAlignmentOptions.Center;
        var empowerment=EndgameUIFactory.Button(root.transform,"EMPOWERMENT",new Vector2(.05f,.81f),new Vector2(.31f,.88f),()=>SetMode(Mode.Empowerment));var infusion=EndgameUIFactory.Button(root.transform,"BOSS INFUSION",new Vector2(.37f,.81f),new Vector2(.63f,.88f),()=>SetMode(Mode.Infusion));var implicitButton=EndgameUIFactory.Button(root.transform,"IMPLICIT REFORGE",new Vector2(.69f,.81f),new Vector2(.95f,.88f),()=>SetMode(Mode.Implicit));
        body=EndgameUIFactory.Text(root.transform,string.Empty,new Vector2(.06f,.22f),new Vector2(.94f,.79f),14);
        var nextItem=EndgameUIFactory.Button(root.transform,"NEXT ITEM",new Vector2(.06f,.12f),new Vector2(.25f,.20f),()=>{itemIndex++;modIndex=0;Refresh();});var nextMod=EndgameUIFactory.Button(root.transform,"NEXT MOD",new Vector2(.27f,.12f),new Vector2(.46f,.20f),()=>{modIndex++;Refresh();});var nextPool=EndgameUIFactory.Button(root.transform,"NEXT POOL",new Vector2(.48f,.12f),new Vector2(.67f,.20f),()=>{poolIndex++;Refresh();});apply=EndgameUIFactory.Button(root.transform,"CONFIRM",new Vector2(.69f,.12f),new Vector2(.94f,.20f),Apply);
        var close=EndgameUIFactory.Button(root.transform,"CLOSE",new Vector2(.40f,.02f),new Vector2(.60f,.09f),Close);root.SetActive(false);authoredView=GetComponent<EndgameCraftingView>()??gameObject.AddComponent<EndgameCraftingView>();authoredView.panel=root;authoredView.body=body;authoredView.openButton=open;authoredView.applyButton=apply;authoredView.nextItemButton=nextItem;authoredView.nextModButton=nextMod;authoredView.nextPoolButton=nextPool;authoredView.closeButton=close;authoredView.modeButtons.Clear();authoredView.modeButtons.Add(empowerment);authoredView.modeButtons.Add(infusion);authoredView.modeButtons.Add(implicitButton);
    }
#endif
    void Update(){Build();if(IsOpen)Refresh();}
    public void Open(){Build();root.SetActive(true);root.transform.SetAsLastSibling();Time.timeScale=0;Refresh();}
    public void Close(){if(root!=null)root.SetActive(false);Time.timeScale=1;}
    void SetMode(Mode value){mode=value;modIndex=poolIndex=0;Refresh();}
    List<Gear> Items(){var r=new List<Gear>();if(Inventory.Instance!=null)foreach(var g in Inventory.Instance.Items)if(g!=null&&!g.IsScrap)r.Add(g);if(EquipmentManager.Instance!=null)foreach(var p in EquipmentManager.Instance.EquippedItems)if(p.Value!=null&&!r.Contains(p.Value))r.Add(p.Value);return r;}
    List<RolledMod> Targets(Gear gear){var r=new List<RolledMod>();if(gear==null)return r;foreach(var m in gear.rolledMods)if(m!=null&&!Gear.IsWeaponBaseStat(m.statType)&&!m.lockedOriginal)r.Add(m);return r;}
    void Refresh()
    {
        var items=Items();selected=items.Count==0?null:items[Mathf.Abs(itemIndex)%items.Count];var mods=Targets(selected);target=mods.Count==0?null:mods[Mathf.Abs(modIndex)%mods.Count];var challenges=WorldContentCatalog.Reference?.challengeEncounters;ChallengeEncounterDefinition c=challenges==null||challenges.Count==0?null:challenges[Mathf.Abs(poolIndex)%challenges.Count];
        int catalysts=CurrencyInventory.Instance?.Count(CraftingCurrencyType.EmpowermentCatalyst)??0,reforgers=EndgameResourceLedger.Instance?.Count(EndgameResourceIds.ImplicitReforger)??0,level=GameManager.Instance?.CurrentCombatLevel??1;
        var s=new StringBuilder();s.AppendLine($"<b>{mode.ToString().ToUpperInvariant()}</b>");s.AppendLine($"Selected: {(selected==null?"NO ITEM":selected.ItemRarity+" "+ItemSlotUI.DisplayType(selected.ItemType)+" / ilvl "+selected.ItemLevel+" / Potential "+selected.CurrentCraftingPotential)}");
        if(mode==Mode.Empowerment){s.AppendLine($"Catalysts: {catalysts} • Empowered: {selected?.EmpoweredModifierCount??0}/{EmpowermentProgressionProfile.MaximumEmpoweredModifiers(level)}");s.AppendLine($"Target: {Describe(target)}\nCost: 1 Catalyst, 0 Potential\nPreview: authored Empowered range (default 125% of T1 endpoint).");apply.interactable=catalysts>0&&EmpowermentCrafting.IsEligible(selected,target,level);}
        else if(mode==Mode.Infusion){int essence=c==null?0:EndgameResourceLedger.Instance?.Count(c.rewardResourceId)??0;string side=target==null?"Prefix/Suffix":AffixPolicy.Side(target).ToString();s.AppendLine($"Pool: {c?.displayName??"NONE"} • Essence: {essence}\nTarget ordinary mod: {Describe(target)}\nCost: 1 matching Essence + 3 Potential\nResult: random compatible {side} from {c?.specialAffixPoolId}.");apply.interactable=c!=null&&essence>0&&selected?.ItemRarity==LootManager.GearRarity.Legendary&&target!=null&&!target.isEmpowered&&!target.isBossSpecial&&selected.CurrentCraftingPotential>=3;}
        else{s.AppendLine($"Implicit Reforgers: {reforgers}\nCurrent implicit: {Describe(selected?.ImplicitMod)}\nWARNING: result is random; only the permanent implicit changes.\nCost: 1 Reforger, 0 Potential.");apply.interactable=reforgers>0&&selected!=null&&selected.ItemLevel==100&&selected.ItemRarity is LootManager.GearRarity.Rare or LootManager.GearRarity.Legendary;}
        s.AppendLine("\n<b>ENDGAME RESOURCES</b>");if(challenges!=null)foreach(var x in challenges)s.AppendLine($"{x.displayName}: key {EndgameResourceLedger.Instance?.Count(x.entryResourceId)??0} / essence {EndgameResourceLedger.Instance?.Count(x.rewardResourceId)??0}");body.text=s.ToString();
    }
    void Apply(){int level=GameManager.Instance?.CurrentCombatLevel??1;var challenges=WorldContentCatalog.Reference?.challengeEncounters;var c=challenges==null||challenges.Count==0?null:challenges[Mathf.Abs(poolIndex)%challenges.Count];bool ok=mode switch{Mode.Empowerment=>EndgameCraftingService.TryEmpower(selected,target,level),Mode.Infusion=>EndgameCraftingService.TryBossInfuse(selected,target,c,level),_=>EndgameCraftingService.TryReforgeImplicit(selected)};if(!ok)Debug.LogWarning("Endgame craft was invalid; no resource was consumed.");Refresh();}
    static string Describe(RolledMod m)=>m==null?"NONE":$"{StatDisplayFormatting.ToFriendlyName(m.statType)} {(m.isEmpowered?"EMPOWERED":m.isBossSpecial?"APEX":"T"+m.tierIndex)} {m.value:0.##}";
}
