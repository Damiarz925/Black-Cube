using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModManager : MonoBehaviour
{
    public static ModManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ModDatabase modDatabase;
    [SerializeField] private LootManager lootManager;

    [Header("Default Weights")]
    [SerializeField] private int defaultStatWeight = 50;        //This is the default total weight of stats

    [Header("Tier Bias By Rarity (index = tierIndex - 1")]
    [SerializeField] private float[] magicTierBias = { 0.2f, 0.6f, 1.0f, 1.2f, 1.5f };      //These 3 are slight mod tier biasing based on the gear's rarity
    [SerializeField] private float[] rareTierBias = { 1.5f, 1.2f, 1.0f, 0.6f, 0.3f };
    [SerializeField] private float[] legendaryTierBias = { 2.0f, 1.5f, 1.0f, 0.5f, 0.1f };

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

        //Crit Scaling
        {StatTypes.CritChance, 70 },
        {StatTypes.CritMult, 15 },
        {StatTypes.BaseCritChance, 5 },

        // +1 skills
        {StatTypes.Plus1Phys, 3 },
        {StatTypes.Plus1Fire, 3 },
        {StatTypes.Plus1Cold, 3 },
        {StatTypes.Plus1Light, 3 },
        {StatTypes.Plus1Poison, 3 },
        {StatTypes.Plus1Bleed, 3 },
        {StatTypes.Plus1Ignite, 3 },
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (modDatabase != null)
            modDatabase.Initialize();
    }

    //Function used to roll mods for items
    public List<RolledMod> RollModsForItem(
        LootManager.GearType itemType,      //Takes args for gear type, rarity, ilvl and mod count
        LootManager.GearRarity rarity,
        int itemLevel,
        int modCount)
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
            RolledMod mod = RollSingleMod(itemType, rarity, itemLevel, usedStats, usedGroups);  //Grab a mod by callilng rollsinglemod
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

    //This adds a guaranteed weapon base stat
    private void AddGuaranteedWeaponBaseStat(
        StatTypes stat,
        LootManager.GearRarity rarity,
        int itemLevel,
        List<RolledMod> mods,
        HashSet<StatTypes> usedStats,
        HashSet<string> usedGroups)
    {
        RolledMod mod = RollTierAndValue(stat, rarity, itemLevel);  //Uses the stat, rarity, and item level to roll the tier of the stat and its value
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
        HashSet<string> usedGroups)
    {
        List<StatTypes> pool = GearStatLists.Instance.GetStatPoolForType(itemType);     //this grabs the stat pool for the passed in item type and stores it in pool
        var candidates = new List<(StatTypes stat, int weight)>();      //Create a list of candidates, with the key being the stat and the value being the weight of that stat

        foreach (var stat in pool)  //for each stat in the pool
        {
            if (usedStats.Contains(stat)) continue; //if it's in used stats, skip it

            if (!modDatabase.TryGetDefinition(stat, out AffixDefinitions def))  //If we can't find it's definition, skip it. If we can store it in def
                continue;

            if (!GroupsAvailable(def, usedGroups))  //Check if the group is available, if not, skip it
                continue;

            var availableTier = def.tiers.Where(t => t.minItemLevel <= itemLevel).ToList();     //Decide what item tiers are valid to roll based on the item's ilvl and the min ilvl that each tier rolls at
            if (availableTier.Count == 0)   //If it can't roll more than 0 tiers, skip it
                continue;

            int baseWeight = baseStatWeights.TryGetValue(stat, out int w) ? w : defaultStatWeight;  //grab the base weight, and store it in w if it has a special weighting, otherwise use default
            if (baseWeight <= 0) continue;

            candidates.Add((stat, baseWeight)); //Add the stat and its weight to candidates
        }

        if (candidates.Count == 0)  //If candidates is 0, return
            return null;

        StatTypes chosenStat = WeightedRandomPick(candidates);  //Call weightedrandompick to choose a random mod from candidates
        return RollTierAndValue(chosenStat, rarity, itemLevel);     //Call rolltierandvalue to decide on the actual mod tier and value within that tier, then return that value
    }

    private RolledMod RollTierAndValue(StatTypes stat, LootManager.GearRarity rarity, int itemLevel)
    {
        AffixDefinitions def = modDatabase.GetDefinition(stat);     //grab definition of the passed in stat
        var available = def.tiers.Where(t => t.minItemLevel <= itemLevel).ToList();     //create a list of available tiers
        if (available.Count == 0) return null;  //if there are no available tiers, return

        float[] biasArray = GetBiasArray(rarity);   //grab the bias array, using the rarity of the item
        var tierCandidates = new List<(AffixTier tier, float weight)>();        //Create a new list of tier candidates, using the tier as key and weight of that tier as the value

        foreach (var tier in available)
        {
            int index = Mathf.Clamp(tier.tierIndex - 1, 0, biasArray.Length - 1);   //create an index variable that is the index - 1 clamped between 0 and the length of the bias array
            float finalWeight = tier.weight * biasArray[index];     //calculate the final weight by multiplying the weight by the bias array at index (should be based on the rarity of the item)
            if (finalWeight <= 0) continue;     //if the mod has less than or equal to 0 weight, skip it

            tierCandidates.Add((tier, finalWeight));        //add it to tier candidates, using the final calculated weight
        }

        if (tierCandidates.Count == 0) return null;     //if tier candidates is empty, return null

        AffixTier chosenTier = WeightedRandomPick(tierCandidates);          //call weighted random pick to choose a tier
        float roll = Random.Range(chosenTier.minValue, chosenTier.maxValue);            //choose a roll by rolling a random range between the tier's min and max values

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

    //Function used to grab the bias array for each rarity of gear by calling it with the item's rarity
    private float[] GetBiasArray(LootManager.GearRarity rarity)
    {
        switch (rarity)
        {
            case LootManager.GearRarity.Magic: return magicTierBias;
            case LootManager.GearRarity.Rare: return rareTierBias;
            case LootManager.GearRarity.Legendary: return legendaryTierBias;
            default: return magicTierBias;
        }
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
