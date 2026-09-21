using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class StatusHUDAuthoringBuilder
{
    public const string PrefabPath="Assets/Prefabs/UI/StatusHUD.prefab";
    public const string BadgePrefabPath="Assets/Prefabs/UI/StatusBadge.prefab";

    [MenuItem("Black-Cube/UI Authoring/Build Status HUD")]
    public static void Build()
    {
        StatusBadge badge=BuildBadge();PrefabUtility.SaveAsPrefabAsset(badge.gameObject,BadgePrefabPath);UnityEngine.Object.DestroyImmediate(badge.gameObject);badge=AssetDatabase.LoadAssetAtPath<GameObject>(BadgePrefabPath).GetComponent<StatusBadge>();
        GameObject root=PrefabUtility.LoadPrefabContents(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        try
        {
            PaperBattleHUD hud=root.GetComponentInChildren<PaperBattleHUD>(true);if(hud==null)throw new InvalidOperationException("Production HUD is missing.");Canvas canvas=hud.GetComponentInParent<Canvas>();if(canvas==null)throw new InvalidOperationException("Production Canvas is missing.");
            StatusHUD controller=hud.GetComponent<StatusHUD>()??hud.gameObject.AddComponent<StatusHUD>();StatusHUDView view=canvas.GetComponentInChildren<StatusHUDView>(true);
            if(view==null)view=BuildView(canvas.transform,badge);else view.badgePrefab=badge;
            PrefabUtility.SaveAsPrefabAsset(view.gameObject,PrefabPath);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(view);PrefabUtility.SaveAsPrefabAsset(root,PersistentUIAuthoringInstaller.GameplayPrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("STATUS HUD AUTHORING: Built authored strips, tooltip, and badge prefab.");
    }

    static StatusHUDView BuildView(Transform parent,StatusBadge badge)
    {
        GameObject group=new("Status HUD",typeof(RectTransform),typeof(StatusHUDView));group.transform.SetParent(parent,false);RectTransform gr=(RectTransform)group.transform;gr.anchorMin=Vector2.zero;gr.anchorMax=Vector2.one;gr.offsetMin=gr.offsetMax=Vector2.zero;
        StatusHUDView view=group.GetComponent<StatusHUDView>();view.authoredRoot=gr;view.playerStrip=Strip(group.transform,"WANDERER EFFECTS",new Vector2(650,-235));view.enemyStrip=Strip(group.transform,"ENEMY EFFECTS",new Vector2(960,-235));
        GameObject tip=Box(group.transform,"Status details",new Color(.025f,.028f,.035f,1));view.tooltip=(RectTransform)tip.transform;view.tooltip.anchorMin=view.tooltip.anchorMax=new Vector2(.5f,.77f);view.tooltip.pivot=new Vector2(.5f,1);view.tooltip.sizeDelta=new Vector2(380,148);view.tooltipText=Text(tip.transform,"",13);view.tooltipText.alignment=TextAlignmentOptions.TopLeft;view.tooltipText.rectTransform.offsetMin=new Vector2(14,10);view.tooltipText.rectTransform.offsetMax=new Vector2(-14,-10);tip.GetComponent<Image>().raycastTarget=false;tip.SetActive(false);view.badgePrefab=badge;return view;
    }
    static RectTransform Strip(Transform parent,string title,Vector2 position){GameObject root=Box(parent,title,new Color(.025f,.028f,.035f,.98f));RectTransform r=(RectTransform)root.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=new Vector2(300,50);var grid=root.AddComponent<GridLayoutGroup>();grid.padding=new RectOffset(4,4,13,2);grid.cellSize=new Vector2(55,35);grid.spacing=new Vector2(4,0);TMP_Text label=Text(root.transform,title,10);label.color=new Color(.8f,.82f,.85f);label.rectTransform.anchorMin=new Vector2(0,.75f);var layout=label.gameObject.AddComponent<LayoutElement>();layout.ignoreLayout=true;return r;}
    static StatusBadge BuildBadge(){GameObject go=Box(null,"Status Badge",new Color(.035f,.04f,.05f,.98f));RectTransform rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(55,35);StatusBadge badge=go.AddComponent<StatusBadge>();badge.Label=Text(go.transform,"",13);badge.Label.alignment=TextAlignmentOptions.Center;badge.Label.rectTransform.anchorMin=new Vector2(.48f,0);GameObject icon=new("Status icon",typeof(RectTransform),typeof(StatusGlyph));icon.transform.SetParent(go.transform,false);RectTransform ir=(RectTransform)icon.transform;ir.anchorMin=new Vector2(.1f,.2f);ir.anchorMax=new Vector2(.45f,.8f);ir.offsetMin=ir.offsetMax=Vector2.zero;badge.Glyph=icon.GetComponent<StatusGlyph>();badge.Glyph.raycastTarget=false;return badge;}
    static GameObject Box(Transform parent,string name,Color color){GameObject go=new(name,typeof(RectTransform),typeof(Image));if(parent!=null)go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go;}
    static TMP_Text Text(Transform parent,string value,int size){GameObject go=new("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);TMP_Text t=go.GetComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.raycastTarget=false;t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;return t;}
}
