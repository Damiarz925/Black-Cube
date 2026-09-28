using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Only authored branch roots and camera framing move.
public sealed class PassiveTreePresentation : MonoBehaviour
{
    PassiveTreeView view;PassiveTreeLayoutSO config;PlayerProgression progression;string layoutKey;
    Vector2 overviewPan;float overviewZoom;Coroutine transition;
    public bool WeaponFocused{get;private set;}
    public float FitZoom{get;private set;}=.2f;
    public float MaximumZoom=>config!=null?config.maximumZoom:1.35f;
    public float ZoomStep=>config!=null?config.zoomStep:.08f;
    public void Initialize(PassiveTreeView authored,PlayerProgression progress,System.Action refresh=null)
    {
        view=authored;progression=progress;config=view.layout!=null?view.layout:Resources.Load<PassiveTreeLayoutSO>("GameData/PassiveTree/SO_PassiveTreeLayout");
        view.GetComponent<PassiveTreeNavigation>()?.Initialize(view,this,progression);
    }
    public bool Visible(PassiveNodeDefinition node)
    {
        if(progression==null)return false;
        if(node.IsClassRoute)return !WeaponFocused&&(node.RouteClassId==progression.ActiveClassId||progression.SelectedClassRoutes.Contains(node.RouteClassId));
        return node.RouteWeaponId==progression.SelectedWeaponTreeId;
    }
    public bool Refresh()
    {
        if(view==null||config==null||progression==null)return false;
        if(string.IsNullOrEmpty(progression.SelectedWeaponTreeId))WeaponFocused=false;
        string key=progression.ActiveClassId+"|"+string.Join(",",progression.SelectedClassRoutes)+"|"+progression.SelectedWeaponTreeId+"|"+WeaponFocused;
        if(layoutKey==key)return false;layoutKey=key;
        if(view.playerHub!=null){view.playerHub.gameObject.SetActive(!WeaponFocused);view.playerHub.anchoredPosition=Vector2.zero;view.playerHub.sizeDelta=Vector2.one*config.hubSize;}
        foreach(var branch in view.branches)
        {
            bool weapon=branch.Data is PassiveWeaponBranchSO;bool visible=weapon?branch.RouteId==progression.SelectedWeaponTreeId:!WeaponFocused&&(branch.RouteId==progression.ActiveClassId||progression.SelectedClassRoutes.Contains(branch.RouteId));branch.gameObject.SetActive(visible);
            var rect=(RectTransform)branch.transform;rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one;
            if(weapon)
            {
                float scale=WeaponFocused?1:.13f;rect.localScale=Vector3.one*scale;rect.anchoredPosition=new Vector2(0,-540*scale);
                var group=branch.GetComponent<CanvasGroup>();
                if(group==null)group=branch.gameObject.AddComponent<CanvasGroup>();
                group.interactable=WeaponFocused;group.blocksRaycasts=WeaponFocused;
            }
            else
            {
                int index=PassiveTreeDefinition.ClassIndex(branch.RouteId);float angle=(90-index*60)*Mathf.Deg2Rad;
                int count=1+progression.SelectedClassRoutes.Count;
                rect.anchoredPosition=count==1?Vector2.zero:new Vector2(Mathf.Cos(angle)*1550,Mathf.Sin(angle)*1300-1050);
            }
        }
        foreach(var badge in view.classBadges)badge.gameObject.SetActive(false);
        if(!WeaponFocused&&view.classBadges.Count>0)
        {
            var badge=view.classBadges[0];badge.gameObject.SetActive(true);((RectTransform)badge.transform).anchoredPosition=new Vector2(0,config.badgeRadius);badge.sprite=config.Badge(progression.ActiveClassId);badge.preserveAspect=true;var label=badge.GetComponentInChildren<TMP_Text>(true);if(label!=null){label.text=progression.ActiveClassId.Replace("class.","").ToUpperInvariant();label.gameObject.SetActive(badge.sprite==null);}
        }
        FitContent();return true;
    }
    public bool EnterWeaponFocus()
    {
        if(WeaponFocused||string.IsNullOrEmpty(progression?.SelectedWeaponTreeId))return false;
        overviewPan=view.content.anchoredPosition;overviewZoom=view.content.localScale.x;WeaponFocused=true;Refresh();
        Vector2 target=view.content.anchoredPosition;float zoom=FitZoom;view.content.anchoredPosition=overviewPan;view.content.localScale=Vector3.one*overviewZoom;Animate(target,zoom);return true;
    }
    public void ExitWeaponFocus(){if(!WeaponFocused)return;WeaponFocused=false;Refresh();Animate(overviewPan,Mathf.Clamp(overviewZoom,FitZoom,MaximumZoom));}
    void Animate(Vector2 pan,float zoom){if(transition!=null)StopCoroutine(transition);if(!Application.isPlaying){view.content.anchoredPosition=pan;view.content.localScale=Vector3.one*zoom;GetComponent<SkillTreeUI>()?.SyncPresentationZoom(zoom);return;}transition=StartCoroutine(Transition(pan,zoom));}
    System.Collections.IEnumerator Transition(Vector2 pan,float zoom)
    {
        Vector2 start=view.content.anchoredPosition;float old=view.content.localScale.x;view.scroll.StopMovement();
        for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){float blend=Mathf.SmoothStep(0,1,t/.3f);view.content.anchoredPosition=Vector2.Lerp(start,pan,blend);float current=Mathf.Lerp(old,zoom,blend);view.content.localScale=Vector3.one*current;GetComponent<SkillTreeUI>()?.SyncPresentationZoom(current);yield return null;}
        view.content.anchoredPosition=pan;view.content.localScale=Vector3.one*zoom;GetComponent<SkillTreeUI>()?.SyncPresentationZoom(zoom);transition=null;
    }
    void FitContent()
    {
        if(view.content==null||view.scroll==null)return;var bounds=new Bounds();bool first=true;
        void Include(RectTransform rect){if(rect==null)return;var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var corner in corners){Vector3 p=view.content.InverseTransformPoint(corner);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}}
        if(!WeaponFocused)Include(view.playerHub);foreach(var badge in view.classBadges)if(badge.gameObject.activeSelf)Include((RectTransform)badge.transform);
        foreach(var branch in view.branches.Where(x=>x.gameObject.activeSelf)){foreach(var tier in branch.Tiers){Include((RectTransform)tier.spine.transform);Include(tier.leftSlot!=null?(RectTransform)tier.leftSlot.transform:null);Include(tier.rightSlot!=null?(RectTransform)tier.rightSlot.transform:null);}if(branch.keystoneSlot!=null)Include((RectTransform)branch.keystoneSlot.transform);}
        Vector2 center=bounds.center;foreach(var branch in view.branches)((RectTransform)branch.transform).anchoredPosition-=center;if(view.playerHub!=null)view.playerHub.anchoredPosition-=center;foreach(var badge in view.classBadges)((RectTransform)badge.transform).anchoredPosition-=center;
        view.content.sizeDelta=(Vector2)bounds.size+Vector2.one*(config.fitPadding*2);RecalculateFit();view.content.localScale=Vector3.one*FitZoom;view.content.anchoredPosition=Vector2.zero;view.scroll.StopMovement();view.scroll.movementType=ScrollRect.MovementType.Clamped;
    }
    public void RecalculateFit(){if(view?.scroll?.viewport==null)return;FitZoom=CalculateFit(view.scroll.viewport.rect.size,view.content.sizeDelta,MaximumZoom);}
    public static float CalculateFit(Vector2 viewport,Vector2 content,float maximum)=>Mathf.Clamp(Mathf.Min(viewport.x/Mathf.Max(1,content.x),viewport.y/Mathf.Max(1,content.y)),.01f,maximum);
}
