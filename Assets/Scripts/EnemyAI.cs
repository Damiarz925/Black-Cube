// Developer map: Builds enemy attack snapshots from generated gear and StatsComponent; BattleManager owns attack scheduling. Enemy rarity is randomly rolled separately from the HealthComponent boss role.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // Boss pressure is role-owned and separate from the authored level-1
    // prefab Life seeds and the shared intrinsic level curve.
    public const float BossRoleDamageMultiplier = 1.8f;
    [Header("Base stats / refs")]
    public float baseSpeed = 0.5f;        //Enemy base speed, defaulted to 2f (overriden by stats component I think)

    [SerializeField] private LootManager lootManger;        //field for loot manager
    [SerializeField] private ModManager modManager;         //field for mod manager
    [SerializeField] private EnemyDropTable dropTable = new EnemyDropTable();

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
    private WorldContentDatabase contentDatabase;
    private EnemyArchetypeDefinition contentArchetype;
    private BossDefinition contentBoss;
    private CorruptionMechanicProfile corruptionProfile;
    private LocationMechanicProfile locationProfile;
    private int authoredTurnCount;
    private EnemySkillDefinition activeAuthoredSkill;
    private float activeBossPhaseDamageMultiplier=1f;
    public int CurrentAuthoredHitCount { get; private set; } = 1;
    public string ContentId => contentBoss?.stableId ?? contentArchetype?.stableId ?? string.Empty;
    public string ContentDisplayName => contentBoss?.displayName ?? contentArchetype?.displayName ?? name;
    public EnemySkillDefinition ActiveAuthoredSkill => activeAuthoredSkill;
    public int EnemyLevel => enemyLevel;
    public float IntrinsicDamageFactor
    {
        get
        {
            var setup = GetComponent<EnemyStatSetup>();
            return setup != null && setup.Intrinsic.Level > 0 ? setup.Intrinsic.DamageFactor : 1f;
        }
    }
    public IReadOnlyList<Gear> EquippedItems => equippedItems;
    public EnemyBuildOptimizer.Evaluation LastBuildEvaluation { get; private set; }
    public float[] LastOptimizerBaseStats { get; private set; }
    public Element WeaponMainElement => equippedWeapon != null ? equippedWeapon.BaseElement : Element.Phys;
    public float EquippedWeaponBaseDamage => equippedWeapon != null ? equippedWeapon.GetEffectiveBaseDamage() : 0f;
    public Gear EquippedWeapon => equippedWeapon;
    public EnemyDropTable DropTable { get { dropTable??=new EnemyDropTable();dropTable.EnsureDefaults();return dropTable; } }

    public Gear GetEquippedGear(LootManager.GearType type)
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            Gear item = equippedItems[i];
            if (item != null && item.ItemType == type)
                return item;
        }
        return null;
    }

    [Header("Equipment")]
    [SerializeField] private Gear equippedWeapon;                   //private gear field for the enemy's equipped weapon
    private readonly List<Gear> equippedItems = new List<Gear>();               //readonly list of the enemy's equipped items
    private static readonly LootManager.GearType[] NonWeaponGearSlots = new LootManager.GearType[]
    {
        LootManager.GearType.Helmets,
        LootManager.GearType.Amulets,
        LootManager.GearType.BodyArmours,
        LootManager.GearType.Gloves,
        LootManager.GearType.Boots,
        LootManager.GearType.Rings,
        LootManager.GearType.Belts
    };
    private const int MaxNonWeaponSlots = 7;
    private const int MaxTotalEnemyGearPieces = 8;

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

    public void ConfigureWorldContent(WorldContentDatabase database, EnemyArchetypeDefinition archetype,
        BossDefinition boss, CorruptionTierDefinition corruption, LocationDefinition location)
    {
        contentDatabase=database;contentArchetype=archetype;contentBoss=boss;
        corruptionProfile=database?.CorruptionMechanic(corruption?.mechanicProfileId);
        locationProfile=database?.LocationMechanic(location?.mechanicProfileId);
        authoredTurnCount=0;activeAuthoredSkill=null;
        activeBossPhaseDamageMultiplier=1f;CurrentAuthoredHitCount=1;
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

        EnemyStatSetup statSetup = GetComponent<EnemyStatSetup>();
        if (statSetup != null)
            statSetup.SetupForZone(enemyLevel, health != null && health.IsBoss);

        GenerateGearForEnemy(enemyLevel);       //call generate gear for enemy using the enemy's level
        ApplyWorldContentProfile();
        health?.RestoreFullLife();               //enter combat full after Life and LifePercent gear modifiers are applied

        Debug.Log($"EnemyAI: Initialized enemy '{name}' level={enemyLevel}, rarity={CurrentRarity}, weaponElement={WeaponMainElement}", this);
    }

    void ApplyWorldContentProfile()
    {
        if(stats==null)return;
        float damage=contentArchetype?.damageMultiplier??1f;
        float speed=(contentArchetype?.attackSpeedMultiplier??1f)*(corruptionProfile?.speedMultiplier??1f)*(locationProfile?.enemySpeedMultiplier??1f);
        float life=contentArchetype?.lifeMultiplier??1f;
        if(damage>1f)stats.AddModifier(new StatModifier(StatTypes.GenericDmg,StatOp.Additive,damage-1f,this));
        if(speed>1f)stats.AddModifier(new StatModifier(StatTypes.AttackSpeed,StatOp.Additive,speed-1f,this));
        if(life>1f)stats.AddModifier(new StatModifier(StatTypes.LifePercent,StatOp.Additive,life-1f,this));
        string loadout=contentBoss?.skillLoadoutId??contentArchetype?.skillLoadoutId;
        var ailment=EnemyActionPlanner.Select(contentDatabase,loadout,2);
        if(ailment!=null)ApplyAilmentIdentity(ailment.kind);
    }

    void ApplyAilmentIdentity(EnemySkillKind kind)
    {
        StatTypes? stat=kind switch{EnemySkillKind.ApplyBleed=>StatTypes.BleedChance,EnemySkillKind.ApplyIgnite=>StatTypes.IgniteChance,EnemySkillKind.ApplyChill=>StatTypes.ChillChance,EnemySkillKind.ApplyShock=>StatTypes.ShockChance,EnemySkillKind.ApplyPoison=>StatTypes.PoisonChance,_=>null};
        if(stat.HasValue)stats.AddModifier(new StatModifier(stat.Value,StatOp.Additive,.2f,this));
    }

    public EnemySkillDefinition BeginAuthoredTurn()
    {
        string loadout=contentBoss?.skillLoadoutId??contentArchetype?.skillLoadoutId;
        activeBossPhaseDamageMultiplier=1f;
        if(contentBoss!=null&&contentDatabase!=null&&health!=null)
        {
            var profile=contentDatabase.BossPhase(contentBoss.phaseProfileId);
            if(profile?.phases!=null)foreach(var phase in profile.phases)
                if(health.MaxLife>0f&&health.CurrentLife/health.MaxLife<=phase.beginsAtLifeFraction&&!string.IsNullOrWhiteSpace(phase.skillLoadoutId)){loadout=phase.skillLoadoutId;activeBossPhaseDamageMultiplier=phase.damageMultiplier;}
        }
        int turn=authoredTurnCount++;
        int corruption=corruptionProfile?.percentage??0;
        int cadenceTurn=turn+(corruption>=40?turn/3:0)+(corruption==100?1:0);
        activeAuthoredSkill=EnemyActionPlanner.Select(contentDatabase,loadout,cadenceTurn);
        CurrentAuthoredHitCount=Mathf.Max(1,activeAuthoredSkill?.hitCount??1);
        if(corruption>=80&&(turn+1)%(corruption==100?3:4)==0)CurrentAuthoredHitCount++;
        if(activeAuthoredSkill?.kind==EnemySkillKind.Recover&&health!=null)
            health.RestoreLife(health.MaxLife*.025f*(corruptionProfile?.recoveryMultiplier??1f));
        return activeAuthoredSkill;
    }

#if UNITY_EDITOR
    // Balance-lab entry: isolated actor, same production setup/generation/optimizer, no scene level lookup.
    public void GenerateIsolatedBuild(int level, ModManager roller, EnemyRarity rarity)
    {
        stats ??= GetComponent<StatsComponent>();
        health ??= GetComponent<HealthComponent>();
        // EditMode prefab instances do not run HealthComponent.Awake; seed the isolated snapshot.
        if (health != null && health.CurrentLife <= 0f)
            health.RestoreCheckpointLife(health.PrefabMaxLife);
        enemyLevel = Mathf.Max(1, level);
        modManager = roller;
        CurrentRarity = rarity;
        GetComponent<EnemyStatSetup>()?.SetupForZone(enemyLevel, health != null && health.IsBoss);
        GenerateGearForEnemy(enemyLevel);
        health?.RestoreFullLife();
    }
#endif

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
        if (modManager == null && ModManager.Instance != null)
            modManager = ModManager.Instance;

        if (modManager == null)
            modManager = FindFirstObjectByType<ModManager>();

        if (modManager == null)
        {
            Debug.LogWarning("EnemyAI has no ModManager assigned; generating base enemy gear without rolled mods.", this); //If mod monater is null, give error and return
        }

        int itemCount = Mathf.Min(GetItemCountForLevel(zoneLevel), MaxTotalEnemyGearPieces);    //item count is given by getitemcountforlevel function, passing in the zone level
        int nonWeaponCount = Mathf.Max(0, itemCount - 1);     //always keep one slot for weapon

        List<LootManager.GearType> selectedSlots = RollDistinctNonWeaponSlots(nonWeaponCount);   //choose unique non-weapon slots for this enemy
        int candidateCount = EnemyBuildOptimizer.CandidateCountForLevel(zoneLevel);
        List<EnemyBuildOptimizer.CandidateSlot> candidateSlots =
            new List<EnemyBuildOptimizer.CandidateSlot>(selectedSlots.Count + 1);
        List<Gear> allCandidates = new List<Gear>((selectedSlots.Count + 1) * candidateCount);

        AddCandidateSlot(LootManager.GearType.Weapons, zoneLevel, candidateCount, candidateSlots, allCandidates);
        for (int i = 0; i < selectedSlots.Count; i++)
            AddCandidateSlot(selectedSlots[i], zoneLevel, candidateCount, candidateSlots, allCandidates);

        // Candidate generation is now complete. Search never asks ModManager for a
        // reroll and evaluates isolated stat snapshots rather than mutating this enemy.
        LastOptimizerBaseStats = EnemyBuildOptimizer.CaptureBaseStats(stats);
        EnemyBuildOptimizer.BuildResult winner = EnemyBuildOptimizer.SelectBestBuild(
            candidateSlots, LastOptimizerBaseStats, baseSpeed, IntrinsicDamageFactor);
        LastBuildEvaluation = winner != null ? winner.Evaluation : default;
        HashSet<Gear> selected = new HashSet<Gear>();
        if (winner != null)
        {
            stats?.BeginUpdate();
            try
            {
                for (int i = 0; i < winner.Items.Count; i++)
                {
                    Gear gear = winner.Items[i];
                    if (gear == null) continue;
                    selected.Add(gear);
                    equippedItems.Add(gear);
                    if (gear.ItemType == LootManager.GearType.Weapons)
                        EquipWeapon(gear);
                    else
                        ApplyGlobalModsFromGear(gear);
                }
            }
            finally
            {
                stats?.EndUpdate();
            }
        }

        for (int i = 0; i < allCandidates.Count; i++)
            if (!selected.Contains(allCandidates[i]))
                DiscardGearCandidate(allCandidates[i]);
    }

    private void AddCandidateSlot(LootManager.GearType slot, int level, int candidateCount,
        List<EnemyBuildOptimizer.CandidateSlot> slots, List<Gear> allCandidates)
    {
        List<Gear> candidates = new List<Gear>(candidateCount);
        for (int i = 0; i < candidateCount; i++)
        {
            Gear candidate = CreateItemForEnemy(slot, level);
            if (candidate == null) continue;
            candidates.Add(candidate);
            allCandidates.Add(candidate);
        }
        slots.Add(new EnemyBuildOptimizer.CandidateSlot(slot, candidates));
    }

    private int GetItemCountForLevel(int level)     //Calculates the range of items possible and rolls within that range, then returns the count to be used for item generation
    {
        if (level < 35) return Random.Range(1, 3);
        if (level < 50) return Random.Range(2, 5);
        if (level < 75) return Random.Range(4, 7);
        return 9;
    }

    private List<LootManager.GearType> RollDistinctNonWeaponSlots(int count)
    {
        count = Mathf.Clamp(count, 0, MaxNonWeaponSlots);
        if (count == 0)
            return new List<LootManager.GearType>();

        List<LootManager.GearType> availableSlots = new List<LootManager.GearType>(NonWeaponGearSlots);
        List<LootManager.GearType> selectedSlots = new List<LootManager.GearType>(count);

        for (int i = availableSlots.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            LootManager.GearType tmp = availableSlots[i];
            availableSlots[i] = availableSlots[swapIndex];
            availableSlots[swapIndex] = tmp;
        }

        for (int i = 0; i < count; i++)
            selectedSlots.Add(availableSlots[i]);

        return selectedSlots;
    }

    private static void DiscardGearCandidate(Gear candidate)
    {
        if (candidate == null)
            return;

        candidate.gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(candidate.gameObject);
        else DestroyImmediate(candidate.gameObject);
    }

    private Gear CreateItemForEnemy(LootManager.GearType type, int zoneLevel)       //Function used to create the item for the enemy to use
    {
        LootManager.GearRarity gearRarity = MapEnemyRarityToGearRarity(CurrentRarity);      //Set the gear rarity equal to the enemy rarity

        GameObject go = new GameObject($"Enemy_{type}_{gearRarity}");       //create a new game object
        go.transform.SetParent(transform);

        Gear gear = go.AddComponent<Gear>();            //create a gear object, which is go with the gear component added

        var element = RollItemElement(type == LootManager.GearType.Weapons);

        // Enemy gear is not player loot. Its authored tier access must grow much
        // more slowly because intrinsic damage already compounds by combat level.
        // Keeping the optimizer and enemy rarity/mod counts intact avoids a
        // second exponential weapon/affix multiplier at the 50/75 tier gates.
        int itemLevel = EffectiveEnemyGearItemLevel(zoneLevel);
        gear.Initialize(type, gearRarity, itemLevel, element);       //initailize gear, passing in the gear type, rarity, ilvl, and element

        int modCount = Gear.RollEnemyModNumber(gearRarity); //preserve the existing enemy intrinsic count profile
        var rolledMods = modManager != null
            ? modManager.RollModsForItem(type, gearRarity, itemLevel, modCount, element, forEnemy: true)
            : new List<RolledMod>();     //create variable for rolled mods, using the rollmodsforitem function from modmanager
        gear.ApplyMods(rolledMods);     //use gear.applymods with the rolled mods list to apply those mods to the gear item

        if (type == LootManager.GearType.Weapons)       //if the gear type is weapon
        {
            if (gear.BaseDamage <= 0f)      //if the base damage is less than 0
            {
                gear.BaseElement = Element.Phys;        //set its element to phys
                gear.BaseDamage = 2.5f + 1f * itemLevel;        //set its base damage to 2.5f + 1f * ilvl
                gear.BaseDamageMin = gear.BaseDamage * .8f;
                gear.BaseDamageMax = gear.BaseDamage * 1.2f;
                gear.BaseAttackSpeed = 1.0f + 0.01f * itemLevel;        //set its base attack speed to 1 + (.01 * ilvl)
                gear.BaseCritChance = 0.05f;        //set it's base crit chance to 5%
            }
        }

        return gear;    //return the gear item
    }

    public static int EffectiveEnemyGearItemLevel(int combatLevel) =>
        Mathf.Min(15,1+Mathf.Max(1,combatLevel)/10);

    private void EquipWeapon(Gear weapon)
    {
        if (equippedWeapon != null)     //if equipped weapon is not null
        {
            if (stats != null)
                stats.RemoveModifiersFromSource(equippedWeapon);        //remove the modifiers given from the currently equipped weapon
        }

        equippedWeapon = weapon;        //set the equippedweapon field to the new passed in weapon
        if (equippedWeapon == null || stats == null) return;     //if the equipped weapon is now null, return

        foreach (var mod in equippedWeapon.globalRolledMods)        //for each global mod on the equipped weapon, get the operation for the stat, get the modifier, and add it to the statscomponent
        {
            StatOp op = GetOperationForStat(mod.statType);
            var statMod = new StatModifier(mod.statType, op, mod.value, equippedWeapon);
            stats.AddModifier(statMod);
        }
    }

    private void ApplyGlobalModsFromGear(Gear gear)     //applies the global mods from a gear item
    {
        if (gear == null || stats == null)
            return;

        foreach (var mod in gear.globalRolledMods)      //for each mod in in the items global rolled mods list, grab the operation, grab the stat modifier, and add the modifier to the stats component
        {
            StatOp op = GetOperationForStat(mod.statType);
            var statMod = new StatModifier(mod.statType, op, mod.value, gear);
            stats.AddModifier(statMod);
        }
    }

    private StatOp GetOperationForStat(StatTypes stat)      //used to get the operation for a given stat
    {
        return StatMappings.GetRolledModifierOperation(stat);
    }

    public DamageContext BuildNonCriticalAttackContext(bool logStats = false)
        => BuildNonCriticalAttackContext(logStats, false);

    public DamageContext BuildNonCriticalAttackContextAtRangeEnd(bool maximum)
    {
        if (equippedWeapon == null) return BuildNonCriticalAttackContext();
        equippedWeapon.GetEffectiveBaseDamageRange(out float minimum, out float high);
        return BuildNonCriticalAttackContext(false, false, maximum ? high : minimum);
    }

    private DamageContext BuildNonCriticalAttackContext(bool logStats, bool rollWeapon, float? weaponOverride = null)
    {
        DamageContext ctx = new DamageContext(4);       //create a damage context with an initial capacity of 4

        if (equippedWeapon == null || stats == null) return ctx;     //if equippedweapon is null, return the context now

        Element weaponElement = equippedWeapon.BaseElement;     //store the weapon's base element in weaponElement
        float weaponBaseDamage = weaponOverride ?? (rollWeapon ? equippedWeapon.RollEffectiveBaseDamage()
            : equippedWeapon.GetEffectiveBaseDamage());

        AddScaledElementalDamage(ctx, weaponElement, weaponBaseDamage, logStats);     //call addscaledelemental damage to add the scaled ele damage (the base element damage scaled by local mods matching that element on the item)
        AddGlobalFlatElements(ctx, weaponElement);      //call addgloablflatelements to add any flat elemental damage that does not match the weapon's base element
        // Scale the completed pre-crit attack package once. Ailment magnitude derives from this source hit.
        float authored=activeAuthoredSkill?.damageMultiplier??1f;
        authored*=corruptionProfile?.damageMultiplier??1f;
        authored*=locationProfile?.enemyDamageMultiplier??1f;
        authored*=activeBossPhaseDamageMultiplier;
        EnemyScalingMath.ScaleOutgoing(ctx, IntrinsicDamageFactor
            * (health != null && health.IsBoss ? BossRoleDamageMultiplier : 1f) * authored);

        return ctx;
    }

    public DamageContext BuildAttackContext()       //builds the attack context, called when enemy attacks
    {
        DamageContext ctx = BuildNonCriticalAttackContext(logStats: true, rollWeapon: true);

        if (equippedWeapon == null || stats == null)
            return ctx;

        float critChance = GetFinalCritChance();        //call get final crit chance and store it in critchance
        float critMult = CombatCalculator.BaseCriticalMultiplier + stats.GetStat(StatTypes.CritMult);

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

    private void AddScaledElementalDamage(DamageContext ctx, Element element, float baseAmount, bool logStats)
    {
        float flatGlobal = stats.GetStat(StatMappings.GetFlatDamageStat(element))
            + DerivedStatCalculator.AddedFlatDamage(stats, element);      //get the flat global of the passed in element
        float incElement = stats.GetStat(StatMappings.GetIncDamageStat(element))
            + DerivedStatCalculator.ElementIncreasedDamage(stats, element);       //get the inc ele damage of the passed in element
        float incGeneric = stats.GetStat(StatTypes.GenericDmg)
            + DerivedStatCalculator.GlobalIncreasedDamage(stats);             //get the global inc damage
        float moreElement = stats.GetStat(StatMappings.GetMoreDamageStat(element));     //effective more fraction after compounding each matching roll
        float moreGeneric = stats.GetStat(StatTypes.GenericMult);
        if (logStats)
            Debug.Log($"Enemy dmg stats: base={baseAmount}, flatG={flatGlobal}, incElem={incElement}, incGen={incGeneric}, moreElem={moreElement}, moreGen={moreGeneric}");

        float amount = CombatCalculator.ScaleOutgoingDamage(
            baseAmount + flatGlobal, incGeneric, incElement, moreGeneric, moreElement);

        ctx.AddDamage(element, amount);     //add this damage to the damage context passing in the element of the damage and the amount
    }

    private void AddGlobalFlatElements(DamageContext ctx, Element weaponElement)        //add all of the extra flat elemental damage that isn't matching the base element
    {
        AddExtraElementIfNotBase(ctx, Element.Phys, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Fire, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Cold, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Light, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Void, weaponElement);
    }

    private void AddExtraElementIfNotBase(DamageContext ctx, Element element, Element weaponElement)
    {
        if (element == weaponElement) return;       //if the passed in element matches the weapon element, return

        float flatGlobal = stats.GetStat(StatMappings.GetFlatDamageStat(element))
            + DerivedStatCalculator.AddedFlatDamage(stats, element);      //get the flat damage stat, if it's 0, return
        if (flatGlobal <= 0f) return;

        float incElement = stats.GetStat(StatMappings.GetIncDamageStat(element))
            + DerivedStatCalculator.ElementIncreasedDamage(stats, element);       //get the increased damage stat for the current element, add it to global increased damage to calc inctotal
        float incGeneric = stats.GetStat(StatTypes.GenericDmg) + DerivedStatCalculator.GlobalIncreasedDamage(stats);
        float moreElement = stats.GetStat(StatMappings.GetMoreDamageStat(element));     //effective matching more fraction; multiply by the generic factor
        float moreGeneric = stats.GetStat(StatTypes.GenericMult);

        float amount = CombatCalculator.ScaleOutgoingDamage(
            flatGlobal, incGeneric, incElement, moreGeneric, moreElement);
        ctx.AddDamage(element, amount);     //add this element to damage context
    }

    public float GetFinalCritChance()
    {
        if (equippedWeapon == null) return 0f;      //if equipped weapon is null, return 0f for crit chance

        float weaponCrit = equippedWeapon.GetEffectiveBaseCrit(stats.GetStat(StatTypes.BaseCritChance));
        float incCritGlobal = stats.GetStat(StatTypes.CritChance);      //get the global increased crit stat on the enemy
        return Mathf.Clamp01(weaponCrit * (1f + incCritGlobal));
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
            incASGlobal = stats.GetStat(StatTypes.AttackSpeed) + DerivedStatCalculator.AttackSpeedIncreased(stats);     //if stats is not null, grab gloabl inc attack speed stat
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
            if (health != null)
                health.LoseLife(damage);        //call loselife from health component, passing in the damage amount

            if (damagePopup != null)        //if damage popup isn't null, spawn the poup with the transform and damage amount
            {
                damagePopup.Spawn(damage, transform, effect);
            }
        }
    }

    public Element RollItemElement(bool forWeapon = false)
    {
        if (!forWeapon) return (Element)Random.Range(0, (int)Element.Count);
        int roll = Random.Range(0, 5);
        return roll < (int)Element.Poison ? (Element)roll : Element.Void;
    }
}
