using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Base stats / refs")]
    public float baseSpeed = 2f;        //Enemy base speed, defaulted to 2f (overriden by stats component I think)

    [SerializeField] private LootManager lootManger;        //field for loot manager
    [SerializeField] private ModManager modManager;         //field for mod manager

    private StatsComponent stats;           //fields for stats, health, statuscont, zone manager, and damage popup
    private HealthComponent health;
    private StatusController statusController;
    private DamageReceiver damageReceiver;
    private ZoneManager zoneManager;
    private DamagePopup damagePopup;

    public EnemyRarity CurrentRarity { get; private set; } = EnemyRarity.Normal;        //public getter/private setter for the enemy's rarity, defaulted to normal

    private bool loggedMissingWeaponSpeedWarning = false;       //bools for whether warnings were logged or not
    private bool loggedMissingStatsSpeedWarning = false;

    private int enemyLevel;
    public int EnemyLevel => enemyLevel;
    public Element WeaponMainElement => equippedWeapon != null ? equippedWeapon.BaseElement : Element.Phys;

    [Header("Equipment")]
    [SerializeField] private Gear equippedWeapon;                   //private gear field for the enemy's equipped weapon
    private readonly List<Gear> equippedItems = new List<Gear>();               //readonly list of the enemy's equipped items

    private bool _initialized;      //bool for whether the enemy has been initialized or not

    public enum EnemyRarity         //enum for enemy rarity Normal, magic, rare, legendary
    {
        Normal,
        Magic,
        Rare,
        Legendary
    }

    private readonly Dictionary<EnemyRarity, int> enemyRarityWeights = new();       //dictionary with enemy rarity as key and an int as value, used for the rarity weights used when spawning enemies
    private int totalEnemyRarityWeight;         //variable used to store the total enemy rarity weight

    private void Awake()        //on awake, grab the stats component, health component, and status controller
    {
        stats = GetComponent<StatsComponent>();
        health = GetComponent<HealthComponent>();
        statusController = GetComponent<StatusController>();
        damageReceiver = GetComponent<DamageReceiver>();
    }

    public void InitializeEnemy(int zoneLevel)      //initialize the enemy using the zone level
    {
        if (_initialized) return;       //if the enemy is already initialized (_initialized is true), return
        _initialized = true;            //set initialized to true

        zoneManager = FindFirstObjectByType<ZoneManager>();     //grab the zone manager
        damagePopup = FindFirstObjectByType<DamagePopup>();     //grab the damage popup

        enemyLevel = zoneManager != null ? zoneManager.zoneLevel : zoneLevel;       //set the enemy level equal to the zone level if zone manager is not null, otherwise set it to 1

        InitRarityWeights();            //call initrarityweights
        CurrentRarity = RollEnemyRarity();      //set current rarity by calling rollenemyrarity

        GenerateGearForEnemy(enemyLevel);       //call generate gear for enemy using the enemy's level

        Debug.Log($"EnemyAI: Initialized enemy '{name}' level={enemyLevel}, rarity={CurrentRarity}, weaponElement={WeaponMainElement}", this);
    }

    private void InitRarityWeights()        //initialize rairty weights by clearing the list, adding the raritys and their weights to the dictionary then for each pair in the list, add that to the total weight.
    {
        enemyRarityWeights.Clear();
        enemyRarityWeights.Add(EnemyRarity.Normal, 40);
        enemyRarityWeights.Add(EnemyRarity.Magic, 20);
        enemyRarityWeights.Add(EnemyRarity.Rare, 10);
        enemyRarityWeights.Add(EnemyRarity.Legendary, 1);

        totalEnemyRarityWeight = 0;
        foreach (var kvp in enemyRarityWeights)
            totalEnemyRarityWeight += kvp.Value;
    }

    public EnemyRarity RollEnemyRarity()        //roll the enemy rarity
    {
        int roll = Random.Range(0, totalEnemyRarityWeight);     //roll is a value between 0 and the total enemy rarity weight calculated in initrarityweights

        foreach (var pair in enemyRarityWeights)        //for each pair in enemyrarityweights
        {
            EnemyRarity rarity = pair.Key;      //rarity is the key
            int weight = pair.Value;        //weight is the value

            if (roll < weight)      //if the roll is less than the weight, return the current rarity
                return rarity;

            roll -= weight;     //subtract the weight from the roll and loop
        }

        return EnemyRarity.Normal;      //if no weight chosen in loop, return 
    }

    private LootManager.GearRarity MapEnemyRarityToGearRarity(EnemyRarity rarity)       //maps the enemy rarity to gear rarity with a switch statement setting each rarity to the same rarity for gear. Default to normal
    {
        return rarity switch
        {
            EnemyRarity.Normal => LootManager.GearRarity.Normal,
            EnemyRarity.Magic => LootManager.GearRarity.Magic,
            EnemyRarity.Rare => LootManager.GearRarity.Rare,
            EnemyRarity.Legendary => LootManager.GearRarity.Legendary,
            _ => LootManager.GearRarity.Normal
        };
    }

    private void GenerateGearForEnemy(int zoneLevel)        
    {
        if (modManager == null)
        {
            Debug.LogWarning("EnemyAI has no ModManager assigned; no gear generated."); //If mod monater is null, give error and return
            return;
        }

        int itemCount = GetItemCountForLevel(zoneLevel);    //item count is given by getitemcountforlevel function, passing in the zone level

        // Weapon first
        Gear weapon = CreateItemForEnemy(LootManager.GearType.Weapons, zoneLevel);      //call create item for enemy, specifying weapon and the zone level
        equippedItems.Add(weapon);      //add the weapon to the equipped items list
        EquipWeapon(weapon);        //call equipweapon, passing in the generated weapon

        // Other items
        for (int i = 1; i < itemCount; i++)     //generate nonweapon items up to the item count. i starts at 1, so if item count is 1, generates no additional items (because we already have a weapon). Adds the item to the list, and applies mods from the item to the enemy
        {
            LootManager.GearType type = RollRandomNonWeaponType();
            Gear gear = CreateItemForEnemy(type, zoneLevel);
            equippedItems.Add(gear);
            ApplyGlobalModsFromGear(gear);
        }
    }

    private int GetItemCountForLevel(int level)     //Calculates the range of items possible and rolls within that range, then returns the count to be used for item generation
    {
        if (level < 35) return Random.Range(1, 3);
        if (level < 50) return Random.Range(2, 5);
        if (level < 75) return Random.Range(4, 7);
        return 9;
    }

    private LootManager.GearType RollRandomNonWeaponType()      //rolls random non weapon types by rolling between 0 and 7, then picks from the item slot using a switch statement
    {
        int roll = Random.Range(0, 8);

        return roll switch
        {
            0 => LootManager.GearType.Helmets,
            1 => LootManager.GearType.Amulets,
            2 => LootManager.GearType.BodyArmours,
            3 => LootManager.GearType.Gloves,
            4 => LootManager.GearType.Boots,
            5 => LootManager.GearType.Rings,
            6 => LootManager.GearType.Belts,
            _ => LootManager.GearType.Helmets
        };
    }

    private Gear CreateItemForEnemy(LootManager.GearType type, int zoneLevel)       //Function used to create the item for the enemy to use
    {
        LootManager.GearRarity gearRarity = MapEnemyRarityToGearRarity(CurrentRarity);      //Set the gear rarity equal to the enemy rarity

        GameObject go = new GameObject($"Enemy_{type}_{gearRarity}");       //create a new game object
        go.transform.SetParent(transform);

        Gear gear = go.AddComponent<Gear>();            //create a gear object, which is go with the gear component added

        var element = RollItemElement();

        int itemLevel = zoneLevel;          //set item level equal to zone level
        gear.Initialize(type, gearRarity, itemLevel, element);       //initailize gear, passing in the gear type, rarity, ilvl, and element

        int modCount = gear.ModCount;        //roll for the mod count of the item
        var rolledMods = modManager.RollModsForItem(type, gearRarity, itemLevel, modCount);     //create variable for rolled mods, using the rollmodsforitem function from modmanager
        gear.ApplyMods(rolledMods);     //use gear.applymods with the rolled mods list to apply those mods to the gear item

        if (type == LootManager.GearType.Weapons)       //if the gear type is weapon
        {
            if (gear.BaseDamage <= 0f)      //if the base damage is less than 0
            {
                gear.BaseElement = Element.Phys;        //set its element to phys
                gear.BaseDamage = 2.5f + 1f * itemLevel;        //set its base damage to 2.5f + 1f * ilvl
                gear.BaseAttackSpeed = 1.0f + 0.01f * itemLevel;        //set its base attack speed to 1 + (.01 * ilvl)
                gear.BaseCritChance = 0.05f;        //set it's base crit chance to 5%
            }
        }

        return gear;    //return the gear item
    }

    private void EquipWeapon(Gear weapon)
    {
        if (equippedWeapon != null)     //if equipped weapon is not null
        {
            stats.RemoveModifiersFromSource(equippedWeapon);        //remove the modifiers given from the currently equipped weapon
        }

        equippedWeapon = weapon;        //set the equippedweapon field to the new passed in weapon
        if (equippedWeapon == null) return;     //if the equipped weapon is now null, return

        foreach (var mod in equippedWeapon.globalRolledMods)        //for each global mod on the equipped weapon, get the operation for the stat, get the modifier, and add it to the statscomponent
        {
            StatOp op = GetOperationForStat(mod.statType);
            var statMod = new StatModifier(mod.statType, op, mod.value, equippedWeapon);
            stats.AddModifier(statMod);
        }
    }

    private void ApplyGlobalModsFromGear(Gear gear)     //applies the global mods from a gear item
    {
        foreach (var mod in gear.globalRolledMods)      //for each mod in in the items global rolled mods list, grab the operation, grab the stat modifier, and add the modifier to the stats component
        {
            StatOp op = GetOperationForStat(mod.statType);
            var statMod = new StatModifier(mod.statType, op, mod.value, gear);
            stats.AddModifier(statMod);
        }
    }

    private StatOp GetOperationForStat(StatTypes stat)      //used to get the operation for a given stat
    {
        string name = stat.ToString();      //grabs the name of the stat casted to a string
        if (name.StartsWith("Flat")) return StatOp.Flat;        //if it starts with flat (this is consistent for all flat mods currently), return statop.flat
        if (name.EndsWith("Mult")) return StatOp.Additive;        //if it ends with mult (this is consistent for all mult mods currently), return statop.multiplicative
        return StatOp.Additive;     //otherwisse, return statop.additive
    }

    public DamageContext BuildAttackContext()       //builds the attack context, called when enemy attacks
    {
        DamageContext ctx = new DamageContext(4);       //create a damage context with an initial capacity of 4

        if (equippedWeapon == null) return ctx;     //if equippedweapon is null, return the context now

        Element weaponElement = equippedWeapon.BaseElement;     //store the weapon's base element in weaponElement
        float weaponBaseDamage = equippedWeapon.GetEffectiveBaseDamage();       //store the weapon's base damage in weaponBaseDamage (call GetEffectiveBaseDamage from Gear class on equippedWeapon)

        AddScaledElementalDamage(ctx, weaponElement, weaponBaseDamage);     //call addscaledelemental damage to add the scaled ele damage (the base element damage scaled by local mods matching that element on the item)
        AddGlobalFlatElements(ctx, weaponElement);      //call addgloablflatelements to add any flat elemental damage that does not match the weapon's base element

        float critChance = GetFinalCritChance();        //call get final crit chance and store it in critchance
        float critMult = 1f + stats.GetStat(StatTypes.CritMult);        //grab the crit multi and add 1 to it, store it in critmult

        bool isCrit = Random.value < Mathf.Clamp01(critChance);         //decide if the attack is a critical hit by checking if a random value is less than the crit chance (clamped between 0 and 1)

        ctx.IsCrit = isCrit;        //set context.is crit based on the previous random roll
        ctx.CritMultiplier = isCrit ? critMult : 1f;        //if the attack is a crit, context.critmulti is set to critmult, otherwise set to 1.

        if (isCrit)     //if iscrit is true
        {
            for (int i = 0; i < ctx.Hits.Count; i++)        //multiply the damage amount of each hit in ctx.hits by critmult
            {
                var h = ctx.Hits[i];
                h.Amount *= critMult;
                ctx.Hits[i] = h;
            }
        }

        return ctx; //return context
    }

    private void AddScaledElementalDamage(DamageContext ctx, Element element, float baseAmount)
    {
        float flatGlobal = stats.GetStat(StatMappings.GetFlatDamageStat(element));      //get the flat global of the passed in element
        float incElement = stats.GetStat(StatMappings.GetIncDamageStat(element));       //get the inc ele damage of the passed in element
        float incGeneric = stats.GetStat(StatTypes.GenericDmg);             //get the global inc damage
        float incTotal = incElement + incGeneric;           //calculate total inc damage as element + gloabl

        float moreElement = stats.GetStat(StatMappings.GetMoreDamageStat(element));     //repeat above for more damage to calculate total more damage as more + generic
        float moreGeneric = stats.GetStat(StatTypes.GenericMult);
        float moreTotal = (1f + moreElement) * (1f + moreGeneric);
        Debug.Log($"Enemy dmg stats: base={baseAmount}, flatG={flatGlobal}, incElem={incElement}, incGen={incGeneric}, moreElem={moreElement}, moreGen={moreGeneric}");

        float amount = (baseAmount + flatGlobal) * (1f + incTotal) * moreTotal;    //calculate final damage amount as basedmg + matching flat damage times (1 + increasedtotal) time (1 + moretotal) 

        ctx.AddDamage(element, amount);     //add this damage to the damage context passing in the element of the damage and the amount
    }

    private void AddGlobalFlatElements(DamageContext ctx, Element weaponElement)        //add all of the extra flat elemental damage that isn't matching the base element
    {
        AddExtraElementIfNotBase(ctx, Element.Phys, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Fire, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Cold, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Light, weaponElement);
    }

    private void AddExtraElementIfNotBase(DamageContext ctx, Element element, Element weaponElement)
    {
        if (element == weaponElement) return;       //if the passed in element matches the weapon element, return

        float flatGlobal = stats.GetStat(StatMappings.GetFlatDamageStat(element));      //get the flat damage stat, if it's 0, return
        if (flatGlobal <= 0f) return;

        float incElement = stats.GetStat(StatMappings.GetIncDamageStat(element));       //get the increased damage stat for the current element, add it to global increased damage to calc inctotal
        float incGeneric = stats.GetStat(StatTypes.GenericDmg);
        float incTotal = incElement + incGeneric;

        float moreElement = stats.GetStat(StatMappings.GetMoreDamageStat(element));     //get the more damage stat for the current element, add it to global more damage to calc moretotal
        float moreGeneric = stats.GetStat(StatTypes.GenericMult);
        float moreTotal = (1f + moreElement) * (1f + moreGeneric);

        float amount = flatGlobal * (1f + incTotal) * moreTotal;      //calculate total damage for this element as the flat * (1+totalinc) * (1+totalmore)
        ctx.AddDamage(element, amount);     //add this element to damage context
    }

    private float GetFinalCritChance()
    {
        if (equippedWeapon == null) return 0f;      //if equipped weapon is null, return 0f for crit chance

        float weaponCrit = equippedWeapon.GetEffectiveBaseCrit();       //call geteffectivebasecrit on equipped weapon, assign that to weaponcrit variable
        float incCritGlobal = stats.GetStat(StatTypes.CritChance);      //get the global increased crit stat on the enemy
        float extraBaseCritGlobal = stats.GetStat(StatTypes.BaseCritChance);        //get the extra base crit stat on the enemy

        float baseCritAll = weaponCrit + extraBaseCritGlobal;       //get the total base crit by adding the weapon base crit and the extra base crit
        return Mathf.Clamp01(baseCritAll * (1f + incCritGlobal));       //calculate the total crit chance as the basecritall * (1+inccritglobal) and clamp is between 0 and 1.
    }

    public float GetFinalAttackSpeed()
    {
        float incASGlobal = 0f;     //float for global inc attack speed

        if (stats == null)      //if stats is null, give warning, set log bool to true
        {
            if (!loggedMissingStatsSpeedWarning)
            {
                Debug.LogWarning("EnemyAI.GetFinalAttackSpeed: stats is null, treating global AS as 0", this);
                loggedMissingStatsSpeedWarning = true;
            }
        }
        else
        {
            incASGlobal = stats.GetStat(StatTypes.AttackSpeed);     //if stats is not null, grab gloabl inc attack speed stat
        }

        if (equippedWeapon == null)     //if weapon is null, give warning, set log bool to true
        {
            if (!loggedMissingWeaponSpeedWarning)
            {
                Debug.LogWarning("EnemyAI.GetFinalAttackSpeed: equippedWeapon is null, using baseSpeed", this);
                loggedMissingWeaponSpeedWarning = true;
            }

            return baseSpeed * (1f + incASGlobal);      //return the base speed time (1+incASGlobal)
        }

        float weaponAS = equippedWeapon.GetEffectiveAttackSpeed();      //set weaponAS equal to the effective attack speed returned from the Gear.GetEffectiveAttackSpeed() function called on the equipped weapon
        return weaponAS * (1f + incASGlobal);       //return the effective attack speed time (1+incASGlobal)
    }

    public float GetAttackDamagePreview()
    {
        DamageContext ctx = BuildAttackContext();       //build the attack context
        float total = 0f;
        foreach (var hit in ctx.Hits) total += hit.Amount;      //calculate the total damage coming out of the context
        return total;       //return the total
    }

    public void TakeDamage(float damage, StatusEffects effect = null)
    {
        if (damage <= 0f) return;       //if the damage is less than or equal to 0, return

        if (damageReceiver == null)
            damageReceiver = GetComponent<DamageReceiver>();

        if (damageReceiver != null)
        {
            damageReceiver.TakeDamage(damage, Element.Phys, effect);
        }
        else
        {
            health.LoseLife(damage);        //call loselife from health component, passing in the damage amount

            if (damagePopup != null)        //if damage popup isn't null, spawn the poup with the transform and damage amount
            {
                damagePopup.Spawn(damage, transform, effect);
            }
        }
    }

    public void OnStatusTick(float strength, StatusEffects effect)  //(Modify this function to manage non damaging ailments as well later)
    {
        if (effect == null || strength <= 0f) return;   //if the effect is null, or has less than or equal to 0 strength, return

        if (effect._StatusType == StatusEffects.StatusType.DamageOverTime)      //if the effect is a damage over time effect, call take damage, passing in the effect and its strength
        {
            TakeDamage(strength, effect);
        }
    }

    public Element RollItemElement()
    {
        int max = (int)Element.Count;
        int roll = Random.Range(0, max);
        return (Element)roll;
    }
}
