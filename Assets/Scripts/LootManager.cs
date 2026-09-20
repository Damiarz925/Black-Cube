// Developer map: Rolls item type/rarity/element and delegates affixes to ModManager, then returns one Gear to GameManager. Random weapons currently exclude unfinished Void bases.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    Dictionary<GearType, int> TypeDictionary = new();       //Dictionary with geartype as key, integer as value
    int totalTypeWeight;

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
        totalTypeWeight = 0;

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

        initialized = true;
    }

    public Gear GenerateLoot(EnemyAI.EnemyRarity enemyRarity,ILootRandomSource random=null)
    {
        random??=LootRandomSourceFactory.CreateProduction();
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

        var type = RollItemType(random);
        var element = RollItemElement(type==GearType.Weapons,random);

        int zoneLevel = zoneManager != null ? zoneManager.zoneLevel : 1;
        int itemLevel = zoneLevel + enemyRarityMod;     //Calculate item level as the level of the zone + the enemy rarity modifier

        var rarity = RollItemRarity(itemLevel,random);
        string weaponTypeId=type==GearType.Weapons?RollWeaponTypeId(random):null;

        Debug.Log($"[Loot] Rolled type={type}, rarity={rarity}, zoneLevel={zoneLevel}, enemyRarity={enemyRarity}");

        GameObject obj = gearPrefab != null ? Instantiate(gearPrefab) : new GameObject("Generated Gear");       //instantiate a gear item using the gear prefab
        var gear = obj.GetComponent<Gear>();            //assign variable gear by grabbing the gear component from the gear item
        if (gear == null)
            gear = obj.AddComponent<Gear>();

        gear.Initialize(type,rarity,itemLevel,element,weaponTypeId);

        Debug.Log($"[Loot] After Initialize: gear.ItemType={gear.ItemType}, gear.ItemRarity={gear.ItemRarity}, ilvl={gear.ItemLevel}, modCount={gear.ModCount}");

        List<RolledMod> mods = null;
        if (ModManager.Instance != null)
        {
            // Exclusive groups can dead-end a full 3P/3S construction. Retry
            // construction without changing the rolled rarity or drop rate.
            for (int attempt = 0; attempt < 64 && mods == null; attempt++)
                mods = ModManager.Instance.RollEquipmentModsForItem(type, rarity, itemLevel, element,gear.WeaponTypeId,random);
        }

        if (mods == null)
        {
            Debug.LogError($"[Loot] Cannot construct {rarity} {type} at ilvl {itemLevel}; rarity was not silently downgraded.");
            Destroy(obj);
            return null;
        }

        Debug.Log($"[Loot] Rolled mods count = {(mods == null ? -1 : mods.Count)}");

        gear.ApplyMods(mods);   //call apply mods on the gear item, passing in the list of mods to apply
        if(type==GearType.Weapons)ApplyNaturalWeaponProfile(gear);

        return gear;        //return the gear item
    }

    public GearType RollItemType()=>RollItemType(UnityLootRandomSource.Instance);
    public GearType RollItemType(ILootRandomSource random)
    {
        InitializeLootTables();

        if (totalTypeWeight <= 0)
            return GearType.Helmets;

        int roll = random.Range(0, totalTypeWeight);        //Rolls a number between 0 and the total item type weight
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
        if(zoneManager==null)zoneManager=FindFirstObjectByType<ZoneManager>();
        return RollItemRarity(zoneManager!=null?zoneManager.zoneLevel:1);
    }

    public static string RollWeaponTypeId()=>RollWeaponTypeId(UnityLootRandomSource.Instance);
    public static string RollWeaponTypeId(ILootRandomSource random)
    {
        var all=WeaponTypeCatalog.All;return all[random.Range(0,all.Count)].Id;
    }

    public static void ApplyNaturalWeaponProfile(Gear gear)
    {
        if(gear==null||gear.ItemType!=GearType.Weapons||!WeaponTypeCatalog.TryGet(gear.WeaponTypeId,out var profile))return;
        float damageScale=gear.ItemLevel<=1?1f:Mathf.Max(.01f,gear.BaseDamage/40f);
        float speedScale=gear.ItemLevel<=1?1f:Mathf.Max(.01f,gear.BaseAttackSpeed/.60f);
        float critScale=gear.ItemLevel<=1?1f:Mathf.Max(.01f,gear.BaseCritChance/.05f);
        gear.BaseDamageMin=profile.BaseDamageMin*damageScale;gear.BaseDamageMax=profile.BaseDamageMax*damageScale;
        gear.BaseDamage=(gear.BaseDamageMin+gear.BaseDamageMax)*.5f;gear.BaseAttackSpeed=profile.AttacksPerSecond*speedScale;gear.BaseCritChance=profile.BaseCritChance*critScale;
    }

    // Player drops have their own level-dependent rates; enemy-equipped rarity is
    // still driven by EnemyAI.CurrentRarity and is never sampled from this table.
    public static Vector4 RarityRatesForLevel(int level)
    {
        level=Mathf.Max(1,level);
        if(level<20)return Blend(level,1,20,new Vector4(60,31,8,1),new Vector4(42,39,17,2));
        if(level<50)return Blend(level,20,50,new Vector4(42,39,17,2),new Vector4(28,41,28,3));
        if(level<75)return Blend(level,50,75,new Vector4(28,41,28,3),new Vector4(19,35,42,4));
        return new Vector4(19,35,42,4);
    }

    static Vector4 Blend(int level,int low,int high,Vector4 start,Vector4 end)
    {
        float t=Mathf.SmoothStep(0f,1f,(level-low)/(float)(high-low));
        return Vector4.Lerp(start,end,t);
    }

    public GearRarity RollItemRarity(int itemLevel)=>RollItemRarity(itemLevel,UnityLootRandomSource.Instance);
    public GearRarity RollItemRarity(int itemLevel,ILootRandomSource random)
    {
        Vector4 rates=RarityRatesForLevel(itemLevel);
        float roll=random.Value()*100f;
        if(roll<rates.x)return GearRarity.Normal;
        if(roll<rates.x+rates.y)return GearRarity.Magic;
        if(roll<rates.x+rates.y+rates.z)return GearRarity.Rare;
        return GearRarity.Legendary;
    }

    public Element RollItemElement(bool forWeapon = false)=>RollItemElement(forWeapon,UnityLootRandomSource.Instance);
    public Element RollItemElement(bool forWeapon,ILootRandomSource random)
    {
        if (!forWeapon) return (Element)random.Range(0, (int)Element.Count);
        int roll = random.Range(0, 5); // Phys, Fire, Cold, Lightning, Void; legacy Poison is skipped.
        return roll < (int)Element.Poison ? (Element)roll : Element.Void;
    }
}
