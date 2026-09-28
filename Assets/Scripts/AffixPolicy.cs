// Shared item-side legality for loot, crafting and enemy item generation.
using System.Collections.Generic;

public static class AffixPolicy
{
    // Enemy intrinsic equipment is a separate pre-12.5G profile. This pass
    // changes player itemization, not enemy affix-count/side scaling.
    public static int EnemyMaximumTotal(LootManager.GearRarity rarity) => rarity switch
    {
        LootManager.GearRarity.Normal => 1,
        LootManager.GearRarity.Magic => 2,
        LootManager.GearRarity.Rare => 4,
        _ => 6
    };
    public static int EnemyMaximumOnSide(LootManager.GearRarity rarity) => rarity switch
    {
        LootManager.GearRarity.Normal => 1,
        LootManager.GearRarity.Magic => 1,
        _ => 3
    };

    public static bool CanAddEnemy(IReadOnlyList<RolledMod> existing,
        LootManager.GearRarity rarity, AffixSide side)
    {
        int total=0,onSide=0;
        if(existing!=null)foreach(var mod in existing)
        {
            if(mod==null||Gear.IsWeaponBaseStat(mod.statType))continue;
            total++;
            if(Side(mod.statType)==side)onSide++;
        }
        return total<EnemyMaximumTotal(rarity)&&onSide<EnemyMaximumOnSide(rarity);
    }

    public static AffixSide Side(StatTypes stat)
    {
        if(stat==StatTypes.CullingStrikeChance)return AffixSide.Suffix;
        if(stat is StatTypes.RageGeneration or StatTypes.RageDecayReduction)return AffixSide.Prefix;
        if(stat is StatTypes.ProjectileAmount or StatTypes.CooldownReduction or StatTypes.AuraEffect or StatTypes.AxePhysicalRage or StatTypes.CullingStrike)return AffixSide.Suffix;
        if (stat is StatTypes.ColdRes or StatTypes.FireRes or StatTypes.LightRes or StatTypes.VoidRes
            or StatTypes.AllRes or StatTypes.MaxColdRes or StatTypes.MaxFireRes
            or StatTypes.MaxLightRes or StatTypes.MaxVoidRes or StatTypes.MaxAllRes
            or StatTypes.Strength or StatTypes.Dexterity or StatTypes.Intelligence
            or StatTypes.StrengthPercent or StatTypes.DexterityPercent or StatTypes.IntelligencePercent
            or StatTypes.AttackSpeed or StatTypes.CritChance or StatTypes.CritMult
            or StatTypes.BaseCritChance or StatTypes.GenericDotMult
            or StatTypes.LifeOnKill or StatTypes.ManaOnKill or StatTypes.ManaOnHit
            or StatTypes.LifeRegeneration or StatTypes.ManaRegeneration
            or StatTypes.LifeOnHit or StatTypes.ChanceToHitTwice
            or StatTypes.PoisonChance or StatTypes.BleedChance or StatTypes.IgniteChance
            or StatTypes.ShockChance or StatTypes.ChillChance
            or StatTypes.PoisonDuration or StatTypes.BleedDuration or StatTypes.IgniteDuration
            or StatTypes.ShockDuration or StatTypes.ChillDuration
            or StatTypes.PoisonTickRate or StatTypes.BleedTickRate or StatTypes.IgniteTickRate
            or StatTypes.ShockEffect or StatTypes.ChillEffect)
            return AffixSide.Suffix;
        return AffixSide.Prefix;
    }

    public static AffixSide Side(RolledMod mod) => mod != null && mod.isBossSpecial
        ? mod.specialAffixSide : Side(mod != null ? mod.statType : default);

    public static int MaximumTotal(LootManager.GearRarity rarity) => rarity switch
    {
        LootManager.GearRarity.Normal => 0,
        LootManager.GearRarity.Magic => 2,
        LootManager.GearRarity.Rare => 4,
        _ => 6
    };

    public static int MaximumOnSide(LootManager.GearRarity rarity) => rarity switch
    {
        LootManager.GearRarity.Normal => 0,
        LootManager.GearRarity.Magic => 1,
        LootManager.GearRarity.Rare => 2,
        _ => 3
    };

    public static bool CanAdd(IReadOnlyList<RolledMod> existing, LootManager.GearRarity rarity,
        AffixSide side, RolledMod excluded = null)
    {
        int total=0, sideCount=0, otherSideCount=0;
        if(existing!=null)foreach(var mod in existing)
        {
            if(mod==null || ReferenceEquals(mod,excluded) || Gear.IsWeaponBaseStat(mod.statType)
                || mod.lockedOriginal)continue;
            total++;
            if(Side(mod)==side)sideCount++;else otherSideCount++;
        }
        return total < MaximumTotal(rarity) && sideCount < MaximumOnSide(rarity)
            && otherSideCount <= MaximumOnSide(rarity);
    }
}
