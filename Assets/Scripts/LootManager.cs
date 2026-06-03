using System.Collections.Generic;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    Dictionary<GearType, int> TypeDictionary = new();       //Dictionary with geartype as key, integer as value
    Dictionary<GearRarity, int> RarityDictionary = new();   //Dictionary with rarity as key, integer as value
    int totalTypeWeight;
    int totalRarityWeight;

    public enum GearType        //GearType enum stores the various types of gear
    {
        Helmets,
        Amulets,
        BodyArmours,
        Gloves,
        Boots,
        Rings,
        Belts,
        Weapons
    }

    public enum GearRarity      //GearRarity enum stores the rarity levels that gear can have
    {
        Normal,
        Magic,
        Rare,
        Legendary
    }

    [Header("Loot Prefab")]
    [SerializeField] private GameObject gearPrefab;         //GearPrefab that is used to instantiate gear items
    [SerializeField] private ZoneManager zoneManager;       //Field for the zone manager, necessary for figuring out item level of items to generate

    private bool initialized;

    private void Awake()
    {
        InitializeLootTables();
    }

    void Start()        //Add all of the gear types with their weightings into the dictionary
    {
        InitializeLootTables();
    }

    private void InitializeLootTables()
    {
        if (initialized)
            return;

        TypeDictionary.Clear();
        RarityDictionary.Clear();
        totalTypeWeight = 0;
        totalRarityWeight = 0;

        TypeDictionary.Add(GearType.Weapons, 20);
        TypeDictionary.Add(GearType.Helmets, 20);
        TypeDictionary.Add(GearType.BodyArmours, 20);
        TypeDictionary.Add(GearType.Gloves, 10);
        TypeDictionary.Add(GearType.Boots, 10);
        TypeDictionary.Add(GearType.Amulets, 5);
        TypeDictionary.Add(GearType.Rings, 5);
        TypeDictionary.Add(GearType.Belts, 5);

        foreach (var item in TypeDictionary)    //Calculate the total Type weight by addint the weights of each item
            totalTypeWeight += item.Value;

        RarityDictionary.Add(GearRarity.Normal, 40);        //Add all of the Rarities and their weightings to the rarity dictionary, and calculate  a total weighting for rarity
        RarityDictionary.Add(GearRarity.Magic, 20);
        RarityDictionary.Add(GearRarity.Rare, 10);
        RarityDictionary.Add(GearRarity.Legendary, 1);

        foreach (var item in RarityDictionary)
            totalRarityWeight += item.Value;

        initialized = true;
    }

    public Gear GenerateLoot(EnemyAI.EnemyRarity enemyRarity)
    {
        InitializeLootTables();

        if (zoneManager == null)
            zoneManager = FindFirstObjectByType<ZoneManager>();

        //Depending on the rarity of the enemy evaluate to 0-3 for the enemy rarity modifier
        int enemyRarityMod = enemyRarity switch
        {
            EnemyAI.EnemyRarity.Normal => 0,
            EnemyAI.EnemyRarity.Magic => 1,
            EnemyAI.EnemyRarity.Rare => 2,
            _ => 3
        };

        var element = RollItemElement();

        int zoneLevel = zoneManager != null ? zoneManager.zoneLevel : 1;
        int itemLevel = zoneLevel + enemyRarityMod;     //Calculate item level as the level of the zone + the enemy rarity modifier

        var type = RollItemType();          //assign variable type by rolling an item type
        var rarity = RollItemRarity();      //assign variable rarity by rolling the item rarity

        Debug.Log($"[Loot] Rolled type={type}, rarity={rarity}, zoneLevel={zoneLevel}, enemyRarity={enemyRarity}");

        GameObject obj = gearPrefab != null ? Instantiate(gearPrefab) : new GameObject("Generated Gear");       //instantiate a gear item using the gear prefab
        var gear = obj.GetComponent<Gear>();            //assign variable gear by grabbing the gear component from the gear item
        if (gear == null)
            gear = obj.AddComponent<Gear>();

        gear.Initialize(type, rarity, itemLevel, element);       //Initialize the gear item

        Debug.Log($"[Loot] After Initialize: gear.ItemType={gear.ItemType}, gear.ItemRarity={gear.ItemRarity}, ilvl={gear.ItemLevel}, modCount={gear.ModCount}");

        var mods = ModManager.Instance != null
            ? ModManager.Instance.RollModsForItem(type, rarity, itemLevel, gear.ModCount)
            : new List<RolledMod>();     //generate mods by calling rollmodsforitem (returns a list of mods)

        Debug.Log($"[Loot] Rolled mods count = {(mods == null ? -1 : mods.Count)}");

        gear.ApplyMods(mods);   //call apply mods on the gear item, passing in the list of mods to apply

        return gear;        //return the gear item
    }

    public GearType RollItemType()
    {
        InitializeLootTables();

        if (totalTypeWeight <= 0)
            return GearType.Helmets;

        int roll = Random.Range(0, totalTypeWeight);        //Rolls a number between 0 and the total item type weight
        foreach (var pair in TypeDictionary)        //For each pair in the type dictionary
        {
            GearType type = pair.Key;       //set type to the gear type of the current gear item
            int weight = pair.Value;        //set weight to the weight of the current gear item
            if (roll < weight) return type; //if the roll is less than the weight of the current item, return this item
            roll -= weight;     //subtract the weight from roll then loop to next item
        }
        return GearType.Helmets;        //If we finished the loop without returning, default to returning a helmet type
    }

    public GearRarity RollItemRarity()
    {
        InitializeLootTables();

        if (totalRarityWeight <= 0)
            return GearRarity.Normal;

        int roll = Random.Range(0, totalRarityWeight);      //Roll randomly between 0 and the total rarity weight
        foreach (var pair in RarityDictionary)
        {
            GearRarity rarity = pair.Key;       //Create variable rarity and give it the rarity value for the item from the dictionary
            int weight = pair.Value;            //create variable weight and give it the weight value for the item from the dictionary
            if (roll < weight) return rarity;   //If the roll is less than the weight, return that rarity
            roll -= weight;     //Subtract weight from the roll and loop
        }
        return GearRarity.Normal;   //If we didn't pick a rarity in the loop, return normal by default
    }

    public Element RollItemElement()
    {
        int max = (int)Element.Count;
        int roll = Random.Range(0, max);
        return (Element)roll;
    }
}
