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
    GameObject editingRoot;
    string editingPath = PassiveTreePrefabBuilder.PrefabPath;
    bool dirty;
    PassiveConnectionBinding selectedLine;
    public GameObject EditingRoot=>editingRoot;
    public bool HasUnsavedChanges=>dirty;
    [MenuItem("Black-Cube/Passive Tree Authoring/Layout")]
    public static void Open(){var window=GetWindow<PassiveTreeLayoutWindow>("Passive Layout");window.data=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(WarriorPassiveReauthoring.BranchPath);window.Show();}
    public static void OpenBranch(PassiveBranchDataSO selectedBranch){Open();var window=GetWindow<PassiveTreeLayoutWindow>();window.data=selectedBranch;window.FindBranch();window.Frame();}
    void OnEnable(){Undo.undoRedoPerformed+=OnUndo;}
    void OnUndo(){if(editingRoot!=null)dirty=true;Repaint();}
    void OnDisable(){Undo.undoRedoPerformed-=OnUndo;if(editingRoot!=null){if(dirty&&EditorUtility.DisplayDialog("Unsaved passive layout","Save changes before closing?","Save","Discard"))Save();PrefabUtility.UnloadPrefabContents(editingRoot);editingRoot=null;}}
    public void LoadEditingCopy() => LoadEditingCopy(PassiveTreePrefabBuilder.PrefabPath);
    public void LoadEditingCopy(string prefabPath)
    {
        if(editingRoot!=null){if(dirty&&!EditorUtility.DisplayDialog("Discard unsaved layout?","Reload discards staged changes.","Discard","Cancel"))return;PrefabUtility.UnloadPrefabContents(editingRoot);}
        editingRoot=PrefabUtility.LoadPrefabContents(prefabPath);editingPath=prefabPath;selected=null;selectedLine=null;dirty=false;FindBranch();Frame();
    }
    public void Save(){if(editingRoot==null)return;foreach(var line in editingRoot.GetComponentsInChildren<PassiveConnectionBinding>(true))line.UpdateGeometry();PrefabUtility.SaveAsPrefabAsset(editingRoot,editingPath);dirty=false;}
    public void MoveNode(RectTransform rect,Vector2 position){Undo.RecordObject(rect,"Move passive node");rect.anchoredPosition=position;dirty=true;Repaint();}
    public void Discard(){dirty=false;if(editingRoot!=null)PrefabUtility.UnloadPrefabContents(editingRoot);editingRoot=null;branch=null;selected=null;selectedLine=null;}
    void OnGUI()
    {
        EditorGUILayout.HelpBox("Drag multiple nodes in an isolated editing copy; only SAVE writes/reimports the production prefab. Do not edit that prefab elsewhere while this copy is open. Connections have normalized anchors (0–1) plus local-unit offsets. Values/options/icons live in the Branch SO.",MessageType.Info);
        EditorGUI.BeginChangeCheck();data=(PassiveBranchDataSO)EditorGUILayout.ObjectField("Branch",data,typeof(PassiveBranchDataSO),false);if(EditorGUI.EndChangeCheck()){selected=null;selectedLine=null;FindBranch();Frame();}
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button(editingRoot==null?"Open Editing Copy":"Reload / Discard"))LoadEditingCopy();
        if(GUILayout.Button("Frame Branch")){FindBranch();Frame();}
        using(new EditorGUI.DisabledScope(editingRoot==null))if(GUILayout.Button(dirty?"SAVE *":"SAVE"))Save();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(dirty?"UNSAVED LAYOUT CHANGES":"Layout saved / unchanged");
        zoom=EditorGUILayout.Slider("Zoom",zoom,.08f,1.25f);
        if(selected!=null)
        {
            EditorGUILayout.LabelField(selected.name,EditorStyles.boldLabel);Vector2 next=EditorGUILayout.Vector2Field("Local X / Y",selected.anchoredPosition);
            if(next!=selected.anchoredPosition)MoveNode(selected,next);
        }
        FindBranch();
        if(branch!=null)
        {
            var lines=branch.GetComponentsInChildren<PassiveConnectionBinding>(true).Where(x=>x.From!=null&&x.To!=null).ToArray();string[] labels=new[]{"Select connection…"}.Concat(lines.Select(x=>x.From.name+" → "+x.To.name)).ToArray();int current=System.Array.IndexOf(lines,selectedLine)+1;int next=EditorGUILayout.Popup("Line attachment",current,labels);selectedLine=next>0?lines[next-1]:null;
            if(selectedLine!=null)
            {
                EditorGUI.BeginChangeCheck();Vector2 a=EditorGUILayout.Vector2Field("From anchor (0–1)",selectedLine.FromAnchor),b=EditorGUILayout.Vector2Field("To anchor (0–1)",selectedLine.ToAnchor),c=EditorGUILayout.Vector2Field("From local offset",selectedLine.FromOffset),d=EditorGUILayout.Vector2Field("To local offset",selectedLine.ToOffset);
                if(EditorGUI.EndChangeCheck()){Undo.RecordObject(selectedLine,"Edit line attachments");selectedLine.FromAnchor=a;selectedLine.ToAnchor=b;selectedLine.FromOffset=c;selectedLine.ToOffset=d;dirty=true;}
                if(GUILayout.Button("Center both attachments")){Undo.RecordObject(selectedLine,"Center line");selectedLine.FromAnchor=selectedLine.ToAnchor=Vector2.one*.5f;selectedLine.FromOffset=selectedLine.ToOffset=Vector2.zero;dirty=true;}
            }
        }
        Rect canvas=GUILayoutUtility.GetRect(100,10000,100,10000);GUI.Box(canvas,GUIContent.none);
        if(branch==null){GUI.Label(canvas,"Open the production prefab, then select a branch.");return;}
        var elements=branch.GetComponentsInChildren<PassiveNodeBinding>(true).Select(x=>(RectTransform)x.transform).Concat(branch.GetComponentsInChildren<PassiveChoiceSlotView>(true).Select(x=>(RectTransform)x.transform)).ToArray();
        Vector2 Endpoint(Vector3 world){Vector2 local=branch.transform.InverseTransformPoint(world);return canvas.center+pan+new Vector2(local.x,-local.y)*zoom;}
        Handles.BeginGUI();Handles.color=new Color(.75f,.55f,.25f);
        foreach(var line in branch.GetComponentsInChildren<PassiveConnectionBinding>(true))if(line.From!=null&&line.To!=null){Handles.color=line==selectedLine?Color.cyan:new Color(.75f,.55f,.25f);Handles.DrawLine(Endpoint(line.FromWorldPoint),Endpoint(line.ToWorldPoint));if(line==selectedLine){Handles.DrawSolidDisc(Endpoint(line.FromWorldPoint),Vector3.forward,4);Handles.DrawSolidDisc(Endpoint(line.ToWorldPoint),Vector3.forward,4);}}Handles.EndGUI();
        Event e=Event.current;
        foreach(var rect in elements)
        {
            // Old branches contain both native/generic prefab variants; hide inactive ones.
            if(!rect.gameObject.activeSelf||rect.parent!=branch.transform&&!rect.parent.gameObject.activeSelf)continue;
            Vector2 center=Endpoint(rect.TransformPoint(rect.rect.center));float size=Mathf.Clamp(88*zoom,24,55);Rect box=new(center-Vector2.one*size*.5f,Vector2.one*size);var binding=rect.GetComponent<PassiveNodeBinding>();var slot=rect.GetComponent<PassiveChoiceSlotView>();var image=binding!=null?binding.Icon:slot?.icon;
            GUI.color=rect==selected?Color.cyan:Color.white;GUI.Box(box,GUIContent.none);GUI.color=Color.white;
            if(image?.sprite!=null)GUI.DrawTexture(box,image.sprite.texture,ScaleMode.ScaleToFit,true);
            if(e.type==EventType.MouseDown&&e.button==0&&box.Contains(e.mousePosition)){selected=rect;dragging=true;Undo.RecordObject(selected,"Drag passive node");e.Use();}
        }
        if(e.type==EventType.MouseDrag&&canvas.Contains(e.mousePosition))
        {
            if(dragging&&selected!=null){selected.anchoredPosition+=new Vector2(e.delta.x,-e.delta.y)/zoom;dirty=true;foreach(var line in branch.GetComponentsInChildren<PassiveConnectionBinding>(true))line.UpdateGeometry();}
            else if(e.button==1||e.button==2)pan+=e.delta;e.Use();Repaint();
        }
        if(e.rawType==EventType.MouseUp)dragging=false;
        if(e.type==EventType.ScrollWheel&&canvas.Contains(e.mousePosition)){zoom=Mathf.Clamp(zoom*(1-e.delta.y*.05f),.08f,1.25f);e.Use();Repaint();}
    }
    void FindBranch()
    {branch=editingRoot!=null?editingRoot.GetComponentsInChildren<PassiveBranchBinding>(true).FirstOrDefault(x=>x.Data==data):null;}
    void Frame()
    {if(branch==null)return;var rects=branch.GetComponentsInChildren<RectTransform>(true).Where(x=>x.GetComponent<PassiveNodeBinding>()!=null||x.GetComponent<PassiveChoiceSlotView>()!=null).ToArray();if(rects.Length==0)return;var local=rects.Select(x=>(Vector2)branch.transform.InverseTransformPoint(x.position)).ToArray();float bottom=local.Min(x=>x.y),top=local.Max(x=>x.y);zoom=Mathf.Clamp((position.height-250)/Mathf.Max(1,top-bottom+180),.08f,1);pan=new Vector2(0,(bottom+top)*.5f*zoom);}
}
