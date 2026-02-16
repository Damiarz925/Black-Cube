public static class StatCategoryMapping
{
    public static StatCategory GetCategory(StatTypes t)
    {
        // This mapping is based on how your StatTypes enum is grouped.
        // If you re-order your enum later, revisit these cases.

        switch (t)
        {
            // Weapon base
            case StatTypes.WeaponBaseDmg:
            case StatTypes.WeaponBaseAttackSpeed:
            case StatTypes.WeaponBaseCrit:
            case StatTypes.ChanceToBlock:
                return StatCategory.WeaponBase;

            // Flat elemental damage
            case StatTypes.FlatPhys:
            case StatTypes.FlatCold:
            case StatTypes.FlatLight:
            case StatTypes.FlatFire:
                return StatCategory.FlatDamage;

            // Increased damage
            case StatTypes.GenericDmg:
            case StatTypes.PhysDmg:
            case StatTypes.ColdDmg:
            case StatTypes.LightDmg:
            case StatTypes.FireDmg:
            case StatTypes.PoisonDmg:
            case StatTypes.IgniteDmg:
            case StatTypes.BleedDmg:
                return StatCategory.IncreasedDamage;

            // More damage / multipliers
            case StatTypes.GenericMult:
            case StatTypes.GenericDotMult:
            case StatTypes.PhysMult:
            case StatTypes.ColdMult:
            case StatTypes.LightMult:
            case StatTypes.FireMult:
            case StatTypes.PoisonMult:
            case StatTypes.IgniteMult:
            case StatTypes.BleedMult:
                return StatCategory.MoreDamage;

            // Penetration
            case StatTypes.PhysPenetration:
            case StatTypes.ColdPenetration:
            case StatTypes.LightPenetration:
            case StatTypes.FirePenetration:
            case StatTypes.PoisonPenetration:
            case StatTypes.IgnitePenetration:
            case StatTypes.BleedPenetration:
                return StatCategory.Penetration;

            // DoT specific
            case StatTypes.PoisonChance:
            case StatTypes.IgniteChance:
            case StatTypes.BleedChance:
            case StatTypes.PoisonTickRate:
            case StatTypes.IgniteTickRate:
            case StatTypes.BleedTickRate:
            case StatTypes.PoisonDuration:
            case StatTypes.IgniteDuration:
            case StatTypes.BleedDuration:
                return StatCategory.DamageOverTime;

            // Non-damaging ailments
            case StatTypes.ShockChance:
            case StatTypes.ChillChance:
            case StatTypes.ShockEffect:
            case StatTypes.ChillEffect:
            case StatTypes.ShockDuration:
            case StatTypes.ChillDuration:
                return StatCategory.Ailments;

            // Defenses
            case StatTypes.FlatArmour:
            case StatTypes.FlatEvasion:
            case StatTypes.ArmourPercent:
            case StatTypes.EvasionPercent:
            case StatTypes.ColdRes:
            case StatTypes.LightRes:
            case StatTypes.FireRes:
            case StatTypes.AllRes:
            case StatTypes.MaxColdRes:
            case StatTypes.MaxLightRes:
            case StatTypes.MaxFireRes:
            case StatTypes.MaxAllRes:
            case StatTypes.PoisonRes:
            case StatTypes.IgniteRes:
            case StatTypes.BleedRes:
            case StatTypes.ShockRes:
            case StatTypes.ChillRes:
            case StatTypes.AllAilmentRes:
                return StatCategory.Defenses;

            // Resources
            case StatTypes.Life:
            case StatTypes.Mana:
            case StatTypes.LifePercent:
            case StatTypes.ManaPercent:
            case StatTypes.LifeRegeneration:
            case StatTypes.ManaRegeneration:
            case StatTypes.LifeOnHit:
            case StatTypes.ManaOnHit:
            case StatTypes.LifeOnKill:
            case StatTypes.ManaOnKill:
            case StatTypes.ManaCost:
            case StatTypes.DmgPerMaxMana:
            case StatTypes.DmgPerCurrentMana:
                return StatCategory.Resources;

            // Utility
            case StatTypes.AttackSpeed:
            case StatTypes.Accuracy:
            case StatTypes.ChanceToHitTwice:
            case StatTypes.CooldownRecovery:
            case StatTypes.CritChance:
            case StatTypes.CritMult:
            case StatTypes.BaseCritChance:
                return StatCategory.Utility;

            // Attributes / scaling
            case StatTypes.Strength:
            case StatTypes.Intelligence:
            case StatTypes.Dexterity:
            case StatTypes.StrengthPercent:
            case StatTypes.IntelligencePercent:
            case StatTypes.DexterityPercent:
            case StatTypes.LifePerStrength:
            case StatTypes.DamagePerStrength:
            case StatTypes.ManaPerIntelligence:
            case StatTypes.DoTMultPerIntelligence:
            case StatTypes.AttackSpeedPerDexterity:
            case StatTypes.AccuracyPerDexterity:
            case StatTypes.FlatFirePerStrength:
            case StatTypes.FlatLightPerIntelligence:
            case StatTypes.FlatColdPerDexterity:
            case StatTypes.DmgPerLowestStat:
                return StatCategory.Attributes;

            // "+1" type stats
            case StatTypes.Plus1Phys:
            case StatTypes.Plus1Fire:
            case StatTypes.Plus1Cold:
            case StatTypes.Plus1Light:
            case StatTypes.Plus1Poison:
            case StatTypes.Plus1Bleed:
            case StatTypes.Plus1Ignite:
                return StatCategory.Other;

            default:
                return StatCategory.Other;
        }
    }

    public static string GetHeaderName(StatCategory cat)
    {
        return cat switch
        {
            StatCategory.WeaponBase => "Weapon Base",
            StatCategory.FlatDamage => "Flat Damage",
            StatCategory.IncreasedDamage => "Increased Damage",
            StatCategory.MoreDamage => "More Damage",
            StatCategory.Penetration => "Penetration",
            StatCategory.DamageOverTime => "Damage Over Time",
            StatCategory.Ailments => "Ailments",
            StatCategory.Defenses => "Defenses",
            StatCategory.Resources => "Resources",
            StatCategory.Utility => "Utility",
            StatCategory.Attributes => "Attributes",
            _ => "Other"
        };
    }
}