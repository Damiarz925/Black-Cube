using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Reusable status badges for both combatants. Rebinds when enemies are replaced.</summary>
public class StatusHUD : MonoBehaviour
{
    class Strip
    {
        public RectTransform Root;
        public StatusController Target;
        public readonly Dictionary<StatusEffects,StatusBadge> Badges=new();
    }
    Strip playerStrip,enemyStrip;
    PaperBattleHUD hud;
    RectTransform tooltip;
    TMP_Text tooltipText;
    StatusBadge hovered;
    void Start()
    {
        hud=GetComponent<PaperBattleHUD>();
        var canvas=hud.GetComponentInParent<Canvas>().transform;
        playerStrip=CreateStrip(canvas,"WANDERER EFFECTS",.30f,.49f);
        enemyStrip=CreateStrip(canvas,"ENEMY EFFECTS",.53f,.72f);
        var tip=Box(canvas,"Status details",new Color(.025f,.028f,.035f,1));tooltip=tip.GetComponent<RectTransform>();
        tooltip.anchorMin=tooltip.anchorMax=new Vector2(.5f,.77f);tooltip.pivot=new Vector2(.5f,1);tooltip.sizeDelta=new Vector2(380,148);
        tooltipText=Text(tip.transform,"",13);tooltipText.alignment=TextAlignmentOptions.TopLeft;
        var tr=tooltipText.rectTransform;tr.offsetMin=new Vector2(14,10);tr.offsetMax=new Vector2(-14,-10);
        tip.GetComponent<Image>().raycastTarget=false;tip.SetActive(false);
    }
    Strip CreateStrip(Transform canvas,string title,float min,float max)
    {
        var root=Box(canvas,title,new Color(.025f,.028f,.035f,.98f));root.GetComponent<Image>().raycastTarget=false;
        root.transform.SetSiblingIndex(hud.transform.GetSiblingIndex()+1);
        var r=root.GetComponent<RectTransform>();r.anchorMin=new Vector2(min,.815f);r.anchorMax=new Vector2(max,.89f);r.offsetMin=r.offsetMax=Vector2.zero;
        var label=Text(root.transform,title,10);label.color=new Color(.8f,.82f,.85f);label.rectTransform.anchorMin=new Vector2(0,.75f);
        return new Strip{Root=r};
    }
    void Update()
    {
        if(hud==null)return;
        Refresh(playerStrip,hud.player!=null?hud.player.GetComponent<StatusController>():null);
        var enemy=BattleManager.Instance!=null?BattleManager.Instance.CurrentEnemyAI:null;
        Refresh(enemyStrip,enemy!=null?enemy.GetComponent<StatusController>():null);
        if(hovered!=null && hovered.gameObject.activeInHierarchy) tooltipText.text=hovered.Summary.Tooltip;
        else HideTooltip();
    }
    void Refresh(Strip strip,StatusController target)
    {
        if(strip.Target!=target)
        {
            foreach(var badge in strip.Badges.Values){if(hovered==badge)HideTooltip();Destroy(badge.gameObject);}
            strip.Badges.Clear();strip.Target=target;
        }
        var hp=target!=null?target.GetComponent<HealthComponent>():null;
        var summaries=target!=null && (hp==null || hp.CurrentLife>0)?target.GetStatusSummaries():new List<StatusController.Summary>();
        var active=new HashSet<StatusEffects>();
        for(int i=0;i<summaries.Count;i++)
        {
            var summary=summaries[i];
            active.Add(summary.Effect);
            if(!strip.Badges.TryGetValue(summary.Effect,out var badge))
            {
                var go=Box(strip.Root,summary.DisplayName,new Color(.035f,.04f,.05f,.98f));
                badge=go.AddComponent<StatusBadge>();badge.Owner=this;
                badge.Label=Text(go.transform,"",13);badge.Label.alignment=TextAlignmentOptions.Center;badge.Label.rectTransform.anchorMin=new Vector2(.48f,0);
                var icon=new GameObject("Status icon",typeof(RectTransform),typeof(StatusGlyph));icon.transform.SetParent(go.transform,false);
                var ir=icon.GetComponent<RectTransform>();ir.anchorMin=new Vector2(.1f,.2f);ir.anchorMax=new Vector2(.45f,.8f);ir.offsetMin=ir.offsetMax=Vector2.zero;
                badge.Glyph=icon.GetComponent<StatusGlyph>();badge.Glyph.raycastTarget=false;
                strip.Badges.Add(summary.Effect,badge);
            }
            badge.gameObject.SetActive(true);badge.Summary=summary;
            var rect=(RectTransform)badge.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,0);rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(i*59,0);rect.sizeDelta=new Vector2(55,35);
            badge.Label.text=summary.Count.ToString();badge.Label.color=Tint(summary);
            badge.Glyph.kind=summary.DisplayName;badge.Glyph.color=Tint(summary);badge.Glyph.SetVerticesDirty();
        }
        foreach(var pair in strip.Badges)if(!active.Contains(pair.Key))pair.Value.gameObject.SetActive(false);
    }
    static string Code(StatusController.Summary s)=>s.DisplayName switch {"Poison"=>"P", "Burn"=>"F", "Bleed"=>"B", "Shock"=>"S", "Chill"=>"C", _=>"D"};
    static Color Tint(StatusController.Summary s)=>s.DisplayName switch {"Poison"=>new Color(.3f,1,.22f),"Burn"=>new Color(1,.35f,.1f),"Bleed"=>new Color(1,.16f,.25f),"Shock"=>Color.yellow,"Chill"=>new Color(.2f,.75f,1),_=>Color.white};
    public void ShowTooltip(StatusBadge badge)
    {hovered=badge;tooltipText.text=badge.Summary.Tooltip;tooltip.gameObject.SetActive(true);tooltip.SetAsLastSibling();}
    public void HideTooltip(){hovered=null;if(tooltip!=null)tooltip.gameObject.SetActive(false);}
    void OnDisable(){HideTooltip();}
    static GameObject Box(Transform parent,string name,Color color)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go;}
    static TMP_Text Text(Transform parent,string value,int size)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var t=go.GetComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.raycastTarget=false;
        t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;return t;
    }
}
