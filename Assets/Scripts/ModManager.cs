// Developer map: Weighted stat and tier selection, item-level gating and duplicate-group exclusion. Weapon base rolls are guaranteed; random weapon affixes are filtered by base element.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModManager : MonoBehaviour
{
    public static ModManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ModDatabase modDatabase;
    public ModDatabase Database => modDatabase;
#if UNITY_EDITOR
    private bool useIsolatedPools;
#endif

    [Header("Default Weights")]
    [SerializeField] private int defaultStatWeight = 50;        //This is the default total weight of stats

    // Base rarity-independent weights for specific stats
    private static readonly Dictionary<StatTypes, int> baseStatWeights = new Dictionary<StatTypes, int>
    {
        // Common generic damage
        {StatTypes.GenericDmg, 100 },
        {StatTypes.GenericMult, 80 },
        {StatTypes.GenericDotMult, 80 },

        // Element / phys damage
        {StatTypes.PhysDmg, 80 },
        {StatTypes.ColdDmg, 80 },
        {StatTypes.LightDmg, 80 },
        {StatTypes.FireDmg, 80 },
        {StatTypes.VoidDmg, 80 },

        //Crit Scaling
        {StatTypes.CritChance, 70 },
        {StatTypes.CritMult, 15 },
        {StatTypes.BaseCritChance, 5 },

        // +1 skills
        {StatTypes.Plus1Phys, 5 },
        {StatTypes.Plus1Fire, 5 },
        {StatTypes.Plus1Cold, 5 },
        {StatTypes.Plus1Light, 5 },
        {StatTypes.Plus1Poison, 5 },
        {StatTypes.Plus1Bleed, 5 },
        {StatTypes.Plus1Ignite, 5 },
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        if (modDatabase != null)
            modDatabase.Initialize();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

#if UNITY_EDITOR
    public void ConfigureForIsolatedRolling(ModDatabase database)
    {
        modDatabase = database;
        modDatabase?.Initialize();
        useIsolatedPools = true;
    }
#endif

    //Function used to roll mods for items
    public List<RolledMod> RollModsForItem(
        LootManager.GearType itemType,      //Takes args for gear type, rarity, ilvl and mod count
        LootManager.GearRarity rarity,
        int itemLevel,
        int modCount,
        Element weaponElement,
        bool forEnemy = false)
    {
        var mods = new List<RolledMod>();       //create list of rolled mods to store item's mods
        var usedStats = new HashSet<StatTypes>();       //Create HashSet for usedstats
        var usedGroups = new HashSet<string>();         //Create HashSet for usedgroups

        int remaining = modCount;       //Set remaining to modcount's value

        // GUARANTEED BASE STATS FOR WEAPONS
        if (itemType == LootManager.GearType.Weapons)       //If the item type passed in is a weapon
        {
            AddGuaranteedWeaponBaseStat(StatTypes.WeaponBaseDmg, rarity, itemLevel, mods, usedStats, usedGroups);       //Call add base stat for base dmg, attack speed, and crit
            AddGuaranteedWeaponBaseStat(StatTypes.WeaponBaseAttackSpeed, rarity, itemLevel, mods, usedStats, usedGroups);
            AddGuaranteedWeaponBaseStat(StatTypes.WeaponBaseCrit, rarity, itemLevel, mods, usedStats, usedGroups);
        }

        // NORMAL RANDOM MODS
        for (int i = 0; i < remaining; i++) //Loop through all of remaining 
        {
            RolledMod mod = RollSingleMod(itemType, rarity, itemLevel, usedStats, usedGroups, weaponElement, forEnemy, mods);
            if (mod == null) break; //If the mod is null, break

            mods.Add(mod);  //Add the mod to mods
            usedStats.Add(mod.statType);    //Add the stat type to usedstats so that we can't repeat mods

            var def = modDatabase.GetDefinition(mod.statType);  //grab the definition from mod database
            if (def.groups != null) //If the definition isn't null
            {
                foreach (string g in def.groups)    //Add the string to used groups to ensure we don't reuse certain mod groups that aren't able to be rolled more than once
                    usedGroups.Add(g);
            }
        }

        return mods;    //Return mods
    }

    /// <summary>Uses the drop generator's pools, groups, weights and item-level gates for crafting.</summary>
    public RolledMod RollAdditionalMod(Gear gear, LootManager.GearRarity rarity)
    {
        if (gear == null) return null;
        var usedStats = new HashSet<StatTypes>();
        ReserveIntrinsicWeaponStats(gear, usedStats);
        var usedGroups = new HashSet<string>();
        foreach (var existing in gear.rolledMods)
        {
            if (existing == null) continue;
            usedStats.Add(existing.statType);
            if (modDatabase != null && modDatabase.TryGetDefinition(existing.statType, out var definition) && definition.groups != null)
                foreach (string group in definition.groups) usedGroups.Add(group);
        }
        return RollSingleMod(gear.ItemType, rarity, gear.ItemLevel, usedStats, usedGroups, gear.BaseElement, false, gear.rolledMods);
    }

    public RolledMod RerollModifier(Gear gear, RolledMod replaced, LootManager.GearRarity rarity)
    {
        if (gear == null || replaced == null || replaced.lockedOriginal) return null;
        var usedStats = new HashSet<StatTypes>();
        ReserveIntrinsicWeaponStats(gear, usedStats);
        var usedGroups = new HashSet<string>();
        foreach (var existing in gear.rolledMods)
        {
            if (existing == null || ReferenceEquals(existing, replaced)) continue;
            usedStats.Add(existing.statType);
            if (modDatabase != null && modDatabase.TryGetDefinition(existing.statType, out var definition) && definition.groups != null)
                foreach (string group in definition.groups) usedGroups.Add(group);
        }
        return RollSingleMod(gear.ItemType, rarity, gear.ItemLevel, usedStats, usedGroups, gear.BaseElement, false, gear.rolledMods, replaced);
    }

    static void ReserveIntrinsicWeaponStats(Gear gear, HashSet<StatTypes> usedStats)
    {
        if (gear.ItemType != LootManager.GearType.Weapons) return;
        usedStats.Add(StatTypes.WeaponBaseDmg);
        usedStats.Add(StatTypes.WeaponBaseAttackSpeed);
        usedStats.Add(StatTypes.WeaponBaseCrit);
    }

    //This adds a guaranteed weapon base stat
    private void AddGuaranteedWeaponBaseStat(
        StatTypes stat,
        LootManager.GearRarity rarity,
        int itemLevel,
        List<RolledMod> mods,
        HashSet<StatTypes> usedStats,
        HashSet<string> usedGroups)
    {
        RolledMod mod = RollTierAndValue(stat, LootManager.GearType.Weapons, itemLevel);
        if (mod == null) return;

        mods.Add(mod);  //adds the mod, ensures its specific mod type and mod group can't roll again
        usedStats.Add(stat);

        if (modDatabase.TryGetDefinition(stat, out var def) && def.groups != null)
        {
            foreach (var g in def.groups)
                usedGroups.Add(g);
        }
    }

    //This rolls a single mod, this does the actual work of rolling the mod
    private RolledMod RollSingleMod(
        LootManager.GearType itemType,
        LootManager.GearRarity rarity,
        int itemLevel,
        HashSet<StatTypes> usedStats,
        HashSet<string> usedGroups,
        Element weaponElement,
        bool forEnemy,
        IReadOnlyList<RolledMod> existing,
        RolledMod excluded = null)
    {
#if UNITY_EDITOR
        List<StatTypes> pool = useIsolatedPools
            ? GearStatLists.GetCanonicalStatPoolForType(itemType)
            : GearStatLists.Instance != null
                ? GearStatLists.Instance.GetStatPoolForType(itemType)
                : GearStatLists.GetCanonicalStatPoolForType(itemType);
#else
        List<StatTypes> pool = GearStatLists.Instance != null
            ? GearStatLists.Instance.GetStatPoolForType(itemType)
            : GearStatLists.GetCanonicalStatPoolForType(itemType);
#endif
        var candidates = new List<(StatTypes stat, int weight)>();      //Create a list of candidates, with the key being the stat and the value being the weight of that stat

        foreach (var stat in pool)  //for each stat in the pool
        {
            if (usedStats.Contains(stat)) continue; //if it's in used stats, skip it
            if (forEnemy && IsPlayerOnlyAffix(stat)) continue;
            if (itemType == LootManager.GearType.Weapons && !IsWeaponAffixEligible(stat, weaponElement))
                continue;

            if (!modDatabase.TryGetDefinition(stat, out AffixDefinitions def))  //If we can't find it's definition, skip it. If we can store it in def
                continue;

            if (!GroupsAvailable(def, usedGroups))  //Check if the group is available, if not, skip it
                continue;

            if (!AffixPolicy.CanAdd(existing, rarity, def.side, excluded)) continue;

            var availableTier = ApplicableTiers(def,itemType).Where(t => t.minItemLevel <= itemLevel).ToList();
            if (availableTier.Count == 0)   //If it can't roll more than 0 tiers, skip it
                continue;

            int baseWeight = baseStatWeights.TryGetValue(stat, out int w) ? w : defaultStatWeight;  //grab the base weight, and store it in w if it has a special weighting, otherwise use default
            if (baseWeight <= 0) continue;

            candidates.Add((stat, baseWeight)); //Add the stat and its weight to candidates
        }

        if (candidates.Count == 0)  //If candidates is 0, return
            return null;

        StatTypes chosenStat = WeightedRandomPick(candidates);  //Call weightedrandompick to choose a random mod from candidates
        return RollTierAndValue(chosenStat, itemType, itemLevel);
    }

    /// <summary>Stats that remain valid player affixes but cannot benefit enemies without player-only systems.</summary>
    public static bool IsPlayerOnlyAffix(StatTypes stat)
    {
        return stat is StatTypes.Mana or StatTypes.ManaPercent or StatTypes.ManaRegeneration
            or StatTypes.ManaOnHit or StatTypes.ManaOnKill or StatTypes.DmgPerMaxMana
            or StatTypes.DmgPerCurrentMana or StatTypes.ManaPerIntelligence
            or StatTypes.LifeOnKill
            or >= StatTypes.Plus1Phys and <= StatTypes.Plus1Ignite;
    }

    private static bool IsWeaponAffixEligible(StatTypes stat, Element weaponElement)
    {
        switch (stat)
        {
            case StatTypes.FlatPhys:
            case StatTypes.PhysDmg:
            case StatTypes.PhysMult:
            case StatTypes.PhysPenetration:
            case StatTypes.Plus1Phys:
                return weaponElement == Element.Phys;

            case StatTypes.FlatFire:
            case StatTypes.FireDmg:
            case StatTypes.FireMult:
            case StatTypes.FirePenetration:
            case StatTypes.FlatFirePerStrength:
            case StatTypes.Plus1Fire:
                return weaponElement == Element.Fire;

            case StatTypes.IgniteDmg:
            case StatTypes.IgniteMult:
            case StatTypes.IgniteChance:
            case StatTypes.IgniteTickRate:
            case StatTypes.IgniteDuration:
            case StatTypes.IgnitePenetration:
            case StatTypes.Plus1Ignite:
                return weaponElement == Element.Fire;

            case StatTypes.FlatCold:
            case StatTypes.ColdDmg:
            case StatTypes.ColdMult:
            case StatTypes.ColdPenetration:
            case StatTypes.FlatColdPerDexterity:
            case StatTypes.Plus1Cold:
                return weaponElement == Element.Cold;

            case StatTypes.ChillChance:
            case StatTypes.ChillEffect:
            case StatTypes.ChillDuration:
                return weaponElement == Element.Cold;

            case StatTypes.FlatLight:
            case StatTypes.LightDmg:
            case StatTypes.LightMult:
            case StatTypes.LightPenetration:
            case StatTypes.FlatLightPerIntelligence:
            case StatTypes.Plus1Light:
                return weaponElement == Element.Light;

            case StatTypes.ShockChance:
            case StatTypes.ShockEffect:
            case StatTypes.ShockDuration:
                return weaponElement == Element.Light;

            case StatTypes.FlatVoid:
            case StatTypes.VoidDmg:
            case StatTypes.VoidMult:
            case StatTypes.VoidPenetration:
                return weaponElement == Element.Void || weaponElement == Element.Poison;

            case StatTypes.PoisonDmg:
            case StatTypes.PoisonMult:
            case StatTypes.PoisonChance:
            case StatTypes.PoisonTickRate:
            case StatTypes.PoisonDuration:
            case StatTypes.PoisonPenetration:
            case StatTypes.Plus1Poison:
                return weaponElement is Element.Phys or Element.Fire or Element.Cold or Element.Light or Element.Void or Element.Poison;

            case StatTypes.BleedDmg:
            case StatTypes.BleedMult:
            case StatTypes.BleedChance:
            case StatTypes.BleedTickRate:
            case StatTypes.BleedDuration:
            case StatTypes.BleedPenetration:
            case StatTypes.Plus1Bleed:
                return weaponElement == Element.Phys;

            case StatTypes.GenericDotMult:
                return weaponElement is Element.Phys or Element.Fire or Element.Cold or Element.Light or Element.Void or Element.Poison;

            default:
                return true;
        }
    }

    public static List<AffixTier> ApplicableTiers(AffixDefinitions def,LootManager.GearType slot)
    {
        if(def==null)return new List<AffixTier>();
        return PoedbAffixCatalog.TryGet(def.statType,slot,out var direct)
            ? direct : def.tiers ?? new List<AffixTier>();
    }

    private RolledMod RollTierAndValue(StatTypes stat, LootManager.GearType slot, int itemLevel)
    {
        AffixDefinitions def = modDatabase.GetDefinition(stat);     //grab definition of the passed in stat
        var available = ApplicableTiers(def,slot).Where(t => t.minItemLevel <= itemLevel).ToList();
        if (available.Count == 0) return null;  //if there are no available tiers, return

        // Temporary equal weighting across all eligible tiers, independent of
        // rarity; family-level weighted selection remains separate above.
        AffixTier chosenTier = available[Random.Range(0,available.Count)];
        float roll = Random.Range(chosenTier.minValue, chosenTier.maxValue);            //choose a roll by rolling a random range between the tier's min and max values

        if(chosenTier.pairedDamage)
            return new RolledMod(stat,chosenTier.tierIndex,roll,
                Random.Range(chosenTier.minHighValue,chosenTier.maxHighValue),false);

        // This intrinsic tier stores an average base value. Resolve a natural
        // 80%-120% weapon range while preserving that exact expected average.
        if (stat == StatTypes.WeaponBaseDmg)
            return new RolledMod(stat, chosenTier.tierIndex, roll * .8f, roll * 1.2f, false);

        return new RolledMod(stat, chosenTier.tierIndex, roll);     //return a new rolledmod using the calculated values.
    }

    //Function used to determine which groups have been pulled from already
    private bool GroupsAvailable(AffixDefinitions def, HashSet<string> usedGroups)
    {
        if (def.groups == null || def.groups.Length == 0) return true;

        foreach (var g in def.groups)
        {
            if (usedGroups.Contains(g)) return false;
        }
        return true;
    }

    //Function used to do a random weighted pick
    private StatTypes WeightedRandomPick(List<(StatTypes stat, int weight)> options)
    {
        int total = 0;
        foreach (var o in options) total += o.weight;

        int roll = Random.Range(0, total);
        int accum = 0;

        foreach (var o in options)
        {
            accum += o.weight;
            if (roll < accum) return o.stat;
        }

        return options[options.Count - 1].stat;
    }

    //Overload of weighted random pick
    private T WeightedRandomPick<T>(List<(T item, float weight)> options)
    {
        float total = 0f;
        foreach (var o in options) total += o.weight;

        float roll = Random.Range(0f, total);
        float accum = 0f;

        foreach (var o in options)
        {
            accum += o.weight;
            if (roll <= accum) return o.item;
        }

        return options[options.Count - 1].item;
    }
}
