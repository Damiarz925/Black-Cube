// Responsive shared owner for the runtime-built bottom HUD controls.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BottomActionBarLayout:MonoBehaviour
{
    static BottomActionBarLayout instance;
    readonly SortedDictionary<int,RectTransform> controls=new();
    public IReadOnlyCollection<RectTransform> Controls=>controls.Values;
    void Awake(){instance=this;Configure();}
    void OnDestroy(){if(instance==this)instance=null;}
    void Configure()
    {
        var rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,12);rect.sizeDelta=new Vector2(1180,64);
        var layout=GetComponent<HorizontalLayoutGroup>()??gameObject.AddComponent<HorizontalLayoutGroup>();layout.padding=new RectOffset(0,0,0,0);layout.spacing=8;layout.childAlignment=TextAnchor.MiddleCenter;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=false;layout.childForceExpandHeight=true;
    }
    public static BottomActionBarLayout For(Canvas canvas)
    {
        if(instance!=null&&instance)return instance;if(canvas==null)return null;
        var existing=canvas.transform.Find("Bottom Action Bar");if(existing!=null)return instance=existing.GetComponent<BottomActionBarLayout>()??existing.gameObject.AddComponent<BottomActionBarLayout>();
        var go=new GameObject("Bottom Action Bar",typeof(RectTransform),typeof(HorizontalLayoutGroup),typeof(BottomActionBarLayout));go.transform.SetParent(canvas.transform,false);return instance=go.GetComponent<BottomActionBarLayout>();
    }
    public static void Attach(Button button,int order,float preferredWidth)
    {
        if(button==null)return;Canvas canvas=button.GetComponentInParent<Canvas>();var bar=For(canvas);if(bar==null)return;
        RectTransform rect=(RectTransform)button.transform;rect.SetParent(bar.transform,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        var element=button.GetComponent<LayoutElement>()??button.gameObject.AddComponent<LayoutElement>();element.minWidth=Mathf.Min(110,preferredWidth);element.preferredWidth=preferredWidth;element.flexibleWidth=.2f;element.minHeight=54;element.preferredHeight=64;
        foreach(var old in new List<int>(bar.controls.Keys))if(bar.controls[old]==null)bar.controls.Remove(old);bar.controls[order]=rect;
        int sibling=0;foreach(var pair in bar.controls){pair.Value.SetSiblingIndex(sibling++);var text=pair.Value.GetComponentInChildren<TMP_Text>(true);if(text!=null){text.enableAutoSizing=true;text.fontSizeMin=9;text.fontSizeMax=Mathf.Max(12,text.fontSize);text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;}}
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)bar.transform);
    }
}

public static class HUDRectOverlapValidator
{
    public static List<string> FindOverlaps(IEnumerable<RectTransform> rects,float tolerance=.5f)
    {
        var active=new List<RectTransform>();if(rects!=null)foreach(var rect in rects)if(rect!=null&&rect.gameObject.activeInHierarchy)active.Add(rect);var result=new List<string>();
        for(int i=0;i<active.Count;i++)for(int j=i+1;j<active.Count;j++){Rect a=WorldRect(active[i]),b=WorldRect(active[j]);if(Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin)>tolerance&&Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin)>tolerance)result.Add(active[i].name+" overlaps "+active[j].name);}
        return result;
    }
    static Rect WorldRect(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}
}
