// Developer map: Friendly names and raw-value formatting for stats and tooltips. Percent classification must agree with StatsComponent; display formatting never changes gameplay units.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class StatDisplayFormatting
{
    // Optional: override names that look bad when auto-split
    private static readonly Dictionary<StatTypes, string> Overrides = new()
    {
        { StatTypes.Life, "Maximum HP" },
        { StatTypes.DamageTakenFromManaBeforeLife, "Damage Taken From Mana Before Life" },
        { StatTypes.AxePhysicalRage, "Local More Physical Damage / Rage Generation" },
        { StatTypes.CullingStrike, "Culling Strike Life Threshold" },
        { StatTypes.LifeRegeneration, "Maximum Life Regenerated per Second" },
        { StatTypes.RevengeEffect, "Increased Revenge Effect" },
        { StatTypes.PoisonLifeLeech, "Poison Damage Leeched as Life" },
        { StatTypes.LifeRecoveryEffect, "Increased Life Recovery Effect" },
        { StatTypes.AllDamagingAilmentChance, "All Damaging Ailment Chance" },
        { StatTypes.GrantsPhysicalAura, "Grants Physical Aura" },
        { StatTypes.GrantsFireAura, "Grants Fire Aura" },
        { StatTypes.GrantsColdAura, "Grants Cold Aura" },
        { StatTypes.GrantsLightningAura, "Grants Lightning Aura" },
        { StatTypes.GrantsVoidAura, "Grants Void Aura" },
        { StatTypes.LifePercent, "Increased Maximum Life" },
        { StatTypes.ArmourPercent, "Increased Armour" },
        { StatTypes.StrengthPercent, "Increased Strength" },
        { StatTypes.UnarmedDamage, "Unarmed Damage" },
        { StatTypes.FlatPhys, "Flat Physical Damage" },
        { StatTypes.FlatFire, "Flat Fire Damage" },
        { StatTypes.FlatCold, "Flat Cold Damage" },
        { StatTypes.FlatLight, "Flat Lightning Damage" },
        { StatTypes.FlatVoid, "Flat Void Damage" },
        { StatTypes.GenericDmg, "Increased Damage" },
        { StatTypes.GenericMult, "More Damage" },
        { StatTypes.GenericDotMult, "Damage Over Time Multiplier" },
        { StatTypes.CritMult, "Critical Damage Multiplier" },
        { StatTypes.CritChance, "Increased Critical Chance" },
        { StatTypes.BaseCritChance, "Added Base Critical Chance (Points)" },
        { StatTypes.AllRes, "All Elemental Resist" },
        { StatTypes.AllAilmentRes, "All Ailment Resist" },
        { StatTypes.ManaCost, "Mana Cost" },
        { StatTypes.ChanceToHitTwice, "Multistrike Chance" },
        { StatTypes.PhysDmg, "Increased Physical Damage" },
        { StatTypes.MagicDmg, "Increased Magic-tagged Damage" },
        { StatTypes.ProjectileDmg, "Increased Projectile-tagged Damage" },
        { StatTypes.MinionDmg, "Increased Minion-tagged Damage" },
        { StatTypes.ProjectileAmount, "Additional Projectile Amount" },
        { StatTypes.ProjectileSpeed, "Increased Projectile Speed" },
        { StatTypes.CastSpeed, "Increased Cast Speed" },
        { StatTypes.ProjectilePrecisionChance, "Projectile Precision Chance" },
        { StatTypes.ProjectilePrecisionMultiplier, "Projectile Precision Damage" },
        { StatTypes.RageGeneration, "Increased Rage Generation" },
        { StatTypes.RageEffect, "Increased Rage Effect" },
        { StatTypes.RageDecayReduction, "Reduced Rage Decay" },
        { StatTypes.DmgPerMaxMana, "Damage per 100 Maximum Mana" },
        { StatTypes.DmgPerCurrentMana, "Damage per 100 Current Mana" },
        { StatTypes.Plus1Phys, "+ Heavy Strike Level" },
        { StatTypes.Plus1Cold, "+ Ice Strike Level" },
        { StatTypes.Plus1Light, "+ Lightning Strike Level" },
        { StatTypes.Plus1Fire, "+ Fireball Level" },
        { StatTypes.Plus1Poison, "+ Envenom Level" },
        { StatTypes.Plus1Bleed, "+ Shiv Level" },
        { StatTypes.Plus1Ignite, "+ Immolate Level" },
        { StatTypes.PoisonDmg, "Increased Poison (Void DoT) Damage" },
        { StatTypes.PoisonMult, "More Poison (Void DoT) Damage" },
        { StatTypes.LifePerStrength, "Maximum Life per Strength" },
        { StatTypes.DamagePerStrength, "Damage per 10 Strength" },
        { StatTypes.ManaPerIntelligence, "Maximum Mana per Intelligence" },
        { StatTypes.DoTMultPerIntelligence, "DoT Multiplier per Intelligence" },
        { StatTypes.AttackSpeedPerDexterity, "Attack Speed per Dexterity" },
        { StatTypes.FlatFirePerStrength, "Added Fire Damage per Strength" },
        { StatTypes.FlatLightPerIntelligence, "Added Lightning Damage per Intelligence" },
        { StatTypes.FlatColdPerDexterity, "Added Cold Damage per Dexterity" },
        { StatTypes.DmgPerLowestStat, "Unarmed Damage per 10 Lowest Attribute" },
        { StatTypes.PhysicalDamageReduction, "Physical Damage Reduction" },
        { StatTypes.ReducedShockEffect, "Reduced Shock Effect" },
        { StatTypes.SpellEchoChance, "Spell Echo Chance" },
        { StatTypes.CullingStrikeChance, "Culling Strike Chance" },
        { StatTypes.PoisonDuration, "Increased Poison Duration" },
        { StatTypes.BleedDuration, "Increased Bleed Duration" },
        { StatTypes.IgniteDuration, "Increased Ignite Duration" },
        { StatTypes.PoisonTickRate, "Increased Poison Tick Speed" },
        { StatTypes.BleedTickRate, "Increased Bleed Tick Speed" },
        { StatTypes.IgniteTickRate, "Increased Ignite Tick Speed" },
        { StatTypes.ReducedChillEffect, "Reduced Chill Effect" },
        { StatTypes.PlusAllSkills, "+Level of All Skills" },
        { StatTypes.RuptureDamage, "More Rupture Damage" },
        { StatTypes.WarriorDotMultiplier, "Warrior Damage Over Time Multiplier" },
        { StatTypes.LifeOnHitVsBleeding, "Life on Hit against Bleeding Enemies" },
        { StatTypes.DeferredWounds, "Life Hit Damage Deferred as Bleeding over 4 Seconds" },
        { StatTypes.EscalatingMultistrike, "More Damage per Prior Multistrike" },
        { StatTypes.UnbrokenAssault, "More Damage per Prior Uninterrupted Attack" },
        { StatTypes.ElementalPlating, "Armour Applies to Elemental Hits" },
    };

    public static string ToFriendlyName(StatTypes t)
    {
        if (Overrides.TryGetValue(t, out var s))
            return s;

        // Split PascalCase: MaxFireRes -> "Max Fire Res"
        var name = t.ToString();
        name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

        // Small Cleanup
        name = name.Replace("Dmg", "Damage");
        name = name.Replace("Mult", "Multiplier");
        name = name.Replace("Phys", "Physical");
        name = name.Replace("Light", "Lightning");

        return name;
    }

    public static string PlayerFacingText(string text) => (text ?? string.Empty).Replace("Chance to Hit Twice", "Multistrike Chance").Replace("Hit Twice Chance", "Multistrike Chance").Replace("Hit Twice", "Multistrike");

    /// <summary>
    /// Uses RAW values so percent stats show as 0-100 numbers
    /// </summary>
    
    public static string FormatValue(StatsComponent stats, StatTypes type)
    {
        float raw = type is StatTypes.Strength or StatTypes.Dexterity or StatTypes.Intelligence
            ? DerivedStatCalculator.Attribute(stats, type)
            : StatsComponent.ToDisplayedValue(type, stats.GetRawStat(type));

        // Hide near-zero from floats
        if (System.MathF.Abs(raw) < 0.0001f)
            return "0";

        bool isPercent = StatsComponent.IsPercentStat(type);

        if (isPercent)
        {
            // Raw is already 0-100 for percent stats
            return $"{raw:0.##}%";
        }

        // Flat stats
        return raw.ToString("0.##");
    }

    public static bool ShouldDisplay (StatsComponent stats, StatTypes type, float epsilon = 0.0001f)
    {
        if (type == StatTypes.Life) return true;
        float raw = stats.GetRawStat(type);
        return System.MathF.Abs(raw) > epsilon;
    }
}
