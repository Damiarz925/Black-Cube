// Read-only legality checks shared by sampled loot/crafting tests and editor diagnostics.
using System;
using System.Collections.Generic;
using System.Linq;

public static class ItemizationValidator
{
    public static void ValidateCatalog(ModDatabase database, IList<string> errors)
    {
        if (database == null) { errors.Add("Missing ModDatabase."); return; }
        var pools = GearStatLists.BuildDefaultStatPools();
        foreach (var pool in pools)
            foreach (StatTypes stat in pool.Value.Distinct())
            {
                var definition = database.GetDefinition(stat);
                if (definition == null) { errors.Add($"{pool.Key}: stable {stat} definition missing."); continue; }
                var tiers = Tiers(definition, pool.Key);
                ValidateTiers(stat, pool.Key, tiers, errors);
                if (definition.side != AffixPolicy.Side(stat))
                    errors.Add($"{pool.Key}: {stat} has no authoritative Prefix/Suffix side.");
            }
        var available = InventoryModFilter.SelectableStats.Where(stat =>
            pools.Any(pool => pool.Value.Contains(stat) && Tiers(database.GetDefinition(stat), pool.Key).Count > 0))
            .ToArray();
        foreach (StatTypes stat in InventoryModFilter.SelectableStats)
            if (!available.Contains(stat)) errors.Add($"Advanced filter {stat} has no generatable family.");
        foreach (var category in InventoryModFilter.SimpleCategories)
            if (!available.Any(stat => (InventoryModFilter.CategoriesFor(stat) & category.Category) != 0))
                errors.Add($"Simple filter {category.Label} has no generatable family.");
    }

    public static void ValidateTiers(StatTypes stat, LootManager.GearType slot,
        IReadOnlyList<AffixTier> tiers, IList<string> errors)
    {
        if (tiers == null || tiers.Count == 0) { errors.Add($"{slot}: {stat} has no rollable tiers."); return; }
        int lastGate = 0;
        for (int index = 0; index < tiers.Count; index++)
        {
            AffixTier tier = tiers[index];
            if (tier == null) { errors.Add($"{slot}: {stat} has a null tier."); continue; }
            if (tier.tierIndex != tiers.Count-index) errors.Add($"{slot}: {stat} T numbering is invalid.");
            if (tier.minItemLevel <= lastGate) errors.Add($"{slot}: {stat} item-level gates are out of order.");
            lastGate = tier.minItemLevel;
            if (tier.weight <= 0 || !Finite(tier.minValue) || !Finite(tier.maxValue)
                || tier.minValue > tier.maxValue || tier.minValue < 0f)
                errors.Add($"{slot}: {stat} has an invalid tier range or weight.");
            if (tier.pairedDamage && (!Finite(tier.minHighValue) || !Finite(tier.maxHighValue)
                || tier.minHighValue > tier.maxHighValue || tier.minHighValue < tier.minValue))
                errors.Add($"{slot}: {stat} has an invalid paired high-roll range.");
        }
    }

    public static void ValidateRolledMods(LootManager.GearType slot, LootManager.GearRarity rarity,
        int itemLevel, IReadOnlyList<RolledMod> mods, ModDatabase database, IList<string> errors,
        bool requireImplicit = false, bool enforceCapacity = true)
    {
        if (mods == null) { errors.Add("Item modifiers are missing."); return; }
        if (database == null) { errors.Add("Missing ModDatabase."); return; }
        var pool = GearStatLists.GetCanonicalStatPoolForType(slot);
        var stats = new HashSet<StatTypes>();
        var groups = new HashSet<string>();
        int total = 0, prefixes = 0, suffixes = 0, implicits = 0;
        foreach (RolledMod mod in mods)
        {
            if (mod == null) { errors.Add("Item contains a null modifier."); continue; }
            if (!mod.lockedOriginal && !stats.Add(mod.statType))
                errors.Add($"{slot}: duplicate explicit family {mod.statType}.");
            var definition = database.GetDefinition(mod.statType);
            if (definition == null) errors.Add($"{slot}: stable {mod.statType} definition missing.");
            else if (!mod.lockedOriginal && definition.groups != null)
                foreach (string group in definition.groups.Where(group => !string.IsNullOrWhiteSpace(group)))
                    if (!groups.Add(group)) errors.Add($"{slot}: duplicate explicit exclusive group {group}.");
            if (Gear.IsWeaponBaseStat(mod.statType))
            {
                if (slot != LootManager.GearType.Weapons)
                    errors.Add($"{slot}: weapon intrinsic {mod.statType} is on a nonweapon.");
                continue;
            }
            if (mod.lockedOriginal) implicits++;
            else
            {
                total++;
                if (AffixPolicy.Side(mod.statType) == AffixSide.Prefix) prefixes++; else suffixes++;
            }
            if (!pool.Contains(mod.statType)) { errors.Add($"{slot}: illegal slot for {mod.statType}."); continue; }
            if (definition == null) continue;
            if (definition.side != AffixPolicy.Side(mod.statType))
                errors.Add($"{slot}: {mod.statType} side is invalid.");
            var tier = Tiers(definition, slot).FirstOrDefault(t => t != null && t.tierIndex == mod.tierIndex);
            if (tier == null || tier.minItemLevel > itemLevel)
            { errors.Add($"{slot}: {mod.statType} T{mod.tierIndex} is illegal at ilvl {itemLevel}."); continue; }
            if (!Finite(mod.value) || mod.value < tier.minValue-.0001f || mod.value > tier.maxValue+.0001f)
                errors.Add($"{slot}: {mod.statType} first roll is outside T{mod.tierIndex}.");
            if (tier.pairedDamage != mod.hasSecondaryValue)
                errors.Add($"{slot}: {mod.statType} has an unhandled dual-roll modifier.");
            if (tier.pairedDamage && (!Finite(mod.secondaryValue)
                || mod.secondaryValue < tier.minHighValue-.0001f
                || mod.secondaryValue > tier.maxHighValue+.0001f
                || mod.secondaryValue < mod.value))
                errors.Add($"{slot}: {mod.statType} second roll is outside T{mod.tierIndex}.");
        }
        if (requireImplicit && implicits != 1)
            errors.Add($"{slot}: equipment must have exactly one permanent implicit; found {implicits}.");
        if (enforceCapacity && (total > AffixPolicy.MaximumTotal(rarity)
            || prefixes > AffixPolicy.MaximumOnSide(rarity)
            || suffixes > AffixPolicy.MaximumOnSide(rarity)))
            errors.Add($"{slot}: {rarity} Prefix/Suffix or total capacity violated.");
    }

    static IReadOnlyList<AffixTier> Tiers(AffixDefinitions definition, LootManager.GearType slot)
    {
        if (definition == null) return Array.Empty<AffixTier>();
        if (PoedbAffixCatalog.TryGet(definition.statType, slot, out var direct)) return direct;
        return definition.tiers != null ? definition.tiers : Array.Empty<AffixTier>();
    }
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
