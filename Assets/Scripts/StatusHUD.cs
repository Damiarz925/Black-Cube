// Developer map: Builds player/enemy status strips and hover tooltips from StatusController summaries. Rebinds to the current spawned enemy and discards stale badges on target replacement.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Reusable status badges for both combatants. Rebinds when enemies are replaced.</summary>
public class StatusHUD : MonoBehaviour
{
    [SerializeField] StatusHUDView authoredView;
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
        if(authoredView==null)authoredView=GetComponentInChildren<StatusHUDView>(true);
        if(authoredView==null||authoredView.playerStrip==null||authoredView.enemyStrip==null||authoredView.tooltip==null||authoredView.tooltipText==null||authoredView.badgePrefab==null){Debug.LogError("StatusHUD requires an authored StatusHUDView and badge prefab.",this);enabled=false;return;}
        playerStrip=new Strip{Root=authoredView.playerStrip};enemyStrip=new Strip{Root=authoredView.enemyStrip};tooltip=authoredView.tooltip;tooltipText=authoredView.tooltipText;tooltip.gameObject.SetActive(false);
    }
    void Update()
    {
        if(hud==null)return;
        Refresh(playerStrip,hud.player!=null?hud.player.GetComponent<StatusController>():null);
        var enemy=BattleManager.Instance!=null?BattleManager.Instance.CurrentEnemyAI:null;
        Refresh(enemyStrip,enemy!=null?enemy.GetComponent<StatusController>():null);
        bool panels = SkillTreeUI.IsOpen || hud.IsEnemyInspectionOpen || (hud.player != null && hud.player.CurrentLife <= 0);
        playerStrip.Root.gameObject.SetActive(!panels);
        enemyStrip.Root.gameObject.SetActive(!panels && !hud.statsPanel.activeSelf);
        if (panels || hud.statsPanel.activeSelf) { HideTooltip(); return; }
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
                badge=Instantiate(authoredView.badgePrefab,strip.Root);badge.name=summary.DisplayName;badge.Owner=this;
                strip.Badges.Add(summary.Effect,badge);
            }
            badge.gameObject.SetActive(true);badge.Summary=summary;
            badge.transform.SetSiblingIndex(i);
            badge.Label.text=summary.Effect.Ailment is StatusEffects.AilmentKind.Bleed or StatusEffects.AilmentKind.Ignite
                ? $"{summary.Count}/{summary.MaximumStackCount}" : summary.Count.ToString();
            badge.Label.color=Tint(summary);
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
}
