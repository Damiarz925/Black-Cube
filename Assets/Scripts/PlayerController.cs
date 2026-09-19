// Developer map: Builds deterministic noncritical previews and randomized actual attacks from equipment/stats. EquipWeapon publishes AttackChanged for UI/art; EquipmentManager applies the global item modifiers.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

// Builds attack snapshots; BattleManager drives PaperSpriteActor separately.
// GetStat returns fractions for percent buckets; stored global rolls use points.
public class PlayerController : MonoBehaviour
{
    [SerializeField] Animator animator; //Field for the player's animator component
    [SerializeField] float baseDamage;  //Field for the player's base damage (>= 0)
    [SerializeField] float baseHealth;  //Field for the player's base health (>= 0)
    [SerializeField] public Transform worldPosition;    //Field to store the player's transform in the world (for hit effects/damage numbers at player pos)
    [SerializeField] private Gear equippedWeapon;       //Field for storing the player's equipped weapon

    public DamagePopup damagePopup; // Legacy scene reference; DamageReceiver owns the active popup path.

    private StatsComponent stats;   //Field for the player's statsComponent
    private HealthComponent health; //Field for the player's health component
    private DamageReceiver damageReceiver;
    private bool ignoreRelicsForIsolatedBaseline;
    public float EquippedWeaponBaseDamage => equippedWeapon != null ? equippedWeapon.GetEffectiveBaseDamage() : 0f;
    public Element EquippedWeaponElement => equippedWeapon != null ? equippedWeapon.BaseElement : Element.Phys;
    public event System.Action AttackChanged;
    public Gear EquippedWeapon => equippedWeapon;
    public float BasicAttackDamage
    {
        get
        {
            float total = 0f;
            foreach (var hit in BuildNonCriticalAttackContext().Hits) total += hit.Amount;
            return total;
        }
    }

    public float baseSpeed = 1f;    // Unarmed attacks/second, used only when UnarmedDamage is positive.

    private void Awake()    //Grabbing the player's stats component and healthcomponent on awake
    {
        stats = GetComponent<StatsComponent>();
        health = GetComponent<HealthComponent>();
        damageReceiver = GetComponent<DamageReceiver>();
    }

    void Start()    //Grabbing the damagepopup object in scene on start, then if the player has no equipped weapon, create the starter weapon and equip it
    {
        damagePopup = FindFirstObjectByType<DamagePopup>();

        // GameManager owns New Game/Load ordering. Isolated player fixtures still
        // receive a weapon without needing the complete persistent runtime.
        if (equippedWeapon == null && GameManager.Instance == null) EnsureStarterWeapon();
        NotifyRelicChanged();
    }

    private Gear CreateStarterWeapon() => CreateStarterWeapon(ModManager.Instance);

#if UNITY_EDITOR
    public Gear CreateStarterWeaponForIsolatedBaseline(ModManager roller)
    {
        ignoreRelicsForIsolatedBaseline = true;
        return CreateStarterWeapon(roller);
    }
#endif

    private Gear CreateStarterWeapon(ModManager roller)  //Creates starter weapon
    {
        PlayerIdentityState identity=GameManager.Instance!=null?GameManager.Instance.GetComponent<PlayerIdentityState>():null;
        string weaponTypeId=identity?.ClassDefinition?.SignatureWeaponTypeId??WeaponTypeCatalog.HistoricalDefaultId;
        WeaponTypeDefinition weaponProfile=WeaponTypeCatalog.Get(weaponTypeId);
        var relics=ignoreRelicsForIsolatedBaseline?null:RelicInventory.Instance;
        int itemLevel=Mathf.Clamp(1+(relics?.StarterItemLevelBonus??0),1,100);
        Element element=relics?.StarterElement??Element.Phys;
        float baseMultiplier=1f+(relics?.StarterBaseDamagePercent??0f)/100f;
        float legendaryChance=relics?.StarterLegendaryChance??0f;
        bool legendary=false;
        var oldRandom=Random.state;
        int seed=GamePersistence.CurrentRunSeed;
        if(seed==0)seed=0x14BC2026;
        seed=unchecked(seed*397^(relics?.CurrentCycle??0));
        Random.InitState(seed);
        try{legendary=legendaryChance>0f&&Random.value*100f<legendaryChance;}
        finally{Random.state=oldRandom;}
        var rarity=legendary?LootManager.GearRarity.Legendary:LootManager.GearRarity.Normal;
        GameObject go = new GameObject("Player_StarterWeapon"); //Creates the object as go, and names it
        go.transform.SetParent(transform);  //Sets the parent of the object's transform
        Gear gear = go.AddComponent<Gear>();    //Adds a gear component to the newly created starter weapon, and assigns that gear component to the variable gear

        gear.Initialize(LootManager.GearType.Weapons, rarity, itemLevel, element, weaponTypeId);
        // 22.5 * .45 with the minimum +7% implicit is about 7.4% below the
        // weakest reasonable level-one natural base (26 * .45) before both
        // weapons' common 5% critical contribution.
        gear.BaseDamageMin = weaponProfile.BaseDamageMin*baseMultiplier;
        gear.BaseDamageMax = weaponProfile.BaseDamageMax*baseMultiplier;
        gear.BaseDamage = (gear.BaseDamageMin+gear.BaseDamageMax)*.5f;
        gear.BaseAttackSpeed = weaponProfile.AttacksPerSecond;
        gear.BaseCritChance = weaponProfile.BaseCritChance;
        // Starter base damage/speed/crit are authored above, so keep those
        // values rather than applying the three random weapon-base rolls.
        // Its one permanent implicit still comes from the same legal natural
        // equipment pool and tier gates used by dropped Normal weapons.
        var generated=new System.Collections.Generic.List<RolledMod>();
        if(legendary&&roller!=null)
        {
            oldRandom=Random.state;Random.InitState(unchecked(seed^0x5A17E21));
            try
            {
                for(int attempt=0;attempt<32&&generated.Count!=7;attempt++)
                {
                    generated.Clear();var natural=roller.RollEquipmentModsForItem(LootManager.GearType.Weapons,rarity,itemLevel,element,weaponTypeId);
                    if(natural!=null)foreach(var mod in natural)if(!Gear.IsWeaponBaseStat(mod.statType))generated.Add(mod);
                }
            }
            finally{Random.state=oldRandom;}
        }
        if(legendary&&generated.Count!=7){Debug.LogError("Starter Legendary generation could not produce one implicit and six legal explicits.");generated.Clear();gear.SetRarity(LootManager.GearRarity.Normal);gear.RestoreCraftingState(LootManager.GearRarity.Normal,CraftingPotentialProfile.Maximum(LootManager.GearRarity.Normal),CraftingPotentialProfile.Maximum(LootManager.GearRarity.Normal));}
        if(generated.Count==0)
        {
            var definition=roller?.Database?.GetDefinition(StatTypes.GenericDmg);
            var tiers=ModManager.ApplicableTiers(definition,LootManager.GearType.Weapons);
            AffixTier weakest=null;foreach(var tier in tiers)if(tier.minItemLevel<=itemLevel&&(weakest==null||tier.tierIndex>weakest.tierIndex))weakest=tier;
            generated.Add(weakest!=null?new RolledMod(StatTypes.GenericDmg,weakest.tierIndex,weakest.minValue,true):new RolledMod(StatTypes.GenericDmg,5,7f,true));
        }
        gear.ApplyMods(generated);

        return gear;    //Return the gear object
    }

    public Gear EnsureStarterWeapon()
    {
        Gear existing=EquipmentManager.Instance?.GetEquipped(LootManager.GearType.Weapons);
        if(existing!=null){if(equippedWeapon!=existing)EquipWeapon(existing);return existing;}
        if(equippedWeapon!=null)return equippedWeapon;
        Gear starter = CreateStarterWeapon();
        if (EquipmentManager.Instance != null) EquipmentManager.Instance.Equip(starter);
        else EquipWeapon(starter);
        return starter;
    }

    public void ResetToStarterWeapon()=>EnsureStarterWeapon();

    public void TakeDamage(float damage, StatusEffects effect = null)  // Already-mitigated damage; effect selects the popup style.
    {
        if (damageReceiver == null)
            damageReceiver = GetComponent<DamageReceiver>();

        if (damageReceiver != null)
        {
            damageReceiver.TakeDamage(damage, Element.Phys, effect);
        }
        else if (health != null) //If healthcomponent exists
        {
            health.LoseLife(damage); //call lose life from healthcomponent and pass in the damage taken
        }
    }

    // --------------------------------------------------------------------
    // ATTACK BUILD – this is the main damage pipeline
    // --------------------------------------------------------------------
    // Shared outgoing hit pipeline. UI inspection does not consume a critical roll.
    public DamageContext BuildNonCriticalAttackContext()
        => BuildNonCriticalAttackContext(false, null);

    public DamageContext BuildNonCriticalAttackContextAtRangeEnd(bool maximum)
    {
        if (equippedWeapon == null) return BuildNonCriticalAttackContext();
        equippedWeapon.GetEffectiveBaseDamageRange(out float minimum, out float high);
        return BuildNonCriticalAttackContext(false, maximum ? high : minimum);
    }

    private DamageContext BuildNonCriticalAttackContext(bool rollWeapon, float? weaponOverride)
    {
        DamageContext ctx = new DamageContext(4);   //Builds a damage context, passing in 4 as the initial capacity
        if (equippedWeapon == null)
        {
            float unarmed = Mathf.Max(0f, stats.GetStat(StatTypes.UnarmedDamage));
            if (unarmed > 0f) AddScaledElementalDamage(ctx, Element.Phys, unarmed);
            return ApplyKeystones(ctx);
        }

        Element weaponElement = equippedWeapon.BaseElement; //assign the weapon's base element to weaponElement variable
        float weaponBaseDamage = weaponOverride ?? (rollWeapon ? equippedWeapon.RollEffectiveBaseDamage()
            : equippedWeapon.GetEffectiveBaseDamage());

        //Pass in context, weapon element, and weapon base. Calculate the scaled final damage of the weapon's base element.
        AddScaledElementalDamage(ctx, weaponElement, weaponBaseDamage);

        //Add all of the global flat damage modifiers that aren't the same element as the weapon's base element
        AddGlobalFlatElements(ctx, weaponElement);

        return ApplyKeystones(ctx);
    }

    public DamageContext BuildAttackContext()
    {
        var ctx = BuildNonCriticalAttackContext(true, null);
        return ApplyCriticalRoll(ctx);
    }

    public DamageContext BuildAttackContext(Element conversionElement, float nonMatchingConversion)
    {
        var ctx = ApplyKeystones(BuildNonCriticalConvertedRaw(conversionElement, nonMatchingConversion, true));
        return ApplyCriticalRoll(ctx);
    }

    public DamageContext BuildAttackContext(Element conversionElement, float nonMatchingConversion, DamageScope scopes)
    {
        var ctx = BuildNonCriticalConvertedRaw(conversionElement, nonMatchingConversion, true);
        ctx.Scopes = scopes;
        return ApplyCriticalRoll(ApplyKeystones(ctx));
    }

    public DamageContext BuildNonCriticalConvertedAttackContext(Element conversionElement, float nonMatchingConversion)
        => ApplyKeystones(BuildNonCriticalConvertedRaw(conversionElement, nonMatchingConversion, false));

    DamageContext BuildNonCriticalConvertedRaw(Element conversionElement, float nonMatchingConversion, bool rollWeapon)
    {
        var ctx = new DamageContext(5);
        float conversion = Mathf.Clamp01(nonMatchingConversion);
        if (equippedWeapon == null)
        {
            AddConvertedRawDamage(ctx, Element.Phys, Mathf.Max(0f, stats.GetStat(StatTypes.UnarmedDamage)),
                conversionElement, conversion);
            return ctx;
        }

        Element weaponElement = equippedWeapon.BaseElement;
        float weaponRaw = (rollWeapon ? equippedWeapon.RollEffectiveBaseDamage() : equippedWeapon.GetEffectiveBaseDamage())
                          + stats.GetStat(StatMappings.GetFlatDamageStat(weaponElement))
                          + DerivedStatCalculator.AddedFlatDamage(stats, weaponElement);
        AddConvertedRawDamage(ctx, weaponElement, weaponRaw, conversionElement, conversion);
        AddConvertedExtraFlat(ctx, Element.Phys, weaponElement, conversionElement, conversion);
        AddConvertedExtraFlat(ctx, Element.Fire, weaponElement, conversionElement, conversion);
        AddConvertedExtraFlat(ctx, Element.Cold, weaponElement, conversionElement, conversion);
        AddConvertedExtraFlat(ctx, Element.Light, weaponElement, conversionElement, conversion);
        AddConvertedExtraFlat(ctx, Element.Void, weaponElement, conversionElement, conversion);
        return ctx;
    }

    DamageContext ApplyKeystones(DamageContext context)
    {
        var keystones = GetComponent<PassiveKeystoneState>();
        return keystones != null ? keystones.TransformOutgoing(context) : context;
    }

    private DamageContext ApplyCriticalRoll(DamageContext ctx)
    {
        if (equippedWeapon == null) return ctx;

        //Get the weapons final critical chance
        float critChance = GetFinalCritChance();
        //Directly calculate the weapon's crit multiplier (assumes Crit multi is a decimal value).
        float critMult = CombatCalculator.BaseCriticalMultiplier + stats.GetStat(StatTypes.CritMult);

        //Roll randomly to decide if the attack is a critical strike or not
        bool isCrit = Random.value < Mathf.Clamp01(critChance);
        //Assign iscrit in the context
        ctx.IsCrit = isCrit;
        //Assign the crit multi to the context if the attack is a critical strike
        ctx.CritMultiplier = isCrit ? critMult : 1f;
        //If the attack is a critical strike
        if (isCrit)
        {
            for (int i = 0; i < ctx.Hits.Count; i++) //For every hit in the context
            {
                var h = ctx.Hits[i];    //var h is the current hit
                h.Amount *= critMult;   //multiply the current hit's damage amount by the crit multiplier
                ctx.Hits[i] = h;        //Update the current hit with the new, modified value
            }
        }

        return ctx; //Return context
    }

    private void AddScaledElementalDamage(DamageContext ctx, Element element, float baseAmount)
    {
        //Grabs all of the flat damage increases for your main stat
        float flatGlobal = stats.GetStat(StatMappings.GetFlatDamageStat(element))
            + DerivedStatCalculator.AddedFlatDamage(stats, element);
        AddScaledRawDamage(ctx, element, baseAmount + flatGlobal);
    }

    private void AddScaledRawDamage(DamageContext ctx, Element element, float rawAmount)
    {
        if (rawAmount <= 0f) return;

        //Grabs all of the increased damage increases for your main stat
        float incElement = stats.GetStat(StatMappings.GetIncDamageStat(element))
            + DerivedStatCalculator.ElementIncreasedDamage(stats, element);  // e.g. 0.40 for +40% phys
        float incGeneric = stats.GetStat(StatTypes.GenericDmg)
            + DerivedStatCalculator.GlobalIncreasedDamage(stats, GetComponent<ManaComponent>()); // snapshots current mana while the hit is built
        float incTotal = incElement + incGeneric;                                // e.g. 1.20 → +120% increased

        //Grabs all of the more damage increases for your main stat
        float moreElement = stats.GetStat(StatMappings.GetMoreDamageStat(element)); // e.g. 0.30 for +30% more phys
        float moreGeneric = stats.GetStat(StatTypes.GenericMult);                   // e.g. 0.50 for +50% more damage
        float moreTotal = (1f + moreElement) * (1f + moreGeneric); // Each stat already compounds its individual rolls; multiply applicable stat factors.

        // Pipeline: base + flat → increased → more
        float afterInc = rawAmount * (1f + incTotal);
        float relicMore = !ignoreRelicsForIsolatedBaseline && RelicInventory.Instance != null
            ? RelicInventory.Instance.DamageMultiplier : 1f;
        float final = afterInc * moreTotal * relicMore;

        ctx.AddDamage(element, final);  //Adds the damage to the context
    }

    //This adds any flat damage modifiers that don't match the weapon's base element
    private void AddGlobalFlatElements(DamageContext ctx, Element weaponElement)
    {
        AddExtraElementIfNotBase(ctx, Element.Phys, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Fire, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Cold, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Light, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Void, weaponElement);
    }

    //Adds the extra element that doesn't match the weapon's base damage
    private void AddExtraElementIfNotBase(DamageContext ctx, Element element, Element weaponElement)
    {
        if (element == weaponElement) return;   //If the element does match the weapon's base, return

        float flatGlobal = stats.GetStat(StatMappings.GetFlatDamageStat(element))
            + DerivedStatCalculator.AddedFlatDamage(stats, element);  //Adds the flat damage values
        if (flatGlobal <= 0f) return;

        AddScaledRawDamage(ctx, element, flatGlobal);
    }

    private void AddConvertedExtraFlat(DamageContext ctx, Element element, Element weaponElement,
        Element conversionElement, float conversion)
    {
        if (element == weaponElement) return;
        AddConvertedRawDamage(ctx, element, stats.GetStat(StatMappings.GetFlatDamageStat(element))
            + DerivedStatCalculator.AddedFlatDamage(stats, element),
            conversionElement, conversion);
    }

    private void AddConvertedRawDamage(DamageContext ctx, Element sourceElement, float rawAmount,
        Element conversionElement, float conversion)
    {
        if (rawAmount <= 0f) return;
        if (conversion <= 0f || sourceElement == conversionElement)
        {
            AddScaledRawDamage(ctx, sourceElement, rawAmount);
            return;
        }
        AddScaledRawDamage(ctx, sourceElement, rawAmount * (1f - conversion));
        AddScaledRawDamage(ctx, conversionElement, rawAmount * conversion);
    }

    //Calculates the final crit chance
    public float GetFinalCritChance()
    {
        if (equippedWeapon == null) return 0f;  //If equipped weapon is null return

        float weaponCrit = equippedWeapon.GetEffectiveBaseCrit(stats.GetStat(StatTypes.BaseCritChance));
        float incCritGlobal = stats.GetStat(StatTypes.CritChance);        // 0.5 for +50% increased crit
        // ALL flat base points precede local and global increased buckets.
        float final = weaponCrit * (1f + incCritGlobal);

        return Mathf.Clamp01(final);
    }

    //Calculates the final attack speed
    public float GetFinalAttackSpeed()
    {
        if (equippedWeapon == null)
            return stats.GetStat(StatTypes.UnarmedDamage) > 0f
                ? baseSpeed * (1f + stats.GetStat(StatTypes.AttackSpeed) + DerivedStatCalculator.AttackSpeedIncreased(stats)) * KeystoneAttackSpeedMultiplier() * RelicAttackSpeedMultiplier() : 0f;

        float weaponAS = equippedWeapon.GetEffectiveAttackSpeed();  //Grabs the weapon's base attack speed (base speed * local weapon attack speed modifier)
        float incASGlobal = stats.GetStat(StatTypes.AttackSpeed) + DerivedStatCalculator.AttackSpeedIncreased(stats); //Gets the player's global attack speed modifier

        var subclass=GetComponent<SubclassCombatState>();if(subclass?.Has(SubclassIds.PriestLight)==true)incASGlobal+=subclass.AuraSecondary(0,.10f);
        return weaponAS * (1f + incASGlobal) * KeystoneAttackSpeedMultiplier() * RelicAttackSpeedMultiplier();
    }

    float KeystoneAttackSpeedMultiplier()
    {
        var keystones = GetComponent<PassiveKeystoneState>();
        return keystones != null ? keystones.AttackSpeedMultiplier : 1f;
    }

    float RelicAttackSpeedMultiplier() => !ignoreRelicsForIsolatedBaseline && RelicInventory.Instance != null
        ? RelicInventory.Instance.AttackSpeedMultiplier : 1f;

    // Called by EquipmentManager when a weapon is equipped.
    public void EquipWeapon(Gear weapon)
    {
        equippedWeapon = weapon;
        AttackChanged?.Invoke();
    }
    public void NotifyRelicChanged()
    {
        if(!ignoreRelicsForIsolatedBaseline)RelicInventory.Instance?.ApplyActiveStatModifiers(stats);
        AttackChanged?.Invoke();
    }
}
