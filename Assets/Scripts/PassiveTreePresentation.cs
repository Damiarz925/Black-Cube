using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Only branch roots move at runtime. Node/slot coordinates remain prefab-authored.
public sealed class PassiveTreePresentation : MonoBehaviour
{
    PassiveTreeView view; PassiveTreeLayoutSO config; PlayerProgression progression;
    string layoutKey;
    public float FitZoom { get; private set; } = .2f;
    public float MaximumZoom => config != null ? config.maximumZoom : 1.35f;
    public float ZoomStep => config != null ? config.zoomStep : .08f;
    public void Initialize(PassiveTreeView authored, PlayerProgression progress, System.Action refresh = null)
    { view=authored;progression=progress;config=view.layout!=null?view.layout:Resources.Load<PassiveTreeLayoutSO>("GameData/PassiveTree/SO_PassiveTreeLayout"); }
    public bool Visible(PassiveNodeDefinition node)
    {
        if(progression==null)return false;
        if(node.IsClassRoute)return node.RouteClassId==progression.ActiveClassId;
        return PassiveTreeDefinition.WeaponClass(node.RouteWeaponId)==progression.ActiveClassId&&progression.NativeSpineComplete;
    }
    public bool Refresh()
    {
        if(view==null||config==null||progression==null)return false;
        string key=progression.ActiveClassId+"|"+progression.NativeSpineComplete;
        foreach(var branch in view.branches)
        { string route=branch.Data is PassiveClassBranchSO c?c.ClassId:((PassiveWeaponBranchSO)branch.Data).OwningClassId;branch.gameObject.SetActive(route==progression.ActiveClassId&&(branch.Data is PassiveClassBranchSO||progression.NativeSpineComplete)); }
        if(layoutKey==key)return false;layoutKey=key;
        if(view.playerHub!=null){view.playerHub.anchoredPosition=config.hubOffset;view.playerHub.sizeDelta=Vector2.one*config.hubSize;}
        foreach(var branch in view.branches)
        {
            var rect=(RectTransform)branch.transform;Vector2 anchor=config.hubOffset+config.Anchor(1,0);
            if(branch.Data is PassiveWeaponBranchSO weapon)
            {
                var owner=view.branches.Find(x=>x.RouteId==weapon.OwningClassId);
                if(owner!=null)anchor.y+=((RectTransform)owner.Tiers[owner.Tiers.Count-1].spine.transform).anchoredPosition.y+config.weaponGap-((RectTransform)branch.Tiers[0].spine.transform).anchoredPosition.y;
            }
            rect.anchoredPosition=anchor;rect.localRotation=Quaternion.identity;
        }
        for(int i=0;i<view.classBadges.Count;i++)
        {
            var badge=view.classBadges[i];badge.gameObject.SetActive(i==0);if(i!=0)continue;
            var rect=(RectTransform)badge.transform;rect.anchoredPosition=config.hubOffset+Vector2.up*config.badgeRadius;rect.sizeDelta=Vector2.one*config.badgeSize;
            badge.sprite=config.Badge(progression.ActiveClassId);badge.preserveAspect=true;badge.color=badge.sprite!=null?Color.white:new Color(.25f,.3f,.4f);
            var label=badge.GetComponentInChildren<TMP_Text>(true);if(label!=null){label.gameObject.SetActive(badge.sprite==null);label.text=progression.ActiveClassId.Replace("class.","").ToUpperInvariant();}
        }
        FitContent();return true;
    }
    void FitContent()
    {
        if(view.content==null||view.scroll==null)return;var bounds=new Bounds(Vector3.zero,Vector3.zero);bool first=true;
        void Include(RectTransform rect)
        {if(rect==null)return;var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var corner in corners){Vector3 p=view.content.InverseTransformPoint(corner);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}}
        Include(view.playerHub);foreach(var badge in view.classBadges)if(badge.gameObject.activeSelf)Include((RectTransform)badge.transform);
        foreach(var branch in view.branches)
        {
            if(!branch.gameObject.activeSelf)continue;
            foreach(var tier in branch.Tiers)
            {
                Include((RectTransform)tier.spine.transform);
                if(tier.leftSlot!=null){Include((RectTransform)tier.leftSlot.transform);Include((RectTransform)tier.rightSlot.transform);}
                else{foreach(var node in tier.left.RuntimeNodes(true))if(node!=null)Include((RectTransform)node.transform);foreach(var node in tier.right.RuntimeNodes(true))if(node!=null)Include((RectTransform)node.transform);}
            }
        }
        Vector2 center=bounds.center;foreach(var branch in view.branches)((RectTransform)branch.transform).anchoredPosition-=center;
        if(view.playerHub!=null)view.playerHub.anchoredPosition-=center;foreach(var badge in view.classBadges)((RectTransform)badge.transform).anchoredPosition-=center;
        view.content.sizeDelta=(Vector2)bounds.size+Vector2.one*(config.fitPadding*2);RecalculateFit();view.content.localScale=Vector3.one*FitZoom;view.content.anchoredPosition=Vector2.zero;view.scroll.StopMovement();view.scroll.movementType=ScrollRect.MovementType.Clamped;
    }
    public void RecalculateFit(){if(view?.scroll?.viewport==null)return;FitZoom=CalculateFit(view.scroll.viewport.rect.size,view.content.sizeDelta,MaximumZoom);}
    public static float CalculateFit(Vector2 viewport,Vector2 content,float maximum)=>Mathf.Clamp(Mathf.Min(viewport.x/Mathf.Max(1,content.x),viewport.y/Mathf.Max(1,content.y)),.01f,maximum);
}
