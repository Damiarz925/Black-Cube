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
        float raw = GetRawStat(type);

        if (IsPercentStat(type))
        {
            // 20 => 0.20
            return raw / 100f;
        }

        return raw;
    }

    /// <summary>
    /// Raw-unit calculated value (e.g., 20 == "20%"). More damage returns
    /// its compounded effective percentage (two +20 rolls return 44), not a roll sum.
    /// </summary>
    public float GetRawStat(StatTypes type)
    {
        if (!_stats.TryGetValue(type, out var stat)) return 0f;
        return stat.GetValue();
    }

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
            case StatTypes.GenericDmg:
            case StatTypes.PhysDmg:
            case StatTypes.FireDmg:
            case StatTypes.ColdDmg:
            case StatTypes.LightDmg:
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

            // Penetration
            case StatTypes.PhysPenetration:
            case StatTypes.ColdPenetration:
            case StatTypes.LightPenetration:
            case StatTypes.FirePenetration:
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
            case StatTypes.MaxFireRes:
            case StatTypes.MaxColdRes:
            case StatTypes.MaxLightRes:
            case StatTypes.MaxAllRes:
            case StatTypes.PoisonRes:
            case StatTypes.BleedRes:
            case StatTypes.IgniteRes:
            case StatTypes.ShockRes:
            case StatTypes.ChillRes:
            case StatTypes.AllAilmentRes:

            // Life / mana % increases. Both regeneration stats remain flat units
            // per second and are therefore intentionally excluded here.
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

            // Ailment scaling – effect
            case StatTypes.ShockEffect:
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
