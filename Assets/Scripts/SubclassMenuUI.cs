// Functional Step 18 subclass choice/respec panel and Light Priest aura readout.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SubclassMenuUI:MonoBehaviour
{
    [SerializeField] SubclassView authoredView;
    PaperBattleHUD hud;GameObject panel;Button open,mode;TMP_Text openLabel,title,description,modeLabel,aura,frozen;readonly Button[] choices=new Button[2];readonly TMP_Text[] choiceLabels=new TMP_Text[2];
    string pending;
    void Start()
    {
        hud=GetComponent<PaperBattleHUD>();var canvas=GetComponentInParent<Canvas>();if(canvas==null)return;
        if(authoredView==null)authoredView=GetComponent<SubclassView>();if(authoredView==null){Debug.LogError("SubclassMenuUI requires an authored SubclassView.",this);return;}BindAuthored();Refresh();
    }
    void BindAuthored(){panel=authoredView.panel;open=authoredView.openButton;mode=authoredView.projectileModeButton;openLabel=authoredView.openLabel;title=authoredView.title;description=authoredView.description;modeLabel=authoredView.projectileModeLabel;aura=authoredView.auraLabel;frozen=authoredView.frozenLabel;for(int i=0;i<2;i++){choices[i]=i<authoredView.choiceButtons.Count?authoredView.choiceButtons[i]:null;choiceLabels[i]=i<authoredView.choiceLabels.Count?authoredView.choiceLabels[i]:null;int slot=i;if(choices[i]!=null){choices[i].onClick.RemoveAllListeners();choices[i].onClick.AddListener(()=>Choose(slot));}}if(open!=null){open.onClick.RemoveAllListeners();open.onClick.AddListener(Toggle);}if(mode!=null){mode.onClick.RemoveAllListeners();mode.onClick.AddListener(ToggleProjectileMode);}panel?.SetActive(false);}
#if UNITY_EDITOR
    public void BuildAuthoring()
    {
        hud=GetComponent<PaperBattleHUD>();var canvas=GetComponentInParent<Canvas>();if(canvas==null)return;
        open=Button(canvas.transform,"Subclass",new Vector2(.03f,.012f),new Vector2(.21f,.09f),out openLabel);open.onClick.AddListener(Toggle);BottomActionBarLayout.Attach(open,20,190);
        panel=Box(canvas.transform,"Subclass Selection",new Vector2(.22f,.22f),new Vector2(.78f,.78f),new Color(.03f,.035f,.05f,.99f));var sorting=panel.AddComponent<Canvas>();sorting.overrideSorting=true;sorting.sortingOrder=120;panel.AddComponent<GraphicRaycaster>();
        title=Text(panel.transform,"Title",new Vector2(.08f,.77f),new Vector2(.92f,.94f),28);title.alignment=TextAlignmentOptions.Center;
        description=Text(panel.transform,"Description",new Vector2(.1f,.36f),new Vector2(.9f,.7f),17);description.alignment=TextAlignmentOptions.Center;
        mode=Button(panel.transform,"Projectile Mode",new Vector2(.28f,.26f),new Vector2(.72f,.35f),out modeLabel);mode.onClick.AddListener(ToggleProjectileMode);
        for(int i=0;i<2;i++){int slot=i;choices[i]=Button(panel.transform,"Choice "+i,new Vector2(.08f+i*.46f,.08f),new Vector2(.46f+i*.46f,.23f),out choiceLabels[i]);choices[i].onClick.AddListener(()=>Choose(slot));}
        aura=Text(canvas.transform,"Light Priest Auras",new Vector2(.74f,.1f),new Vector2(.98f,.22f),13);aura.alignment=TextAlignmentOptions.TopRight;panel.SetActive(false);Refresh();
        frozen=Text(canvas.transform,"Frozen Status",new Vector2(.42f,.78f),new Vector2(.58f,.83f),18);frozen.alignment=TextAlignmentOptions.Center;frozen.color=new Color(.4f,.85f,1f);Refresh();
        authoredView=GetComponent<SubclassView>()??gameObject.AddComponent<SubclassView>();authoredView.panel=panel;authoredView.openButton=open;authoredView.projectileModeButton=mode;authoredView.openLabel=openLabel;authoredView.title=title;authoredView.description=description;authoredView.projectileModeLabel=modeLabel;authoredView.auraLabel=aura;authoredView.frozenLabel=frozen;authoredView.choiceButtons.Clear();authoredView.choiceLabels.Clear();authoredView.choiceButtons.AddRange(choices);authoredView.choiceLabels.AddRange(choiceLabels);
    }
#endif
    void Update()=>Refresh();
    PlayerIdentityState Identity=>GameManager.Instance!=null?GameManager.Instance.GetComponent<PlayerIdentityState>():null;
    void Toggle(){if(Identity?.SubclassChoiceUnlocked!=true)return;panel.SetActive(!panel.activeSelf);pending=null;Refresh();}
    void Choose(int index)
    {
        var identity=Identity;var options=identity!=null?SubclassCatalog.ForClass(identity.BaseClassId):null;if(options==null||index<0||index>=options.Count)return;string id=options[index].Id;
        if(BattleManager.Instance!=null&&BattleManager.Instance.CanCastPlayerSkill){description.text="Subclass changes require combat to be paused or inactive.";return;}
        if(pending!=id){pending=id;description.text=$"Confirm change to {options[index].DisplayName}. Ordinary passives remain; transformed nodes will be cleared.";return;}
        identity.SelectSubclass(id);pending=null;Refresh();
    }
    void ToggleProjectileMode()
    {
        var identity=Identity;if(identity?.SelectedSubclassId!=SubclassIds.RangerProjectile)return;
        if(BattleManager.Instance!=null&&BattleManager.Instance.CanCastPlayerSkill){description.text="Projectile mode changes require combat to be paused or inactive.";return;}
        identity.SetProjectileMode(identity.ProjectileMode==SubclassProjectileMode.Volley?SubclassProjectileMode.Focused:SubclassProjectileMode.Volley);Refresh();
    }
    void Refresh()
    {
        var identity=Identity;if(openLabel==null)return;open.interactable=identity?.SubclassChoiceUnlocked==true;openLabel.text=identity?.SubclassChoiceUnlocked==true?(string.IsNullOrEmpty(identity.SelectedSubclassId)?"CHOOSE SUBCLASS":"SUBCLASS") : "SUBCLASS LOCKED";
        if(panel!=null&&panel.activeSelf&&identity!=null){var options=SubclassCatalog.ForClass(identity.BaseClassId);title.text=$"{identity.ClassDefinition?.DisplayName.ToUpperInvariant()} SUBCLASSES";for(int i=0;i<2;i++){bool exists=i<options.Count;choices[i].gameObject.SetActive(exists);if(exists)choiceLabels[i].text=(identity.SelectedSubclassId==options[i].Id?"CURRENT  ":pending==options[i].Id?"CONFIRM  ":string.Empty)+options[i].DisplayName.ToUpperInvariant();}bool projectile=identity.SelectedSubclassId==SubclassIds.RangerProjectile;mode.gameObject.SetActive(projectile);if(projectile)modeLabel.text=$"PROJECTILE MODE: {identity.ProjectileMode.ToString().ToUpperInvariant()} (CHANGE)";if(pending==null)description.text=string.IsNullOrEmpty(identity.SelectedSubclassId)?"Choose one class-specific subclass. Selection is free to change outside active combat.":SubclassCatalog.TryGet(identity.SelectedSubclassId,out var current)?current.DisplayName+"\n"+current.Description:string.Empty;}
        var state=hud?.player!=null?hud.player.GetComponent<SubclassCombatState>():null;bool light=state?.Has(SubclassIds.PriestLight)==true;aura.gameObject.SetActive(light);if(light){var enemy=BattleManager.Instance?.CurrentEnemyAI?.GetComponent<HealthComponent>();float max=enemy!=null?enemy.MaxLife:0;aura.text=$"AURAS  P {state.AuraIntensity(0,max):P0}  F {state.AuraIntensity(1,max):P0}  C {state.AuraIntensity(2,max):P0}  L {state.AuraIntensity(3,max):P0}";}
        var status=BattleManager.Instance?.CurrentEnemyAI?.GetComponent<StatusController>();if(frozen!=null){frozen.gameObject.SetActive(status?.IsFrozen==true);frozen.text="FROZEN — NEXT ATTACK SKIPPED";}
    }
    static GameObject Box(Transform parent,string name,Vector2 min,Vector2 max,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<Image>().color=color;return go;}
    static TMP_Text Text(Transform parent,string name,Vector2 min,Vector2 max,int size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=go.GetComponent<TextMeshProUGUI>();t.fontSize=size;t.color=Color.white;t.raycastTarget=false;return t;}
    static Button Button(Transform parent,string name,Vector2 min,Vector2 max,out TMP_Text label){var go=Box(parent,name,min,max,new Color(.11f,.13f,.17f,1));var b=go.AddComponent<Button>();b.targetGraphic=go.GetComponent<Image>();label=Text(go.transform,"Label",Vector2.zero,Vector2.one,15);label.alignment=TextAlignmentOptions.Center;return b;}
}
