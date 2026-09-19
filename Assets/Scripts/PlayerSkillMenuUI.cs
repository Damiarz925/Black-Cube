// Step 16 weapon-skill HUD: two equipped-weapon controls sharing one replaceable next-attack queue.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerSkillMenuUI:MonoBehaviour
{
    public static bool IsOpen{get;private set;}
    PaperBattleHUD hud;PlayerSkillController controller;GameObject panel;
    readonly Button[] buttons=new Button[2];readonly TMP_Text[] labels=new TMP_Text[2];
    Button rageFinisherButton;TMP_Text rageFinisherLabel;RageState rage;
    static readonly Color Panel=new(.035f,.043f,.055f,1f),Card=new(.105f,.13f,.16f,1f);

    void Start()
    {
        hud=GetComponent<PaperBattleHUD>();if(hud==null||hud.player==null)return;
        controller=hud.player.GetComponent<PlayerSkillController>()??hud.player.gameObject.AddComponent<PlayerSkillController>();Build();
        rage=hud.player.GetComponent<RageState>()??hud.player.gameObject.AddComponent<RageState>();rage.Changed+=Refresh;
        controller.QueueChanged+=Refresh;controller.WeaponSkillsChanged+=Refresh;controller.CooldownsChanged+=Refresh;controller.Mana.ManaChanged+=Refresh;Refresh();
    }

    void Build()
    {
        Transform canvas=hud.GetComponentInParent<Canvas>().transform;
        for(int i=0;i<2;i++){int index=i;buttons[i]=Button(canvas,"Weapon Skill "+(i+1),new Vector2(.29f+i*.22f,.012f),new Vector2(.49f+i*.22f,.09f),out labels[i]);buttons[i].onClick.AddListener(()=>{controller.TryQueueWeaponSkill(index);Refresh();});}
        rageFinisherButton=Button(canvas,"Rage Finisher",new Vector2(.72f,.012f),new Vector2(.94f,.09f),out rageFinisherLabel);rageFinisherButton.onClick.AddListener(()=>{rage?.TryArmFinisher();Refresh();});
        panel=Box(canvas,"Weapon Skill Information",new Vector2(.23f,.3f),new Vector2(.77f,.7f),Panel);var c=panel.AddComponent<Canvas>();c.overrideSorting=true;c.sortingOrder=110;panel.AddComponent<GraphicRaycaster>();
        var title=Text(panel.transform,"Title",new Vector2(.06f,.68f),new Vector2(.94f,.9f),30);title.text="WEAPON SKILLS";title.alignment=TextAlignmentOptions.Center;
        var body=Text(panel.transform,"Body",new Vector2(.08f,.25f),new Vector2(.92f,.67f),18);body.text="Each weapon type supplies exactly two active-skill slots. Final V1 assignments are intentionally TBD; developer fixtures can exercise this pipeline without creating production mappings.";body.alignment=TextAlignmentOptions.Center;
        var close=Button(panel.transform,"Close",new Vector2(.3f,.06f),new Vector2(.7f,.22f),out var closeLabel);closeLabel.text="RETURN TO BATTLE";close.onClick.AddListener(Close);panel.SetActive(false);IsOpen=false;
    }

    void Update(){if(controller==null)return;if(hud.player!=null&&hud.player.CurrentLife<=0)Close();Refresh();}
    void Refresh()
    {
        if(controller==null||buttons[0]==null)return;
        for(int i=0;i<2;i++)
        {
            var skill=i<controller.WeaponSkills.Count?controller.WeaponSkills[i]:null;
            if(skill==null){labels[i].text=$"SKILL {i+1}  /  UNASSIGNED";buttons[i].interactable=false;CorruptionUIButtonSkin.Ensure(buttons[i])?.SetSelected(false);continue;}
            float cost=controller.ManaCost(skill);bool queued=controller.QueuedSkill==skill;
            if(skill.castMode==PlayerSkillCastMode.AutoCooldown){float remaining=controller.CooldownRemaining(i);bool ready=controller.AutoSkillReady(i);labels[i].text=ready?$"SKILL {i+1}  /  {skill.displayName.ToUpperInvariant()}  /  READY  /  {cost:0} MANA":$"SKILL {i+1}  /  {skill.displayName.ToUpperInvariant()}  /  {remaining:0.0}s";buttons[i].interactable=false;CorruptionUIButtonSkin.Ensure(buttons[i])?.SetSelected(ready);continue;}
            labels[i].text=queued?$"QUEUED  {skill.displayName.ToUpperInvariant()}  /  NEXT ATTACK":$"SKILL {i+1}  /  {skill.displayName.ToUpperInvariant()}  /  {cost:0} MANA";
            buttons[i].interactable=(queued||controller.Mana.CanSpend(cost))&&BattleManager.Instance!=null&&BattleManager.Instance.CanCastPlayerSkill;
            CorruptionUIButtonSkin.Ensure(buttons[i])?.SetSelected(queued);
        }
        if(rageFinisherButton!=null){bool show=rage!=null&&rage.IsSupportedWeapon;rageFinisherButton.gameObject.SetActive(show);if(show){rageFinisherLabel.text=rage.FinisherArmed?$"RAGE {rage.Rage:0}  /  FINISHER ARMED":rage.FinisherAvailable?$"RAGE {rage.Rage:0}  /  EMPOWERED ATTACK":$"RAGE {rage.Rage:0} / 100";rageFinisherButton.interactable=rage.FinisherAvailable&&!rage.FinisherArmed;}}
    }

    public void Toggle(){if(panel==null||hud.player==null||hud.player.CurrentLife<=0)return;if(IsOpen){Close();return;}hud.inventoryPanel.SetActive(false);hud.statsPanel.SetActive(false);hud.CloseEnemyInspection();GetComponent<SkillTreeUI>()?.Close();IsOpen=true;panel.SetActive(true);panel.transform.SetAsLastSibling();Refresh();}
    public void Close(){IsOpen=false;if(panel!=null)panel.SetActive(false);}
    void OnDestroy(){IsOpen=false;if(controller!=null){controller.QueueChanged-=Refresh;controller.WeaponSkillsChanged-=Refresh;controller.CooldownsChanged-=Refresh;controller.Mana.ManaChanged-=Refresh;}if(rage!=null)rage.Changed-=Refresh;if(panel!=null)Destroy(panel);}
    static GameObject Box(Transform parent,string name,Vector2 min,Vector2 max,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<Image>().color=color;return go;}
    static TMP_Text Text(Transform parent,string name,Vector2 min,Vector2 max,int size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=go.GetComponent<TextMeshProUGUI>();t.fontSize=size;t.color=new Color(.92f,.93f,.95f);t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;return t;}
    static Button Button(Transform parent,string name,Vector2 min,Vector2 max,out TMP_Text label){var go=Box(parent,name,min,max,Card);var b=go.AddComponent<Button>();b.targetGraphic=go.GetComponent<Image>();label=Text(go.transform,"Label",Vector2.zero,Vector2.one,16);label.alignment=TextAlignmentOptions.Center;return b;}
}
