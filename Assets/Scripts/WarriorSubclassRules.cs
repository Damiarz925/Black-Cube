using UnityEngine;

// Pure Warrior formulas shared by live combat, tooltips and the Combat Lab.
public static class WarriorSubclassRules
{
    static WarriorSubclassTuningSO tuning;
    public static float MomentumLessDamage
    {
        get
        {
            if(tuning==null)tuning=Resources.Load<WarriorSubclassTuningSO>("GameData/SO_WarriorSubclassTuning");
            return tuning!=null?tuning.MomentumLessDamage:.20f;
        }
    }
    public const float MultistrikeToAttackSpeed = 2f;
    public const float AttackSpeedToMultistrike = .5f;
    public const float BlockedHitMultiplier = .5f;
    public const float DeferredSeconds = 4f;

    // Inputs and outputs are percentage points, before percent-stat conversion.
    public static void MomentumConversion(float sourceAttackSpeed, float sourceMultistrike,
        out float finalAttackSpeed, out float finalMultistrike)
    {
        finalAttackSpeed = sourceAttackSpeed + Mathf.Max(0, sourceMultistrike) * MultistrikeToAttackSpeed;
        finalMultistrike = sourceMultistrike + Mathf.Max(0, sourceAttackSpeed) * AttackSpeedToMultistrike;
    }

    public static float EscalatingMultiplier(int priorMultistrikes, float perPrior)
        => 1f + Mathf.Max(0, priorMultistrikes) * Mathf.Max(0, perPrior);

    public static float AssaultMultiplier(int priorAttacks, float perPrior)
        => 1f + Mathf.Max(0, priorAttacks) * Mathf.Max(0, perPrior);

    public static bool CanRupture(int stacks, int cap, bool alreadyThisTurn)
        => !alreadyThisTurn && cap > 0 && stacks >= cap;

    public static int AcceptedBleedApplications(int existing, int applications, int cap)
        => Mathf.Clamp(applications, 0, Mathf.Max(0, cap - existing));

    public static float RuptureTotal(float actualRemainingBleedDamage, float more)
        => Mathf.Max(0, actualRemainingBleedDamage) * (1f + Mathf.Max(0, more));

    public static float ImmediateLifeDamage(float wouldHitLife, float deferredFraction)
        => Mathf.Max(0, wouldHitLife) * (1f - Mathf.Clamp01(deferredFraction));

    public static float DeferredLifeDamage(float wouldHitLife, float deferredFraction)
        => Mathf.Max(0, wouldHitLife) * Mathf.Clamp01(deferredFraction);
}
