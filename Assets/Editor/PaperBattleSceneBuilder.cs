using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>Explicit, repeatable migration. Original Scene.prefab and source models stay intact.</summary>
public static class PaperBattleSceneBuilder
{
    private const string Art = "Assets/Art/PaperBattle/";
    private const string Output = "Assets/Prefabs/PaperBattle/";

    [MenuItem("Black Cube/Build Paper Battle Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Leave Play mode before building the paper scene."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        System.IO.Directory.CreateDirectory(Output);
        AssetDatabase.Refresh();
        var playerFrames = Frames("Player", new[] {
            new Rect(0,512,768,512), new Rect(768,512,768,512),
            new Rect(0,0,768,512), new Rect(768,0,768,512)
        }, new[] {new Vector2(425,11),new Vector2(380,11),new Vector2(420,30),new Vector2(332,30)}, 145f);
        // The strike extends left of the nominal 2x2 divider: custom, non-overlapping slices.
        var ghoulFrames = Frames("Ghoul", new[] {
            new Rect(15,645,490,554),new Rect(780,645,510,554),
            new Rect(0,0,500,640),new Rect(500,0,812,610)
        }, new[] {new Vector2(300,16),new Vector2(245,16),new Vector2(305,39),new Vector2(570,49)}, 157f);
        var normal = MakeEnemy("Ghoul2D", false, ghoulFrames);
        var boss = MakeEnemy("GhoulBoss2D", true, ghoulFrames);

        var root = PrefabUtility.LoadPrefabContents("Assets/Scenes/Scene.prefab");
        try
        {
            // Disable legacy visual components in the new prefab, retaining their source data.
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (!(r is SpriteRenderer)) r.enabled = false;
            foreach (var t in root.GetComponentsInChildren<Terrain>(true)) t.enabled = false;
            foreach (var l in root.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var a in root.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Environment Root" || t.name.EndsWith("Anchors")) t.gameObject.SetActive(false);
            var zone = root.GetComponentInChildren<ZoneManager>(true);
            SetBool(zone, "generate3DScenery", false);

            var pc = root.GetComponentInChildren<PlayerController>(true);
            pc.transform.localPosition = new Vector3(-2.15f,-2.65f,0);
            pc.transform.localRotation = Quaternion.identity;
            pc.transform.localScale = Vector3.one;
            AddBody(pc.gameObject, playerFrames);
            var battle = root.GetComponentInChildren<BattleManager>(true);
            var so = new SerializedObject(battle);
            var playerSpawn = (Transform)so.FindProperty("playerSpawnPoint").objectReferenceValue;
            var enemySpawn = (Transform)so.FindProperty("enemySpawnPoint").objectReferenceValue;
            playerSpawn.position = pc.transform.position;
            playerSpawn.rotation = Quaternion.identity;
            enemySpawn.position = root.transform.TransformPoint(new Vector3(2.15f,-2.65f,0));
            enemySpawn.rotation = Quaternion.identity;
            so.FindProperty("normalEnemyPrefab").objectReferenceValue = normal;
            so.FindProperty("bossEnemyPrefab").objectReferenceValue = boss;
            so.ApplyModifiedPropertiesWithoutUndo();

            var camera = root.GetComponentInChildren<Camera>(true);
            camera.transform.SetParent(root.transform);
            camera.transform.localPosition = new Vector3(0,0,-10);
            camera.transform.localRotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f,0.045f,0.035f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50;
            var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null) cameraData.renderPostProcessing = false;

            var forestTexture = ImportTexture("Forest");
            var forest = SaveSprite("Forest", forestTexture, new Rect(0,0,forestTexture.width,forestTexture.height), new Vector2(forestTexture.width/2f,forestTexture.height/2f), forestTexture.height/10f);
            var bg = new GameObject("Forest Paper Background", typeof(SpriteRenderer));
            bg.transform.SetParent(root.transform,false);
            bg.transform.localPosition = new Vector3(0,0,5);
            var bgRenderer = bg.GetComponent<SpriteRenderer>();
            bgRenderer.sprite = forest;
            bgRenderer.sortingOrder = -100;
            bgRenderer.sharedMaterial = SpriteMaterial();
            ConfigureUI(root, pc.GetComponent<HealthComponent>());
            PaperBattleUIBuilder.Configure(root);
            DamageNumberStyleBuilder.Configure(root);
            var paperPrefab = PrefabUtility.SaveAsPrefabAsset(root,Output+"PaperBattle.prefab");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(paperPrefab,scene);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/SampleScene.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("PAPER2D: Scene built. Legacy scenery disabled; player/normal/boss sprites and HUD wired.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Texture2D ImportTexture(string name)
    {
        string path=Art+name+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;
        importer.maxTextureSize=4096;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Sprite[] Frames(string name,Rect[] rects,Vector2[] pivots,float ppu)
    {
        var texture=ImportTexture(name);
        return Enumerable.Range(0,4).Select(i=>SaveSprite(name+i,texture,rects[i],pivots[i],ppu)).ToArray();
    }
    private static Sprite SaveSprite(string name,Texture2D texture,Rect rect,Vector2 pixelPivot,float ppu)
    {
        var sprite=Sprite.Create(texture,rect,new Vector2(pixelPivot.x/rect.width,pixelPivot.y/rect.height),ppu,0,SpriteMeshType.FullRect);
        sprite.name=name;
        string path=Output+name+".asset";
        var previous=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(previous!=null) { EditorUtility.CopySerialized(sprite,previous); Object.DestroyImmediate(sprite); return previous; }
        AssetDatabase.CreateAsset(sprite,path);
        return sprite;
    }
    private static Material SpriteMaterial()
    {
        string path=Output+"PaperSprite.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) { material=new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material,path); }
        return material;
    }
    private static void AddBody(GameObject go,Sprite[] frames)
    {
        var child=new GameObject("Paper Body",typeof(SpriteRenderer));
        child.transform.SetParent(go.transform,false);
        var renderer=child.GetComponent<SpriteRenderer>();
        renderer.sortingOrder=10;
        renderer.sharedMaterial=SpriteMaterial();
        go.AddComponent<PaperSpriteActor>().Configure(renderer,frames);
    }
    private static GameObject MakeEnemy(string name,bool boss,Sprite[] frames)
    {
        var enemy=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Enemy Prefabs/Ghoul.prefab");
        try
        {
            enemy.name=name;
            // The gameplay scripts are on the root; its rig/model children are presentation only.
            for(int i=enemy.transform.childCount-1;i>=0;i--) Object.DestroyImmediate(enemy.transform.GetChild(i).gameObject);
            foreach(var r in enemy.GetComponents<Renderer>()) Object.DestroyImmediate(r);
            foreach(var c in enemy.GetComponents<Collider>()) c.enabled=false;
            foreach(var a in enemy.GetComponents<Animator>()) a.enabled=false;
            enemy.transform.localScale=Vector3.one;
            enemy.transform.rotation=Quaternion.identity;
            SetBool(enemy.GetComponent<HealthComponent>(),"isBoss",boss);
            if(boss) { var hp=new SerializedObject(enemy.GetComponent<HealthComponent>()); hp.FindProperty("maxLife").floatValue=500; hp.ApplyModifiedPropertiesWithoutUndo(); }
            AddBody(enemy,frames);
            return PrefabUtility.SaveAsPrefabAsset(enemy,Output+name+".prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(enemy); }
    }
    private static void SetBool(Object obj,string property,bool value)
    {
        var so=new SerializedObject(obj); so.FindProperty(property).boolValue=value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static RectTransform Rect(GameObject go,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
    {
        var r=go.GetComponent<RectTransform>(); r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;r.localScale=Vector3.one;return r;
    }
    private static TMP_Text Label(Transform parent,string text,Vector2 min,Vector2 max,int size=22)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        Rect(go,min,max,Vector2.zero,Vector2.zero);
        var label=go.GetComponent<TextMeshProUGUI>();label.text=text;label.fontSize=size;label.color=new Color(.94f,.92f,.83f);label.alignment=TextAlignmentOptions.MidlineLeft;label.raycastTarget=false;
        return label;
    }
    private static void ConfigureUI(GameObject root,HealthComponent player)
    {
        var scaler=root.GetComponentInChildren<CanvasScaler>(true);
        var canvas=scaler.GetComponent<Canvas>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var inventory=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="InventoryPanel").gameObject;
        var stats=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Player Stat Screen").gameObject;
        inventory.transform.SetParent(canvas.transform,false);stats.transform.SetParent(canvas.transform,false);
        Rect(inventory,new Vector2(0,.12f),new Vector2(0,.86f),new Vector2(22,0),new Vector2(370,0));
        Rect(stats,new Vector2(1,.12f),new Vector2(1,.86f),new Vector2(-370,0),new Vector2(-22,0));
        foreach(var panel in new[]{inventory,stats}) { var img=panel.GetComponent<Image>();if(img!=null)img.color=new Color(.06f,.08f,.075f,.96f);panel.SetActive(false); }
        foreach(var grid in inventory.GetComponentsInChildren<GridLayoutGroup>(true)) {grid.cellSize=new Vector2(148,100);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.spacing=new Vector2(8,8);}
        var hudGO=new GameObject("Paper Battle HUD",typeof(RectTransform),typeof(Image),typeof(PaperBattleHUD));hudGO.transform.SetParent(canvas.transform,false);
        Rect(hudGO,new Vector2(0,.89f),Vector2.one,Vector2.zero,Vector2.zero);
        hudGO.GetComponent<Image>().color=new Color(.035f,.05f,.045f,.93f);
        var hud=hudGO.GetComponent<PaperBattleHUD>();hud.player=player;hud.inventoryPanel=inventory;hud.statsPanel=stats;
        hud.runText=Label(hudGO.transform,"BLACK CUBE / FOREST",new Vector2(.025f,.5f),new Vector2(.42f,1),25);
        hud.playerText=Label(hudGO.transform,"WANDERER",new Vector2(.025f,0),new Vector2(.25f,.5f),19);
        hud.enemyText=Label(hudGO.transform,"GHOUL",new Vector2(.28f,0),new Vector2(.65f,.5f),19);
        var inventoryButton=Button(hudGO.transform,"INVENTORY",.70f,.84f);
        UnityEventTools.AddPersistentListener(inventoryButton.onClick,hud.ToggleInventory);
        var statsButton=Button(hudGO.transform,"STATS",.86f,.98f);
        UnityEventTools.AddPersistentListener(statsButton.onClick,hud.ToggleStats);
        var death=root.GetComponentInChildren<DeathMenuUI>(true);
        if(death!=null)
        {
            var so=new SerializedObject(death);var panel=(GameObject)so.FindProperty("root").objectReferenceValue;
            panel.transform.SetParent(canvas.transform,false);Rect(panel,new Vector2(.25f,.15f),new Vector2(.75f,.8f),Vector2.zero,Vector2.zero);panel.transform.SetAsLastSibling();
            foreach(var text in panel.GetComponentsInChildren<TMP_Text>(true)) {text.color=new Color(.98f,.88f,.75f);text.fontSize=Mathf.Max(text.fontSize,24);}
            foreach(var image in panel.GetComponentsInChildren<Image>(true)) image.color=new Color(.11f,.13f,.11f,.96f);
        }
    }
    private static Button Button(Transform parent,string title,float left,float right)
    {
        var go=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
        Rect(go,new Vector2(left,.25f),new Vector2(right,.78f),Vector2.zero,Vector2.zero);go.GetComponent<Image>().color=new Color(.23f,.29f,.24f);
        var text=Label(go.transform,title,Vector2.zero,Vector2.one,20);text.alignment=TextAlignmentOptions.Center;
        return go.GetComponent<Button>();
    }
}
