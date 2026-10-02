// Developer map: Actor stat store with cached StatValue entries and batched change events. GetStat converts classified percent points to fractions; GetRawStat preserves stored units.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using System.Collections.Generic;

public class StatsComponent : MonoBehaviour
{
    private readonly Dictionary<StatTypes, StatValue> _stats = new();
    public event System.Action StatsChanged;
    private int updateDepth;
    private bool pendingChange;
    public void BeginUpdate() { updateDepth++; }
    public void CopyToPreview(StatsComponent target,object excludedGear,object excludedProgression)
    {
        target._stats.Clear();
        foreach(var pair in _stats)target._stats[pair.Key]=pair.Value.CopyExcluding(excludedGear,excludedProgression);
    }
    public void EndUpdate()
    {
        if (updateDepth <= 0) throw new System.InvalidOperationException("Unbalanced stat update");
        updateDepth--;
        if (updateDepth == 0 && pendingChange) { pendingChange = false; StatsChanged?.Invoke(); }
    }
    private void NotifyChanged()
    {
        if (updateDepth > 0) pendingChange = true;
        else StatsChanged?.Invoke();
    }

    /// <summary>
    /// Main accessor:
    /// - If the stat is a % stat, returns it as a fraction (20 -> 0.20).
    /// - If the stat is flat, returns it as-is (e.g. Life = 100).
    /// </summary>
    public float GetStat(StatTypes type)
    {
        float value = ToGameplayValue(type, GetRawStat(type));
        if(type is StatTypes.BleedChance or StatTypes.IgniteChance or StatTypes.PoisonChance)
            value += ToGameplayValue(StatTypes.AllDamagingAilmentChance,GetRawStat(StatTypes.AllDamagingAilmentChance))+(GetComponent<SubclassCombatState>()?.AuraSecondary(4,.2f)??0);
        return value;
    }

    // Physical Damage Reduction affixes were authored and serialized as fractions
    // (0.02 means 2%), unlike the other percent stats stored as percent points.
    // Keep that identity stable for existing items and use one conversion in every
    // combat/simulation stat store.
    public static float ToGameplayValue(StatTypes type, float raw)
        => type == StatTypes.PhysicalDamageReduction ? raw
            : IsPercentStat(type) ? raw / 100f : raw;

    public static float ToDisplayedValue(StatTypes type, float raw)
        => type == StatTypes.PhysicalDamageReduction ? raw * 100f : raw;

    /// <summary>
    /// Raw-unit calculated value (e.g., 20 == "20%"). More damage returns
    /// its compounded effective percentage (two +20 rolls return 44), not a roll sum.
    /// </summary>
    public float GetRawStat(StatTypes type)
    {
        float raw=GetPreConversionRawStat(type);
        if(GetComponent<SubclassCombatState>()?.Has(SubclassIds.WarriorMultihit)==true)
        {
            if(type==StatTypes.AttackSpeed)
                return raw+Mathf.Max(0,GetPreConversionRawStat(StatTypes.ChanceToHitTwice))*WarriorSubclassRules.MultistrikeToAttackSpeed;
            if(type==StatTypes.ChanceToHitTwice)
                return raw+Mathf.Max(0,GetPreConversionRawStat(StatTypes.AttackSpeed))*WarriorSubclassRules.AttackSpeedToMultistrike;
        }
        return raw;
    }

    public float GetPreConversionRawStat(StatTypes type)
        => _stats.TryGetValue(type,out var stat)?stat.GetValue():0f;

    public void SetBaseStat(StatTypes type, float baseValue)
    {
        if (!_stats.TryGetValue(type, out var stat))
        {
            stat = new StatValue(baseValue, IsPercentStat(type) && type.ToString().EndsWith("Mult"));
            _stats[type] = stat;
        }
        else
        {
            stat.BaseValue = baseValue;
        }
        NotifyChanged();
    }

    public void AddModifier(StatModifier mod)
    {
        if (!_stats.TryGetValue(mod.Stat, out var stat))
        {
            stat = new StatValue(0, IsPercentStat(mod.Stat) && mod.Stat.ToString().EndsWith("Mult"));
            _stats[mod.Stat] = stat;
        }
        stat.AddModifier(mod);
        NotifyChanged();
    }

    public void RemoveModifiersFromSource(object source)
    {
        foreach (var kvp in _stats)
        {
            kvp.Value.RemoveModifiersFromSource(source);
        }
        NotifyChanged();
    }

    /// <summary>
    /// Returns whit StatTypes are currently tracked (i.e., exist as keys).
    /// This is so that only stats that are owned are displayed on player's stat screen
    /// </summary>
    
    public IEnumerable<StatTypes> GetTrackedStats()
    {
        return _stats.Keys;
    }

    /// <summary>
    /// Helper so that UI/other systems can know if a stat exists
    /// </summary>

    public bool HasStat(StatTypes type)
    {
        return _stats.ContainsKey(type); 
    }

    // --------------------------------------------------------------------
    // STAT CLASSIFICATION – which ones are 0–100% that should become 0–1
    // --------------------------------------------------------------------
    // NOTE: If a StatTypes here doesn't exist in your enum, just remove it.
    //       If you add new % stats later, list them here.
    public static bool IsPercentStat(StatTypes t)
    {
        switch (t)
        {
            // Generic & elemental "increased" damage
            case StatTypes.PoisonDuration:
            case StatTypes.BleedDuration:
            case StatTypes.IgniteDuration:
            case StatTypes.PoisonTickRate:
            case StatTypes.BleedTickRate:
            case StatTypes.IgniteTickRate:
            case StatTypes.GenericDmg:
            case StatTypes.PhysDmg:
            case StatTypes.FireDmg:
            case StatTypes.ColdDmg:
            case StatTypes.LightDmg:
            case StatTypes.VoidDmg:
            case StatTypes.PoisonDmg:
            case StatTypes.BleedDmg:
            case StatTypes.IgniteDmg:
            case StatTypes.MagicDmg:
            case StatTypes.ProjectileDmg:
            case StatTypes.MinionDmg:

            // Generic & elemental "more" damage
            case StatTypes.GenericMult:
            case StatTypes.GenericDotMult:
            case StatTypes.PhysMult:
            case StatTypes.FireMult:
            case StatTypes.ColdMult:
            case StatTypes.LightMult:
            case StatTypes.VoidMult:
            case StatTypes.PoisonMult:
            case StatTypes.BleedMult:
            case StatTypes.IgniteMult:

            // Crit / attack speed / utility percentages
            case StatTypes.WeaponBaseCrit:
            case StatTypes.ChanceToBlock:
            case StatTypes.CritChance:
            case StatTypes.CritMult:
            case StatTypes.BaseCritChance:   // +X percentage points to base crit
            case StatTypes.AttackSpeed:
            case StatTypes.Accuracy:
            case StatTypes.ChanceToHitTwice:
            case StatTypes.CooldownRecovery:
            case StatTypes.ProjectileSpeed:
            case StatTypes.CastSpeed:
            case StatTypes.ProjectilePrecisionChance:
            case StatTypes.ProjectilePrecisionMultiplier:
            case StatTypes.RageGeneration:
            case StatTypes.RageEffect:
            case StatTypes.RageDecayReduction:
            case StatTypes.CooldownReduction:
            case StatTypes.CullingStrikeChance:
            case StatTypes.SpellEchoChance:
            case StatTypes.AuraEffect:
            case StatTypes.RevengeEffect:
            case StatTypes.PoisonLifeLeech:
            case StatTypes.LifeRecoveryEffect:
            case StatTypes.AllDamagingAilmentChance:
            case StatTypes.PoisonSpeed:
            case StatTypes.DamageReductionPerRage:
            case StatTypes.TitanRevengeBonus:
            case StatTypes.TitanFullLifeMore:
            case StatTypes.TitanFortification:
            case StatTypes.TitanRageRegeneration:
            case StatTypes.EruptionCoefficient:
            case StatTypes.PhysicalToFireConversion:
            case StatTypes.DamageTakenAsFire:
            case StatTypes.FireLifeLeech:
            case StatTypes.DodgeChance:
            case StatTypes.ToxicSuppression:
            case StatTypes.PrecisionMore:
            case StatTypes.ProjectileGuard:
            case StatTypes.DodgeLifeRecovery:

            // Penetration
            case StatTypes.PhysPenetration:
            case StatTypes.ColdPenetration:
            case StatTypes.LightPenetration:
            case StatTypes.FirePenetration:
            case StatTypes.VoidPenetration:
            case StatTypes.PoisonPenetration:
            case StatTypes.IgnitePenetration:
            case StatTypes.BleedPenetration:

            // Defences / resists / caps
            case StatTypes.ArmourPercent:
            case StatTypes.EvasionPercent:
            case StatTypes.FireRes:
            case StatTypes.ColdRes:
            case StatTypes.LightRes:
            case StatTypes.AllRes:
            case StatTypes.VoidRes:
            case StatTypes.MaxFireRes:
            case StatTypes.MaxColdRes:
            case StatTypes.MaxLightRes:
            case StatTypes.MaxAllRes:
            case StatTypes.MaxVoidRes:
            case StatTypes.PoisonRes:
            case StatTypes.BleedRes:
            case StatTypes.IgniteRes:
            case StatTypes.ShockRes:
            case StatTypes.ChillRes:
            case StatTypes.AllAilmentRes:
            case StatTypes.PhysicalDamageReduction:
            case StatTypes.ReducedShockEffect:
            case StatTypes.ReducedChillEffect:

            // Life regeneration is % maximum Life/sec; Mana regeneration stays flat/sec.
            case StatTypes.LifeRegeneration:
            case StatTypes.CullingStrike:
            case StatTypes.RuptureDamage:
            case StatTypes.WarriorDotMultiplier:
            case StatTypes.DeferredWounds:
            case StatTypes.EscalatingMultistrike:
            case StatTypes.UnbrokenAssault:
            case StatTypes.ElementalPlating:
            case StatTypes.LifePercent:
            case StatTypes.ManaPercent:

            // Attribute increases and percent-per-attribute scalers. The dormant
            // consumers remain intentionally unimplemented, but their stored and
            // displayed units are percentages.
            case StatTypes.StrengthPercent:
            case StatTypes.IntelligencePercent:
            case StatTypes.DexterityPercent:
            case StatTypes.DamagePerStrength:
            case StatTypes.DoTMultPerIntelligence:
            case StatTypes.AttackSpeedPerDexterity:
            case StatTypes.AccuracyPerDexterity:
            case StatTypes.DmgPerLowestStat:
            case StatTypes.DmgPerMaxMana:
            case StatTypes.DmgPerCurrentMana:

            // Ailment scaling – effect
            case StatTypes.ShockEffect:
            case StatTypes.ShockDuration:
            case StatTypes.DamageTakenFromManaBeforeLife:
            case StatTypes.ChillEffect:
            // Chances (we treat them as 0–1 probabilities in gameplay)
            case StatTypes.PoisonChance:
            case StatTypes.BleedChance:
            case StatTypes.IgniteChance:
            case StatTypes.ShockChance:
            case StatTypes.ChillChance:

                return true;

            default:
                return false;
        }
    }
}
