using UnityEngine;
using UnityEngine.UI;

public static class PassiveCircularIconAuthoring
{
    public static Image Wrap(Image original)
    {
        if(original==null||original.transform.parent.GetComponent<PassiveCircleMaskGraphic>()!=null)return original;
        var mask=new GameObject("Circular Icon Mask",typeof(RectTransform),typeof(CanvasRenderer),typeof(PassiveCircleMaskGraphic),typeof(Mask));mask.transform.SetParent(original.transform,false);
        Stretch((RectTransform)mask.transform);mask.GetComponent<Mask>().showMaskGraphic=false;mask.GetComponent<PassiveCircleMaskGraphic>().raycastTarget=false;
        var child=new GameObject("Icon",typeof(RectTransform),typeof(Image));child.transform.SetParent(mask.transform,false);Stretch((RectTransform)child.transform);
        var icon=child.GetComponent<Image>();icon.sprite=original.sprite;icon.color=original.color;icon.preserveAspect=true;icon.raycastTarget=true;icon.enabled=original.enabled;
        original.enabled=false;return icon;
    }
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;rect.localScale=Vector3.one;}
}
