using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackCube
{
    // Snapshot-only duel: production hit, ailment, Shock and Chill calculations,
    // local System.Random rolls and event-driven gauges. No scene objects per hit.
    public static class BalanceCombatSimulator
    {
        public readonly struct Outcome
        {
            public readonly float Seconds;
            public readonly int Winner; // 1 reference player, -1 enemy, 0 horizon/draw
            public readonly int Hits, AilmentTicks, ShockTriggers, ChillApplications;
            public readonly float PlayerRemainingLife, EnemyRemainingLife;
            public Outcome(float seconds, int winner, int hits, int ticks, int shocks, int chills,
                float playerRemainingLife, float enemyRemainingLife)
            { Seconds = seconds; Winner = winner; Hits = hits; AilmentTicks = ticks;
                ShockTriggers = shocks; ChillApplications = chills;
                PlayerRemainingLife = playerRemainingLife; EnemyRemainingLife = enemyRemainingLife; }
        }

        private sealed class Fighter
        {
            public StatsComponent Stats;
            public StatsComponent DefenseStats;
            public DamageContext Context;
            public float Life, MaxLife, Speed, CritChance, CritMultiplier, HitTwice, NextAttack, ChillSlow;
            public int ChillTurns, ShockStacks;
            public readonly List<Dot> Dots = new List<Dot>();
        }

        private sealed class Dot
        {
            public StatusEffects Effect;
            public StatsComponent Source;
            public float PerTick, Interval, NextTick;
            public int TicksLeft;
        }

        public static Outcome Simulate(DamageContext playerContext, StatsComponent playerStats,
            float playerLife, float playerSpeed, float playerCrit, DamageContext enemyContext,
            StatsComponent enemyStats, float enemyLife, float enemySpeed, float enemyCrit,
            StatusEffects[] ailments, int seed, float horizon = 120f)
            => Simulate(playerContext, playerStats, playerStats, playerLife, playerSpeed, playerCrit,
                enemyContext, enemyStats, enemyStats, enemyLife, enemySpeed, enemyCrit,
                ailments, seed, horizon);

        public static Outcome Simulate(DamageContext playerContext, StatsComponent playerStats,
            StatsComponent playerDefenseStats, float playerLife, float playerSpeed, float playerCrit,
            DamageContext enemyContext, StatsComponent enemyStats, StatsComponent enemyDefenseStats,
            float enemyLife, float enemySpeed, float enemyCrit, StatusEffects[] ailments,
            int seed, float horizon = 120f)
        {
            var random = new System.Random(seed);
            var player = Make(playerContext, playerStats, playerDefenseStats, playerLife, playerSpeed, playerCrit);
            var enemy = Make(enemyContext, enemyStats, enemyDefenseStats, enemyLife, enemySpeed, enemyCrit);
            float time = 0f;
            int hits = 0, ticks = 0, shocks = 0, chills = 0;
            for (int events = 0; events < 10000 && time <= horizon; events++)
            {
                float next = Mathf.Min(player.NextAttack, enemy.NextAttack, NextDot(player), NextDot(enemy));
                if (next > horizon || float.IsInfinity(next)) break;
                time = next;
                TickDots(player, enemy, time, ref ticks);
                TickDots(enemy, player, time, ref ticks);
                if (player.Life <= 0f || enemy.Life <= 0f) break;
                if (player.NextAttack <= time + .00001f)
                    Attack(player, enemy, time, random, ailments, ref hits, ref shocks, ref chills);
                if (enemy.Life <= 0f) break;
                if (enemy.NextAttack <= time + .00001f)
                    Attack(enemy, player, time, random, ailments, ref hits, ref shocks, ref chills);
                if (player.Life <= 0f) break;
            }
            int winner = enemy.Life <= 0f ? 1 : player.Life <= 0f ? -1 : 0;
            return new Outcome(time, winner, hits, ticks, shocks, chills, player.Life, enemy.Life);
        }

        private static Fighter Make(DamageContext context, StatsComponent stats, StatsComponent defenseStats,
            float life, float speed, float crit)
        {
            var fighter = new Fighter
            {
                Context = context, Stats = stats, DefenseStats = defenseStats, Life = life, MaxLife = life,
                Speed = Mathf.Max(.0001f, speed), CritChance = Mathf.Clamp01(crit),
                CritMultiplier = 1f + stats.GetStat(StatTypes.CritMult),
                HitTwice = Mathf.Clamp01(stats.GetStat(StatTypes.ChanceToHitTwice))
            };
            fighter.NextAttack = 1f / fighter.Speed;
            return fighter;
        }

        private static void Attack(Fighter source, Fighter target, float time, System.Random random,
            StatusEffects[] ailments, ref int hits, ref int shocks, ref int chills)
        {
            Strike(source, target, time, random, ailments, ref hits, ref shocks, ref chills);
            if (target.Life > 0f && random.NextDouble() < source.HitTwice)
                Strike(source, target, time, random, ailments, ref hits, ref shocks, ref chills);
            source.NextAttack = time + 1f / Mathf.Max(.0001f, source.Speed * (1f - source.ChillSlow));
            if (source.ChillTurns > 0 && --source.ChillTurns == 0) source.ChillSlow = 0f;
        }

        private static void Strike(Fighter source, Fighter target, float time, System.Random random,
            StatusEffects[] ailments, ref int hits, ref int shocks, ref int chills)
        {
            var context = Copy(source.Context);
            if (random.NextDouble() < source.CritChance)
                EnemyScalingMath.ScaleOutgoing(context, source.CritMultiplier);
            float dealt = CombatCalculator.CalculateFinalDamage(context, source.Stats, target.DefenseStats);
            target.Life -= dealt;
            hits++;
            if (target.Life <= 0f) return;
            foreach (StatusEffects effect in ailments)
            {
                if (effect == null || AilmentCalculator.GetSourceHitDamage(effect, context) <= 0f) continue;
                float chance = ApplicationChance(effect.Ailment, source.Stats, target.DefenseStats);
                int applications = (int)Math.Floor(chance);
                if (random.NextDouble() < chance - applications) applications++;
                if (applications <= 0) continue;
                AilmentCalculator.ComputeAilmentFromHit(effect, context, source.Stats,
                    out float strength, out int count, out int interval);
                if (strength <= 0f) continue;
                float globalTurnsPerSecond = source.Speed + target.Speed;
                float secondsPerTick = interval > 0
                    ? interval / Mathf.Max(.0001f, globalTurnsPerSecond)
                    : 1f / Mathf.Max(.0001f, globalTurnsPerSecond * (1 - interval));
                int cap = effect.MaxStacks;
                int activeForEffect = 0;
                foreach (Dot existing in target.Dots) if (existing.Effect == effect) activeForEffect++;
                for (int i = 0; i < applications && activeForEffect < cap; i++, activeForEffect++)
                    target.Dots.Add(new Dot { Effect = effect, Source = source.Stats,
                        PerTick = strength, Interval = secondsPerTick,
                        NextTick = time + secondsPerTick, TicksLeft = count });
            }
            float lightning = CombatCalculator.CalculateFinalElementDamage(context, Element.Light,
                source.Stats, target.DefenseStats);
            float shockResistance = Mathf.Clamp(target.DefenseStats.GetStat(StatTypes.ShockRes)
                + target.DefenseStats.GetStat(StatTypes.AllAilmentRes), -.9f, .9f);
            int shockApplications = lightning > 0f ? RollApplications(random,
                BattleManager.AdjustedChance(source.Stats, StatTypes.ShockChance)
                    * (1f - shockResistance)) : 0;
            if (shockApplications > 0)
            {
                target.ShockStacks += shockApplications;
                while (target.ShockStacks >= 5)
                {
                    target.ShockStacks -= 5;
                    target.Life -= lightning * Mathf.Min(1f, .5f * (1f + source.Stats.GetStat(StatTypes.ShockEffect)));
                    shocks++;
                }
            }
            float cold = CombatCalculator.CalculateFinalElementDamage(context, Element.Cold,
                source.Stats, target.DefenseStats);
            float chillResistance = Mathf.Clamp(target.DefenseStats.GetStat(StatTypes.ChillRes)
                + target.DefenseStats.GetStat(StatTypes.AllAilmentRes), -.9f, .9f);
            if (cold > 0f && RollApplications(random,
                BattleManager.AdjustedChance(source.Stats, StatTypes.ChillChance)
                    * (1f - chillResistance)) > 0)
            {
                float slow = BattleManager.CalculateChillSlow(cold, target.MaxLife,
                    source.Stats.GetStat(StatTypes.ChillEffect)) * (1f - chillResistance);
                target.ChillSlow = Mathf.Max(target.ChillSlow, Mathf.Min(.3f, slow));
                target.ChillTurns = Mathf.Max(target.ChillTurns, Mathf.Max(1,
                    Mathf.RoundToInt((4f + source.Stats.GetRawStat(StatTypes.ChillDuration))
                        * (1f - chillResistance))));
                chills++;
            }
        }

        private static float ApplicationChance(StatusEffects.AilmentKind kind, StatsComponent source,
            StatsComponent target)
        {
            StatTypes chance = kind switch
            {
                StatusEffects.AilmentKind.Poison => StatTypes.PoisonChance,
                StatusEffects.AilmentKind.Bleed => StatTypes.BleedChance,
                _ => StatTypes.IgniteChance
            };
            StatTypes resistance = kind switch
            {
                StatusEffects.AilmentKind.Poison => StatTypes.PoisonRes,
                StatusEffects.AilmentKind.Bleed => StatTypes.BleedRes,
                _ => StatTypes.IgniteRes
            };
            StatTypes penetration = kind switch
            {
                StatusEffects.AilmentKind.Poison => StatTypes.PoisonPenetration,
                StatusEffects.AilmentKind.Bleed => StatTypes.BleedPenetration,
                _ => StatTypes.IgnitePenetration
            };
            float resisted = Mathf.Clamp(target.GetStat(resistance) + target.GetStat(StatTypes.AllAilmentRes)
                - source.GetStat(penetration), -.9f, .9f);
            return Mathf.Max(0f, BattleManager.AdjustedChance(source, chance) * (1f - resisted));
        }

        private static int RollApplications(System.Random random, float chance)
        {
            chance = Mathf.Max(0f, chance);
            int guaranteed = Mathf.FloorToInt(chance);
            return guaranteed + (random.NextDouble() < chance - guaranteed ? 1 : 0);
        }

        private static float NextDot(Fighter fighter)
        {
            float next = float.PositiveInfinity;
            foreach (Dot dot in fighter.Dots) next = Mathf.Min(next, dot.NextTick);
            return next;
        }

        private static void TickDots(Fighter fighter, Fighter other, float time, ref int ticks)
        {
            for (int i = fighter.Dots.Count - 1; i >= 0; i--)
            {
                Dot dot = fighter.Dots[i];
                if (dot.NextTick > time + .00001f) continue;
                fighter.Life -= CombatCalculator.CalculateAilmentTickDamage(dot.PerTick,
                    dot.Effect, dot.Source, fighter.DefenseStats);
                ticks++;
                dot.TicksLeft--;
                if (dot.TicksLeft <= 0) fighter.Dots.RemoveAt(i);
                else dot.NextTick += dot.Interval;
            }
        }

        private static DamageContext Copy(DamageContext source)
        {
            var copy = new DamageContext(source.Hits.Count) { Scopes = source.Scopes };
            foreach (var hit in source.Hits) copy.AddDamage(hit.Element, hit.Amount);
            return copy;
        }
    }
}
