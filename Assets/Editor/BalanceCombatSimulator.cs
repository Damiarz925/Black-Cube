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
            public readonly int SkillCasts;
            public readonly float ManaRemaining, DirectEnemyDamage, AilmentEnemyDamage;
            public Outcome(float seconds, int winner, int hits, int ticks, int shocks, int chills,
                float playerRemainingLife, float enemyRemainingLife,int skillCasts=0,
                float manaRemaining=0f,float directEnemyDamage=0f,float ailmentEnemyDamage=0f)
            { Seconds = seconds; Winner = winner; Hits = hits; AilmentTicks = ticks;
                ShockTriggers = shocks; ChillApplications = chills;
                PlayerRemainingLife = playerRemainingLife; EnemyRemainingLife = enemyRemainingLife;
                SkillCasts=skillCasts;ManaRemaining=manaRemaining;
                DirectEnemyDamage=directEnemyDamage;AilmentEnemyDamage=ailmentEnemyDamage; }
        }

        public readonly struct ActiveSkillPlan
        {
            public readonly PlayerSkillDefinition Definition;
            public readonly DamageContext Direct,Specialized;
            public readonly float Cost,MaxMana,Regeneration,CastSpacing;
            public readonly int ProjectileCount;
            public ActiveSkillPlan(PlayerSkillDefinition definition,DamageContext direct,DamageContext specialized,
                float cost,float maxMana,float regeneration,int projectileCount=1,float castSpacing=1.5f)
            {
                Definition=definition??throw new ArgumentNullException(nameof(definition));
                Direct=direct;Specialized=specialized;
                Cost=Mathf.Max(0f,cost);MaxMana=Mathf.Max(0f,maxMana);
                Regeneration=Mathf.Max(0f,regeneration);
                ProjectileCount=Mathf.Max(1,projectileCount);CastSpacing=Mathf.Max(.01f,castSpacing);
            }
        }

        private sealed class Fighter
        {
            public StatsComponent Stats;
            public StatsComponent DefenseStats;
            public DamageContext Context;
            public DamageContext? LowContext, HighContext;
            public float Life, MaxLife, Speed, CritChance, CritMultiplier, HitTwice, NextAttack, ChillSlow;
            public float Mana,MaxMana,ManaRegeneration,DirectDealt,AilmentTaken;
            public bool RestoreOnHit;
            public int ChillTurns, ShockStacks;
            public readonly List<Dot> Dots = new List<Dot>();
        }

        private sealed class Dot
        {
            public StatusEffects Effect;
            public StatsComponent Source;
            public float PerTick;
            public int Interval, TurnsUntilNextTick, DurationTurns, TicksLeft;
        }

        public static Outcome Simulate(DamageContext playerContext, StatsComponent playerStats,
            float playerLife, float playerSpeed, float playerCrit, DamageContext enemyContext,
            StatsComponent enemyStats, float enemyLife, float enemySpeed, float enemyCrit,
            StatusEffects[] ailments, int seed, float horizon = 120f,
            DamageContext? playerLow = null, DamageContext? playerHigh = null,
            DamageContext? enemyLow = null, DamageContext? enemyHigh = null)
            => Simulate(playerContext, playerStats, playerStats, playerLife, playerSpeed, playerCrit,
                enemyContext, enemyStats, enemyStats, enemyLife, enemySpeed, enemyCrit,
                ailments, seed, horizon, playerLow, playerHigh, enemyLow, enemyHigh);

        public static Outcome Simulate(DamageContext playerContext, StatsComponent playerStats,
            StatsComponent playerDefenseStats, float playerLife, float playerSpeed, float playerCrit,
            DamageContext enemyContext, StatsComponent enemyStats, StatsComponent enemyDefenseStats,
            float enemyLife, float enemySpeed, float enemyCrit, StatusEffects[] ailments,
            int seed, float horizon = 120f,
            DamageContext? playerLow = null, DamageContext? playerHigh = null,
            DamageContext? enemyLow = null, DamageContext? enemyHigh = null)
        {
            var random = new System.Random(seed);
            var player = Make(playerContext, playerLow, playerHigh, playerStats, playerDefenseStats,
                playerLife, playerSpeed, playerCrit);
            var enemy = Make(enemyContext, enemyLow, enemyHigh, enemyStats, enemyDefenseStats,
                enemyLife, enemySpeed, enemyCrit);
            float time = 0f;
            int hits = 0, ticks = 0, shocks = 0, chills = 0;
            for (int events = 0; events < 10000 && time <= horizon; events++)
            {
                float next = Mathf.Min(player.NextAttack, enemy.NextAttack);
                if (next > horizon || float.IsInfinity(next)) break;
                time = next;
                if (player.NextAttack <= time + .00001f)
                {
                    TickDots(player, true, ref ticks);
                    TickDots(enemy, false, ref ticks);
                    if (player.Life <= 0f || enemy.Life <= 0f) break;
                    Attack(player, enemy, time, random, ailments, ref hits, ref shocks, ref chills);
                }
                if (enemy.Life <= 0f) break;
                if (enemy.NextAttack <= time + .00001f)
                {
                    TickDots(player, false, ref ticks);
                    TickDots(enemy, true, ref ticks);
                    if (player.Life <= 0f || enemy.Life <= 0f) break;
                    Attack(enemy, player, time, random, ailments, ref hits, ref shocks, ref chills);
                }
                if (player.Life <= 0f) break;
            }
            int winner = enemy.Life <= 0f ? 1 : player.Life <= 0f ? -1 : 0;
            return new Outcome(time, winner, hits, ticks, shocks, chills, player.Life, enemy.Life);
        }

        // A player can cast between automatic combat turns. The 1.5-second
        // default is an explicit engaged-player cadence, not a runtime cooldown.
        // All damage, ailment strength, resistance, Shock and Chill calculations
        // still go through the same production calculators as the idle duel.
        public static Outcome SimulateWithSkill(DamageContext playerContext,StatsComponent playerStats,
            float playerLife,float playerSpeed,float playerCrit,DamageContext enemyContext,
            StatsComponent enemyStats,float enemyLife,float enemySpeed,float enemyCrit,
            StatusEffects[] ailments,ActiveSkillPlan plan,int seed,float horizon=120f,
            DamageContext? playerLow=null,DamageContext? playerHigh=null,
            DamageContext? enemyLow=null,DamageContext? enemyHigh=null)
        {
            var random=new System.Random(seed);
            var player=Make(playerContext,playerLow,playerHigh,playerStats,playerStats,playerLife,playerSpeed,playerCrit);
            var enemy=Make(enemyContext,enemyLow,enemyHigh,enemyStats,enemyStats,enemyLife,enemySpeed,enemyCrit);
            player.Mana=player.MaxMana=plan.MaxMana;
            player.ManaRegeneration=plan.Regeneration;
            player.RestoreOnHit=enemy.RestoreOnHit=true;
            float time=0f,cooldownReady=0f,nextCast=0f;
            int hits=0,ticks=0,shocks=0,chills=0,casts=0;
            for(int events=0;events<10000&&time<=horizon;events++)
            {
                float next=Mathf.Min(player.NextAttack,enemy.NextAttack,nextCast);
                if(next>horizon||float.IsInfinity(next))break;
                player.Mana=Mathf.Min(player.MaxMana,player.Mana+player.ManaRegeneration*Mathf.Max(0f,next-time));
                time=next;
                if(nextCast<=time+.00001f)
                {
                    if(player.Mana+.0001f>=plan.Cost)
                    {
                        player.Mana=Mathf.Max(0f,player.Mana-plan.Cost);
                        casts++;
                        Cast(player,enemy,time,random,ailments,plan,ref hits,ref shocks,ref chills);
                        cooldownReady=time+plan.CastSpacing;
                    }
                    nextCast=NextCastTime(player,plan.Cost,cooldownReady,time);
                }
                if(enemy.Life<=0f||player.Life<=0f)break;
                if(player.NextAttack<=time+.00001f)
                {
                    TickDots(player,true,ref ticks);
                    TickDots(enemy,false,ref ticks);
                    if(player.Life<=0f||enemy.Life<=0f)break;
                    Attack(player,enemy,time,random,ailments,ref hits,ref shocks,ref chills);
                    nextCast=NextCastTime(player,plan.Cost,cooldownReady,time);
                }
                if(enemy.Life<=0f||player.Life<=0f)break;
                if(enemy.NextAttack<=time+.00001f)
                {
                    TickDots(player,false,ref ticks);
                    TickDots(enemy,true,ref ticks);
                    if(player.Life<=0f||enemy.Life<=0f)break;
                    Attack(enemy,player,time,random,ailments,ref hits,ref shocks,ref chills);
                }
                if(enemy.Life<=0f||player.Life<=0f)break;
            }
            int winner=enemy.Life<=0f?1:player.Life<=0f?-1:0;
            return new Outcome(time,winner,hits,ticks,shocks,chills,player.Life,enemy.Life,
                casts,player.Mana,player.DirectDealt,enemy.AilmentTaken);
        }

        static float NextCastTime(Fighter fighter,float cost,float cooldownReady,float time)
        {
            if(fighter.Mana+.0001f>=cost)return Mathf.Max(cooldownReady,time);
            if(fighter.ManaRegeneration<=0f)return float.PositiveInfinity;
            return Mathf.Max(cooldownReady,time+(cost-fighter.Mana)/fighter.ManaRegeneration);
        }

        static void Cast(Fighter player,Fighter enemy,float time,System.Random random,
            StatusEffects[] ailments,ActiveSkillPlan plan,ref int hits,ref int shocks,ref int chills)
        {
            var skill=plan.Definition;
            int count=skill.projectile?plan.ProjectileCount:Mathf.Max(1,skill.baseHitCount);
            if(skill.additionalHitsFromShockChance)
            {
                int applications=RollApplications(random,BattleManager.AdjustedChance(player.Stats,StatTypes.ShockChance));
                var state=player.Stats.GetComponent<PassiveKeystoneState>();
                float requirement=state!=null?state.ShockStackRequirementMultiplier:1f;
                count+=Mathf.FloorToInt(applications/Mathf.Max(.01f,requirement));
            }
            for(int i=0;i<count&&enemy.Life>0f;i++)
            {
                Strike(player,enemy,time,random,ailments,ref hits,ref shocks,ref chills,
                    plan.Direct,plan.Specialized,skill);
                if(enemy.Life>0f&&random.NextDouble()<player.HitTwice)
                    Strike(player,enemy,time,random,ailments,ref hits,ref shocks,ref chills,
                        plan.Direct,plan.Specialized,skill);
            }
        }

        private static Fighter Make(DamageContext context, DamageContext? low, DamageContext? high,
            StatsComponent stats, StatsComponent defenseStats,
            float life, float speed, float crit)
        {
            var fighter = new Fighter
            {
                Context = context, LowContext = low, HighContext = high,
                Stats = stats, DefenseStats = defenseStats, Life = life, MaxLife = life,
                Speed = Mathf.Max(.0001f, speed), CritChance = Mathf.Clamp01(crit),
                CritMultiplier = CombatCalculator.BaseCriticalMultiplier + stats.GetStat(StatTypes.CritMult),
                HitTwice = Mathf.Clamp01(BattleManager.AdjustedChance(stats,StatTypes.ChanceToHitTwice))
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
            StatusEffects[] ailments, ref int hits, ref int shocks, ref int chills,
            DamageContext? contextOverride=null,DamageContext? specializedBasis=null,
            PlayerSkillDefinition skill=null)
        {
            var context = contextOverride.HasValue?Copy(contextOverride.Value):RollContext(source, random);
            var special=specializedBasis.HasValue?Copy(specializedBasis.Value):context;
            if (random.NextDouble() < source.CritChance)
            {
                EnemyScalingMath.ScaleOutgoing(context, source.CritMultiplier);
                if(specializedBasis.HasValue)EnemyScalingMath.ScaleOutgoing(special,source.CritMultiplier);
            }
            float dealt = CombatCalculator.CalculateFinalDamage(context, source.Stats, target.DefenseStats);
            source.DirectDealt+=Mathf.Min(Mathf.Max(0f,dealt),Mathf.Max(0f,target.Life));
            target.Life -= dealt;
            hits++;
            if(dealt>0f&&source.RestoreOnHit)
            {
                source.Life=Mathf.Min(source.MaxLife,source.Life+source.Stats.GetStat(StatTypes.LifeOnHit));
                source.Mana=Mathf.Min(source.MaxMana,source.Mana+source.Stats.GetStat(StatTypes.ManaOnHit));
            }
            if (target.Life <= 0f) return;
            foreach (StatusEffects effect in ailments)
            {
                if(effect==null)continue;
                bool matching=skill!=null&&skill.specializedAilment==effect.Ailment;
                var basis=matching?special:context;
                if(AilmentCalculator.GetSourceHitDamage(effect,basis)<=0f)continue;
                float chance=ApplicationChance(effect,source.Stats,target.DefenseStats);
                int applications=RollApplications(random,chance)
                    +(matching?Mathf.Max(0,skill.guaranteedAilmentApplications):0);
                if (applications <= 0) continue;
                float coefficient=matching&&effect.Ailment==StatusEffects.AilmentKind.Ignite
                    ?skill.absoluteAilmentCoefficient:-1f;
                AilmentCalculator.ComputeAilmentFromHit(effect,basis,source.Stats,
                    out float strength,out int count,out int interval,coefficient);
                if (strength <= 0f) continue;
                int cap = effect.Ailment switch
                {
                    StatusEffects.AilmentKind.Poison => int.MaxValue,
                    StatusEffects.AilmentKind.Bleed => 5,
                    StatusEffects.AilmentKind.Ignite => 1,
                    _ => Mathf.Max(1, effect.MaxStacks)
                };
                int activeForEffect = 0;
                foreach (Dot existing in target.Dots) if (existing.Effect == effect) activeForEffect++;
                for (int i = 0; i < applications; i++)
                {
                    var incoming = new Dot { Effect = effect, Source = source.Stats,
                        PerTick = strength, Interval = interval,
                        TurnsUntilNextTick = interval > 0 ? interval : 1,
                        DurationTurns = count * Mathf.Max(1, interval), TicksLeft = count };
                    if (activeForEffect < cap) { target.Dots.Add(incoming); activeForEffect++; continue; }
                    int weakest = -1;
                    float weakestTotal = float.PositiveInfinity;
                    for (int index = 0; index < target.Dots.Count; index++)
                    {
                        Dot old = target.Dots[index];
                        if (old.Effect != effect) continue;
                        float remaining = RemainingDotDamage(old, target.DefenseStats);
                        if (remaining < weakestTotal) { weakestTotal = remaining; weakest = index; }
                    }
                    if (weakest >= 0 && RemainingDotDamage(incoming, target.DefenseStats) > weakestTotal + .0001f)
                        target.Dots[weakest] = incoming;
                }
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
                var shockKeystones=source.Stats.GetComponent<PassiveKeystoneState>();
                int threshold=Mathf.Max(1,Mathf.CeilToInt(5f
                    *(shockKeystones?.ShockStackRequirementMultiplier??1f)));
                if(source.Stats.GetComponent<PlayerController>()!=null&&RelicInventory.Instance!=null)
                    threshold=Mathf.Max(3,threshold-RelicInventory.Instance.ShockThresholdReduction);
                float coefficient=Mathf.Min(1f,.5f
                    *(1f+source.Stats.GetStat(StatTypes.ShockEffect))
                    *(shockKeystones?.ShockTriggeredHitMultiplier??1f));
                target.ShockStacks += shockApplications;
                while (target.ShockStacks >= threshold)
                {
                    target.ShockStacks -= threshold;
                    target.Life -= lightning * coefficient;
                    shocks++;
                }
            }
            float cold = CombatCalculator.CalculateFinalElementDamage(context, Element.Cold,
                source.Stats, target.DefenseStats);
            float chillResistance = Mathf.Clamp(target.DefenseStats.GetStat(StatTypes.ChillRes)
                + target.DefenseStats.GetStat(StatTypes.AllAilmentRes), -.9f, .9f);
            if (cold > 0f && RollApplications(random,
                BattleManager.AdjustedChance(source.Stats, StatTypes.ChillChance)
                    * (1f - chillResistance))+(skill?.guaranteedAdditionalChill??0)>0)
            {
                var keystones=source.Stats.GetComponent<PassiveKeystoneState>();
                float cap=.3f+(keystones?.DeepFreezeMaximumEffectIncrease??0f)
                    +(source.Stats.GetComponent<PlayerController>()!=null
                        ?RelicInventory.Instance?.MaximumChillSlowIncrease??0f:0f);
                float slow = BattleManager.CalculateChillSlow(cold, target.MaxLife,
                    source.Stats.GetStat(StatTypes.ChillEffect),
                    keystones?.ChillEffectMultiplier??1f,cap) * (1f - chillResistance);
                target.ChillSlow = Mathf.Max(target.ChillSlow, Mathf.Min(cap, slow));
                target.ChillTurns = Mathf.Max(target.ChillTurns, Mathf.Max(1,
                    Mathf.RoundToInt((4f + source.Stats.GetRawStat(StatTypes.ChillDuration))
                        * (1f - chillResistance))));
                chills++;
            }
        }

        private static float ApplicationChance(StatusEffects effect, StatsComponent source,
            StatsComponent target)
        {
            StatTypes chance = effect.Ailment switch
            {
                StatusEffects.AilmentKind.Poison => StatTypes.PoisonChance,
                StatusEffects.AilmentKind.Bleed => StatTypes.BleedChance,
                _ => StatTypes.IgniteChance
            };
            return BattleManager.AdjustForApplicationResistance(effect,
                BattleManager.AdjustedChance(source,chance),source,target);
        }

        private static int RollApplications(System.Random random, float chance)
        {
            chance = Mathf.Max(0f, chance);
            int guaranteed = Mathf.FloorToInt(chance);
            return guaranteed + (random.NextDouble() < chance - guaranteed ? 1 : 0);
        }

        private static float RemainingDotDamage(Dot dot, StatsComponent defense)
            => dot.TicksLeft * CombatCalculator.CalculateAilmentTickDamage(dot.PerTick,
                dot.Effect, dot.Source, defense);

        private static void TickDots(Fighter fighter, bool afflictedActorTurn, ref int ticks)
        {
            for (int i = fighter.Dots.Count - 1; i >= 0; i--)
            {
                Dot dot = fighter.Dots[i];
                if (dot.Effect.Ailment != StatusEffects.AilmentKind.Poison
                    && dot.Effect.Ailment != StatusEffects.AilmentKind.GenericDot
                    && !afflictedActorTurn) continue;
                dot.DurationTurns--;
                int due = 0;
                if (dot.Interval > 0)
                {
                    if (--dot.TurnsUntilNextTick <= 0)
                    { due = 1; dot.TurnsUntilNextTick = dot.Interval; }
                }
                else due = 1 - dot.Interval;
                while (due-- > 0 && dot.TicksLeft > 0)
                {
                    float damage=CombatCalculator.CalculateAilmentTickDamage(dot.PerTick,
                        dot.Effect, dot.Source, fighter.DefenseStats);
                    fighter.AilmentTaken+=Mathf.Min(damage,Mathf.Max(0f,fighter.Life));
                    fighter.Life-=damage;
                    ticks++;
                    dot.TicksLeft--;
                }
                if (dot.TicksLeft <= 0 || dot.DurationTurns <= 0) fighter.Dots.RemoveAt(i);
            }
        }

        private static DamageContext RollContext(Fighter source, System.Random random)
        {
            if (!source.LowContext.HasValue || !source.HighContext.HasValue) return Copy(source.Context);
            var low = source.LowContext.Value;
            var high = source.HighContext.Value;
            var elements = new List<Element>();
            foreach (var hit in low.Hits) if (!elements.Contains(hit.Element)) elements.Add(hit.Element);
            foreach (var hit in high.Hits) if (!elements.Contains(hit.Element)) elements.Add(hit.Element);
            bool variable = false;
            foreach (Element element in elements)
                if (Mathf.Abs(Amount(low, element) - Amount(high, element)) > .00001f) { variable = true; break; }
            if (!variable) return Copy(source.Context);
            float fraction = (float)random.NextDouble(); // one seeded weapon roll per actual strike
            var rolled = new DamageContext(elements.Count) { Scopes = source.Context.Scopes };
            foreach (Element element in elements)
            {
                float minimum = Amount(low, element);
                rolled.AddDamage(element, minimum + (Amount(high, element) - minimum) * fraction);
            }
            return rolled;
        }

        private static float Amount(DamageContext context, Element element)
        {
            if (context.Hits == null) return 0f;
            foreach (var hit in context.Hits) if (hit.Element == element) return hit.Amount;
            return 0f;
        }

        private static DamageContext Copy(DamageContext source)
        {
            var copy = new DamageContext(source.Hits.Count) { Scopes = source.Scopes };
            foreach (var hit in source.Hits) copy.AddDamage(hit.Element, hit.Amount);
            return copy;
        }
    }
}
