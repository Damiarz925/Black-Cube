using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//StatusController is used to manage all of the statuses on the GameObject that the controller is on. Contains code for managing stacks and applying Status Damage/Effects
public class StatusController : MonoBehaviour
{
    private PlayerController playerCont;
    private EnemyAI enemyCont;
    private StatsComponent stats;

    readonly Dictionary<StatusEffects, StatusInstance> StatusDictionary = new();    //Dictionary used for holding status instances linked the the effect
    readonly Dictionary<StatusEffects, List<StatusInstance>> IndependentDictionary = new();     //Dictionary used for holding a list of status instances linked to the effect (used for effects that have multiple independent stacks of the same type)

    private struct PendingEffect    //Pending effect struct stores what the effect is and its strength
    {
        public float Strength;
        public StatusEffects Effect;

        public PendingEffect(float strength, StatusEffects effect)
        {
            Strength = strength;
            Effect = effect;
        }
    }

    //Grab the GameObject's StatsComponent, and grab either the playerController or EnemyAI depending on what this script is attached to.
    private void Awake()
    {
        stats = GetComponent<StatsComponent>();
    }

    private void Start()
    {
        if (gameObject.CompareTag("Player"))
            playerCont = GetComponent<PlayerController>();

        if (gameObject.CompareTag("Enemy"))
            enemyCont = GetComponent<EnemyAI>();
    }

    // --------------------------------------------------------------------
    // APPLY FROM HIT
    // --------------------------------------------------------------------
    public void ApplyAilmentFromHit(StatusEffects effect, DamageContext context, StatsComponent attackerStats, int stacksPerHit = 1)
    {
        if (effect == null || attackerStats == null || context.Hits == null || context.Hits.Count == 0)     //If effect, attacker's stats, or context's hits is null, return
            return;

        // Call Compute ailment to grab the damage of the ailment.
        AilmentCalculator.ComputeAilmentFromHit(
            effect,
            context,
            attackerStats,
            out float damagePerTick,
            out int tickCount,
            out int effectiveInterval);

        if (damagePerTick <= 0f || tickCount <= 0)  //If damagepertick or tick count is less than or equal to 0, return
            return;

        ApplyStatus(effect, stacksPerHit, damagePerTick, tickCount, attackerStats, effectiveInterval);      //Call apply status
    }

    private void ApplyStatus(StatusEffects effect, int stacksPerHit, float damagePerTick, int tickCount, StatsComponent sourceStats, int effectiveInterval)
    {
        switch (effect._StackPolicy)    //Decide which stack policy to use based on the ailments defined stack policy from its SO
        {
            case StatusEffects.StackPolicy.StackAndRefresh:
                StackAndRefresh(effect, stacksPerHit, damagePerTick, tickCount, sourceStats, effectiveInterval);
                break;

            case StatusEffects.StackPolicy.StackIndependently:
                StackIndependently(effect, stacksPerHit, damagePerTick, tickCount, sourceStats, effectiveInterval);
                break;

            case StatusEffects.StackPolicy.ReplaceIfStronger:
                ReplaceIfStronger(effect, stacksPerHit, damagePerTick, tickCount, sourceStats, effectiveInterval);
                break;

            case StatusEffects.StackPolicy.ReplaceAlways:
                ReplaceAlways(effect, stacksPerHit, damagePerTick, tickCount, sourceStats, effectiveInterval);
                break;

            default:
                Debug.LogWarning("Invalid Stack Policy given to ApplyStatus", this);    //If stack policy is invalid, give debug log and break
                break;
        }
    }

    private void StackAndRefresh(StatusEffects effect, int stacksPerHit, float damagePerTick, int tickCount, StatsComponent sourceStats, int effectiveInterval)
    {
        int currentStacks = 0;
        if (StatusDictionary.TryGetValue(effect, out var existing))     //Search the dictionary for the effect, if it exists set current stacks to the existing stack count
            currentStacks = existing.stacks;

        int newStacks = currentStacks + stacksPerHit;       //Create variable newStacks and set that to the current stacks + the stacksPerHit(The num of stacks that is applied by the hit applying the ailment)
        if (effect.MaxStacks > 0)   
            newStacks = Mathf.Min(newStacks, effect.MaxStacks);     //If the maxstacks is greater than 0, grab the minimum between newStacks and max stacks (so as to not exceed the maximum stacks -- Clamping)

        var instance = new StatusInstance(effect, damagePerTick, newStacks, tickCount, sourceStats, effectiveInterval);     //Create a new status instance with the updated stacks
        StatusDictionary[effect] = instance;    //Replace the existing instance in the dictionary with the new one
    }

    private void StackIndependently(StatusEffects effect, int stacksPerHit, float damagePerTick, int tickCount, StatsComponent sourceStats, int effectiveInterval)
    {
        if (!IndependentDictionary.TryGetValue(effect, out var list))   //Search the dictionary for the effect, if it's not in the list, out list
        {
            list = new List<StatusInstance>();  //Create new list of status instances
            IndependentDictionary[effect] = list;   //Add the list to the effect
        }

        var instance = new StatusInstance(effect, damagePerTick, stacksPerHit, tickCount, sourceStats, effectiveInterval);  //Create the instance using the passed in values
        list.Add(instance);     //Add our new instance to the list
    }

    // Used especially for Ignite: keep the instance with highest total damage over its lifetime.
    private void ReplaceIfStronger(StatusEffects effect, int stacksPerHit, float damagePerTick, int tickCount, StatsComponent sourceStats, int effectiveInterval)
    {
        float newPower = damagePerTick * stacksPerHit * tickCount;      //Calculate the new ailment's power as damagePerTick * stacksPerHit * tickCount

        if (StatusDictionary.TryGetValue(effect, out var existing))     //Search the dictionary for the effect, if it exists, out existing
        {
            float existingPower = existing.TotalPlannedDamage();        //new float existingPower set to the existing effect's total planned damage (over the entirety of its duration)
            if (newPower <= existingPower)      //If new power is less than existing power, return doing nothing
            {
                // Keep the stronger ignite (or other DOT).
                return;
            }
        }

        var instance = new StatusInstance(effect, damagePerTick, stacksPerHit, tickCount, sourceStats, effectiveInterval);      //If the new effect was stronger, we create a new instance using that effect
        StatusDictionary[effect] = instance;    //Replace the old instance with the new one.
    }

    //This function just always replaces the status effect no matter what
    private void ReplaceAlways(StatusEffects effect, int stacksPerHit, float damagePerTick, int tickCount, StatsComponent sourceStats, int effectiveInterval)
    {
        var instance = new StatusInstance(effect, damagePerTick, stacksPerHit, tickCount, sourceStats, effectiveInterval);
        StatusDictionary[effect] = instance;
    }

    // --------------------------------------------------------------------
    // TICKING – call this once per global "turn" from BattleManager
    // --------------------------------------------------------------------
    public void TickStatuses()
    {
        if (StatusDictionary.Count == 0 && IndependentDictionary.Count == 0)    //If both dictionaries are empty, return
            return;

        List<PendingEffect> pendingEffects = new List<PendingEffect>(); //Create a new list of pending effects

        var updated = new Dictionary<StatusEffects, StatusInstance>(StatusDictionary.Count);    //Dictionary used to store updated instances

        foreach (var kvp in StatusDictionary)   //For each pair in the status dictionary, we set variables for the effect and instance (key and value)
        {
            StatusEffects effect = kvp.Key;
            StatusInstance instance = kvp.Value;

            if (effect == null || instance.remainingTicks <= 0 || instance.stacks <= 0) //If the instance is already dead, skip this loop
                continue;

            TickInstance(instance, pendingEffects); //Call TickInstance

            if (instance.remainingTicks > 0 && instance.stacks > 0 && effect != null)   //Check again for no remaining ticks or stacks, add to keys to remove if none remaining
            {
                updated[effect] = instance; //Add the new instance at the proper position in the updated dictionary
            }
        }

        StatusDictionary.Clear();
        foreach (var kvp in updated)
            StatusDictionary[kvp.Key] = kvp.Value;   //Set statusDictionary entries to updated dictionary entries

        // ----------- Independent stack statuses -----------
        foreach (var kvp in IndependentDictionary)  //For each pair in independent dictionary, create a variable list that stores the pair's value (The instance list at that key)
        {
            List<StatusInstance> list = kvp.Value;
            if (list == null || list.Count == 0) continue;  //If list is null or empty, continue

            List<StatusInstance> toRemove = new List<StatusInstance>(); //Create list of status instances used to store keys that will be removed

            for (int i = 0; i < list.Count; i++)        //Loop through the length of list
            {
                var instance = list[i];     //instance is list at i

                if (instance.remainingTicks <= 0 || instance.stacks <= 0 || instance.effect == null)
                {
                    toRemove.Add(instance);     //if remaining ticks, stacks, or effects is 0/null, add that instance to the remove list and continue
                    continue;
                }

                TickInstance(instance, pendingEffects); //Tick the instance

                if (instance.remainingTicks <= 0 || instance.stacks <= 0)   //Check again if the effect should expire, if so add it to the remove list
                    toRemove.Add(instance);
                else
                    list[i] = instance; //Otherwise replace the instance at i with the modified instance
            }

            foreach (var inst in toRemove)  //remove any instances that are in the to remove list
                list.Remove(inst);
        }

        // ----------- Apply pending effects -----------
        if (pendingEffects.Count == 0)  //If pendingeffects is empty, return
            return;

        Dictionary<StatusEffects, float> strengthByEffect = new Dictionary<StatusEffects, float>(); //Create new dictionary with effect as the key, and a float as the pair for holding the effect and its strength

        foreach (var pe in pendingEffects)      //for each pending effect in pendingeffects list
        {
            if (pe.Effect == null) continue;    //if the effect is null, continue

            if (strengthByEffect.TryGetValue(pe.Effect, out float current)) //if the effect is in the list, current is its value
                strengthByEffect[pe.Effect] = current + pe.Strength;        //set the effect at this position to its current power + the pending effect's strength (A bit unsure about this. Why add the strength together?)
            else
                strengthByEffect[pe.Effect] = pe.Strength;  //Otherwise, set the effect to the pending effect's strength
        }

        foreach (var kvp in strengthByEffect)   //Loop through every pair in strengthbyeffect, effect is the key, totalStrength is the value
        {
            StatusEffects effect = kvp.Key;
            float totalStrength = kvp.Value;

            if (effect == null || totalStrength <= 0f)  //If key or value is null/0, continue
                continue;

            if (effect._StatusType == StatusEffects.StatusType.DamageOverTime)  //If the effect is a DoT effect, call apply dot damage, passing the strength and the effect type
            {
                // For DOTs we treat Strength as final damage already.
                ApplyDotDamage(totalStrength, effect);
            }
            else
            {
                //For chill/shock, this is not fully implemented, this simply has a delay. TODO: Implement shock/chill application logic
                if (playerCont != null)
                    StartCoroutine(EffectDelay(playerCont, totalStrength, effect));
                if (enemyCont != null)
                    StartCoroutine(EffectDelay(enemyCont, totalStrength, effect));
            }
        }
    }

    private void TickInstance(StatusInstance instance, List<PendingEffect> pendingEffects)
    {
        StatusEffects effect = instance.effect; //effect is the instance's effect type

        // Decide if we tick this turn, and how many times.
        int effectiveInterval = instance.effectiveInterval; //effectiveinterval is the instance's interval

        if (effectiveInterval > 0)  //if the interval is greater than 0, decrement the turns until next tick
        {
            // Tick every N turns.
            instance.turnsUntilNextTick--;
            if (instance.turnsUntilNextTick > 0) //if the turns until next tick is still greater than 0, return
                return; // no tick this turn

            // 1 tick this turn
            instance.turnsUntilNextTick = effectiveInterval;    //If it wasn't greater than 0, we reset the turnsuntilnexttick back to the effective interval and call applytick
            ApplyTick(instance, pendingEffects);
        }
        else
        {
            // effectiveInterval <= 0: multiple ticks per turn.
            int ticksThisTurn = 1 - effectiveInterval; // 0 -> 1, -1 -> 2, -2 -> 3, etc.

            for (int i = 0; i < ticksThisTurn; i++) //Loop as long as i is less than ticksThisTurn
            {
                if (instance.remainingTicks <= 0 || instance.stacks <= 0)   //If the remaining ticks or stacks hits 0 before i is greater than the number of ticks this turn, break the loop
                    break;

                ApplyTick(instance, pendingEffects);    //Call applytick using the instance and pendingeffects
            }
        }
    }

    private void ApplyTick(StatusInstance instance, List<PendingEffect> pendingEffects)
    {
        StatusEffects effect = instance.effect; //effect is the instance's effect, if it's null return
        if (effect == null)
            return;

        instance.remainingTicks--;  //decrement the instance's remaining ticks

        float baseTick = 0f;    //create variable for baseTick set to 0

        if (effect._StatusType == StatusEffects.StatusType.DamageOverTime)  //if the effect is a DoT
        {
            // Actual DOT damage.
            baseTick = instance.GetTickDamage();    //grab the tick damage and store it in baseTick, use combat calculator to calculate the tick damage

            float finalTick = CombatCalculator.CalculateAilmentTickDamage(
                baseTick,
                effect,
                instance.sourceStats,
                stats);

            if (finalTick > 0f) //if the final tick damage is greater than 0, add it to pending effects
                pendingEffects.Add(new PendingEffect(finalTick, effect));
        }
        else
        {
            // For non-damaging effects we treat damagePerTick as a generic "strength" scalar.
            float strength = instance.damagePerTick * instance.stacks;
            if (strength > 0f)  //if strength is greater than 0, add it to pending effects
                pendingEffects.Add(new PendingEffect(strength, effect));
        }
    }

    private void ApplyDotDamage(float damage, StatusEffects effect)
    {
        if (damage <= 0f) return;   //if the passed in damage is less than 0, return

        // Apply directly to the owning entity.
        if (playerCont != null) //if player controller isn't null, call take damage passing in the effect, and its damage
        {
            playerCont.TakeDamage(damage, effect);
        }
        else if (enemyCont != null) //if enemycont isn't null, call take damage passing in the effect and its damage
        {
            enemyCont.TakeDamage(damage, effect);
        }
    }

    //These two functions I think are no longer necessary, they were inteded to be delays that would not immediately apply status effects
    //so that the damage numbers could be displayed one after another instead of a bunch of numbers appearing at once
    //I believe now I'm going to do the damage numbers somewhere else entirely, and it should be possible to add a slight delay in there instead.
    public IEnumerator EffectDelay(PlayerController target, float strength, StatusEffects effect)   
    {
        yield return new WaitForSeconds(0.3f);

        if (target != null && effect != null)
            target.OnStatusTick(strength, effect);
    }

    public IEnumerator EffectDelay(EnemyAI target, float strength, StatusEffects effect)
    {
        yield return new WaitForSeconds(0.3f);

        if (target != null && effect != null)
            target.OnStatusTick(strength, effect);
    }
}
