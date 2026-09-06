using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Repeatable normal-stage UI pass; original source UI prefabs remain untouched.</summary>
public static class PaperBattleUIBuilder
{
    const string Folder="Assets/Prefabs/PaperBattle/";
    static readonly Color Ink=new Color(.027f,.03f,.037f,.98f);
    static readonly Color Red=new Color(.78f,.12f,.16f);
    static readonly Color Muted=new Color(.55f,.57f,.61f);
    [MenuItem("Black Cube/Apply Normal UI")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)return;
        var root=PrefabUtility.LoadPrefabContents(Folder+"PaperBattle.prefab");
        try {Configure(root);PrefabUtility.SaveAsPrefabAsset(root,Folder+"PaperBattle.prefab");}
        finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Debug.Log("PAPER2D UI: normal inventory, equipment glyphs, HUD and death menu saved.");
    }
    public static void Configure(GameObject root)
    {
        var hud=root.GetComponentInChildren<PaperBattleHUD>(true);
        var canvas=hud.GetComponentInParent<Canvas>();
        var inventory=hud.inventoryPanel;
        var old=inventory.GetComponentInChildren<InventoryUI>(true);
        if(old)Object.DestroyImmediate(old);
        for(int i=inventory.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(inventory.transform.GetChild(i).gameObject);
        var inv=inventory.AddComponent<InventoryUI>();
        Place(inventory,new Vector2(0,0),new Vector2(0,1),new Vector2(24,28),new Vector2(432,-120));
        Surface(inventory,Ink);Edge(inventory.transform);
        Text(inventory.transform,"FIELD INVENTORY",24,.08f,.87f,.88f,.96f);
        Text(inventory.transform,"RECOVERED EQUIPMENT",11,.08f,.82f,.9f,.87f,Muted);
        var close=Button(inventory.transform,"X",.86f,.89f,.96f,.96f,14);
        UnityEventTools.AddPersistentListener(close.onClick,hud.ToggleInventory);
        var scrollGO=Box(inventory.transform,"Equipment list",.07f,.1f,.93f,.8f,Color.clear);
        var scroll=scrollGO.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
        var viewport=Box(scrollGO.transform,"Viewport",0,0,1,1,Color.clear);viewport.AddComponent<RectMask2D>();
        var content=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));content.transform.SetParent(viewport.transform,false);
        var rect=Place(content,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);rect.pivot=new Vector2(.5f,1);
        var layout=content.GetComponent<VerticalLayoutGroup>();layout.spacing=8;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=rect;
        var iso=new SerializedObject(inv);iso.FindProperty("contentParent").objectReferenceValue=content.transform;iso.FindProperty("itemSlotPrefab").objectReferenceValue=Slot();iso.ApplyModifiedPropertiesWithoutUndo();
        Text(inventory.transform,"SELECT AN ITEM TO EQUIP",11,.08f,.025f,.92f,.08f,Muted);
        inventory.SetActive(false);

        for(int i=hud.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(hud.transform.GetChild(i).gameObject);
        Place(hud.gameObject,new Vector2(0,1),Vector2.one,new Vector2(0,-98),Vector2.zero);Surface(hud.gameObject,Ink);Edge(hud.transform);
        hud.runText=Text(hud.transform,"BLACK CUBE  /  FOREST 1",22,.028f,.49f,.43f,.91f);
        hud.playerText=Text(hud.transform,"WANDERER",15,.028f,.16f,.28f,.49f);
        hud.enemyText=Text(hud.transform,"GHOUL",15,.31f,.16f,.7f,.49f);
        var ib=Button(hud.transform,"INVENTORY",.75f,.25f,.875f,.76f,14);UnityEventTools.AddPersistentListener(ib.onClick,hud.ToggleInventory);
        var sb=Button(hud.transform,"STATS",.89f,.25f,.972f,.76f,14);UnityEventTools.AddPersistentListener(sb.onClick,hud.ToggleStats);
        hud.inventoryPanel=inventory;
        // Keep the existing stats view and its data bindings, inside a readable panel.
        Place(hud.statsPanel,new Vector2(1,.08f),new Vector2(1,.85f),new Vector2(-540,0),new Vector2(-24,0));
        Surface(hud.statsPanel,Ink);
        var death=root.GetComponentInChildren<DeathMenuUI>(true);
        var dso=new SerializedObject(death);
        var panel=(GameObject)dso.FindProperty("root").objectReferenceValue;
        foreach(var child in panel.GetComponentsInChildren<Transform>(true).Where(t=>t!=panel.transform && (t.name=="Signal line" || t.name=="BLACK CUBE  /  SIGNAL LOST")).ToArray())
            Object.DestroyImmediate(child.gameObject);
        Place(panel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-260,-260),new Vector2(260,240));
        Surface(panel,Ink);
        // Remove layout controllers before explicitly anchoring the existing wired controls.
        foreach(var group in panel.GetComponentsInChildren<LayoutGroup>(true))Object.DestroyImmediate(group);
        foreach(var fitter in panel.GetComponentsInChildren<ContentSizeFitter>(true))Object.DestroyImmediate(fitter);
        var title=(TMP_Text)dso.FindProperty("titleText").objectReferenceValue;
        var details=(TMP_Text)dso.FindProperty("detailsText").objectReferenceValue;
        title.transform.SetParent(panel.transform,false);details.transform.SetParent(panel.transform,false);
        Place(title.gameObject,new Vector2(.09f,.69f),new Vector2(.91f,.84f),Vector2.zero,Vector2.zero);
        Style(title,40,Color.white);title.text="YOU DIED";
        Place(details.gameObject,new Vector2(.09f,.52f),new Vector2(.91f,.68f),Vector2.zero,Vector2.zero);
        Style(details,16,Muted);details.text="Your run has ended.";
        int index=0;
        foreach(var key in new[]{"restartLevelButton","returnToMainMenuButton","quitGameButton"})
        {
            var b=(Button)dso.FindProperty(key).objectReferenceValue;b.transform.SetParent(panel.transform,false);
            float y=.37f-index*.13f;
            Place(b.gameObject,new Vector2(.09f,y),new Vector2(.91f,y+.105f),Vector2.zero,Vector2.zero);
            StyleButton(b,index==0?Red:new Color(.1f,.105f,.12f));
            var label=b.GetComponentInChildren<TMP_Text>(true);Place(label.gameObject,Vector2.zero,Vector2.one,new Vector2(14,0),new Vector2(-14,0));Style(label,16,Color.white);label.alignment=TextAlignmentOptions.Center;
            label.text=new[]{"RESTART LEVEL","RETURN TO MAIN MENU","QUIT GAME"}[index++];
        }
        foreach(var t in panel.GetComponentsInChildren<TMP_Text>(true))t.raycastTarget=false;
        Edge(panel.transform);Text(panel.transform,"BLACK CUBE  /  SIGNAL LOST",11,.09f,.86f,.91f,.94f,Red);
        panel.transform.SetAsLastSibling();panel.SetActive(false);
        PaperBattleStatsBuilder.Configure(root);
    }
    static GameObject Slot()
    {
        var go=new GameObject("Equipment Slot",typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement),typeof(ItemSlotUI));
        go.GetComponent<LayoutElement>().preferredHeight=82;
        var bg=go.GetComponent<Image>();bg.color=new Color(.065f,.068f,.08f);StyleButton(go.GetComponent<Button>(),bg.color);
        var stripe=Box(go.transform,"Rarity",0,0,.01f,1,Color.white).GetComponent<Image>();
        var glyph=new GameObject("Equipment glyph",typeof(RectTransform),typeof(EquipmentGlyph));glyph.transform.SetParent(go.transform,false);
        Place(glyph,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(14,-29),new Vector2(72,29));glyph.GetComponent<EquipmentGlyph>().raycastTarget=false;glyph.GetComponent<EquipmentGlyph>().color=new Color(.85f,.86f,.88f);
        var name=Text(go.transform,"EQUIPMENT",15,.26f,.42f,.96f,.85f);name.enableAutoSizing=true;name.fontSizeMin=11;name.fontSizeMax=15;
        var detail=Text(go.transform,"ITEM LEVEL",11,.26f,.14f,.9f,.43f,Muted);
        var so=new SerializedObject(go.GetComponent<ItemSlotUI>());
        so.FindProperty("nameText").objectReferenceValue=name;so.FindProperty("detailText").objectReferenceValue=detail;so.FindProperty("button").objectReferenceValue=go.GetComponent<Button>();so.FindProperty("backgroundImage").objectReferenceValue=bg;so.FindProperty("rarityOverlay").objectReferenceValue=stripe;so.ApplyModifiedPropertiesWithoutUndo();
        var result=PrefabUtility.SaveAsPrefabAsset(go,Folder+"EquipmentSlot.prefab");Object.DestroyImmediate(go);return result;
    }
    static RectTransform Place(GameObject go,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi)
    {var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=lo;r.offsetMax=hi;r.localScale=Vector3.one;return r;}
    static void Surface(GameObject go,Color c){var im=go.GetComponent<Image>();if(!im)im=go.AddComponent<Image>();im.sprite=null;im.color=c;}
    static GameObject Box(Transform p,string n,float x,float y,float xx,float yy,Color c)
    {var g=new GameObject(n,typeof(RectTransform),typeof(Image));g.transform.SetParent(p,false);Place(g,new Vector2(x,y),new Vector2(xx,yy),Vector2.zero,Vector2.zero);g.GetComponent<Image>().color=c;return g;}
    static void Edge(Transform p){var g=Box(p,"Signal line",0,1,1,1,Red);Place(g,new Vector2(0,1),Vector2.one,new Vector2(0,-2),Vector2.zero);g.GetComponent<Image>().raycastTarget=false;}
    static TMP_Text Text(Transform p,string value,int size,float x,float y,float xx,float yy,Color? tint=null)
    {var g=new GameObject(value,typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(p,false);Place(g,new Vector2(x,y),new Vector2(xx,yy),Vector2.zero,Vector2.zero);var t=g.GetComponent<TextMeshProUGUI>();Style(t,size,tint??new Color(.91f,.92f,.93f));t.text=value;return t;}
    static void Style(TMP_Text t,int size,Color c){t.fontSize=size;t.color=c;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;}
    static Button Button(Transform p,string label,float x,float y,float xx,float yy,int size)
    {var g=Box(p,label,x,y,xx,yy,new Color(.12f,.125f,.14f));var b=g.AddComponent<Button>();StyleButton(b,g.GetComponent<Image>().color);var t=Text(g.transform,label,size,0,0,1,1);t.alignment=TextAlignmentOptions.Center;return b;}
    static void StyleButton(Button b,Color baseColor)
    {Surface(b.gameObject,baseColor);b.targetGraphic=b.GetComponent<Image>();var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.35f,1.2f,1.2f);c.pressedColor=new Color(.7f,.6f,.6f);c.selectedColor=Color.white;c.fadeDuration=.08f;b.colors=c;}
}
