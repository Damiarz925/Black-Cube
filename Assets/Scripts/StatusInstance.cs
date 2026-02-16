public class StatusInstance
{
    public StatusEffects effect;    //Public field effect of StatusEffects type
    public int stacks;  //Public field for an int stacks to hold the number of stacks this instance has (stacks >= 1)
    public int remainingTicks;  //Public field for an int remainingTicks to store the number of ticks remaining in the instance

    // Damage per *tick* for 1 stack (we multiply by stacks at tick time). (damageperTick >= 0)
    public float damagePerTick;

    // Who applied this status (for penetration & offensive stats if needed).
    public StatsComponent sourceStats;

    // Turn-based tick timing
    // effectiveInterval > 0  => tick every N turns
    // effectiveInterval <= 0 => multiple ticks per turn: ticksPerTurn = 1 - effectiveInterval
    public int effectiveInterval;
    public int turnsUntilNextTick;

    //This is the constructor for the StatusInstance
    public StatusInstance(
        StatusEffects effect,   
        float damagePerTick,
        int stacks,
        int totalTicks,
        StatsComponent sourceStats,
        int effectiveInterval) //Passed in arguments are effect, dmg per tick, stacks, total ticks, source stats, and effective interval
    {
        this.effect = effect;   //Set fields to all passed in arguments
        this.damagePerTick = damagePerTick;
        this.stacks = stacks;
        this.remainingTicks = totalTicks;
        this.sourceStats = sourceStats;
        this.effectiveInterval = effectiveInterval;
        this.turnsUntilNextTick = effectiveInterval - 1;
    }

    //Returns the damage per tick multiplied by the number of stacks for the effective damage of a particular tick
    public float GetTickDamage()
    {
        return damagePerTick * stacks;
    }

    //Returns the total amount of damage that the status effect would do from now until it ends
    public float TotalPlannedDamage()
    {
        return damagePerTick * stacks * remainingTicks;
    }
}
