// Shared item-side legality for loot, crafting and enemy item generation.
using System.Collections.Generic;

public static class AffixPolicy
{
    public static AffixSide Side(StatTypes stat)
    {
        if (stat is StatTypes.ColdRes or StatTypes.FireRes or StatTypes.LightRes or StatTypes.VoidRes
            or StatTypes.AllRes or StatTypes.Strength or StatTypes.Dexterity or StatTypes.Intelligence
            or StatTypes.AttackSpeed or StatTypes.CritChance or StatTypes.CritMult
            or StatTypes.LifeOnKill or StatTypes.ManaOnKill or StatTypes.ManaOnHit
            or StatTypes.LifeOnHit or StatTypes.ChanceToHitTwice
            or StatTypes.PoisonChance or StatTypes.BleedChance or StatTypes.IgniteChance
            or StatTypes.ShockChance or StatTypes.ChillChance
            or StatTypes.PoisonDuration or StatTypes.BleedDuration or StatTypes.IgniteDuration
            or StatTypes.ShockDuration or StatTypes.ChillDuration)
            return AffixSide.Suffix;
        return AffixSide.Prefix;
    }

    public static int MaximumTotal(LootManager.GearRarity rarity) => rarity switch
    {
        LootManager.GearRarity.Normal => 1,
        LootManager.GearRarity.Magic => 2,
        LootManager.GearRarity.Rare => 4,
        _ => 6
    };

    public static int MaximumOnSide(LootManager.GearRarity rarity) => rarity switch
    {
        LootManager.GearRarity.Normal => 1,
        LootManager.GearRarity.Magic => 1,
        _ => 3
    };

    public static bool CanAdd(IReadOnlyList<RolledMod> existing, LootManager.GearRarity rarity,
        AffixSide side, RolledMod excluded = null)
    {
        int total=0, sideCount=0;
        if(existing!=null)foreach(var mod in existing)
        {
            if(mod==null || ReferenceEquals(mod,excluded) || Gear.IsWeaponBaseStat(mod.statType))continue;
            total++;
            if(Side(mod.statType)==side)sideCount++;
        }
        return total < MaximumTotal(rarity) && sideCount < MaximumOnSide(rarity);
    }
}
