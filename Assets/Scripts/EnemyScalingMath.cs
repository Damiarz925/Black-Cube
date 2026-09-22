using System;
using UnityEngine;

// Pure, shared runtime/editor math. Percentage-point resistance is deliberately not max resistance.
public static class EnemyScalingMath
{
    public readonly struct Intrinsic
    {
        public readonly int Level;
        public readonly float LifeFactor;
        public readonly float DamageFactor;
        public readonly float Armour;
        public readonly float ResistancePoints;
        public Intrinsic(int level, float life, float damage, float armour, float resistance)
        {
            Level = level; LifeFactor = life; DamageFactor = damage;
            Armour = armour; ResistancePoints = resistance;
        }
        public float ScaledLife(float authoredLife) => Mathf.Max(0f, authoredLife) * LifeFactor;
    }

    public static Intrinsic Calculate(int level, EnemyScalingProfile profile = null)
    {
        profile ??= EnemyScalingProfile.Default;
        return Calculate(level,profile.Capture());
    }

    public static Intrinsic Calculate(int level, EnemyScalingValues values)
    {
        level = Math.Max(1, level);
        int breakLevel=Math.Max(2,values.curveBreakLevel);int first = Math.Min(level - 1, breakLevel - 1);
        int later = Math.Max(0, level - breakLevel);
        float life = (float)(Math.Pow(values.lifeGrowth, first) * Math.Pow(values.lateLifeGrowth, later));
        float damage = (float)(Math.Pow(values.damageGrowth, first) * Math.Pow(values.lateDamageGrowth, later));
        return new Intrinsic(level, life, damage, values.armourPerLevel * (level - 1),
            Mathf.Min(values.resistancePointsCap, values.resistancePointsPerLevel * (level - 1)));
    }

    public static void ScaleOutgoing(DamageContext context, float factor)
    {
        if (context.Hits == null || factor == 1f) return;
        for (int i = 0; i < context.Hits.Count; i++)
        {
            var hit = context.Hits[i];
            hit.Amount *= factor;
            context.Hits[i] = hit;
        }
    }
}
