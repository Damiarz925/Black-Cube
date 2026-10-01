using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Pickup policy is independent of the existing-inventory highlight policy.
[Serializable] public sealed class LootModRequirement
{
    public StatTypes stat;
    public int minimumTier = 99, minimumImplicitTier = 99;
    // Keep the serialized flag: false now means either source, true means implicit only.
    public bool requireImplicit;
    public bool Matches(RolledMod mod) => mod != null && mod.statType == stat
        && (requireImplicit ? mod.lockedOriginal && mod.tierIndex <= minimumImplicitTier
            : mod.tierIndex <= minimumTier);
}
[Serializable] public sealed class ItemTypeLootFilter
{
    public LootManager.GearType itemType;
    public string weaponTypeId;
    public bool enabled, all = true;
    public int minimumMatches = 1;
    public List<LootModRequirement> requirements = new();
    public bool Matches(Gear gear)
    {
        if (!enabled || requirements.Count == 0) return true;
        var mods = gear.rolledMods.Where(m => m != null && !Gear.IsWeaponBaseStat(m.statType)).ToArray();
        int matches = requirements.Count(r => mods.Any(r.Matches));
        if (!all) return matches >= Mathf.Clamp(minimumMatches, 1, requirements.Count);
        if (matches == requirements.Count) return true;
        // An implicit can satisfy an EITHER requirement without using explicit capacity.
        int explicitRequirements = requirements.Count(r => !r.requireImplicit
            && !mods.Any(m => m.lockedOriginal && r.Matches(m)));
        int max = AffixPolicy.MaximumTotal(gear.ItemRarity);
        if (explicitRequirements <= max || max == 0) return false;
        // Over-full ALL means a full legal explicit set from the desired pool;
        // implicit requirements remain mandatory rather than disappearing here.
        var explicits = mods.Where(m => !m.lockedOriginal).ToArray();
        return explicits.Length == max && explicits.All(m => requirements.Any(r => !r.requireImplicit && r.Matches(m)))
            && requirements.Where(r => r.requireImplicit).All(r => mods.Any(r.Matches));
    }
}
[Serializable] public sealed class AdvancedLootFilter
{
    const string Key = "BlackCube.AdvancedLootFilter.V1";
    public int version;
    public bool enabled;
    public bool autoDismantleFilteredItems=true;
    public int keptRarities = 31, keptElements = 47, keptWeapons = 63;
    public List<ItemTypeLootFilter> itemTypes = new();
    public static readonly string[] Weapons = {WeaponTypeIds.Sword,WeaponTypeIds.TwoHandedAxe,WeaponTypeIds.Bow,WeaponTypeIds.Staff,WeaponTypeIds.Sceptre,WeaponTypeIds.Dagger};
    public ItemTypeLootFilter For(LootManager.GearType type, string weapon = null)
    {
        weapon = type == LootManager.GearType.Weapons ? weapon ?? Weapons[0] : "";
        var filter = itemTypes.Find(f => f.itemType == type && f.weaponTypeId == weapon);
        if (filter != null) return filter;
        filter = new ItemTypeLootFilter { itemType = type, weaponTypeId = weapon }; itemTypes.Add(filter); return filter;
    }
    public bool Keeps(Gear item)
    {
        if (item == null || item.IsScrap || item.IsLocked || item.ItemRarity==LootManager.GearRarity.Unique || !enabled) return true;
                if ((keptRarities & (1 << (int)item.ItemRarity)) == 0) return false;
        if(item.ItemRarity==LootManager.GearRarity.Unique)return true; // Unique rarity is decisive; ordinary prefix/suffix rules do not apply.
        if (item.ItemType == LootManager.GearType.Weapons)
        {
            int weapon = Array.IndexOf(Weapons,item.WeaponTypeId);
            if (weapon < 0 || (keptWeapons & (1 << weapon)) == 0 || (keptElements & (1 << (int)item.BaseElement)) == 0) return false;
        }
        return For(item.ItemType,item.WeaponTypeId).Matches(item);
    }
    // Retaining a rarity is not a mod match. Empty/inactive mod policies never highlight.
    public bool Highlights(Gear item)
    {
        if (!enabled || item == null || item.IsScrap) return false;
        var policy = itemTypes.Find(f => f.itemType == item.ItemType
            && f.weaponTypeId == (item.ItemType == LootManager.GearType.Weapons ? item.WeaponTypeId : ""));
        return policy != null && policy.enabled && policy.requirements.Count > 0 && policy.Matches(item);
    }
    public static IReadOnlyList<StatTypes> LegalStats(LootManager.GearType type,string weapon)
    {
        var db = ModManager.Instance?.Database;
        return GearStatLists.GetCanonicalStatPoolForType(type).Where(s => !Gear.IsWeaponBaseStat(s)
            && (type != LootManager.GearType.Weapons || WeaponExclusiveAffixRules.Allows(s,weapon))
            && (db == null || ModManager.ApplicableTiers(db.GetDefinition(s),type,weapon).Count > 0))
            .Distinct().OrderBy(StatDisplayFormatting.ToFriendlyName).ToArray();
    }
    public void ClearSlotModifiers(LootManager.GearType type,string weapon=null)=>For(type,weapon).requirements.Clear();
    public void ClearAllModifiers(){foreach(var policy in itemTypes)policy.requirements.Clear();}
    public void Save() { version=2;PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save(); }
    public static AdvancedLootFilter Load()
    {
        try { var result=PlayerPrefs.HasKey(Key)?JsonUtility.FromJson<AdvancedLootFilter>(PlayerPrefs.GetString(Key))??new():new();if(result.version<2){result.keptRarities|=16;result.version=2;}return result; }
        catch (Exception) { return new(); }
    }
}
