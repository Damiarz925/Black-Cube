using UnityEditor;
using UnityEngine;

/// <summary>Deterministic focused checks for independent damage-more rolls.</summary>
public static class DamageStackingChecks
{
    private const float Tolerance = 0.001f;

    [MenuItem("Black Cube/Play Checks/Verify Damage Stacking")]
    public static void Verify()
    {
        var owner = new GameObject("DamageStackingChecks");
        var stats = owner.AddComponent<StatsComponent>();

        try
        {
            Add(stats, StatTypes.ColdDmg, StatOp.Additive, 40f, "cold increased");
            Add(stats, StatTypes.GenericDmg, StatOp.Additive, 30f, "generic increased");
            Add(stats, StatTypes.ColdMult, StatOp.Multiplicative, 20f, "cold more");
            Add(stats, StatTypes.GenericMult, StatOp.Multiplicative, 10f, "generic more");

            float expected = 100f * 1.70f * 1.20f * 1.10f;
            RequireClose(OutgoingDamageCalculator.Calculate(100f, 0f, Element.Cold, stats), expected,
                "base cold damage uses additive increased and independent more rolls");
            RequireClose(OutgoingDamageCalculator.Calculate(0f, 100f, Element.Cold, stats), expected,
                "off-element flat cold damage uses the same scaling");

            var duplicateOwner = new GameObject("DuplicateMoreChecks");
            try
            {
                var duplicateStats = duplicateOwner.AddComponent<StatsComponent>();
                Add(duplicateStats, StatTypes.ColdMult, StatOp.Multiplicative, 20f, "gear one");
                Add(duplicateStats, StatTypes.ColdMult, StatOp.Multiplicative, 20f, "gear two");
                RequireClose(duplicateStats.GetRawStat(StatTypes.ColdMult), 40f,
                    "raw/UI value remains the summed percent points");
                RequireClose(duplicateStats.GetMoreDamageMultiplier(StatTypes.ColdMult), 1.44f,
                    "two separate 20% rolls multiply to 1.44");

                Add(duplicateStats, StatTypes.FireMult, StatOp.Multiplicative, 50f, "nonmatching fire");
                RequireClose(OutgoingDamageCalculator.Calculate(100f, 0f, Element.Cold, duplicateStats), 144f,
                    "nonmatching elemental more does not apply");

                VerifyPlayerAndEnemyPipelines(duplicateStats);
            }
            finally
            {
                Object.DestroyImmediate(duplicateOwner);
            }

            VerifyAilmentMoreRolls();
            Debug.Log("PASS: independent hit and ailment more-damage stacking, element filtering, off-element scaling, raw display totals, and shared player/enemy calculation verified.");
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    private static void VerifyAilmentMoreRolls()
    {
        var owner = new GameObject("AilmentMoreChecks");
        var effect = ScriptableObject.CreateInstance<StatusEffects>();
        try
        {
            var stats = owner.AddComponent<StatsComponent>();
            Add(stats, StatTypes.GenericMult, StatOp.Multiplicative, 50f, "already in source hit");
            Add(stats, StatTypes.GenericDotMult, StatOp.Multiplicative, 10f, "generic dot");
            Add(stats, StatTypes.PoisonMult, StatOp.Multiplicative, 20f, "poison one");
            Add(stats, StatTypes.PoisonMult, StatOp.Multiplicative, 20f, "poison two");

            var serialized = new SerializedObject(effect);
            serialized.FindProperty("ailmentKind").enumValueIndex = (int)StatusEffects.AilmentKind.Poison;
            serialized.FindProperty("effectMagnitude").floatValue = 1f;
            serialized.FindProperty("tickDuration").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var context = new DamageContext(4);
            context.AddDamage(Element.Phys, 100f);
            AilmentCalculator.ComputeAilmentFromHit(effect, context, stats,
                out float damagePerTick, out int tickCount, out _);

            // Generic hit more (50%) is deliberately absent: the supplied 100
            // source hit already embodies all hit scaling.
            Require(tickCount == 1, "ailment fixture has one tick");
            RequireClose(damagePerTick, 100f * 1.10f * 1.20f * 1.20f,
                "generic DoT and each poison-more roll multiply independently without hit double-scaling");
        }
        finally
        {
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(owner);
        }
    }

    private static void VerifyPlayerAndEnemyPipelines(StatsComponent stats)
    {
        var playerOwner = new GameObject("PlayerDamagePipelineCheck");
        var enemyOwner = new GameObject("EnemyDamagePipelineCheck");
        try
        {
            var player = playerOwner.AddComponent<PlayerController>();
            var enemy = enemyOwner.AddComponent<EnemyAI>();
            SetPrivateField(player, "stats", stats);
            SetPrivateField(enemy, "stats", stats);

            float playerDamage = InvokeBaseElementPipeline(player, Element.Cold, 100f);
            float enemyDamage = InvokeBaseElementPipeline(enemy, Element.Cold, 100f);
            RequireClose(playerDamage, 144f, "player pipeline applies independent rolls");
            RequireClose(enemyDamage, playerDamage, "player and enemy pipelines agree");
        }
        finally
        {
            Object.DestroyImmediate(playerOwner);
            Object.DestroyImmediate(enemyOwner);
        }
    }

    private static float InvokeBaseElementPipeline(object actor, Element element, float baseAmount)
    {
        var context = new DamageContext(4);
        var method = actor.GetType().GetMethod("AddScaledElementalDamage",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Require(method != null, $"{actor.GetType().Name} exposes its base-element pipeline");
        method.Invoke(actor, new object[] { context, element, baseAmount });
        Require(context.Hits.Count == 1, $"{actor.GetType().Name} produced one elemental hit");
        return context.Hits[0].Amount;
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Require(field != null, $"{target.GetType().Name}.{name} exists");
        field.SetValue(target, value);
    }

    private static void Add(StatsComponent stats, StatTypes type, StatOp operation, float value, object source)
    {
        stats.AddModifier(new StatModifier(type, operation, value, source));
    }

    private static void RequireClose(float actual, float expected, string description)
    {
        Require(Mathf.Abs(actual - expected) <= Tolerance,
            $"{description}: expected {expected}, got {actual}");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new System.InvalidOperationException(description);
    }
}
