using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PaperBattleStatsBuilder
{
    const string Folder = "Assets/Prefabs/PaperBattle/";
    static readonly Color Ink = new Color(.027f,.03f,.037f,.98f);
    static readonly Color Red = new Color(.78f,.12f,.16f);

    [MenuItem("Black Cube/Apply Stats Equipment UI")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) return;
        var root = PrefabUtility.LoadPrefabContents(Folder + "PaperBattle.prefab");
        try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, Folder + "PaperBattle.prefab"); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Debug.Log("PAPER2D: Eight equipment categories and readable scrolling stats saved.");
    }
    public static void Configure(GameObject root)
    {
        var hud = root.GetComponentInChildren<PaperBattleHUD>(true);
        var panel = hud.statsPanel;
        var previous = panel.GetComponentInChildren<PlayerStatsPanelUI>(true);
        var source = new SerializedObject(previous);
        var target = source.FindProperty("playerStats").objectReferenceValue;
        Object.DestroyImmediate(previous);
        var oldEquipment = panel.GetComponent<EquipmentStatsUI>();
        if (oldEquipment) Object.DestroyImmediate(oldEquipment);
        for (int i = panel.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);
        foreach (var layout in panel.GetComponents<LayoutGroup>()) Object.DestroyImmediate(layout);
        foreach (var fitter in panel.GetComponents<ContentSizeFitter>()) Object.DestroyImmediate(fitter);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1,0); rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-604,28); rect.offsetMax = new Vector2(-24,-120);
        panel.GetComponent<Image>().color = Ink;
        Box(panel.transform,"Signal line",0,.997f,1,1,Red);
        Text(panel.transform,"WANDERER",24,.05f,.9f,.8f,.97f);
        Text(panel.transform,"EQUIPPED GEAR",11,.05f,.86f,.85f,.9f,Red);
        var close = Box(panel.transform,"Close",.88f,.91f,.96f,.97f,new Color(.12f,.125f,.14f)).AddComponent<Button>();
        Text(close.transform,"X",14,0,0,1,1).alignment = TextAlignmentOptions.Center;
        UnityEventTools.AddPersistentListener(close.onClick,hud.ToggleStats);
        var equipment = panel.AddComponent<EquipmentStatsUI>();
        var types = (LootManager.GearType[])System.Enum.GetValues(typeof(LootManager.GearType));
        equipment.slots = new EquipmentStatsUI.Slot[types.Length];
        for (int i=0;i<types.Length;i++)
        {
            float x = .05f + (i%2)*.46f, y = .744f - (i/2)*.11f;
            var border = Box(panel.transform,ItemSlotUI.DisplayType(types[i]),x,y,x+.44f,y+.095f,new Color(.29f,.31f,.35f));
            var inner = Box(border.transform,"Surface",0,0,1,1,new Color(.06f,.066f,.078f));
            var ir=inner.GetComponent<RectTransform>();ir.offsetMin=new Vector2(1,1);ir.offsetMax=new Vector2(-1,-1);
            var glyph = new GameObject("Category icon",typeof(RectTransform),typeof(EquipmentGlyph));glyph.transform.SetParent(inner.transform,false);
            var gr = glyph.GetComponent<RectTransform>();gr.anchorMin=new Vector2(0,.5f);gr.anchorMax=new Vector2(0,.5f);gr.sizeDelta=new Vector2(48,48);gr.anchoredPosition=new Vector2(32,0);
            var graphic=glyph.GetComponent<EquipmentGlyph>();graphic.gearType=types[i];graphic.color=new Color(.29f,.31f,.35f);graphic.raycastTarget=false;
            equipment.slots[i]=new EquipmentStatsUI.Slot {
                type=types[i],glyph=graphic,border=border.GetComponent<Image>(),
                label=Text(inner.transform,ItemSlotUI.DisplayType(types[i]),15,.27f,.43f,.97f,.88f),
                detail=Text(inner.transform,"EMPTY",10,.27f,.12f,.97f,.45f,new Color(.48f,.5f,.54f))
            };
        }
        Text(panel.transform,"ACTIVE STATS",11,.05f,.345f,.8f,.395f,Red);
        var scrollGO=Box(panel.transform,"Stats scroll",.05f,.055f,.95f,.34f,Color.clear);
        var scroll=scrollGO.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
        var viewport=Box(scrollGO.transform,"Viewport",0,0,1,1,Color.clear);viewport.AddComponent<RectMask2D>();
        var content=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));content.transform.SetParent(viewport.transform,false);
        var cr=content.GetComponent<RectTransform>();cr.anchorMin=new Vector2(0,1);cr.anchorMax=Vector2.one;cr.pivot=new Vector2(.5f,1);cr.sizeDelta=Vector2.zero;cr.anchoredPosition=Vector2.zero;
        var group=content.GetComponent<VerticalLayoutGroup>();group.childControlWidth=true;group.childControlHeight=true;group.childForceExpandWidth=true;group.childForceExpandHeight=false;group.spacing=3;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=cr;
        Text(panel.transform,"SCROLL TO VIEW ALL STATS",10,.05f,.012f,.95f,.048f,new Color(.48f,.5f,.54f));
        var stats=panel.AddComponent<PlayerStatsPanelUI>();var so=new SerializedObject(stats);
        so.FindProperty("playerStats").objectReferenceValue=target;so.FindProperty("contentRoot").objectReferenceValue=content.transform;
        so.FindProperty("headerPrefab").objectReferenceValue=Header();so.FindProperty("rowPrefab").objectReferenceValue=Row();
        so.FindProperty("autoRefreshInterval").floatValue=.5f;so.ApplyModifiedPropertiesWithoutUndo();panel.SetActive(false);
    }
    static StatHeaderUI Header()
    {
        var go=new GameObject("Stat Header",typeof(RectTransform),typeof(LayoutElement),typeof(StatHeaderUI));go.GetComponent<LayoutElement>().preferredHeight=25;
        var so=new SerializedObject(go.GetComponent<StatHeaderUI>());so.FindProperty("headerText").objectReferenceValue=Text(go.transform,"CATEGORY",12,0,0,1,1,Red);so.ApplyModifiedPropertiesWithoutUndo();
        var result=PrefabUtility.SaveAsPrefabAsset(go,Folder+"StatHeader.prefab").GetComponent<StatHeaderUI>();Object.DestroyImmediate(go);return result;
    }
    static StatRowUI Row()
    {
        var go=new GameObject("Stat Row",typeof(RectTransform),typeof(LayoutElement),typeof(StatRowUI));go.GetComponent<LayoutElement>().preferredHeight=25;
        var so=new SerializedObject(go.GetComponent<StatRowUI>());so.FindProperty("nameText").objectReferenceValue=Text(go.transform,"Stat",13,0,0,.78f,1);
        var value=Text(go.transform,"0",13,.79f,0,1,1);value.alignment=TextAlignmentOptions.MidlineRight;so.FindProperty("valueText").objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
        var result=PrefabUtility.SaveAsPrefabAsset(go,Folder+"StatRow.prefab").GetComponent<StatRowUI>();Object.DestroyImmediate(go);return result;
    }
    static GameObject Box(Transform parent,string name,float x,float y,float xx,float yy,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        Place(go,x,y,xx,yy);go.GetComponent<Image>().color=color;return go;
    }
    static TMP_Text Text(Transform parent,string text,int size,float x,float y,float xx,float yy,Color? color=null)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);Place(go,x,y,xx,yy);
        var t=go.GetComponent<TextMeshProUGUI>();t.text=text;t.fontSize=size;t.color=color??new Color(.91f,.92f,.93f);t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;
        t.enableAutoSizing=true;t.fontSizeMin=size-2;t.fontSizeMax=size;return t;
    }
    static void Place(GameObject go,float x,float y,float xx,float yy)
    {var r=go.GetComponent<RectTransform>();r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;}
}
