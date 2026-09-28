using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Uses the existing prefab/RectTransform authoring model, including Unity Undo.
public sealed class PassiveTreeLayoutWindow : EditorWindow
{
    PassiveBranchDataSO data;
    PassiveBranchBinding branch;
    RectTransform selected;
    Vector2 pan;
    float zoom=.3f;
    bool dragging;
    [MenuItem("Black-Cube/Passive Tree Authoring/Layout")]
    public static void Open(){var window=GetWindow<PassiveTreeLayoutWindow>("Passive Layout");window.data=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(WarriorPassiveReauthoring.BranchPath);window.Show();}
    void OnEnable(){Undo.undoRedoPerformed+=Repaint;}
    void OnDisable(){Undo.undoRedoPerformed-=Repaint;}
    void OnGUI()
    {
        EditorGUILayout.HelpBox("Edit the production PassiveTreePanel prefab. Drag nodes/choice slots, or select one and type X/Y. Lines follow their endpoints. Values/options/icons live in the Branch SO. Save in Prefab Mode; do not regenerate the prefab.",MessageType.Info);
        data=(PassiveBranchDataSO)EditorGUILayout.ObjectField("Branch",data,typeof(PassiveBranchDataSO),false);
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Open Production Prefab")){PrefabStageUtility.OpenPrefab(PassiveTreePrefabBuilder.PrefabPath);FindBranch();Frame();}
        if(GUILayout.Button("Frame Branch")){FindBranch();Frame();}
        if(GUILayout.Button("Save Prefab")){var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null&&stage.assetPath==PassiveTreePrefabBuilder.PrefabPath)PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot,stage.assetPath);}
        EditorGUILayout.EndHorizontal();
        zoom=EditorGUILayout.Slider("Zoom",zoom,.08f,1.25f);
        if(selected!=null)
        {
            EditorGUILayout.LabelField(selected.name,EditorStyles.boldLabel);Vector2 next=EditorGUILayout.Vector2Field("Local X / Y",selected.anchoredPosition);
            if(next!=selected.anchoredPosition){Undo.RecordObject(selected,"Move passive node");selected.anchoredPosition=next;EditorUtility.SetDirty(selected);EditorApplication.QueuePlayerLoopUpdate();}
        }
        FindBranch();Rect canvas=GUILayoutUtility.GetRect(100,10000,100,10000);GUI.Box(canvas,GUIContent.none);
        if(branch==null){GUI.Label(canvas,"Open the production prefab, then select a branch.");return;}
        var elements=branch.GetComponentsInChildren<PassiveNodeBinding>(true).Select(x=>(RectTransform)x.transform).Concat(branch.GetComponentsInChildren<PassiveChoiceSlotView>(true).Select(x=>(RectTransform)x.transform)).ToArray();
        Vector2 Screen(RectTransform rect){Vector2 local=branch.transform.InverseTransformPoint(rect.position);return canvas.center+pan+new Vector2(local.x,-local.y)*zoom;}
        Handles.BeginGUI();Handles.color=new Color(.75f,.55f,.25f);
        foreach(var line in branch.GetComponentsInChildren<PassiveConnectionBinding>(true))if(line.From!=null&&line.To!=null)Handles.DrawLine(Screen(line.From),Screen(line.To));Handles.EndGUI();
        Event e=Event.current;
        foreach(var rect in elements)
        {
            // Old branches contain both native/generic prefab variants; hide inactive ones.
            if(!rect.gameObject.activeSelf||rect.parent!=branch.transform&&!rect.parent.gameObject.activeSelf)continue;
            Vector2 center=Screen(rect);float size=Mathf.Clamp(88*zoom,24,55);Rect box=new(center-Vector2.one*size*.5f,Vector2.one*size);var image=rect.GetComponent<UnityEngine.UI.Image>();
            GUI.color=rect==selected?Color.cyan:Color.white;GUI.Box(box,GUIContent.none);GUI.color=Color.white;
            if(image?.sprite!=null)GUI.DrawTexture(box,image.sprite.texture,ScaleMode.ScaleToFit,true);
            if(e.type==EventType.MouseDown&&e.button==0&&box.Contains(e.mousePosition)){selected=rect;dragging=true;Undo.RecordObject(selected,"Drag passive node");e.Use();}
        }
        if(e.type==EventType.MouseDrag&&canvas.Contains(e.mousePosition))
        {
            if(dragging&&selected!=null){selected.anchoredPosition+=new Vector2(e.delta.x,-e.delta.y)/zoom;EditorUtility.SetDirty(selected);EditorApplication.QueuePlayerLoopUpdate();}
            else if(e.button==1||e.button==2)pan+=e.delta;e.Use();Repaint();
        }
        if(e.rawType==EventType.MouseUp)dragging=false;
        if(e.type==EventType.ScrollWheel&&canvas.Contains(e.mousePosition)){zoom=Mathf.Clamp(zoom*(1-e.delta.y*.05f),.08f,1.25f);e.Use();Repaint();}
    }
    void FindBranch()
    {var stage=PrefabStageUtility.GetCurrentPrefabStage();branch=stage!=null&&stage.assetPath==PassiveTreePrefabBuilder.PrefabPath?stage.prefabContentsRoot.GetComponentsInChildren<PassiveBranchBinding>(true).FirstOrDefault(x=>x.Data==data):null;}
    void Frame()
    {if(branch==null)return;var rects=branch.GetComponentsInChildren<RectTransform>(true).Where(x=>x.GetComponent<PassiveNodeBinding>()!=null||x.GetComponent<PassiveChoiceSlotView>()!=null).ToArray();if(rects.Length==0)return;var local=rects.Select(x=>(Vector2)branch.transform.InverseTransformPoint(x.position)).ToArray();float bottom=local.Min(x=>x.y),top=local.Max(x=>x.y);zoom=Mathf.Clamp((position.height-250)/Mathf.Max(1,top-bottom+180),.08f,1);pan=new Vector2(0,(bottom+top)*.5f*zoom);}
}
