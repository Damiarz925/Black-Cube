using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class WeaponSkillTooltip : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public int skillIndex;
    public GameObject panel;
    public TMP_Text body;
    PlayerSkillController controller;
    bool hovered;
    public void Initialize(PlayerSkillController value)=>controller=value;
    public void OnPointerEnter(PointerEventData eventData){hovered=true;Refresh();}
    public void OnPointerExit(PointerEventData eventData){hovered=false;if(panel!=null)panel.SetActive(false);}
    void Update(){if(hovered)Refresh();}
    void OnDisable(){hovered=false;if(panel!=null)panel.SetActive(false);}
    void Refresh()
    {
        if(panel==null||body==null||controller==null)return;
        bool show=GameplayOptions.WeaponSkillTooltips&&skillIndex<controller.WeaponSkills.Count;
        panel.SetActive(show);if(!show)return;
        var skill=controller.WeaponSkills[skillIndex];var player=controller.GetComponent<PlayerController>();var stats=controller.GetComponent<StatsComponent>();
        var ctx=CharacterDamageEstimate.SearchHit(player.BuildNonCriticalAttackContext(skill.conversionElement,skill.nonMatchingConversion,skill.DamageScopes),stats);
        float basis=0;var types=new StringBuilder();foreach(var hit in ctx.Hits){basis+=hit.Amount;if(types.Length>0)types.Append(" + ");types.Append(ItemTooltipUI.ElementName(hit.Element));}
        int hits=skill.effect==WeaponSkillEffect.RapidFlurry?WeaponMechanicProfile.RapidFlurryHits(stats.GetStat(StatTypes.AttackSpeed)):Mathf.Max(1,skill.baseHitCount);
        int projectiles=skill.projectile?BattleManager.CalculateProjectileCount(stats.GetRawStat(StatTypes.ProjectileAmount),0):1;if(skill.effect==WeaponSkillEffect.DoubleProjectiles)projectiles*=2;
        float multiplier=skill.effect==WeaponSkillEffect.ArmourStrike?WeaponMechanicProfile.ArmourStrikeMultiplier(ItemArmourProfile.Final(stats)):skill.hitDamageMultiplier;
        float total=basis*multiplier*PlayerSkillController.SkillDamageLevelFactor(controller.EffectiveSkillLevel(skill))*hits*projectiles;
        if(skill.effect==WeaponSkillEffect.RapidFlurry&&stats.GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.WarriorConsolidation)==true)total=total/hits*ClassKeystoneMechanics.ConsolidatedMultiplier(hits-1);
        string cooldown=skill.castMode==PlayerSkillCastMode.QueuedAttackReplacement?"Next attack replacement":$"{controller.EffectiveCooldown(skill):0.##}s cooldown ({controller.CooldownRemaining(skillIndex):0.##}s remaining)";
        body.text=$"<b>{skill.displayName}</b>\n{skill.description}\n\nBasis: current weapon + character damage\nTypes: {types}\nMana: {controller.ManaCost(skill):0}\n{cooldown}\nHits: {hits}   Projectiles: {(skill.projectile?projectiles:0)}\nProjectile speed: {(skill.projectile?skill.baseProjectileSpeed*(1+stats.GetStat(StatTypes.ProjectileSpeed)):0):0.##}×\nEstimated direct damage/use: {total:0.#}\nNoncritical, before defenses; ailments/conditional triggers excluded.";
        panel.transform.SetAsLastSibling();
    }
}
