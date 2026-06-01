using UnityEngine;

//Builds attack snapshots and routes incoming damage/status ticks; does not handle animations yet.
//All increased/more stats should be stored as decimals
public class PlayerController : MonoBehaviour
{
    [SerializeField] Animator animator; //Field for the player's animator component
    [SerializeField] float baseDamage;  //Field for the player's base damage (>= 0)
    [SerializeField] float baseHealth;  //Field for the player's base health (>= 0)
    [SerializeField] public Transform worldPosition;    //Field to store the player's transform in the world (for hit effects/damage numbers at player pos)
    [SerializeField] private Gear equippedWeapon;       //Field for storing the player's equipped weapon

    public DamagePopup damagePopup; //Field for storing the DamagePopup object to be used for damage number display (not implemented currently)

    private StatsComponent stats;   //Field for the player's statsComponent
    private HealthComponent health; //Field for the player's health component

    public float baseSpeed = 1f;    //Field for the player's base speed (currently set at 2f for testing. Likely 1f in the future)

    private void Awake()    //Grabbing the player's stats component and healthcomponent on awake
    {
        stats = GetComponent<StatsComponent>();
        health = GetComponent<HealthComponent>();
    }

    void Start()    //Grabbing the damagepopup object in scene on start, then if the player has no equipped weapon, create the starter weapon and equip it
    {
        damagePopup = FindFirstObjectByType<DamagePopup>();

        if (equippedWeapon == null)
        {
            Gear starter = CreateStarterWeapon();
            EquipWeapon(starter);
        }
    }

    private Gear CreateStarterWeapon()  //Creates starter weapon
    {
        GameObject go = new GameObject("Player_StarterWeapon"); //Creates the object as go, and names it
        go.transform.SetParent(transform);  //Sets the parent of the object's transform
        Gear gear = go.AddComponent<Gear>();    //Adds a gear component to the newly created starter weapon, and assigns that gear component to the variable gear

        gear.BaseElement = Element.Phys;    //Sets the weapon's base element to phys
        gear.BaseDamage = 80f;  //Sets base damage to 20
        gear.BaseAttackSpeed = 1.2f;    //sets base attack speed to 1
        gear.BaseCritChance = 0.05f;    //sets base crit chance to 5%

        return gear;    //Return the gear object
    }

    public void TakeDamage(float damage, StatusEffects effect)  //Called when the player takes damage (effect currently unused, implement later)
    {
        if (health != null) //If healthcomponent exists
        {
            health.LoseLife(damage); //call lose life from healthcomponent and pass in the damage taken
        }
    }

    //This is called whenever a status effect ticks on the player
    public void OnStatusTick(float strength, StatusEffects effect)
    {
        if (effect == null) return; //If the effect is null, return

        switch (effect._StatusType)
        {
            case StatusEffects.StatusType.DamageOverTime:   //If the effect is damage over time, and the strength is greater than 0, call take damage passing strength and the effect
                if (strength > 0f)
                    TakeDamage(strength, effect);
                break;

            case StatusEffects.StatusType.Shock:    //If the effect is shock, call apply shock, passing in strength and effect
                ApplyShock(strength, effect);
                break;

            case StatusEffects.StatusType.Chill:    //If the effect is chill, call apply chill, passing in strength and effect
                ApplyChill(strength, effect);
                break;
        }
    }

    void ApplyShock(float strength, StatusEffects effect) { }   //Not implemented yet, shock will increase damage taken by strength * base shock effect (Defined in shock SO).
    void ApplyChill(float strength, StatusEffects effect) { }   //Not implemented yet, chill will increase damage taken by strength * base chill effect (Defined in chill SO). 

    // --------------------------------------------------------------------
    // ATTACK BUILD – this is the main damage pipeline
    // --------------------------------------------------------------------
    public DamageContext BuildAttackContext()
    {
        DamageContext ctx = new DamageContext(4);   //Builds a damage context, passing in 4 as the initial capacity
        if (equippedWeapon == null) return ctx; //If no equipped weapon, return just the context

        Element weaponElement = equippedWeapon.BaseElement; //assign the weapon's base element to weaponElement variable
        float weaponBaseDamage = equippedWeapon.GetEffectiveBaseDamage();   //get the weapon's effective base damage, then assign that to the weaponBaseDamage variable

        //Pass in context, weapon element, and weapon base. Calculate the scaled final damage of the weapon's base element.
        AddScaledElementalDamage(ctx, weaponElement, weaponBaseDamage);

        //Add all of the global flat damage modifiers that aren't the same element as the weapon's base element
        AddGlobalFlatElements(ctx, weaponElement);

        //Get the weapons final critical chance
        float critChance = GetFinalCritChance();
        //Directly calculate the weapon's crit multiplier (assumes Crit multi is a decimal value).
        float critMult = 1f + stats.GetStat(StatTypes.CritMult); // CritMult is % more crit damage

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
        float flatGlobal = stats.GetStat(StatMappings.GetFlatDamageStat(element));

        //Grabs all of the increased damage increases for your main stat
        float incElement = stats.GetStat(StatMappings.GetIncDamageStat(element));  // e.g. 0.40 for +40% phys
        float incGeneric = stats.GetStat(StatTypes.GenericDmg);                    // e.g. 0.80 for +80% generic
        float incTotal = incElement + incGeneric;                                // e.g. 1.20 → +120% increased

        //Grabs all of the more damage increases for your main stat
        float moreElement = stats.GetStat(StatMappings.GetMoreDamageStat(element)); // e.g. 0.30 for +30% more phys
        float moreGeneric = stats.GetStat(StatTypes.GenericMult);                   // e.g. 0.50 for +50% more damage
        float moreTotal = (1f + moreElement) * (1f + moreGeneric);                              // e.g. 0.80 → +80% more

        // Pipeline: base + flat → increased → more
        float afterInc = (baseAmount + flatGlobal) * (1f + incTotal);
        float final = afterInc * moreTotal;

        ctx.AddDamage(element, final);  //Adds the damage to the context
    }

    //This adds any flat damage modifiers that don't match the weapon's base element
    private void AddGlobalFlatElements(DamageContext ctx, Element weaponElement)
    {
        AddExtraElementIfNotBase(ctx, Element.Phys, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Fire, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Cold, weaponElement);
        AddExtraElementIfNotBase(ctx, Element.Light, weaponElement);
    }

    //Adds the extra element that doesn't match the weapon's base damage
    private void AddExtraElementIfNotBase(DamageContext ctx, Element element, Element weaponElement)
    {
        if (element == weaponElement) return;   //If the element does match the weapon's base, return

        float flatGlobal = stats.GetStat(StatMappings.GetFlatDamageStat(element));  //Adds the flat damage values
        if (flatGlobal <= 0f) return;

        // INC bucket
        float incElement = stats.GetStat(StatMappings.GetIncDamageStat(element));   //Adds the increased elemental damage values
        float incGeneric = stats.GetStat(StatTypes.GenericDmg); //Adds the generic increased damage
        float incTotal = incElement + incGeneric;   //Adds them together for a total increased damage amount

        // MORE bucket
        float moreElement = stats.GetStat(StatMappings.GetMoreDamageStat(element)); //Adds the more elemental damage values
        float moreGeneric = stats.GetStat(StatTypes.GenericMult);   //Adds the more generic damage values
        float moreTotal = (1f + moreElement) * (1f + moreGeneric);    //Adds them together for the total more damage value

        float afterInc = flatGlobal * (1f + incTotal);  //Multiplies increased values with float global
        float final = afterInc * moreTotal;  //Multiplies more damage values with the previously calculated value

        ctx.AddDamage(element, final);  //Adds this to the damage context
    }

    //Calculates the final crit chance
    float GetFinalCritChance()
    {
        if (equippedWeapon == null) return 0f;  //If equipped weapon is null return

        float weaponCrit = equippedWeapon.GetEffectiveBaseCrit();       //This grabs the weapons base effective crit (Base + Extra base crit on weapon * local increased crit chance on weapon)
        float incCritGlobal = stats.GetStat(StatTypes.CritChance);        // 0.5 for +50% increased crit
        float extraBaseCritPP = stats.GetStat(StatTypes.BaseCritChance);    // 0.02 for +2% base

        float baseCritAll = weaponCrit + extraBaseCritPP;   //Add weaponCrit and any sources of extraBaseCrit
        float final = baseCritAll * (1f + incCritGlobal);   //Multiply that by any sources of global increased crit chance

        return Mathf.Clamp01(final);
    }

    //Calculates the final attack speed
    public float GetFinalAttackSpeed()
    {
        if (equippedWeapon == null) return 0f;  //If no equipped weapon, return

        float weaponAS = equippedWeapon.GetEffectiveAttackSpeed();  //Grabs the weapon's base attack speed (base speed * local weapon attack speed modifier)
        float incASGlobal = stats.GetStat(StatTypes.AttackSpeed); //Gets the player's global attack speed modifier

        return weaponAS * (1f + incASGlobal);   //Returns the weapon's calculated attack speed * (1 + increased global attack speed). This attack speed modifiers are decimal values.
    }

    // Called by EquipmentManager when a weapon is equipped.
    public void EquipWeapon(Gear weapon)
    {
        equippedWeapon = weapon;
    }
}
