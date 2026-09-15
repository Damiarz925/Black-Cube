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
        level = Math.Max(1, level);
        int first = Math.Min(level - 1, profile.CurveBreakLevel - 1);
        int later = Math.Max(0, level - profile.CurveBreakLevel);
        float life = (float)(Math.Pow(profile.LifeGrowth, first) * Math.Pow(profile.LateLifeGrowth, later));
        float damage = (float)(Math.Pow(profile.DamageGrowth, first) * Math.Pow(profile.LateDamageGrowth, later));
        return new Intrinsic(level, life, damage, profile.ArmourPerLevel * (level - 1),
            Mathf.Min(profile.ResistancePointsCap, profile.ResistancePointsPerLevel * (level - 1)));
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
