// Developer map: Target-owned stacks with global-turn ticking and pending effect aggregation. Mutation is separated from application because damage/death callbacks can clear statuses or replace targets.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using UnityEngine;

//StatusController is used to manage all of the statuses on the GameObject that the controller is on. Contains code for managing stacks and applying Status Damage/Effects
public partial class StatusController : MonoBehaviour
{
    bool frozen;
    float frozenChillStrength;
    readonly List<float> shockInstances=new();
    readonly List<int> shockDurations=new();
    public bool IsFrozen=>frozen;
    public float FrozenChillStrength=>frozenChillStrength;
    public float CombinedShockEffect
    {
        get{float product=1f;foreach(float value in shockInstances)product*=1f+Mathf.Max(0,value);return Mathf.Max(0,product-1f);}
    }
    public bool ApplyFreeze(float chillStrength){if(chillStrength<=0)return false;frozen=true;frozenChillStrength=Mathf.Clamp01(chillStrength);return true;}
    public bool ConsumeFrozenAttackSkip(){if(!frozen)return false;frozen=false;frozenChillStrength=0;return true;}
    public bool TryConsumeFreeze(out float chillStrength){chillStrength=frozenChillStrength;if(!frozen)return false;frozen=false;frozenChillStrength=0;return true;}
    public void AddShockInstance(float strength,int duration=1,int maximum=1){strength=Mathf.Max(0,strength);if(strength<=0)return;maximum=Mathf.Max(1,maximum);if(shockInstances.Count>=maximum){shockInstances.RemoveAt(0);shockDurations.RemoveAt(0);}shockInstances.Add(strength);shockDurations.Add(Mathf.Max(1,duration));}
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
    public void ApplyAilmentFromHit(StatusEffects effect, DamageContext context, StatsComponent attackerStats,
        int stacksPerHit = 1, float magnitudeOverride = -1f)
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
            out int effectiveInterval,
            magnitudeOverride);

        if (damagePerTick <= 0f || tickCount <= 0)  //If damagepertick or tick count is less than or equal to 0, return
            return;

        ApplyStatus(effect, stacksPerHit, damagePerTick, tickCount, attackerStats, effectiveInterval);      //Call apply status
    }

    public void ApplyStatus(StatusEffects effect, int stacksPerHit, float damagePerTick, int tickCount, StatsComponent sourceStats, int effectiveInterval)
    {
        if (effect == null || stacksPerHit <= 0 || tickCount <= 0 || damagePerTick <= 0) return;
        int initialCap = EffectiveStackCap(effect, sourceStats);
        if (initialCap > 0) stacksPerHit = Mathf.Min(stacksPerHit, initialCap);
        var health = GetComponent<HealthComponent>();
        if (health != null && health.CurrentLife <= 0) return;
        if(IsScheduledDamagingAilment(effect))
        {
            AddIndependentDamagingStacks(effect,stacksPerHit,damagePerTick,tickCount,sourceStats,effectiveInterval);
            return;
        }
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

    public int AddShockStacks(StatusEffects effect, int addedStacks, int duration, float coefficient,
        int threshold = 5)
    {
        if (effect == null || addedStacks <= 0) return 0;
        threshold = Mathf.Max(1, threshold);
        int existingStacks = StatusDictionary.TryGetValue(effect, out StatusInstance existing)
            ? Mathf.Max(0, existing.stacks) : 0;
        int total = existingStacks + addedStacks;
        int triggers = total / threshold;
        int remainder = total % threshold;
        if (remainder > 0)
        {
            var instance = new StatusInstance(effect, Mathf.Clamp01(coefficient), remainder,
                Mathf.Max(1, duration), null, 1) { threshold = threshold };
            StatusDictionary[effect] = instance;
        }
        else
            StatusDictionary.Remove(effect);
        return triggers;
    }

    public bool ApplyChill(StatusEffects effect, float slow, int duration,float maximumSlow=.3f)
    {
        if (effect == null || slow <= 0f) return false;
        slow = Mathf.Clamp(slow, 0f, Mathf.Max(.3f,maximumSlow));
        duration = Mathf.Max(1, duration);
        if (StatusDictionary.TryGetValue(effect, out StatusInstance existing)
            && existing.damagePerTick > slow + .0001f)
            return false;
        StatusDictionary[effect] = new StatusInstance(effect, slow, 1, duration, null, 1);
        return true;
    }

    public float CurrentChillSlow
    {
        get
        {
            float strongest = 0f;
            foreach (var pair in StatusDictionary)
                if (pair.Key != null && pair.Key._StatusType == StatusEffects.StatusType.Chill
                    && pair.Value.remainingTicks > 0)
                    strongest = Mathf.Max(strongest, pair.Value.damagePerTick);
            return Mathf.Clamp(strongest, 0f, .6f);
        }
    }
    public bool HasAilment(StatusEffects.AilmentKind kind)
    {
        foreach(var pair in StatusDictionary)if(pair.Key!=null&&pair.Key.Ailment==kind&&pair.Value.remainingTicks>0&&pair.Value.stacks>0)return true;
        foreach(var pair in IndependentDictionary)if(pair.Key!=null&&pair.Key.Ailment==kind)foreach(var instance in pair.Value)if(instance!=null&&instance.remainingTicks>0&&instance.stacks>0)return true;
        return false;
    }
    public int DistinctAilmentCount()
    {int count=0;foreach(StatusEffects.AilmentKind kind in new[]{StatusEffects.AilmentKind.Poison,StatusEffects.AilmentKind.Bleed,StatusEffects.AilmentKind.Ignite})if(HasAilment(kind))count++;if(CurrentChillSlow>0)count++;if(CombinedShockEffect>0)count++;return count;}
    public float RemainingAilmentDamage(StatusEffects.AilmentKind kind)
    {float total=0;foreach(var pair in IndependentDictionary)if(pair.Key!=null&&pair.Key.Ailment==kind)foreach(var instance in pair.Value)if(instance!=null)total+=RemainingMitigatedDamage(instance);foreach(var pair in StatusDictionary)if(pair.Key!=null&&pair.Key.Ailment==kind&&pair.Value!=null)total+=RemainingMitigatedDamage(pair.Value);return total;}

    private void StackAndRefresh(StatusEffects effect, int stacksPerHit, float damagePerTick, int tickCount, StatsComponent sourceStats, int effectiveInterval)
    {
        int currentStacks = 0;
        if (StatusDictionary.TryGetValue(effect, out var existing))     //Search the dictionary for the effect, if it exists set current stacks to the existing stack count
            currentStacks = existing.stacks;

        int newStacks = currentStacks + stacksPerHit;       //Create variable newStacks and set that to the current stacks + the stacksPerHit(The num of stacks that is applied by the hit applying the ailment)
        int effectiveCap = EffectiveStackCap(effect, sourceStats);
        if (effectiveCap > 0)
            newStacks = Mathf.Min(newStacks, effectiveCap);

        int added = newStacks - currentStacks;
        if (existing != null && currentStacks > 0)
            damagePerTick = (existing.damagePerTick * currentStacks + damagePerTick * added) / newStacks;
        var instance = new StatusInstance(effect, damagePerTick, newStacks, tickCount, sourceStats, effectiveInterval);     //Refresh retains the weighted strength of existing stacks
        StatusDictionary[effect] = instance;    //Replace the existing instance in the dictionary with the new one
    }

    private void StackIndependently(StatusEffects effect, int stacksPerHit, float damagePerTick, int tickCount, StatsComponent sourceStats, int effectiveInterval)
    {
        if (!IndependentDictionary.TryGetValue(effect, out var list))   //Search the dictionary for the effect, if it's not in the list, out list
        {
            list = new List<StatusInstance>();  //Create new list of status instances
            IndependentDictionary[effect] = list;   //Add the list to the effect
        }

        int effectiveCap = EffectiveStackCap(effect, sourceStats);
        if (effectiveCap > 0)
        {
            int count = 0; foreach (var active in list) count += active.stacks;
            stacksPerHit = Mathf.Min(stacksPerHit, effectiveCap - count);
            if (stacksPerHit <= 0) return;
        }
        var instance = new StatusInstance(effect, damagePerTick, stacksPerHit, tickCount, sourceStats, effectiveInterval);  //Create the instance using the passed in values
        list.Add(instance);     //Add our new instance to the list
    }

    static int EffectiveStackCap(StatusEffects effect, StatsComponent sourceStats)
    {
        if(effect==null)return 0;
        if(effect.Ailment==StatusEffects.AilmentKind.Poison)return 0;
        var keystones=sourceStats!=null?sourceStats.GetComponent<PassiveKeystoneState>():null;
        bool playerSource=sourceStats!=null&&sourceStats.GetComponent<PlayerController>()!=null;
        var relics=playerSource?RelicInventory.Instance:null;
        if(effect.Ailment==StatusEffects.AilmentKind.Bleed)
        {
            int cap=keystones!=null&&keystones.Has(PassiveKeystone.OpenWounds)?10:5;
            return cap+(relics?.MaximumBleedStackBonus??0);
        }
        if(effect.Ailment==StatusEffects.AilmentKind.Ignite)
        {
            int cap=keystones!=null&&keystones.Has(PassiveKeystone.Wildfire)?2:1;
            return cap+(relics?.MaximumIgniteStackBonus??0);
        }
        return keystones!=null?keystones.EffectiveAilmentStackCap(effect):effect.MaxStacks;
    }

    static bool IsScheduledDamagingAilment(StatusEffects effect)=>effect._StatusType==StatusEffects.StatusType.DamageOverTime
        && effect.Ailment is StatusEffects.AilmentKind.Poison or StatusEffects.AilmentKind.Bleed or StatusEffects.AilmentKind.Ignite;

    void AddIndependentDamagingStacks(StatusEffects effect,int applications,float damagePerTick,int tickCount,
        StatsComponent sourceStats,int interval)
    {
        if(!IndependentDictionary.TryGetValue(effect,out var list))IndependentDictionary[effect]=list=new List<StatusInstance>();
        int cap=EffectiveStackCap(effect,sourceStats);
        for(int i=0;i<applications;i++)
        {
            var incoming=new StatusInstance(effect,damagePerTick,1,tickCount,sourceStats,interval);
            if(cap<=0||list.Count<cap){list.Add(incoming);continue;}
            int weakest=-1;float weakestTotal=float.PositiveInfinity;
            for(int j=0;j<list.Count;j++)
            {
                float remaining=RemainingMitigatedDamage(list[j]);
                if(remaining<weakestTotal){weakestTotal=remaining;weakest=j;}
            }
            if(weakest>=0&&RemainingMitigatedDamage(incoming)>weakestTotal+0.0001f)list[weakest]=incoming;
        }
    }
    float RemainingMitigatedDamage(StatusInstance instance)=>instance.remainingTicks
        * CombatCalculator.CalculateAilmentTickDamage(instance.damagePerTick,instance.effect,instance.sourceStats,stats);

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
    public void TickStatuses(bool afflictedActorTurn=true)
    {
        var health = GetComponent<HealthComponent>();
        if (health != null && health.CurrentLife <= 0) { ClearStatuses(); return; }
        for(int i=shockDurations.Count-1;i>=0;i--)if(--shockDurations[i]<=0){shockDurations.RemoveAt(i);shockInstances.RemoveAt(i);}
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

            if (effect._StatusType == StatusEffects.StatusType.DamageOverTime)
            {
                if(ShouldAdvance(effect,afflictedActorTurn))TickInstance(instance,pendingEffects);
            }
            else
                instance.remainingTicks--; // Shock and Chill lifetimes count every global turn; their effects are queried dynamically.

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

                if(ShouldAdvance(instance.effect,afflictedActorTurn))TickInstance(instance,pendingEffects);

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
                strengthByEffect[pe.Effect] = current + pe.Strength;        // Sum due stacks/ticks into one damage application and popup per effect this turn.
            else
                strengthByEffect[pe.Effect] = pe.Strength;  //Otherwise, set the effect to the pending effect's strength
        }

        foreach (var kvp in strengthByEffect)   //Loop through every pair in strengthbyeffect, effect is the key, totalStrength is the value
        {
            StatusEffects effect = kvp.Key;
            float totalStrength = kvp.Value;

            if (effect == null || totalStrength <= 0f)  //If key or value is null/0, continue
                continue;

            // Only damaging effects enter the pending-tick queue. Shock and Chill
            // are queried directly from their persistent status state.
            if (effect._StatusType == StatusEffects.StatusType.DamageOverTime)
                ApplyDotDamage(totalStrength, effect);
        }
    }

    public float ConsumeRemainingAilmentDamage(StatusEffects.AilmentKind ailment)
    {
        float total=0;var remove=new List<StatusEffects>();
        foreach(var pair in IndependentDictionary)if(pair.Key!=null&&pair.Key.Ailment==ailment)
        {foreach(var instance in pair.Value)total+=RemainingMitigatedDamage(instance);remove.Add(pair.Key);}
        foreach(var key in remove)IndependentDictionary.Remove(key);
        return total;
    }

    public void ClearStep18TransientState(){frozen=false;frozenChillStrength=0;shockInstances.Clear();shockDurations.Clear();}

    private void TickInstance(StatusInstance instance, List<PendingEffect> pendingEffects)
    {
        StatusEffects effect = instance.effect; //effect is the instance's effect type

        // Decide if we tick this turn, and how many times.
        int effectiveInterval = instance.effectiveInterval; //effectiveinterval is the instance's interval

        instance.remainingDurationTurns--;
        if(!Mathf.Approximately(instance.TickRateMultiplier,1f))
        {
            float baseTicksPerTurn=effectiveInterval>0?1f/effectiveInterval:1-effectiveInterval;
            instance.TickProgress+=baseTicksPerTurn*instance.TickRateMultiplier;
            int acceleratedTicks=Mathf.FloorToInt(instance.TickProgress);instance.TickProgress-=acceleratedTicks;
            for(int i=0;i<acceleratedTicks&&instance.remainingTicks>0&&instance.stacks>0;i++)ApplyTick(instance,pendingEffects);
            if(instance.remainingDurationTurns<=0)instance.remainingTicks=0;
            return;
        }
        if (effectiveInterval > 0)  //if the interval is greater than 0, decrement the turns until next tick
        {
            instance.turnsUntilNextTick--;
            if (instance.turnsUntilNextTick > 0) return;

            // 1 tick this turn
            instance.turnsUntilNextTick = effectiveInterval;
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
        if(instance.remainingDurationTurns<=0)instance.remainingTicks=0;
    }

    static bool ShouldAdvance(StatusEffects effect,bool afflictedActorTurn)=>effect.Ailment==StatusEffects.AilmentKind.Poison
        || effect.Ailment==StatusEffects.AilmentKind.GenericDot || afflictedActorTurn;

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

            float finalTick = CombatCalculator.CalculateAilmentTickDamage(baseTick, effect, instance.sourceStats, stats);

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

}
