using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HudControlTests
{
    const string ArtFolder="Assets/Resources/UI/TopHUD/";

    [Test]
    public void PixelMap_IsContainedAndAspectStableAtRepresentativeResolutions()
    {
        Assert.That(TopHUDLayout.Artwork,Is.EqualTo(new RectInt(12,169,2148,232)));
        Assert.That(new[]{TopHUDLayout.Skills,TopHUDLayout.Passives,TopHUDLayout.Enemy,TopHUDLayout.Inventory,TopHUDLayout.Stats},Is.EqualTo(new[]{new RectInt(1418,216,123,143),new RectInt(1543,216,118,143),new RectInt(1663,216,122,143),new RectInt(1787,216,118,143),new RectInt(1907,216,112,143)}));
        Assert.That(TopHUDLayout.Pause,Is.EqualTo(new RectInt(2049,198,65,79)));Assert.That(TopHUDLayout.Play,Is.EqualTo(new RectInt(2049,291,65,79)));
        Assert.That(TopHUDLayout.PlayerName.center.y,Is.EqualTo(233f));Assert.That(TopHUDLayout.EnemyName.center.y,Is.EqualTo(233f));
        Assert.That(TopHUDLayout.PlayerHealth,Is.EqualTo(new RectInt(260,275,394,26)));Assert.That(TopHUDLayout.PlayerMana,Is.EqualTo(new RectInt(260,325,394,26)));
        Assert.That(TopHUDLayout.PlayerHealthFillInset.left,Is.EqualTo(4));Assert.That(TopHUDLayout.PlayerHealthFillInset.right,Is.EqualTo(4));
        Assert.That(TopHUDLayout.EnemyName,Is.EqualTo(new RectInt(930,214,420,38)));
        Assert.That(TopHUDLayout.EnemyHealth,Is.EqualTo(new RectInt(980,275,346,26)));Assert.That(TopHUDLayout.EnemyMana,Is.EqualTo(new RectInt(970,325,356,26)));
        Assert.That(TopHUDLayout.PlayerHealth.center.y,Is.EqualTo(288f));Assert.That(TopHUDLayout.EnemyHealth.center.y,Is.EqualTo(288f));
        Assert.That(TopHUDLayout.PlayerMana.center.y,Is.EqualTo(338f));Assert.That(TopHUDLayout.EnemyMana.center.y,Is.EqualTo(338f));
        Assert.That(TopHUDLayout.PlayerHealth.xMin,Is.GreaterThan(TopHUDLayout.PlayerName.xMin));Assert.That(TopHUDLayout.EnemyHealth.xMin,Is.GreaterThan(TopHUDLayout.EnemyName.xMin));
        Assert.That(TopHUDLayout.MappedRects.All(TopHUDLayout.IsContained),Is.True);
        foreach((int width,int height) in new[]{(1280,720),(1920,1080),(2560,1440),(3440,1440)})
        {
            float artworkHeight=width/TopHUDLayout.Aspect;
            Assert.That(artworkHeight,Is.LessThanOrEqualTo(height));
            foreach(RectInt pixels in TopHUDLayout.MappedRects)
            {
                Rect n=TopHUDLayout.Normalized(pixels);
                Assert.That(n.xMin,Is.GreaterThanOrEqualTo(0f));Assert.That(n.yMin,Is.GreaterThanOrEqualTo(0f));
                Assert.That(n.xMax,Is.LessThanOrEqualTo(1f));Assert.That(n.yMax,Is.LessThanOrEqualTo(1f));
            }
        }
        Rect[] hits=System.Enum.GetValues(typeof(TopHUDButtonKind)).Cast<TopHUDButtonKind>().Select(k=>TopHUDLayout.Normalized(TopHUDLayout.ButtonRect(k))).ToArray();
        for(int i=0;i<hits.Length;i++)for(int j=i+1;j<hits.Length;j++)Assert.That(hits[i].Overlaps(hits[j]),Is.False,$"{i} overlaps {j}");
    }

    [Test]
    public void PortraitSelection_IsDeterministicStillAndPaddingAwareWithEnemyScale()
    {
        var texture=new Texture2D(100,100,TextureFormat.RGBA32,false);var pixels=Enumerable.Repeat(new Color(0,0,0,0),10000).ToArray();
        for(int y=20;y<80;y++)for(int x=10;x<50;x++)pixels[y*100+x]=Color.white;texture.SetPixels(pixels);texture.Apply();
        Sprite neutral=Sprite.Create(texture,new Rect(0,0,100,100),Vector2.one*.5f),other=Sprite.Create(texture,new Rect(0,0,100,100),Vector2.one*.5f);
        var actorObject=new GameObject("Actor",typeof(SpriteRenderer),typeof(PaperSpriteActor));
        try
        {
            var renderer=actorObject.GetComponent<SpriteRenderer>();var actor=actorObject.GetComponent<PaperSpriteActor>();actor.ConfigureIdle(renderer,new[]{neutral,other});renderer.sprite=other;
            Assert.That(actor.StillPortraitSprite,Is.SameAs(neutral),"Portrait must not follow the animated world frame");
            HUDPortraitFraming player=HUDPortraitFraming.Calculate(neutral,false),enemy=HUDPortraitFraming.Calculate(neutral,true);
            Assert.That(player.Scale,Is.GreaterThanOrEqualTo(1.5f));Assert.That(enemy.Scale,Is.GreaterThanOrEqualTo(2.6f));Assert.That(enemy.Scale,Is.GreaterThan(player.Scale));
            Assert.That(player.Offset.x,Is.GreaterThan(.1f),"Off-center transparent padding must be compensated");
            var imageObject=new GameObject("Portrait",typeof(RectTransform),typeof(Image));RectTransform authoredRect=(RectTransform)imageObject.transform;authoredRect.anchorMin=new Vector2(.2f,.3f);authoredRect.anchorMax=new Vector2(.8f,.9f);PaperBattleHUD.SetPortrait(imageObject.GetComponent<Image>(),neutral,true);
            Assert.That(imageObject.GetComponent<Image>().sprite,Is.SameAs(neutral));Assert.That(authoredRect.anchorMin,Is.EqualTo(new Vector2(.2f,.3f)));Assert.That(authoredRect.anchorMax,Is.EqualTo(new Vector2(.8f,.9f)),"Runtime portrait binding must preserve editor-authored geometry");Object.DestroyImmediate(imageObject);
        }
        finally{Object.DestroyImmediate(actorObject);Object.DestroyImmediate(neutral);Object.DestroyImmediate(other);Object.DestroyImmediate(texture);}
    }

    [Test]
    public void NullPortrait_IsTransparentAndDisabledWhileValidPortraitIsVisible()
    {
        var texture=new Texture2D(4,4);var sprite=Sprite.Create(texture,new Rect(0,0,4,4),Vector2.one*.5f);var go=new GameObject("Portrait",typeof(RectTransform),typeof(Image));
        try
        {
            Image image=go.GetComponent<Image>();PaperBattleHUD.SetPortrait(image,null,true);Assert.That(image.sprite,Is.Null);Assert.That(image.enabled,Is.False);Assert.That(image.color.a,Is.Zero);
            PaperBattleHUD.SetPortrait(image,sprite,true);Assert.That(image.enabled,Is.True);Assert.That(image.color,Is.EqualTo(Color.white));
        }
        finally{Object.DestroyImmediate(go);Object.DestroyImmediate(sprite);Object.DestroyImmediate(texture);}
    }

    [Test]
    public void GoblinPrefab_ProvidesStableNamedStillPortrait()
    {
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PaperBattle/Goblin2D.prefab");Assert.That(prefab,Is.Not.Null);
        PaperSpriteActor actor=prefab.GetComponentInChildren<PaperSpriteActor>(true);Assert.That(actor,Is.Not.Null);Assert.That(actor.StillPortraitSprite,Is.Not.Null);Assert.That(actor.DisplayName,Is.EqualTo("Goblin"));
        Assert.That(actor.StillPortraitSprite,Is.SameAs(actor.StillPortraitSprite));
        Assert.DoesNotThrow(()=>HUDPortraitFraming.Calculate(actor.StillPortraitSprite,true),"The authored non-readable Goblin texture must use the framing fallback without aborting HUD binding");
        HUDPortraitFraming framing=HUDPortraitFraming.Calculate(actor.StillPortraitSprite,true);
        Assert.That(framing.Offset.x,Is.EqualTo(.065f));Assert.That(framing.Offset.y,Is.EqualTo(.07f));
    }

    [TestCase(null,"Wanderer")]
    [TestCase("   ","Wanderer")]
    [TestCase("  Aria  ","Aria")]
    public void PlayerName_DefaultsAndNormalizes(string raw,string expected)=>Assert.That(PlayerDisplayNameProvider.Normalize(raw),Is.EqualTo(expected));

    [Test]
    public void StateSlices_AreMeasuredAndDoNotBleedAcrossStates()
    {
        foreach(TopHUDButtonKind kind in System.Enum.GetValues(typeof(TopHUDButtonKind)))
        {
            RectInt[] states={TopHUDLayout.StateSlice(kind,0),TopHUDLayout.StateSlice(kind,1),TopHUDLayout.StateSlice(kind,2)};
            Assert.That(states.All(r=>r.xMin>=0&&r.yMin>=0&&r.xMax<=TopHUDLayout.SourceWidth&&r.yMax<=TopHUDLayout.SourceHeight),Is.True);
            Assert.That(states[0].Overlaps(states[1]),Is.False);Assert.That(states[1].Overlaps(states[2]),Is.False);
            Assert.That(states.All(r=>r.width<450&&r.height<450),Is.True,"State crop retained excess 2172x724 export canvas");
        }
    }

    [Test]
    public void SpriteState_PrecedenceIsDisabledActivePointerHoverNormal()
    {
        var eventSystem=new GameObject("EventSystem",typeof(EventSystem));var go=new GameObject("Button",typeof(RectTransform),typeof(Image),typeof(Button),typeof(HUDSpriteState));
        try
        {
            Button button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();HUDSpriteState state=go.GetComponent<HUDSpriteState>();state.Initialize(button,TopHUDButtonKind.Skills);
            var pointer=new PointerEventData(eventSystem.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left};
            Assert.That(state.VisualStateIndex,Is.EqualTo(0));state.OnPointerEnter(pointer);Assert.That(state.VisualStateIndex,Is.EqualTo(1));state.OnPointerDown(pointer);Assert.That(state.VisualStateIndex,Is.EqualTo(2));
            state.SetPersistentActive(true);state.OnPointerUp(pointer);Assert.That(state.VisualStateIndex,Is.EqualTo(2));button.interactable=false;Assert.That(state.VisualStateIndex,Is.EqualTo(-1));
        }
        finally{Object.DestroyImmediate(go);Object.DestroyImmediate(eventSystem);}
    }

    [TestCase("Legendary Goblin","Goblin")]
    [TestCase("magic Rare Skeleton(Clone)","Skeleton")]
    [TestCase("Orc Warrior","Orc Warrior")]
    [TestCase("Normality Golem","Normality Golem")]
    public void EnemySpecies_StripsOnlyWholeRarityPrefixes(string input,string expected)=>Assert.That(PaperBattleHUD.CleanEnemySpeciesName(input),Is.EqualTo(expected));

    [Test]
    public void Resources_ClampFillAndKeepCenteredValueReadableAtEmptyAndFull()
    {
        var root=new GameObject("Resource",typeof(RectTransform));var fillObject=new GameObject("Fill",typeof(RectTransform),typeof(CanvasRenderer),typeof(HUDResourceBar));fillObject.transform.SetParent(root.transform,false);var textObject=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));textObject.transform.SetParent(root.transform,false);
        try
        {
            HUDResourceBar fill=fillObject.GetComponent<HUDResourceBar>();TMP_Text text=textObject.GetComponent<TMP_Text>();text.alignment=TextAlignmentOptions.Center;
            PaperBattleHUD.SetResource(fill,text,0,100);Assert.That(fill.FillAmount,Is.Zero);Assert.That(text.text,Is.EqualTo("0 / 100"));
            PaperBattleHUD.SetResource(fill,text,120,100);Assert.That(fill.FillAmount,Is.EqualTo(1));Assert.That(text.text,Is.EqualTo("100 / 100"));Assert.That(text.alignment,Is.EqualTo(TextAlignmentOptions.Center));
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void InventoryCoexistsWithEitherStatsPanelButStatsPanelsExcludeEachOther()
    {
        BuildFixture(out GameObject canvas,out GameObject playerObject,out PaperBattleHUD hud);
        try
        {
            hud.ToggleInventory();Assert.That(hud.inventoryPanel.activeSelf,Is.True);
            hud.ToggleStats();Assert.That(hud.inventoryPanel.activeSelf,Is.True);
            Assert.That(hud.statsPanel.activeSelf,Is.True);
            hud.ToggleStats();Assert.That(hud.inventoryPanel.activeSelf,Is.True);
            Assert.That(hud.statsPanel.activeSelf,Is.False);
            hud.ToggleEnemyInspection();Assert.That(hud.inventoryPanel.activeSelf,Is.True);
            Assert.That(hud.IsEnemyInspectionOpen,Is.True);
            hud.ToggleStats();Assert.That(hud.IsEnemyInspectionOpen,Is.False);
            Assert.That(hud.statsPanel.activeSelf,Is.True);
            Assert.That(hud.inventoryPanel.activeSelf,Is.True);
            hud.ToggleEnemyInspection();Assert.That(hud.statsPanel.activeSelf,Is.False);
            Assert.That(hud.IsEnemyInspectionOpen,Is.True);
            hud.ToggleInventory();Assert.That(hud.inventoryPanel.activeSelf,Is.False);
            Assert.That(hud.IsEnemyInspectionOpen,Is.True);
            hud.ToggleInventory();Assert.That(hud.inventoryPanel.activeSelf,Is.True);
            hud.CloseEnemyInspection();Assert.That(hud.inventoryPanel.activeSelf,Is.True);
            hud.PauseGameplay();Assert.That(hud.inventoryPanel.activeSelf,Is.False);
            Assert.That(hud.PauseMenu.IsOpen,Is.True);
            hud.PlayGameplay();
        }
        finally{Time.timeScale=1f;Object.DestroyImmediate(canvas);Object.DestroyImmediate(playerObject);}
    }

    [Test]
    public void MainControls_AreUniqueOwnCallbacksNavigateAndPersistPanelState()
    {
        BuildFixture(out GameObject canvas,out GameObject playerObject,out PaperBattleHUD hud);
        try
        {
            Assert.That(hud.transform.Cast<Transform>().Count(t=>t.name=="Top HUD Artwork"),Is.EqualTo(1));
            Assert.That(hud.transform.Cast<Transform>().Any(t=>new[]{"ENEMY","INVENTORY","STATS","PAUSE","PLAY","Skills","Active Skills","Wanderer Health Bar","Enemy Health Bar"}.Contains(t.name)),Is.False);
            Button skills=hud.GetButton(TopHUDButtonKind.Skills),passives=hud.GetButton(TopHUDButtonKind.Passives),enemy=hud.GetButton(TopHUDButtonKind.Enemy),inventory=hud.GetButton(TopHUDButtonKind.Inventory),stats=hud.GetButton(TopHUDButtonKind.Stats),pause=hud.GetButton(TopHUDButtonKind.Pause),play=hud.GetButton(TopHUDButtonKind.Play);
            Assert.That(new[]{skills,passives,enemy,inventory,stats,pause,play}.Distinct().Count(),Is.EqualTo(7));
            Assert.That(skills.navigation.selectOnRight,Is.SameAs(passives));Assert.That(passives.navigation.selectOnRight,Is.SameAs(enemy));Assert.That(enemy.navigation.selectOnRight,Is.SameAs(inventory));Assert.That(inventory.navigation.selectOnRight,Is.SameAs(stats));Assert.That(stats.navigation.selectOnRight,Is.SameAs(pause));Assert.That(pause.navigation.selectOnDown,Is.SameAs(play));
            inventory.onClick.Invoke();Assert.That(hud.inventoryPanel.activeSelf,Is.True);Assert.That(inventory.GetComponent<HUDSpriteState>().PersistentActive,Is.True);
            inventory.onClick.Invoke();Assert.That(hud.inventoryPanel.activeSelf,Is.False);
            stats.onClick.Invoke();Assert.That(hud.statsPanel.activeSelf,Is.True);Assert.That(stats.GetComponent<HUDSpriteState>().PersistentActive,Is.True);stats.onClick.Invoke();Assert.That(hud.statsPanel.activeSelf,Is.False);
            Time.timeScale=0f;typeof(PaperBattleHUD).GetMethod("RefreshMenuStates",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);Assert.That(pause.GetComponent<HUDSpriteState>().PersistentActive,Is.True);Assert.That(play.GetComponent<HUDSpriteState>().PersistentActive,Is.False);
            Time.timeScale=1f;typeof(PaperBattleHUD).GetMethod("RefreshMenuStates",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);Assert.That(play.GetComponent<HUDSpriteState>().PersistentActive,Is.True);
        }
        finally{Time.timeScale=1f;Object.DestroyImmediate(canvas);Object.DestroyImmediate(playerObject);}
    }

    [Test]
    public void EnemyRebind_UnsubscribesOldHealthAndClearsWhenNone()
    {
        BuildFixture(out GameObject canvas,out GameObject playerObject,out PaperBattleHUD hud);var first=new GameObject("Rare Goblin");var second=new GameObject("Legendary Orc");
        try
        {
            var firstAI=first.AddComponent<EnemyAI>();var firstHealth=first.AddComponent<HealthComponent>();var secondAI=second.AddComponent<EnemyAI>();var secondHealth=second.AddComponent<HealthComponent>();
            MethodInfo bind=typeof(PaperBattleHUD).GetMethod("BindEnemy",BindingFlags.Instance|BindingFlags.NonPublic);bind.Invoke(hud,new object[]{firstAI});bind.Invoke(hud,new object[]{secondAI});
            TMP_Text health=(TMP_Text)typeof(PaperBattleHUD).GetField("enemyHealthText",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);string secondValue=health.text;firstHealth.LoseLife(10);Assert.That(health.text,Is.EqualTo(secondValue));
            bind.Invoke(hud,new object[]{null});Assert.That(hud.enemyText.text,Is.Empty);Assert.That(health.text,Is.EqualTo("0 / 0"));
        }
        finally{Object.DestroyImmediate(first);Object.DestroyImmediate(second);Object.DestroyImmediate(canvas);Object.DestroyImmediate(playerObject);}
    }

    [Test]
    public void EnemyLifecycle_ExistingLateReplacementDeathAndAbsentManaStayConsistentWithoutDuplicateHandlers()
    {
        BuildFixture(out GameObject canvas,out GameObject playerObject,out PaperBattleHUD hud);var first=new GameObject("Rare Goblin");var second=new GameObject("Legendary Orc");
        try
        {
            var firstAI=first.AddComponent<EnemyAI>();var firstHealth=first.AddComponent<HealthComponent>();var actor=first.AddComponent<PaperSpriteActor>();var renderer=first.AddComponent<SpriteRenderer>();var texture=new Texture2D(4,4);var sprite=Sprite.Create(texture,new Rect(0,0,4,4),Vector2.one*.5f);actor.ConfigureIdle(renderer,new[]{sprite});
            var secondAI=second.AddComponent<EnemyAI>();var secondHealth=second.AddComponent<HealthComponent>();
            MethodInfo bind=typeof(PaperBattleHUD).GetMethod("BindEnemy",BindingFlags.Instance|BindingFlags.NonPublic);bind.Invoke(hud,new object[]{firstAI});bind.Invoke(hud,new object[]{firstAI});
            Assert.That(SubscriberCount(firstHealth,"Changed",hud),Is.EqualTo(1),"Repeated initial sync must not duplicate the health callback");
            TMP_Text mana=(TMP_Text)typeof(PaperBattleHUD).GetField("enemyManaText",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);Assert.That(mana.text,Is.EqualTo("0 / 0"));
            Image portrait=(Image)typeof(PaperBattleHUD).GetField("enemyPortrait",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);Assert.That(portrait.sprite,Is.SameAs(sprite));Assert.That(portrait.enabled,Is.True);
            bind.Invoke(hud,new object[]{secondAI});Assert.That(SubscriberCount(firstHealth,"Changed",hud),Is.Zero);Assert.That(SubscriberCount(secondHealth,"Changed",hud),Is.EqualTo(1));
            bind.Invoke(hud,new object[]{null});Assert.That(SubscriberCount(secondHealth,"Changed",hud),Is.Zero);Assert.That(portrait.enabled,Is.False);Assert.That(hud.enemyText.text,Is.Empty);
            Object.DestroyImmediate(sprite);Object.DestroyImmediate(texture);
        }
        finally{Object.DestroyImmediate(first);Object.DestroyImmediate(second);Object.DestroyImmediate(canvas);Object.DestroyImmediate(playerObject);}
    }

    [Test]
    public void InitialPlayerSync_ShowsNameAuthoritativeManaAndLeftOriginContainedHealthFill()
    {
        BuildFixture(out GameObject canvas,out GameObject playerObject,out PaperBattleHUD hud);
        try
        {
            Assert.That(hud.playerText.text,Is.EqualTo(playerObject.GetComponent<PlayerDisplayNameProvider>().DisplayName));Assert.That(hud.playerText.text,Is.Not.Empty);Assert.That(hud.playerText.enabled,Is.True);Assert.That(hud.playerText.gameObject.activeInHierarchy,Is.True);
            TMP_Text mana=(TMP_Text)typeof(PaperBattleHUD).GetField("playerManaText",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);Assert.That(mana.text,Is.EqualTo("100 / 100"));
            RectTransform fill=(RectTransform)hud.ArtworkRect.Find("Player Health/Fill");Assert.That(fill,Is.Not.Null);Assert.That(fill.pivot.x,Is.Zero);Assert.That(fill.offsetMin.x,Is.EqualTo(4));Assert.That(fill.offsetMax.x,Is.EqualTo(-4));
        }
        finally{Object.DestroyImmediate(canvas);Object.DestroyImmediate(playerObject);}
    }

    [Test]
    public void BattleManagerEvents_SyncEnemyExistingBeforeHudThenLateReplacementAndDeath()
    {
        var managerObject=new GameObject("Battle Manager");var manager=managerObject.AddComponent<BattleManager>();var first=new GameObject("Goblin");var second=new GameObject("Orc");GameObject canvas=null,playerObject=null;
        try
        {
            var firstAI=first.AddComponent<EnemyAI>();var firstHealth=first.AddComponent<HealthComponent>();SetManagerEnemy(manager,firstAI,firstHealth);
            BuildFixture(out canvas,out playerObject,out PaperBattleHUD hud);typeof(PaperBattleHUD).GetMethod("HandleBattleManagerChanged",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,new object[]{manager});Assert.That(hud.enemyText.text,Does.Contain("Goblin"));
            var secondAI=second.AddComponent<EnemyAI>();var secondHealth=second.AddComponent<HealthComponent>();SetManagerEnemy(manager,secondAI,secondHealth);PublishEnemy(manager,secondAI);Assert.That(hud.enemyText.text,Does.Contain("Orc"));
            Assert.That(SubscriberCount(firstHealth,"Changed",hud),Is.Zero);Assert.That(SubscriberCount(secondHealth,"Changed",hud),Is.EqualTo(1));
            manager.NotifyEnemyDied(secondHealth);Assert.That(hud.enemyText.text,Is.Empty);Assert.That(SubscriberCount(secondHealth,"Changed",hud),Is.Zero);
        }
        finally{if(canvas!=null)Object.DestroyImmediate(canvas);if(playerObject!=null)Object.DestroyImmediate(playerObject);Object.DestroyImmediate(first);Object.DestroyImmediate(second);Object.DestroyImmediate(managerObject);}
    }

    [Test]
    public void ImportedArtwork_HashesAreExactImportsWithUniqueSpriteMetas()
    {
        var expected=new Dictionary<string,string>{{"TopHUDBarEmpty.png","FF74609BAFF5165C786947512274E3346EAD65D16D0E8B73E1B20F333F3655F2"},{"TopHUDBar.png","CDE0B35B3246084CA926C369DE1BDA6D9F58665279933F6D899F68553B951777"},{"SkillsButton.png","AD880E19894C693635DE6BA5A38B81EE959C9A8A7D1463164899E5417A4D4DC3"},{"PassivesButton.png","08FB35B10FDB9E164B6E0F5B6267F49EF74114E32B6EE79B2F09B5E0170D6C9A"},{"EnemyButton.png","D1C96E512747B1875DAF04C749A1AAA10587C3E182A475C59F63563EB8697205"},{"InventoryButton.png","8A53E65F52DA34127AA2E71887E7E0EA5E34C10F3F5F2F824DD1B795A21CBF48"},{"StatsButton.png","61751FFBC1C89B760FB080F35224512E88C4EF37C1AB24D656941B76600493AD"},{"PauseButton.png","0404FAF7CD4FE4D97EF1EFE37EDA9CD361C7DB59E7EB328BDDFEC6EA9E854393"},{"PlayButton.png","8C15B043F7B59525D7BD0249142CD1A937210920C69660FF6539691192D27FF3"}};
        var guids=new HashSet<string>();
        foreach(var pair in expected)
        {
            string path=ArtFolder+pair.Key;using var stream=File.OpenRead(path);string hash=string.Concat(SHA256.Create().ComputeHash(stream).Select(b=>b.ToString("X2")));Assert.That(hash,Is.EqualTo(pair.Value));
            string guid=AssetDatabase.AssetPathToGUID(path);Assert.That(guid,Is.Not.Empty);Assert.That(guids.Add(guid),Is.True,"Duplicate GUID: "+pair.Key);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;Assert.That(importer,Is.Not.Null);Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite));Assert.That(importer.isReadable,Is.True);Assert.That(importer.mipmapEnabled,Is.False);
        }
    }

    static void BuildFixture(out GameObject canvasObject,out GameObject playerObject,out PaperBattleHUD hud)
    {
        canvasObject=new GameObject("Canvas",typeof(Canvas));var inventory=new GameObject("InventoryPanel",typeof(RectTransform));inventory.transform.SetParent(canvasObject.transform,false);inventory.SetActive(false);var stats=new GameObject("Player Stat Screen",typeof(RectTransform));stats.transform.SetParent(canvasObject.transform,false);stats.SetActive(false);
        playerObject=new GameObject("Player");var statsComponent=playerObject.AddComponent<StatsComponent>();statsComponent.SetBaseStat(StatTypes.Mana,100f);var mana=playerObject.AddComponent<ManaComponent>();typeof(ManaComponent).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(mana,null);var player=playerObject.AddComponent<HealthComponent>();typeof(HealthComponent).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);var hudObject=new GameObject("Paper Battle HUD",typeof(RectTransform),typeof(Image));hudObject.transform.SetParent(canvasObject.transform,false);hudObject.SetActive(false);hud=hudObject.AddComponent<PaperBattleHUD>();hudObject.AddComponent<SkillTreeUI>();hud.player=player;hud.inventoryPanel=inventory;hud.statsPanel=stats;
        GameplayHUDView view=BuildAuthoredView(hudObject,canvasObject.transform,inventory,stats);hud.SetAuthoredView(view);var enemy=hudObject.AddComponent<EnemyInspectionPanelUI>();enemy.BuildAuthoring(hud);var pause=hudObject.AddComponent<PauseMenuUI>();pause.BuildAuthoring(hud);
        typeof(PaperBattleHUD).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);hudObject.SetActive(true);typeof(PaperBattleHUD).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);Canvas.ForceUpdateCanvases();
    }

    static GameplayHUDView BuildAuthoredView(GameObject hudObject,Transform canvas,GameObject inventory,GameObject stats)
    {
        var view=hudObject.AddComponent<GameplayHUDView>();var screen=hudObject.AddComponent<UIAuthoringScreen>();screen.Configure(UIAuthoringScreenKind.GameplayHUD,"test.hud",AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(UIVisualLibraryBuilder.LibraryPath));view.authoredRoot=hudObject.transform as RectTransform;
        var artwork=new GameObject("Top HUD Artwork",typeof(RectTransform),typeof(Image));artwork.transform.SetParent(hudObject.transform,false);view.artworkRoot=artwork.transform as RectTransform;
        view.playerPortrait=ImageChild(artwork.transform,"Player Portrait");view.enemyPortrait=ImageChild(artwork.transform,"Enemy Portrait");view.playerName=TextChild(artwork.transform,"Player Name");view.enemyName=TextChild(artwork.transform,"Enemy Species");view.runSummary=TextChild(canvas,"Run Summary");
        Resource(artwork.transform,"Player Health",out view.playerLifeFill,out view.playerLifeText,true);Resource(artwork.transform,"Player Mana",out view.playerManaFill,out view.playerManaText,false);Resource(artwork.transform,"Enemy Health",out view.enemyLifeFill,out view.enemyLifeText,false);Resource(artwork.transform,"Enemy Mana",out view.enemyManaFill,out view.enemyManaText,false);
        view.skillsButton=ButtonChild(artwork.transform,TopHUDButtonKind.Skills);view.passivesButton=ButtonChild(artwork.transform,TopHUDButtonKind.Passives);view.enemyButton=ButtonChild(artwork.transform,TopHUDButtonKind.Enemy);view.inventoryButton=ButtonChild(artwork.transform,TopHUDButtonKind.Inventory);view.statsButton=ButtonChild(artwork.transform,TopHUDButtonKind.Stats);view.pauseButton=ButtonChild(artwork.transform,TopHUDButtonKind.Pause);view.playButton=ButtonChild(artwork.transform,TopHUDButtonKind.Play);view.inventoryPanel=inventory;view.statsPanel=stats;return view;
    }
    static Image ImageChild(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);return go.GetComponent<Image>();}
    static TMP_Text TextChild(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);return go.GetComponent<TMP_Text>();}
    static void Resource(Transform parent,string name,out HUDResourceBar fill,out TMP_Text text,bool inset){var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);var fillObject=new GameObject("Fill",typeof(RectTransform),typeof(CanvasRenderer),typeof(HUDResourceBar));fillObject.transform.SetParent(root.transform,false);RectTransform rect=fillObject.transform as RectTransform;rect.pivot=new Vector2(0,.5f);if(inset){rect.offsetMin=new Vector2(4,2);rect.offsetMax=new Vector2(-4,-2);}fill=fillObject.GetComponent<HUDResourceBar>();text=TextChild(root.transform,"Value");}
    static Button ButtonChild(Transform parent,TopHUDButtonKind kind){var go=new GameObject(kind+" Button",typeof(RectTransform),typeof(Image),typeof(Button),typeof(HUDSpriteState));go.transform.SetParent(parent,false);Button button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();HUDButtonVisualSet visuals=AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(UIVisualLibraryBuilder.LibraryPath).HUDButton(kind);go.GetComponent<HUDSpriteState>().Configure(button,visuals.normal,visuals.hover,visuals.pressed);return button;}

    static int SubscriberCount(object source,string eventField,object target)
    {
        var callback=(System.Delegate)source.GetType().GetField(eventField,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source);
        return callback==null?0:callback.GetInvocationList().Count(d=>ReferenceEquals(d.Target,target));
    }

    static void SetManagerEnemy(BattleManager manager,EnemyAI enemy,HealthComponent health)
    {
        typeof(BattleManager).GetField("enemyAI",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,enemy);typeof(BattleManager).GetField("enemyHealth",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,health);typeof(BattleManager).GetField("currentEnemy",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,enemy.gameObject);
    }

    static void PublishEnemy(BattleManager manager,EnemyAI enemy)
    {
        var callback=(System.Action<EnemyAI>)typeof(BattleManager).GetField("CurrentEnemyChanged",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(manager);callback?.Invoke(enemy);
    }
}
