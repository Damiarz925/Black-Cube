// Developer map: Owns the supplied-art top HUD, authoritative panel controls and event-driven actor resource bindings.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PaperBattleHUD : MonoBehaviour
{
    public TMP_Text runText;
    public TMP_Text playerText;
    public TMP_Text enemyText;
    public SlantedHealthBar playerHealthBar;
    public SlantedHealthBar enemyHealthBar;
    public HealthComponent player;
    public GameObject inventoryPanel;
    public GameObject statsPanel;

    readonly Dictionary<TopHUDButtonKind,Button> buttons=new();
    readonly Dictionary<TopHUDButtonKind,HUDSpriteState> buttonStates=new();
    EnemyInspectionPanelUI enemyInspection;
    RectTransform artworkRect;
    Image playerPortrait,enemyPortrait;
    TMP_Text playerHealthText,playerManaText,enemyHealthText,enemyManaText;
    HUDResourceBar playerHealthFill,playerManaFill,enemyHealthFill,enemyManaFill;
    ManaComponent playerMana,enemyMana;
    PlayerDisplayNameProvider displayNameProvider;
    BattleManager battleManager;
    PauseMenuUI pauseMenu;
    HealthComponent boundPlayer;
    EnemyAI boundEnemy;
    HealthComponent enemyHealth;

    static readonly Color HealthRed=new Color(.78f,.045f,.035f,.92f);
    static readonly Color ManaBlue=new Color(.035f,.32f,.84f,.92f);
    static readonly Regex RarityPrefix=new(@"^(?:(?:Normal|Magic|Rare|Legendary)\s+)+",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);

    public bool IsEnemyInspectionOpen => enemyInspection != null && enemyInspection.IsOpen;
    public bool IsExplicitlyPaused => Mathf.Approximately(Time.timeScale,0f);
    public PauseMenuUI PauseMenu => pauseMenu;
    public RectTransform ArtworkRect => artworkRect;
    public Button GetButton(TopHUDButtonKind kind) => buttons.TryGetValue(kind,out Button value)?value:null;

    void Awake()
    {
        RemoveLegacyTopHUD();
        if(GetComponent<StatusHUD>()==null)gameObject.AddComponent<StatusHUD>();
        if(GetComponent<SkillTreeUI>()==null)gameObject.AddComponent<SkillTreeUI>();
        if(GetComponent<PlayerSkillMenuUI>()==null)gameObject.AddComponent<PlayerSkillMenuUI>();
        var rebirth=GetComponent<RebirthConfirmationUI>();if(rebirth==null)rebirth=gameObject.AddComponent<RebirthConfirmationUI>();rebirth.Build();
        enemyInspection=GetComponent<EnemyInspectionPanelUI>();if(enemyInspection==null)enemyInspection=gameObject.AddComponent<EnemyInspectionPanelUI>();enemyInspection.Initialize(this);
        InventoryEquipmentPanelUI.SeparateStats(statsPanel);
        BuildTopHUD();
        pauseMenu=GetComponent<PauseMenuUI>();if(pauseMenu==null)pauseMenu=gameObject.AddComponent<PauseMenuUI>();pauseMenu.Initialize(this);
        var corruptionTheme=GetComponent<CorruptionUITheme>();if(corruptionTheme==null)corruptionTheme=gameObject.AddComponent<CorruptionUITheme>();corruptionTheme.Initialize(this,enemyInspection);
    }

    void OnEnable()
    {
        if(artworkRect==null)return;
        BattleManager.InstanceChanged-=HandleBattleManagerChanged;BattleManager.InstanceChanged+=HandleBattleManagerChanged;
        BindPlayer();HandleBattleManagerChanged(BattleManager.Instance);RefreshAll();
    }

    void OnDisable(){ReleaseBindings();}

    void Update()
    {
        RefreshMenuStates();RefreshRunText();
        if(player!=null&&player.CurrentLife<=0f){CloseGameplayPanels();pauseMenu?.Close();}
    }

    public void ToggleSkills(){if(CanOpenGameplayPanel())GetComponent<PlayerSkillMenuUI>()?.Toggle();}
    public void TogglePassives(){if(CanOpenGameplayPanel())GetComponent<SkillTreeUI>()?.Toggle();}
    public void ToggleInventory(){if(!CanOpenGameplayPanel())return;if(inventoryPanel!=null)inventoryPanel.SetActive(!inventoryPanel.activeSelf);RefreshMenuStates();}
    public void ToggleStats(){if(!CanOpenGameplayPanel()||statsPanel==null)return;bool open=!statsPanel.activeSelf;enemyInspection?.Close();GetComponent<SkillTreeUI>()?.Close();statsPanel.SetActive(open);RefreshMenuStates();}
    public void ToggleEnemyInspection(){if(!CanOpenGameplayPanel())return;bool open=!IsEnemyInspectionOpen;if(statsPanel!=null)statsPanel.SetActive(false);GetComponent<SkillTreeUI>()?.Close();if(open)enemyInspection?.Open();else enemyInspection?.Close();RefreshMenuStates();}
    public void CloseEnemyInspection(){enemyInspection?.Close();RefreshMenuStates();}
    public void PauseGameplay(){if(!CanOpenPanel())return;CloseGameplayPanels();Time.timeScale=0f;pauseMenu?.Open();RefreshPlaybackControls();}
    public void PlayGameplay(){if(!CanOpenPanel())return;pauseMenu?.Close();Time.timeScale=1f;RefreshPlaybackControls();}

    public void PositionBelowArtwork(RectTransform target,float left,float topGap,float width,float height)
    {
        if(target==null)return;
        float artHeight=artworkRect!=null?artworkRect.rect.height:Screen.width/TopHUDLayout.Aspect;
        target.anchorMin=target.anchorMax=new Vector2(0f,1f);target.pivot=new Vector2(0f,1f);
        target.anchoredPosition=new Vector2(left,-artHeight-topGap);target.sizeDelta=new Vector2(width,height);
    }

    public static string CleanEnemySpeciesName(string raw)
    {
        if(string.IsNullOrWhiteSpace(raw))return string.Empty;
        string value=raw.Replace("(Clone)",string.Empty).Trim();value=RarityPrefix.Replace(value,string.Empty).Trim();
        return string.IsNullOrEmpty(value)?raw.Trim():value;
    }

    void BuildTopHUD()
    {
        if(transform.Find("Top HUD Artwork")!=null)return;
        RectTransform rootRect=transform as RectTransform;if(rootRect!=null){rootRect.anchorMin=Vector2.zero;rootRect.anchorMax=Vector2.one;rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;rootRect.localScale=Vector3.one;}
        Image rootImage=GetComponent<Image>();if(rootImage!=null){rootImage.sprite=null;rootImage.color=Color.clear;rootImage.raycastTarget=false;rootImage.enabled=false;}
        var artObject=new GameObject("Top HUD Artwork",typeof(RectTransform),typeof(Image),typeof(AspectRatioFitter));artObject.transform.SetParent(transform,false);
        artworkRect=(RectTransform)artObject.transform;artworkRect.anchorMin=new Vector2(0f,1f);artworkRect.anchorMax=new Vector2(1f,1f);artworkRect.pivot=new Vector2(.5f,1f);artworkRect.offsetMin=artworkRect.offsetMax=Vector2.zero;
        var fitter=artObject.GetComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.WidthControlsHeight;fitter.aspectRatio=TopHUDLayout.Aspect;
        var art=artObject.GetComponent<Image>();art.sprite=CreateBaseSprite();art.color=Color.white;art.preserveAspect=false;art.raycastTarget=false;
        playerPortrait=CreatePortrait(artworkRect,"Player Portrait",TopHUDLayout.PlayerPortrait);enemyPortrait=CreatePortrait(artworkRect,"Enemy Portrait",TopHUDLayout.EnemyPortrait);
        playerText=CreateText(artworkRect,"Player Name",TopHUDLayout.PlayerName,22);enemyText=CreateText(artworkRect,"Enemy Species",TopHUDLayout.EnemyName,22);
        CreateResource(artworkRect,"Player Health",TopHUDLayout.PlayerHealth,HealthRed,TopHUDLayout.PlayerHealthFillInset,out playerHealthFill,out playerHealthText);
        CreateResource(artworkRect,"Player Mana",TopHUDLayout.PlayerMana,ManaBlue,null,out playerManaFill,out playerManaText);
        CreateResource(artworkRect,"Enemy Health",TopHUDLayout.EnemyHealth,HealthRed,null,out enemyHealthFill,out enemyHealthText);
        CreateResource(artworkRect,"Enemy Mana",TopHUDLayout.EnemyMana,ManaBlue,null,out enemyManaFill,out enemyManaText);
        CreateButton(TopHUDButtonKind.Skills,ToggleSkills);CreateButton(TopHUDButtonKind.Passives,TogglePassives);CreateButton(TopHUDButtonKind.Enemy,ToggleEnemyInspection);CreateButton(TopHUDButtonKind.Inventory,ToggleInventory);CreateButton(TopHUDButtonKind.Stats,ToggleStats);CreateButton(TopHUDButtonKind.Pause,PauseGameplay);CreateButton(TopHUDButtonKind.Play,PlayGameplay);ConfigureNavigation();
        Canvas canvas=GetComponentInParent<Canvas>();if(canvas!=null){runText=CreateText(canvas.transform,"Run Summary",new RectInt(),15,false);runText.alignment=TextAlignmentOptions.MidlineLeft;PositionBelowArtwork(runText.rectTransform,18,3,620,24);}
    }

    void RemoveLegacyTopHUD()
    {
        playerText=enemyText=runText=null;playerHealthBar=enemyHealthBar=null;
        for(int i=transform.childCount-1;i>=0;i--){GameObject child=transform.GetChild(i).gameObject;if(Application.isPlaying){child.SetActive(false);Destroy(child);}else DestroyImmediate(child);}
    }

    Sprite CreateBaseSprite()
    {
        Texture2D texture=Resources.Load<Texture2D>("UI/TopHUD/TopHUDBarEmpty");if(texture==null)return null;RectInt p=TopHUDLayout.Artwork;
        var sprite=Sprite.Create(texture,new Rect(p.x,texture.height-p.yMax,p.width,p.height),Vector2.one*.5f,100f,0,SpriteMeshType.FullRect);sprite.name="TopHUDBarEmpty Artwork";return sprite;
    }

    static Image CreatePortrait(Transform parent,string name,RectInt pixels)
    {
        var maskObject=new GameObject(name+" Mask",typeof(RectTransform),typeof(CanvasRenderer),typeof(HUDPortraitMaskGraphic),typeof(Mask));maskObject.transform.SetParent(parent,false);TopHUDLayout.Apply((RectTransform)maskObject.transform,pixels);maskObject.GetComponent<Mask>().showMaskGraphic=false;
        var imageObject=new GameObject(name,typeof(RectTransform),typeof(Image));imageObject.transform.SetParent(maskObject.transform,false);var rect=(RectTransform)imageObject.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=imageObject.GetComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;return image;
    }

    static TMP_Text CreateText(Transform parent,string name,RectInt pixels,float size,bool mapped=true)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=8;text.fontSizeMax=size;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;text.raycastTarget=false;
        var shadow=go.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.95f);shadow.effectDistance=new Vector2(1,-1);if(mapped)TopHUDLayout.Apply(text.rectTransform,pixels);return text;
    }

    static void CreateResource(Transform parent,string name,RectInt pixels,Color color,RectOffset inset,out HUDResourceBar fill,out TMP_Text label)
    {
        var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);TopHUDLayout.Apply((RectTransform)root.transform,pixels);
        inset??=new RectOffset(0,0,2,2);
        var fillObject=new GameObject("Fill",typeof(RectTransform),typeof(CanvasRenderer),typeof(HUDResourceBar));fillObject.transform.SetParent(root.transform,false);var fillRect=(RectTransform)fillObject.transform;fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;fillRect.pivot=new Vector2(0f,.5f);fillRect.offsetMin=new Vector2(inset.left,inset.bottom);fillRect.offsetMax=new Vector2(-inset.right,-inset.top);fill=fillObject.GetComponent<HUDResourceBar>();fill.color=color;fill.raycastTarget=false;
        label=CreateText(root.transform,"Value",new RectInt(),18,false);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
    }

    void CreateButton(TopHUDButtonKind kind,UnityAction action)
    {
        var go=new GameObject(kind+" Button",typeof(RectTransform),typeof(Image),typeof(Button),typeof(HUDSpriteState));go.transform.SetParent(artworkRect,false);TopHUDLayout.Apply((RectTransform)go.transform,TopHUDLayout.ButtonRect(kind));
        var button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();button.onClick.AddListener(action);var state=go.GetComponent<HUDSpriteState>();state.Initialize(button,kind);buttons[kind]=button;buttonStates[kind]=state;
    }

    void ConfigureNavigation()
    {
        Button skills=GetButton(TopHUDButtonKind.Skills),passives=GetButton(TopHUDButtonKind.Passives),enemy=GetButton(TopHUDButtonKind.Enemy),inventory=GetButton(TopHUDButtonKind.Inventory),stats=GetButton(TopHUDButtonKind.Stats),pause=GetButton(TopHUDButtonKind.Pause),play=GetButton(TopHUDButtonKind.Play);
        SetNavigation(skills,null,passives,null,null);SetNavigation(passives,skills,enemy,null,null);SetNavigation(enemy,passives,inventory,null,null);SetNavigation(inventory,enemy,stats,null,null);SetNavigation(stats,inventory,pause,null,null);SetNavigation(pause,stats,null,null,play);SetNavigation(play,stats,null,pause,null);
    }

    static void SetNavigation(Button button,Selectable left,Selectable right,Selectable up,Selectable down){button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=left,selectOnRight=right,selectOnUp=up,selectOnDown=down};}

    void BindPlayer()
    {
        if(player==null){GameObject found=GameObject.FindGameObjectWithTag("Player");player=found!=null?found.GetComponent<HealthComponent>():null;}
        if(player==null){UnbindPlayer();RefreshPlayerIdentity();RefreshPlayerResources();return;}
        if(boundPlayer==player){RefreshPlayerIdentity();RefreshPlayerResources();return;}
        UnbindPlayer();boundPlayer=player;boundPlayer.Changed+=RefreshPlayerResources;
        playerMana=player.GetComponent<ManaComponent>();if(playerMana==null&&player.GetComponent<StatsComponent>()!=null)playerMana=player.gameObject.AddComponent<ManaComponent>();if(playerMana!=null)playerMana.ManaChanged+=RefreshPlayerResources;
        displayNameProvider=player.GetComponent<PlayerDisplayNameProvider>();if(displayNameProvider==null)displayNameProvider=player.gameObject.AddComponent<PlayerDisplayNameProvider>();displayNameProvider.Changed-=RefreshPlayerIdentity;displayNameProvider.Changed+=RefreshPlayerIdentity;
        SetPortrait(playerPortrait,player.GetComponent<PaperSpriteActor>()?.StillPortraitSprite,false);RefreshPlayerIdentity();RefreshPlayerResources();
    }

    void HandleBattleManagerChanged(BattleManager current)
    {
        if(current==battleManager){HandleCurrentEnemyChanged(current!=null?current.CurrentEnemyAI:null);return;}
        if(battleManager!=null)battleManager.CurrentEnemyChanged-=HandleCurrentEnemyChanged;
        battleManager=current;
        if(battleManager!=null)battleManager.CurrentEnemyChanged+=HandleCurrentEnemyChanged;
        HandleCurrentEnemyChanged(battleManager!=null?battleManager.CurrentEnemyAI:null);
    }

    void HandleCurrentEnemyChanged(EnemyAI enemy)
    {
        EnemyAI authoritative=battleManager!=null?battleManager.CurrentEnemyAI:null;
        if(authoritative!=null&&enemy!=authoritative)enemy=authoritative;
        BindEnemy(enemy);
    }

    void BindEnemy(EnemyAI enemy)
    {
        if(enemy==null)enemy=null; // Normalize Unity's destroyed-object null to a real null reference.
        if(enemy==boundEnemy&&enemyHealth!=null){RefreshEnemyIdentity();RefreshEnemyResources();return;}
        UnbindEnemy();boundEnemy=enemy;enemyHealth=enemy!=null?enemy.GetComponent<HealthComponent>():null;enemyMana=enemy!=null?enemy.GetComponent<ManaComponent>():null;
        if(enemyHealth!=null)enemyHealth.Changed+=RefreshEnemyResources;if(enemyMana!=null)enemyMana.ManaChanged+=RefreshEnemyResources;
        PaperSpriteActor actor=boundEnemy!=null?boundEnemy.GetComponentInChildren<PaperSpriteActor>(true):null;
        SetPortrait(enemyPortrait,actor!=null?actor.StillPortraitSprite:null,true);RefreshEnemyIdentity();RefreshEnemyResources();
    }

    void RefreshPlayerIdentity(){if(playerText==null)return;playerText.enabled=true;playerText.gameObject.SetActive(true);playerText.color=Color.white;playerText.text=displayNameProvider!=null?displayNameProvider.DisplayName:"Wanderer";playerText.transform.SetAsLastSibling();}
    void RefreshEnemyIdentity(){if(enemyText==null)return;if(boundEnemy==null){enemyText.text=string.Empty;return;}PaperSpriteActor actor=boundEnemy.GetComponentInChildren<PaperSpriteActor>(true);enemyText.enabled=true;enemyText.gameObject.SetActive(true);enemyText.color=Color.white;enemyText.text=CleanEnemySpeciesName(actor!=null?actor.DisplayName:boundEnemy.name);enemyText.transform.SetAsLastSibling();}
    void RefreshPlayerResources(){SetResource(playerHealthFill,playerHealthText,player!=null?player.CurrentLife:0f,player!=null?player.MaxLife:0f);SetResource(playerManaFill,playerManaText,playerMana!=null?playerMana.CurrentMana:0f,playerMana!=null?playerMana.MaxMana:0f);}
    void RefreshEnemyResources(){SetResource(enemyHealthFill,enemyHealthText,enemyHealth!=null?enemyHealth.CurrentLife:0f,enemyHealth!=null?enemyHealth.MaxLife:0f);SetResource(enemyManaFill,enemyManaText,enemyMana!=null?enemyMana.CurrentMana:0f,enemyMana!=null?enemyMana.MaxMana:0f);}

    public static void SetResource(HUDResourceBar fill,TMP_Text text,float current,float maximum)
    {
        float safeMax=Mathf.Max(0f,maximum),safeCurrent=Mathf.Clamp(current,0f,safeMax);if(fill!=null)fill.FillAmount=safeMax>0f?safeCurrent/safeMax:0f;if(text!=null)text.text=$"{Mathf.CeilToInt(safeCurrent)} / {Mathf.CeilToInt(safeMax)}";
    }

    public static void SetPortrait(Image image,Sprite sprite,bool enemy)
    {
        if(image==null)return;image.sprite=sprite;image.enabled=sprite!=null;image.color=sprite!=null?Color.white:Color.clear;HUDPortraitFraming.Calculate(sprite,enemy).Apply(image.rectTransform);
    }

    void RefreshRunText()
    {
        if(runText==null)return;ZoneManager zone=FindFirstObjectByType<ZoneManager>();GameManager run=GameManager.Instance;string location=zone!=null?zone.LocationLabel.ToUpperInvariant():"FOREST 1";
        string progress=run==null?string.Empty:$"  /  LEVEL {run.CurrentCombatLevel}  /  STAGE {run.EncounterStage}/{run.NormalKillsRequired+1}"+(run.BossActive?"  /  BOSS":string.Empty);runText.text="BLACK CUBE  /  "+location+progress;PositionBelowArtwork(runText.rectTransform,18,3,620,24);
    }

    void RefreshMenuStates()
    {
        SetActive(TopHUDButtonKind.Skills,PlayerSkillMenuUI.IsOpen);SetActive(TopHUDButtonKind.Passives,SkillTreeUI.IsOpen);SetActive(TopHUDButtonKind.Enemy,IsEnemyInspectionOpen);SetActive(TopHUDButtonKind.Inventory,inventoryPanel!=null&&inventoryPanel.activeSelf);SetActive(TopHUDButtonKind.Stats,statsPanel!=null&&statsPanel.activeSelf);RefreshPlaybackControls();
    }

    void RefreshPlaybackControls()
    {
        bool alive=CanOpenPanel(),paused=Mathf.Approximately(Time.timeScale,0f);foreach(TopHUDButtonKind kind in Enum.GetValues(typeof(TopHUDButtonKind)))if(GetButton(kind)!=null)GetButton(kind).interactable=alive;SetActive(TopHUDButtonKind.Pause,paused);SetActive(TopHUDButtonKind.Play,!paused);foreach(HUDSpriteState state in buttonStates.Values)state.Refresh();
    }

    void SetActive(TopHUDButtonKind kind,bool value){if(buttonStates.TryGetValue(kind,out HUDSpriteState state))state.SetPersistentActive(value);}
    bool CanOpenPanel()=>player==null||player.CurrentLife>0f;
    public void CloseGameplayPanels(){if(inventoryPanel!=null)inventoryPanel.SetActive(false);if(statsPanel!=null)statsPanel.SetActive(false);enemyInspection?.Close();GetComponent<SkillTreeUI>()?.Close();GetComponent<PlayerSkillMenuUI>()?.Close();}
    bool CanOpenGameplayPanel()=>CanOpenPanel()&&(pauseMenu==null||!pauseMenu.IsOpen);
    void RefreshAll(){RefreshPlayerIdentity();RefreshPlayerResources();RefreshEnemyIdentity();RefreshEnemyResources();RefreshMenuStates();RefreshRunText();}

    void UnbindPlayer()
    {
        if(boundPlayer!=null)boundPlayer.Changed-=RefreshPlayerResources;if(playerMana!=null)playerMana.ManaChanged-=RefreshPlayerResources;if(displayNameProvider!=null)displayNameProvider.Changed-=RefreshPlayerIdentity;boundPlayer=null;playerMana=null;displayNameProvider=null;
    }

    void UnbindEnemy(){if(enemyHealth!=null)enemyHealth.Changed-=RefreshEnemyResources;if(enemyMana!=null)enemyMana.ManaChanged-=RefreshEnemyResources;boundEnemy=null;enemyHealth=null;enemyMana=null;}
    void ReleaseBindings(){BattleManager.InstanceChanged-=HandleBattleManagerChanged;if(battleManager!=null)battleManager.CurrentEnemyChanged-=HandleCurrentEnemyChanged;battleManager=null;UnbindEnemy();UnbindPlayer();}

    void OnDestroy()
    {
        pauseMenu?.Release();ReleaseBindings();
        if(runText!=null&&runText.transform.parent!=transform)
        {
            GameObject ownedRunText=runText.gameObject;runText=null;
            if(Application.isPlaying)Destroy(ownedRunText);else DestroyImmediate(ownedRunText);
        }
    }
}
