// Pure event-driven combat sequencing used by the Combat Lab. Production wrappers
// and the laboratory share the deterministic proc, timing, mitigation, Rage,
// behavior, phase and weapon-mechanic services referenced here.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.CombatSimulation
{
    public static class CombatDeterministicRules
    {
        public static int RollOverflowApplications(float chance,Func<float> roll)
        {chance=Mathf.Max(0,chance);int guaranteed=Mathf.FloorToInt(chance);float remainder=chance-guaranteed;return guaranteed+(remainder>0&&roll!=null&&roll()<remainder?1:0);}
        public static long DeriveSeed(long seed,int fightIndex)
        {unchecked{ulong x=(ulong)seed+0x9E3779B97F4A7C15UL*(ulong)(fightIndex+1);x=(x^(x>>30))*0xBF58476D1CE4E5B9UL;x=(x^(x>>27))*0x94D049BB133111EBUL;return (long)(x^(x>>31));}}
        public static float AttackInterval(float attacksPerSecond,float chill=0)=>1f/Mathf.Max(.0001f,attacksPerSecond*Mathf.Max(.01f,1-chill));
        public static int ShockBarrageHits(float combinedShock,int maximum=6)=>Mathf.Clamp(1+Mathf.FloorToInt(Mathf.Max(0,combinedShock)/.20f),1,Mathf.Max(1,maximum));
    }

    public enum CombatOutcome{PlayerWin,PlayerLoss,Timeout,SimulationError}
    public enum PlayerActionPolicy{BasicOnly,SkillsWhenAvailable,Skill1Priority,Skill2Priority,AlternateSkills,ManaConservative}
    public enum RageFinisherPolicy{Never,Immediately,NextSkill,Skill1Only,Skill2Only,TargetBelowThreshold}
    public enum EnemySamplingMode{Representative,Population}
    [Flags] public enum CombatTraceFilter{None=0,PlayerAttack=1,EnemyAttack=2,Skill=4,Damage=8,Projectile=16,Ailment=32,Heal=64,Mana=128,Rage=256,Status=512,BossPhase=1024,Behavior=2048,Buff=4096,All=int.MaxValue}
    public enum CombatEventType{PlayerAttackReady,EnemyAttackReady,SkillReady,ProjectileLaunch,ProjectileImpact,AilmentApply,AilmentTick,Regeneration,Rage,BossPhase,BehaviorDecision,Heal,Mana,Status,Death,Safety}

    [Serializable] public sealed class CombatDamageSnapshot
    {public float physical,fire,cold,lightning,voidDamage;public float Total=>physical+fire+cold+lightning+voidDamage;public CombatDamageSnapshot Clone()=>new(){physical=physical,fire=fire,cold=cold,lightning=lightning,voidDamage=voidDamage};public void Scale(float x){physical*=x;fire*=x;cold*=x;lightning*=x;voidDamage*=x;}}
    [Serializable] public sealed class CombatSkillSnapshot
    // Scope data is explicit: spell reactions must not depend on equipped weapon.
    {public string id,name;public PlayerSkillCastMode castMode;public WeaponSkillEffect effect;public float manaCost,cooldown,hitMultiplier=1,secondaryMultiplier,ailmentBasisMultiplier=1,baseProjectileSpeed=1,scopeMultiplier=1;public int hits=1,projectiles=1;public bool projectile,magic,supportsPrecision,suppressDirectDamage;public Element conversionElement;public float conversion;public StatusEffects.AilmentKind specializedAilment;public int guaranteedAilmentApplications,guaranteedAdditionalChill;}
    [Serializable] public sealed class CombatEnemySkillSnapshot
    {public string id,name;public EnemySkillKind kind;public Element element;public float damageMultiplier=1;public int hits=1;}
    [Serializable] public sealed class CombatantSnapshot
    {
        public string id,name,classId,subclassId,weaponTypeId,behaviorProfileId;public bool player,boss,challengeBoss,hasRageFinisher;public int corruption;
        public float maximumLife,maximumMana,attackSpeed,attackSpeedBonus,critChance,critMultiplier=CombatCalculator.BaseCriticalMultiplier,hitTwiceChance,armour,physicalDamageReduction;
        public float fireResistance,coldResistance,lightningResistance,voidResistance,maximumResistance=CombatCalculator.BaseMaximumResistance;
        public float maximumFireResistance,maximumColdResistance,maximumLightningResistance,maximumVoidResistance,basicScopeMultiplier=1;
        public float physicalPenetration,firePenetration,coldPenetration,lightningPenetration,voidPenetration;
        public float lifeRegeneration,manaRegeneration,lifeOnHit,manaOnHit,lifeOnKill,manaOnKill;
        public float poisonChance,bleedChance,igniteChance,shockChance,chillChance,poisonMagnitude,bleedMagnitude,igniteMagnitude,shockEffect,chillEffect;
        // Production adapters supply per-element, per-hit bases. Older laboratory
        // presets retain the fixed-magnitude fallback for serialization compatibility.
        public float[] poisonHitFactors,poisonFullHitFactors,bleedHitFactors,igniteHitFactors;
        public int poisonExtraTicks,bleedExtraTicks,igniteExtraTicks;
        // Legacy ailment-resistance fields remain for serialized laboratory presets.
        // New simulation uses only the explicit effect-reduction fields below.
        public float poisonResistance,bleedResistance,igniteResistance,shockResistance,chillResistance,allAilmentResistance,poisonSpeed;
        public float reducedShockEffect,reducedChillEffect;
        public float projectileTravelTime=1,precisionChance,precisionMultiplier=1.5f,rageGeneration,rageEffect,rageDecayReduction,auraEffect;
        public float revengeEffect,poisonLifeLeech,lifeRecoveryEffect;
        public float wouldBeLifeRegenerationFraction,baseCritBeforeIncreased,increasedCrit,localCritMultiplier=1,cullingStrike;
        public List<PassiveKeystone> classKeystones=new();
        public bool Has(PassiveKeystone key)=>classKeystones!=null&&classKeystones.Contains(key);
        public bool physicalAuraAccess,fireAuraAccess,coldAuraAccess,lightningAuraAccess,voidAuraAccess;
        public int projectileCount=1,maximumBleedStacks=5,maximumIgniteStacks=1,maximumShockInstances=1;
        public SubclassProjectileMode projectileMode=SubclassProjectileMode.Volley;
        public CombatDamageSnapshot basicDamage=new();public List<CombatSkillSnapshot> skills=new();public List<CombatEnemySkillSnapshot> enemySkills=new();
        public EnemyBehaviorProfileDefinition behavior;public BossPhaseProfile phases;
    }
    [Serializable] public sealed class CombatSimulationConfig
    {
        public long seed=51001;public float maximumDuration=120;public int maximumEvents=100000,maximumSameTimestampEvents=256,maximumTriggerDepth=16;
        public PlayerActionPolicy actionPolicy=PlayerActionPolicy.SkillsWhenAvailable;public RageFinisherPolicy ragePolicy=RageFinisherPolicy.Immediately;[Range(0,1)]public float rageTargetThreshold=.25f;[Range(0,1)]public float manaReserve=.15f;public bool retainTrace=true;public int traceLimit=10000;
        public bool disablePlayerLifeRecovery;
        public bool enableSkill1=true,enableSkill2=true;
        public float startingPlayerLife=-1f,startingPlayerMana=-1f;
    }
    [CreateAssetMenu(menuName="Black-Cube/Balance/Player Combat Policy",fileName="SO_CombatPolicy")]
    public sealed class PlayerCombatPolicySO:ScriptableObject
    {public int version=1;public PlayerActionPolicy actionPolicy=PlayerActionPolicy.SkillsWhenAvailable;public RageFinisherPolicy ragePolicy=RageFinisherPolicy.Immediately;[Range(0,1)]public float minimumManaReserve=.15f;[Range(0,1)]public float finisherTargetLife=.25f;public void ApplyTo(CombatSimulationConfig config){if(config==null)return;config.actionPolicy=actionPolicy;config.ragePolicy=ragePolicy;config.manaReserve=minimumManaReserve;config.rageTargetThreshold=finisherTargetLife;}}
    [Serializable] public sealed class CombatTraceEvent
    {public int index;public float time;public CombatEventType type;public CombatTraceFilter category;public string source,target,action,reason,ruleId,phase;public float amount,lifeBefore,lifeAfter,manaBefore,manaAfter,rageBefore,rageAfter,roll,chance;public int projectileIndex,stackCount;}
    [Serializable] public sealed class CombatContribution{public string id;public double total,effective,overheal;public int count;}
    [Serializable] public sealed class CombatAilmentMetric{public string id;public int applications,maxStacks,criticalApplications;public double damage,uptime,magnitudeTotal,stackTime,maximumEffect;}
    [Serializable] public sealed class CombatSkillMetric{public string id;public int activations,resolves,manaFailures,cooldownBypasses,queuedRepeats;public double damage,lastUse=-1,intervalTotal,readyStarvedTime;}
    [Serializable] public sealed class CombatSimulationResult
    {
        public long seed;public string fingerprint,error;public CombatOutcome outcome;public float duration,playerLife,enemyLife,playerMana,playerRage,minMana,averageMana,averageRage,timeAtZeroMana,timeAtMaximumRage;
        public int eventCount,playerAttacks,enemyAttacks,enemyAttacksSkipped,projectilesLaunched,projectilesImpacted,projectilesFizzled,precisionCount,criticalCount,hitTwiceCount,rageFinisherUses,ruptureTriggers,executionTriggers,rapidFlurryUses,rapidFlurryHits,shockBarrageUses,shockBarrageHits,eruptionTriggers,comboResets,maximumCombo,fullLifeBonusHits,injuredBonusHits,shatters;
        public float executionDamage,openingDamage,rageFinisherDamage,rageFinisherCriticalDamage,physicalAuraIntensity,fireAuraIntensity,coldAuraIntensity,lightningAuraIntensity;
        public float manaSpent,manaRegenerated,manaOnHit,rageGenerated,rageSpent,rageLostToDecay,projectileTravelTotal;
        public List<CombatContribution> keystoneTelemetry=new();
        public List<CombatTraceEvent> trace=new();public List<CombatContribution> damage=new(),damageByType=new(),healing=new();public List<CombatAilmentMetric> ailments=new();public List<CombatSkillMetric> skills=new(),enemySkills=new();public List<CombatContribution> phaseTime=new();
        public bool Won=>outcome==CombatOutcome.PlayerWin;
    }

    sealed class DeterministicCombatRandom
    {ulong state;public DeterministicCombatRandom(long seed){state=(ulong)seed;if(state==0)state=0xA0761D6478BD642FUL;}public float Value(){state+=0x9E3779B97F4A7C15UL;ulong z=state;z=(z^(z>>30))*0xBF58476D1CE4E5B9UL;z=(z^(z>>27))*0x94D049BB133111EBUL;return (float)((z^(z>>31))>>40)/(1<<24);}public int Range(int n)=>n<=1?0:Mathf.Min(n-1,(int)(Value()*n));}
    sealed class Scheduled{public float time;public int order;public CombatEventType type;public bool playerSource;public int skill=-1,projectile;public Dot dot;public float revengeMultiplier=1;}
    sealed class Dot{public string id,source;public bool playerSource,scheduled,infinite;public float damage;public int ticks,stacks;public float interval,next;}
    sealed class State
    {
        public CombatantSnapshot data;public float life,mana,rage,rageDecayEligibleAt,chill,chillExpiresAt,phaseStartedAt;public bool frozen,enemyHasActed,finisherArmed,disableLifeRecovery;public int combo,alternate;public string phase;
        public readonly List<Dot> dots=new();public readonly List<(float strength,float expires)> shocks=new();public readonly FiveAuraState auras=new();public float revengeFraction,eventRevengeMultiplier=1;public EnemyBehaviorRuntimeState behavior=new();
        public bool fractured,fractureOnThaw,stealth;
    }

    public static class HeadlessCombatSimulator
    {
        const float Epsilon=.00001f;
        public static CombatSimulationResult Run(CombatantSnapshot player,CombatantSnapshot enemy,CombatSimulationConfig config)
        {
            if(player==null||enemy==null||config==null)throw new ArgumentNullException(player==null?nameof(player):enemy==null?nameof(enemy):nameof(config));
            float initialLife=config.startingPlayerLife<0?player.maximumLife:Mathf.Clamp(config.startingPlayerLife,0,player.maximumLife);
            float initialMana=config.startingPlayerMana<0?player.maximumMana:Mathf.Clamp(config.startingPlayerMana,0,player.maximumMana);
            var result=new CombatSimulationResult{seed=config.seed,playerLife=initialLife,enemyLife=enemy.maximumLife,playerMana=initialMana,minMana=initialMana};
            var p=new State{data=player,life=initialLife,mana=initialMana,disableLifeRecovery=config.disablePlayerLifeRecovery};var e=new State{data=enemy,life=enemy.maximumLife,mana=enemy.maximumMana};var rng=new DeterministicCombatRandom(config.seed);var queue=new List<Scheduled>();int order=0,same=0;float now=0,last=0;
            void Add(float time,CombatEventType type,bool playerSource,int skill=-1,int projectile=0,Dot dot=null)=>queue.Add(new Scheduled{time=Mathf.Max(now,time),order=order++,type=type,playerSource=playerSource,skill=skill,projectile=projectile,dot=dot,revengeMultiplier=p.eventRevengeMultiplier});
            UpdatePhase(e,0,result,config);Add(player.subclassId==SubclassIds.ThiefAssassin?0:CombatDeterministicRules.AttackInterval(player.attackSpeed),CombatEventType.PlayerAttackReady,true);Add(CombatDeterministicRules.AttackInterval(enemy.attackSpeed),CombatEventType.EnemyAttackReady,false);
            for(int i=0;i<player.skills.Count;i++)if((i==0?config.enableSkill1:config.enableSkill2)&&config.actionPolicy!=PlayerActionPolicy.BasicOnly&&(player.skills[i].castMode==PlayerSkillCastMode.AutoCooldown||player.skills[i].castMode==PlayerSkillCastMode.ImmediateCooldown))Add(Mathf.Max(.01f,player.skills[i].cooldown),CombatEventType.SkillReady,true,i);
            try
            {
                while(queue.Count>0&&result.eventCount<config.maximumEvents)
                {
                    queue.Sort((a,b)=>{int x=a.time.CompareTo(b.time);return x!=0?x:a.order.CompareTo(b.order);});var ev=queue[0];queue.RemoveAt(0);if(ev.time>config.maximumDuration){now=config.maximumDuration;break;}same=Mathf.Abs(ev.time-now)<Epsilon?same+1:1;if(same>config.maximumSameTimestampEvents)throw new InvalidOperationException("Maximum same-timestamp event count exceeded.");
                    last=now;now=ev.time;Advance(p,e,now-last,now,result);result.eventCount++;if(!Finite(p)||!Finite(e))throw new InvalidOperationException("Non-finite combat state.");
                    if(ev.type==CombatEventType.AilmentTick){TickDot(ev.dot,p,e,now,result,config,Add);if(Dead(p,e))break;continue;}
                    State source=ev.playerSource?p:e,target=ev.playerSource?e:p;if(source.life<=0||target.life<=0)continue;
                    switch(ev.type)
                    {
                        case CombatEventType.PlayerAttackReady:
                            PlayerAttack(p,e,now,rng,result,config,Add);Add(now+PlayerAttackInterval(p,e),CombatEventType.PlayerAttackReady,true);break;
                        case CombatEventType.EnemyAttackReady:
                            EnemyAttack(e,p,now,rng,result,config,Add);Add(now+CombatDeterministicRules.AttackInterval(e.data.attackSpeed,e.chill),CombatEventType.EnemyAttackReady,false);break;
                        case CombatEventType.SkillReady:
                            ResolveCooldownSkill(p,e,ev.skill,now,rng,result,config,Add);break;
                        case CombatEventType.ProjectileImpact:
                            if(target.life<=0){result.projectilesFizzled++;Trace(result,config,now,CombatEventType.ProjectileImpact,CombatTraceFilter.Projectile,source,target,"Projectile fizzled","Target already dead",projectile:ev.projectile);break;}
                            source.eventRevengeMultiplier=ev.revengeMultiplier;ResolveHit(source,target,ev.skill,now,rng,result,config,ev.projectile);source.eventRevengeMultiplier=1;result.projectilesImpacted++;break;
                    }
                    ScheduleDots(p,e,Add);UpdatePhase(e,now,result,config);if(Dead(p,e)){result.projectilesFizzled+=queue.Count(x=>x.type==CombatEventType.ProjectileImpact);break;}
                }
                ClosePhase(e,now,result);result.duration=now;result.playerLife=Mathf.Max(0,p.life);result.enemyLife=Mathf.Max(0,e.life);result.playerMana=p.mana;result.playerRage=p.rage;if(now>0){result.averageMana/=now;result.averageRage/=now;}
                result.physicalAuraIntensity=p.auras.Intensity(0);
                result.fireAuraIntensity=p.auras.Intensity(1);
                result.coldAuraIntensity=p.auras.Intensity(2);
                result.lightningAuraIntensity=p.auras.Intensity(3);
                result.outcome=e.life<=0?CombatOutcome.PlayerWin:p.life<=0?CombatOutcome.PlayerLoss:result.eventCount>=config.maximumEvents?CombatOutcome.SimulationError:CombatOutcome.Timeout;
                if(result.outcome==CombatOutcome.SimulationError)result.error="Maximum event count exceeded.";
            }
            catch(Exception ex){result.outcome=CombatOutcome.SimulationError;result.error=ex.ToString();result.duration=now;Trace(result,config,now,CombatEventType.Safety,CombatTraceFilter.Status,p,e,"SIMULATION ERROR",ex.Message);}
            return result;
        }

        static void Advance(State p,State e,float delta,float now,CombatSimulationResult r)
        {
            if(delta<=0)return;float pm=p.mana;p.mana=Mathf.Min(p.data.maximumMana,p.mana+p.data.manaRegeneration*delta);r.manaRegenerated+=p.mana-pm;r.averageMana+=((pm+p.mana)*.5f)*delta;r.averageRage+=p.rage*delta;if(p.mana<=Epsilon)r.timeAtZeroMana+=delta;if(p.rage>=100-Epsilon)r.timeAtMaximumRage+=delta;
            Heal(p,p.data.lifeRegeneration*delta,"Life Regeneration",r);Heal(e,e.data.lifeRegeneration*delta*(p.data.subclassId==SubclassIds.PriestLight?.5f:1f),"Enemy Regeneration",r);
            if(p.rage>0&&p.data.weaponTypeId==WeaponTypeIds.TwoHandedAxe){float decayTime=Mathf.Max(0,now-Mathf.Max(now-delta,p.data.Has(PassiveKeystone.BarbarianFullRage)?now-delta:p.rageDecayEligibleAt));float loss=WeaponMechanicProfile.RageDecayPerSecond*(1-Mathf.Clamp01(p.data.rageDecayReduction))*decayTime;loss=Mathf.Min(loss,p.rage);p.rage-=loss;r.rageLostToDecay+=loss;}
            for(int i=0;i<p.shocks.Count;i++)p.shocks[i]=(p.shocks[i].strength,p.shocks[i].expires-delta);for(int i=0;i<e.shocks.Count;i++)e.shocks[i]=(e.shocks[i].strength,e.shocks[i].expires-delta);p.shocks.RemoveAll(x=>x.expires<=0);e.shocks.RemoveAll(x=>x.expires<=0);if(now>=p.chillExpiresAt)p.chill=0;if(now>=e.chillExpiresAt)e.chill=0;
            AccumulateAilments(p,delta,r);AccumulateAilments(e,delta,r);
            if(e.fractured)KeyMetric(r,"Fracture uptime seconds",delta);
            if(p.data.Has(PassiveKeystone.BarbarianFullRage)&&p.rage>=100-Epsilon)KeyMetric(r,"Full-Rage uptime seconds",delta);
            if(p.data.Has(PassiveKeystone.PriestAura))
            {
                int count=Enumerable.Range(0,5).Count(i=>p.auras.Intensity(i)>0);
                KeyMetric(r,"Active aura count × seconds",count*delta);
                KeyMetric(r,"Aura Effect from keystone × seconds",count*PassiveKeystoneState.Value(PassiveKeystone.PriestAura)*delta);
            }
        }
        static void PlayerAttack(State p,State e,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c,Action<float,CombatEventType,bool,int,int,Dot> add)
        {
            p.auras.Tick();e.auras.Tick();p.eventRevengeMultiplier=p.data.revengeEffect>0?GenericPassiveMechanics.RevengeMultiplier(p.revengeFraction,p.data.revengeEffect):1;p.revengeFraction=0;
            int skill=ChooseSkill(p,e,c);r.playerAttacks++;if(skill>=0&&p.data.skills[skill].castMode==PlayerSkillCastMode.QueuedAttackReplacement){var s=p.data.skills[skill];if(!SpendMana(p,s,r,now,c)){skill=-1;}else Metric(r.skills,s.id).activations++;}
            LaunchOrHit(p,e,skill,now,rng,r,c,add);p.eventRevengeMultiplier=1;if(skill>=0)RecordResolve(Metric(r.skills,p.data.skills[skill].id),now);
        }
        static void EnemyAttack(State e,State p,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c,Action<float,CombatEventType,bool,int,int,Dot> add)
        {
            p.auras.Tick();e.auras.Tick();
            if(e.frozen){e.frozen=false;if(e.fractureOnThaw){e.fractured=true;e.fractureOnThaw=false;}r.enemyAttacksSkipped++;Trace(r,c,now,CombatEventType.Status,CombatTraceFilter.Status,e,p,"Enemy attack skipped","Freeze consumed");return;}
            if(p.stealth){p.stealth=false;if(rng.Value()<ClassKeystoneCatalog.Get(PassiveKeystone.ThiefStealth).secondary){KeyMetric(r,"Stealth first-attack misses",1);return;}}
            r.enemyAttacks++;int skill=-1;
            if(e.data.behavior!=null&&e.data.enemySkills.Count>0){e.behavior.completedAttacks=r.enemyAttacks-1;e.behavior.selfLifeFraction=e.life/e.data.maximumLife;e.behavior.playerLifeFraction=p.life/p.data.maximumLife;e.behavior.bossPhaseId=e.phase;e.behavior.random01=rng.Value();var decision=EnemyBehaviorResolver.Resolve(e.data.behavior,e.behavior,id=>e.data.enemySkills.Any(x=>x.id==id));skill=e.data.enemySkills.FindIndex(x=>x.id==decision.selectedSkillId);Trace(r,c,now,CombatEventType.BehaviorDecision,CombatTraceFilter.Behavior,e,p,decision.selectedSkillId,decision.reason,decision.selectedRuleId,e.phase,e.behavior.random01);}
            ResolveHit(e,p,skill,now,rng,r,c);e.enemyHasActed=true;p.enemyHasActed=true;if(p.combo>0)r.comboResets++;p.combo=0;
        }
        static int ChooseSkill(State p,State e,CombatSimulationConfig c)
        {
            var candidates=p.data.skills.Select((x,i)=>(x,i)).Where(x=>(x.i==0?c.enableSkill1:c.enableSkill2)&&x.x.castMode==PlayerSkillCastMode.QueuedAttackReplacement&&p.mana+Epsilon>=x.x.manaCost).ToList();if(c.actionPolicy==PlayerActionPolicy.BasicOnly||candidates.Count==0)return-1;if(c.actionPolicy==PlayerActionPolicy.ManaConservative)candidates=candidates.Where(x=>p.mana-x.x.manaCost>=p.data.maximumMana*c.manaReserve).ToList();if(candidates.Count==0)return-1;if(c.actionPolicy==PlayerActionPolicy.Skill2Priority)return candidates.OrderByDescending(x=>x.i).First().i;if(c.actionPolicy==PlayerActionPolicy.AlternateSkills){int x=candidates[p.alternate++%candidates.Count].i;return x;}return candidates.OrderBy(x=>x.i).First().i;
        }
        static void ResolveCooldownSkill(State p,State e,int index,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c,Action<float,CombatEventType,bool,int,int,Dot> add)
        {
            if(index<0||index>=p.data.skills.Count)return;var s=p.data.skills[index];var metric=Metric(r.skills,s.id);metric.activations++;if(!SpendMana(p,s,r,now,c)){metric.manaFailures++;metric.readyStarvedTime+=.1;add(now+.1f,CombatEventType.SkillReady,true,index,0,null);return;}LaunchOrHit(p,e,index,now,rng,r,c,add);RecordResolve(metric,now);bool bypass=p.data.subclassId==SubclassIds.MageCooldown&&rng.Value()<SubclassBalanceProfile.CooldownIgnoreChance;if(bypass)metric.cooldownBypasses++;add(now+(bypass ? .01f : Mathf.Max(.01f,s.cooldown)),CombatEventType.SkillReady,true,index,0,null);
        }
        static bool SpendMana(State p,CombatSkillSnapshot s,CombatSimulationResult r,float now,CombatSimulationConfig c)
        {if(p.mana+Epsilon<s.manaCost){Trace(r,c,now,CombatEventType.Mana,CombatTraceFilter.Mana,p,null,s.name,"Insufficient Mana",amount:s.manaCost);return false;}float before=p.mana;p.mana-=s.manaCost;r.manaSpent+=s.manaCost;r.minMana=Mathf.Min(r.minMana,p.mana);Trace(r,c,now,CombatEventType.Mana,CombatTraceFilter.Mana,p,null,s.name,"Mana spent",amount:s.manaCost,manaBefore:before,manaAfter:p.mana);return true;}
        static void LaunchOrHit(State s,State t,int skill,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c,Action<float,CombatEventType,bool,int,int,Dot> add)
        {
            if(skill>=0&&s.data.Has(PassiveKeystone.MageSelfBolt)&&s.data.skills[skill].magic)ResolveSpellBolts(s,t,s.data.skills[skill],now,rng,r,c);
            bool projectile=skill>=0?s.data.skills[skill].projectile:s.data.weaponTypeId==WeaponTypeIds.Bow;
            // The adapter has already expanded Double Volley to its final count.
            int count=skill>=0?Mathf.Max(1,s.data.skills[skill].projectiles):Mathf.Max(1,s.data.projectileCount);
            if(s.data.subclassId==SubclassIds.RangerProjectile&&s.data.projectileMode==SubclassProjectileMode.Focused&&count>1)count=1;
            if(!projectile){for(int i=0;i<count;i++)ResolveHit(s,t,skill,now,rng,r,c,i);return;}
            if(s.data.Has(PassiveKeystone.RangerSplit)){KeyMetric(r,"Projectiles before split",count);count*=Mathf.RoundToInt(PassiveKeystoneState.Value(PassiveKeystone.RangerSplit));KeyMetric(r,"Projectiles after split",count);}
            for(int i=0;i<count;i++)
            {
                float travel=skill>=0?Mathf.Max(WeaponMechanicProfile.MinimumProjectileTravelTime,s.data.projectileTravelTime/Mathf.Max(.01f,s.data.skills[skill].baseProjectileSpeed)):s.data.projectileTravelTime;
                float launch=now+i*WeaponMechanicProfile.ProjectileBarrageSpacing,impact=launch+travel;
                r.projectilesLaunched++;r.projectileTravelTotal+=travel;
                Trace(r,c,launch,CombatEventType.ProjectileLaunch,CombatTraceFilter.Projectile,s,t,skill>=0?s.data.skills[skill].name:"Basic Projectile","Snapshot at launch",projectile:i);
                add(impact,CombatEventType.ProjectileImpact,s.data.player,skill,i,null);
            }
        }
        static void ResolveHit(State s,State t,int skill,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c,int projectile=0)
        {
            var packet=s.data.basicDamage.Clone();string action="Basic Attack";float multiplier=1;int hits=1;CombatSkillSnapshot ps=null;CombatEnemySkillSnapshot es=null;
            if(s.data.player&&skill>=0){ps=s.data.skills[skill];action=ps.name;multiplier=ps.effect==WeaponSkillEffect.ArmourStrike?WeaponMechanicProfile.ArmourStrikeMultiplier(s.data.armour):ps.hitMultiplier;hits=Mathf.Max(1,ps.hits);if(ps.effect==WeaponSkillEffect.RapidFlurry)hits=WeaponMechanicProfile.RapidFlurryHits(s.data.attackSpeedBonus);if(ps.effect==WeaponSkillEffect.ShockBarrage)hits=CombatDeterministicRules.ShockBarrageHits(CombinedShock(t));Convert(packet,ps.conversionElement,ps.conversion);}
            else if(!s.data.player&&skill>=0){es=s.data.enemySkills[skill];action=es.name;multiplier=es.damageMultiplier;hits=Mathf.Max(1,es.hits);Convert(packet,es.element,1);Metric(r.enemySkills,es.id).activations++;}
            if(s.data.player)multiplier*=ps!=null?ps.scopeMultiplier:s.data.basicScopeMultiplier;
            // Virtual Poison suppresses the direct packet at resolution, not
            // the would-be hit used to construct its guaranteed ailment.
            if(ps?.effect==WeaponSkillEffect.VirtualPoison)multiplier=ps.scopeMultiplier;
            float subclass=1;if(s.data.player){if(s.data.subclassId==SubclassIds.BarbarianBigHit&&t.life>=t.data.maximumLife-Epsilon){subclass*=1+SubclassBalanceProfile.FullLifeMore;r.fullLifeBonusHits++;}if(s.data.subclassId==SubclassIds.BarbarianBigHit&&s.life<=s.data.maximumLife*.5f){subclass*=1+SubclassBalanceProfile.InjuredMore;r.injuredBonusHits++;}if(s.data.subclassId==SubclassIds.ThiefAssassin&&!t.enemyHasActed)subclass*=1.5f;if(s.data.subclassId==SubclassIds.ThiefAssassin&&t.data.boss&&t.life<=t.data.maximumLife*.1f)subclass*=1.5f;if(s.data.subclassId==SubclassIds.BarbarianFire)packet.fire+=packet.physical*SubclassBalanceProfile.AddedFireFromPhysical;if(s.data.subclassId==SubclassIds.PriestDark)packet.voidDamage*=1+s.data.corruption*.002f;if(s.data.subclassId==SubclassIds.PriestLight){packet.voidDamage=0;}int finalProjectiles=skill>=0?ps.projectiles:s.data.projectileCount;if(s.data.subclassId==SubclassIds.RangerProjectile&&s.data.projectileMode==SubclassProjectileMode.Focused&&finalProjectiles>1)subclass*=SubclassBalanceProfile.FocusedMultiplier(finalProjectiles);}
            bool useFinisher=ShouldUseFinisher(s,t,skill,c);if(useFinisher){s.finisherArmed=true;multiplier*=WeaponMechanicProfile.RageFinisherMoreMultiplier;r.rageFinisherUses++;}
            float rageMult=s.data.weaponTypeId==WeaponTypeIds.TwoHandedAxe?ClassKeystoneMechanics.FullRageMultiplier(s.rage,s.data.rageEffect,s.data.Has(PassiveKeystone.BarbarianFullRage)):1;if(s.data.player){packet.physical*=s.auras.DamageMultiplier(Element.Phys,AuraEffect(s));packet.fire*=s.auras.DamageMultiplier(Element.Fire,AuraEffect(s));packet.cold*=s.auras.DamageMultiplier(Element.Cold,AuraEffect(s));packet.lightning*=s.auras.DamageMultiplier(Element.Light,AuraEffect(s));packet.voidDamage*=s.auras.DamageMultiplier(Element.Void,AuraEffect(s));}packet.Scale(multiplier*subclass*rageMult*(s.data.player?s.eventRevengeMultiplier:1));
            if(ps?.effect==WeaponSkillEffect.RapidFlurry){r.rapidFlurryUses++;r.rapidFlurryHits+=hits;}
            if(ps?.effect==WeaponSkillEffect.ShockBarrage){r.shockBarrageUses++;r.shockBarrageHits+=hits;}
            bool isProjectile=ps?.projectile==true||(ps==null&&s.data.weaponTypeId==WeaponTypeIds.Bow);
            int flurryRepeats=ps?.effect==WeaponSkillEffect.RapidFlurry&&s.data.Has(PassiveKeystone.WarriorConsolidation)?hits-1:0;
            if(flurryRepeats>0)hits=1;
            for(int h=0;h<hits&&t.life>0;h++)
            {
                if(isProjectile&&s.data.Has(PassiveKeystone.RangerPrecision)&&rng.Value()<PassiveKeystoneState.Value(PassiveKeystone.RangerPrecision)){KeyMetric(r,"Projectile misses",1);continue;}
                var hit=packet.Clone();
                if(isProjectile&&s.data.Has(PassiveKeystone.RangerSplit)){var split=ClassKeystoneCatalog.Get(PassiveKeystone.RangerSplit);hit.Scale(split.secondary*split.tertiary);}
                int distinct=t.dots.Select(d=>d.id).Distinct().Count()+(t.chill>0?1:0)+(t.shocks.Count>0?1:0);
                float critChance=s.data.critChance;
                if(s.data.Has(PassiveKeystone.ThiefAilmentCrit)){float added=ClassKeystoneMechanics.AilmentBaseCrit(distinct);critChance=Mathf.Clamp01((s.data.baseCritBeforeIncreased+added)*s.data.localCritMultiplier*(1+s.data.increasedCrit));KeyMetric(r,"Ailment base Crit gained",added);}
                bool crit=rng.Value()<critChance;if(crit){hit.Scale(s.data.critMultiplier);r.criticalCount++;}
                bool precision=(ps?.supportsPrecision==true||s.data.weaponTypeId==WeaponTypeIds.Bow)
                    &&(s.data.Has(PassiveKeystone.RangerPrecision)||rng.Value()<s.data.precisionChance);
                if(precision){hit.Scale(s.data.precisionMultiplier*(s.data.Has(PassiveKeystone.RangerPrecision)?ClassKeystoneCatalog.Get(PassiveKeystone.RangerPrecision).secondary:1));r.precisionCount++;}
                bool canRepeat=!s.data.player||!isProjectile&&GenericPassiveMechanics.SupportsMultistrike(s.data.weaponTypeId);
                bool extra=canRepeat&&rng.Value()<s.data.hitTwiceChance;
                if(s.data.Has(PassiveKeystone.WarriorConsolidation))
                {
                    int repeats=flurryRepeats+(extra?1:0);
                    for(int strike=0;strike<flurryRepeats;strike++)if(rng.Value()<s.data.hitTwiceChance)repeats++;
                    if(repeats>0){float consolidated=ClassKeystoneMechanics.ConsolidatedMultiplier(repeats);hit.Scale(consolidated);KeyMetric(r,"Multistrikes converted",repeats);KeyMetric(r,"Consolidated hit multiplier",consolidated);}
                }
                ApplyPacket(s,t,hit,action,now,rng,r,c,ps,projectile,crit,precision);
                if(extra&&!s.data.Has(PassiveKeystone.WarriorConsolidation)&&t.life>0){r.hitTwiceCount++;ApplyPacket(s,t,hit,action+" (Multistrike)",now,rng,r,c,ps,projectile,crit,precision);}
            }
            if(s.finisherArmed){r.rageSpent+=s.rage;s.rage=0;s.finisherArmed=false;}if(!s.data.player&&es!=null)Metric(r.enemySkills,es.id).resolves++;
        }
        static void ApplyPacket(State s,State t,CombatDamageSnapshot packet,string action,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c,CombatSkillSnapshot skill,int projectile,bool crit,bool precision,bool allowEruption=true)
        {
            packet=packet.Clone();
            float more=(s.data.Has(PassiveKeystone.BarbarianRecovery)?ClassKeystoneCatalog.Get(PassiveKeystone.BarbarianRecovery).secondary:1)
                *(s.data.Has(PassiveKeystone.ThiefStealth)?PassiveKeystoneState.Value(PassiveKeystone.ThiefStealth):1);
            if(s.data.Has(PassiveKeystone.ThiefOpener)){bool full=t.life>=t.data.maximumLife;more*=ClassKeystoneMechanics.TargetLifeMultiplier(full);KeyMetric(r,full?"Full-health hits":"Injured-target hits",1);}
            if(t.fractured){more*=ClassKeystoneCatalog.Get(PassiveKeystone.PriestFracture).secondary;KeyMetric(r,"Fractured damage multiplier",ClassKeystoneCatalog.Get(PassiveKeystone.PriestFracture).secondary);}
            packet.Scale(more);
            if(s.data.Has(PassiveKeystone.PriestAura)){while(PositiveTypeCount(packet)>2){RemoveRandomType(packet,rng);KeyMetric(r,"Discarded hit damage types",1);}}
            var ailmentBasis=packet.Clone();
            // Generic keystone/target multipliers are applied dynamically at DOT
            // resolution, exactly as CombatCalculator.CalculateAilmentTickDamage.
            ailmentBasis.Scale(1/Mathf.Max(.0001f,more));
            if(s.data.Has(PassiveKeystone.BarbarianFire)){KeyMetric(r,"Converted Physical",packet.physical);packet.fire+=packet.physical;packet.physical=0;}
            if(s.data.Has(PassiveKeystone.MageFire)){KeyMetric(r,"Discarded non-Fire damage",packet.Total-packet.fire);packet.physical=packet.cold=packet.lightning=packet.voidDamage=0;packet.fire*=PassiveKeystoneState.Value(PassiveKeystone.MageFire);}
            bool detonate=s.data.Has(PassiveKeystone.MageShatter)&&t.frozen&&!t.fractureOnThaw&&!t.fractured&&packet.lightning>0;
            if(detonate)packet.lightning*=ClassKeystoneCatalog.Get(PassiveKeystone.MageShatter).secondary;
            if(s.data.player&&s.data.subclassId==SubclassIds.WarriorMultihit&&allowEruption)
                packet.Scale(SubclassBalanceProfile.ComboMultiplier(s.combo));
            float shock=CombinedShock(t),before=t.life,defense=!s.data.player&&t.data.weaponTypeId==WeaponTypeIds.TwoHandedAxe?1-Mathf.Min(WeaponMechanicProfile.MaximumRageDefense,t.rage*WeaponMechanicProfile.RageDefensePerPoint*(1+Mathf.Max(0,t.data.rageEffect))):1;float phys=MitigateElement(packet.physical,Element.Phys,s.data,t.data)*(1+shock)*defense,fire=MitigateElement(packet.fire,Element.Fire,s.data,t.data)*(1+shock)*defense,cold=MitigateElement(packet.cold,Element.Cold,s.data,t.data)*(1+shock)*defense,light=MitigateElement(packet.lightning,Element.Light,s.data,t.data)*(1+shock)*defense,vd=MitigateElement(packet.voidDamage,Element.Void,s.data,t.data)*(1+shock)*defense;
            if(skill?.effect==WeaponSkillEffect.VirtualPoison||skill?.suppressDirectDamage==true)phys=fire=cold=light=vd=0;
float damage=phys+fire+cold+light+vd;t.life-=damage;float effective=Mathf.Min(before,damage);if(t.data.player&&damage>0)t.revengeFraction+=effective/t.data.maximumLife;Contribution(r.damage,(s.data.player?"Player/":"Enemy/")+action).total+=effective;if(s.data.subclassId==SubclassIds.ThiefAssassin&&!t.enemyHasActed)r.openingDamage+=effective;if(action=="Eruption")r.eruptionTriggers++;if(s.finisherArmed&&allowEruption){r.rageFinisherDamage+=effective;if(crit)r.rageFinisherCriticalDamage+=effective;}AddType(r,s.data.player,"Physical",phys,before,damage);AddType(r,s.data.player,"Fire",fire,before,damage);AddType(r,s.data.player,"Cold",cold,before,damage);AddType(r,s.data.player,"Lightning",light,before,damage);AddType(r,s.data.player,"Void",vd,before,damage);if(s.data.player&&skill!=null)Metric(r.skills,skill.id).damage+=effective;else if(!s.data.player){var enemySkill=s.data.enemySkills.FirstOrDefault(x=>x.name==action);if(enemySkill!=null)Metric(r.enemySkills,enemySkill.id).damage+=effective;}Trace(r,c,now,s.data.player?CombatEventType.PlayerAttackReady:CombatEventType.EnemyAttackReady,s.data.player?CombatTraceFilter.PlayerAttack|CombatTraceFilter.Damage:CombatTraceFilter.EnemyAttack|CombatTraceFilter.Damage,s,t,action,$"Crit={crit}; Precision={precision}; ShockTaken={shock:0.###}",amount:damage,lifeBefore:before,lifeAfter:t.life,projectile:projectile);
            if(damage>0){HealOrConvert(s,t,s.data.lifeOnHit,"Life on Hit",r);float restored=Mathf.Min(s.data.maximumMana-s.mana,s.data.manaOnHit);s.mana+=restored;if(s.data.player)r.manaOnHit+=restored;if(s.data.player&&s.data.subclassId==SubclassIds.WarriorMultihit){s.combo++;r.maximumCombo=Mathf.Max(r.maximumCombo,s.combo);}if(s.data.player){bool lightPriest=s.data.subclassId==SubclassIds.PriestLight;s.auras.RecordTypedHit(0,phys,t.data.maximumLife,s.data.physicalAuraAccess||lightPriest);s.auras.RecordTypedHit(1,fire,t.data.maximumLife,s.data.fireAuraAccess||lightPriest);s.auras.RecordTypedHit(2,cold,t.data.maximumLife,s.data.coldAuraAccess||lightPriest);s.auras.RecordTypedHit(3,light,t.data.maximumLife,s.data.lightningAuraAccess||lightPriest);}if(s.data.weaponTypeId==WeaponTypeIds.TwoHandedAxe){float gain=WeaponMechanicProfile.RageGainFromDamage(damage,t.data.maximumLife,skill?.effect==WeaponSkillEffect.RageStrike?1.5f:1)*(1+Mathf.Max(0,s.data.rageGeneration));float rb=s.rage;s.rage=Mathf.Min(100,s.rage+gain);s.rageDecayEligibleAt=now+(s.data.Has(PassiveKeystone.BarbarianFullRage)?0:WeaponMechanicProfile.RageDecayDelay);r.rageGenerated+=s.rage-rb;Trace(r,c,now,CombatEventType.Rage,CombatTraceFilter.Rage,s,t,"Rage gained","Damage dealt",amount:s.rage-rb,rageBefore:rb,rageAfter:s.rage);}}
            if(s.data.player){float lifeBeforeAilments=t.life;ApplyAilments(s,t,ailmentBasis,skill,now,rng,r,c);float sacrificeLoss=Mathf.Max(0,lifeBeforeAilments-t.life);if(s.data.Has(PassiveKeystone.BarbarianRecovery))HealOrConvert(s,t,(effective+sacrificeLoss)*s.data.wouldBeLifeRegenerationFraction*PassiveKeystoneState.Value(PassiveKeystone.BarbarianRecovery),"Damage-based Recovery",r);if(skill?.effect==WeaponSkillEffect.HealFromDamage)HealOrConvert(s,t,(effective+sacrificeLoss)*skill.secondaryMultiplier,"Weapon Skill",r);if(s.data.subclassId==SubclassIds.PriestLight)Heal(s,damage*.1f,"Light Priest",r);if(allowEruption&&s.data.subclassId==SubclassIds.BarbarianFire&&rng.Value()<SubclassBalanceProfile.EruptionChance){var erupt=new CombatDamageSnapshot{fire=packet.Total*SubclassBalanceProfile.EruptionMagnitude};ApplyPacket(s,t,erupt,"Eruption",now,rng,r,c,null,0,false,false,false);}if(s.data.subclassId==SubclassIds.ThiefAssassin&&!t.data.boss&&t.life>0&&t.life<=t.data.maximumLife*.1f){float execution=t.life;t.life=0;r.executionTriggers++;r.executionDamage+=execution;Contribution(r.damage,"Player/Execution").total+=execution;}}
            if(detonate&&t.life>0){t.frozen=false;var burst=new CombatDamageSnapshot{cold=t.data.maximumLife*ClassKeystoneCatalog.Get(PassiveKeystone.MageShatter).tertiary};float burstMore=(s.data.Has(PassiveKeystone.BarbarianRecovery)?ClassKeystoneCatalog.Get(PassiveKeystone.BarbarianRecovery).secondary:1)*(s.data.Has(PassiveKeystone.ThiefStealth)?PassiveKeystoneState.Value(PassiveKeystone.ThiefStealth):1)*(s.data.Has(PassiveKeystone.ThiefOpener)?ClassKeystoneMechanics.TargetLifeMultiplier(t.life>=t.data.maximumLife):1)*(t.fractured?ClassKeystoneCatalog.Get(PassiveKeystone.PriestFracture).secondary:1);float final=s.data.Has(PassiveKeystone.MageFire)?0:Mitigate(burst,s.data,t.data)*burstMore*s.auras.DamageMultiplier(Element.Cold,AuraEffect(s))*(1+CombinedShock(t));float lost=Mathf.Min(t.life,final);t.life-=final;r.shatters++;KeyMetric(r,"Lightning detonations",1);KeyMetric(r,"Cold Max-Life Shatter damage",lost);Contribution(r.damage,"Player/Keystone Shatter").total+=lost;if(s.data.Has(PassiveKeystone.BarbarianRecovery))HealOrConvert(s,t,lost*s.data.wouldBeLifeRegenerationFraction*PassiveKeystoneState.Value(PassiveKeystone.BarbarianRecovery),"Damage-based Recovery",r);}
            if(s.data.player&&ClassKeystoneMechanics.Cull(t.life,t.data.maximumLife,s.data.cullingStrike)){float execution=t.life;t.life=0;r.executionTriggers++;r.executionDamage+=execution;Contribution(r.damage,"Player/Culling Strike").total+=execution;}
            if(t.life<=0){if(s.data.Has(PassiveKeystone.ThiefStealth)){s.stealth=true;KeyMetric(r,"Stealth activations",1);}Heal(s,s.data.lifeOnKill,"Life on Kill",r);s.mana=Mathf.Min(s.data.maximumMana,s.mana+s.data.manaOnKill);Trace(r,c,now,CombatEventType.Death,CombatTraceFilter.Status,s,t,t.data.name+" died",action);}
        }
        static void ResolveSpellBolts(State s,State t,CombatSkillSnapshot skill,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c)
        {
            float basis=s.data.basicDamage.Total*skill.hitMultiplier*PassiveKeystoneState.Value(PassiveKeystone.MageSelfBolt);
            float before=t.life;ApplyPacket(s,t,new CombatDamageSnapshot{lightning=basis},"Keystone Lightning Bolt",now,rng,r,c,null,0,false,false,false);KeyMetric(r,"Enemy bolt damage",Mathf.Min(before,before-t.life));
            float incoming=CombatCalculator.ApplyResistanceValue(basis,s.data.lightningResistance,0,ResistanceCap(Element.Light,s.data))*(1+CombinedShock(s));
            float lost=Mathf.Min(s.life,incoming);s.life-=incoming;s.revengeFraction+=lost/s.data.maximumLife;KeyMetric(r,"Incoming self-hit damage",lost);Contribution(r.damage,"Enemy/Incoming Self Bolt").total+=lost;
            if(s.data.weaponTypeId==WeaponTypeIds.TwoHandedAxe)s.rage=Mathf.Min(100,s.rage+WeaponMechanicProfile.RageGainFromDamage(lost,s.data.maximumLife)*(1+Mathf.Max(0,s.data.rageGeneration)));
            if(lost>0&&CombatDeterministicRules.RollOverflowApplications(s.data.shockChance,rng.Value)>0){s.shocks.Clear();s.shocks.Add((.5f*(1+s.data.shockEffect)*(1-Mathf.Clamp01(s.data.reducedShockEffect)),5));KeyMetric(r,"Player Shocks from self-bolt",1);}
        }
        static void KeyMetric(CombatSimulationResult r,string name,float value){var metric=Contribution(r.keystoneTelemetry,name);metric.count++;metric.total+=value;}
        static int PositiveTypeCount(CombatDamageSnapshot packet)=>(packet.physical>0?1:0)+(packet.fire>0?1:0)+(packet.cold>0?1:0)+(packet.lightning>0?1:0)+(packet.voidDamage>0?1:0);
        static void RemoveRandomType(CombatDamageSnapshot p,DeterministicCombatRandom rng)
        {
            var types=new List<int>();if(p.physical>0)types.Add(0);if(p.fire>0)types.Add(1);if(p.cold>0)types.Add(2);if(p.lightning>0)types.Add(3);if(p.voidDamage>0)types.Add(4);
            switch(types[rng.Range(types.Count)]){case 0:p.physical=0;break;case 1:p.fire=0;break;case 2:p.cold=0;break;case 3:p.lightning=0;break;case 4:p.voidDamage=0;break;}
        }
        static float AuraEffect(State s)
        {int count=0;for(int i=0;i<5;i++)if(s.auras.Intensity(i)>0)count++;return s.data.auraEffect+(s.data.Has(PassiveKeystone.PriestAura)?count*PassiveKeystoneState.Value(PassiveKeystone.PriestAura):0);}
        static void ApplyAilments(State s,State t,CombatDamageSnapshot packet,CombatSkillSnapshot skill,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c)
        {
            bool darkVoid=s.data.subclassId==SubclassIds.PriestDark&&packet.voidDamage>0;
            int poison=0,ignite=0;float voidChance=s.auras.Bonus(4,.2f,AuraEffect(s));
            bool poisonEligible=packet.voidDamage>0||s.data.subclassId==SubclassIds.RangerPoison||skill?.specializedAilment==StatusEffects.AilmentKind.Poison;
            if(poisonEligible)
            {
                float basis=DotMagnitude(s.data,packet,"Poison",skill?.effect==WeaponSkillEffect.VirtualPoison)*(skill?.specializedAilment==StatusEffects.AilmentKind.Poison?Mathf.Max(1,skill.ailmentBasisMultiplier):1);
                poison=ApplyDot("Poison",s.data.poisonChance+voidChance,skill?.specializedAilment==StatusEffects.AilmentKind.Poison?skill.guaranteedAilmentApplications:0,
                    basis,4,s.data.poisonExtraTicks,AilmentCalculator.PoisonTickInterval(2,s.data.poisonSpeed),int.MaxValue,s,t,now,rng,r,c);
            }
            int bleed=0;
            if(packet.physical>0||darkVoid)
                bleed=ApplyDot("Bleed",s.data.bleedChance+voidChance,skill?.specializedAilment==StatusEffects.AilmentKind.Bleed?skill.guaranteedAilmentApplications:0,
                    DotMagnitude(s.data,packet,"Bleed")*(skill?.specializedAilment==StatusEffects.AilmentKind.Bleed?Mathf.Max(1,skill.ailmentBasisMultiplier):1),5,s.data.bleedExtraTicks,2,s.data.maximumBleedStacks,s,t,now,rng,r,c);
            if(bleed>0&&s.data.subclassId==SubclassIds.WarriorBleed&&rng.Value()<SubclassBalanceProfile.RuptureChance)
            {
                float remaining=0;
                foreach(var dot in t.dots.Where(x=>x.id=="Bleed").ToArray())
                {remaining+=MitigateElement(dot.damage,Element.Phys,s.data,t.data)*dot.ticks;t.dots.Remove(dot);}
                if(remaining>0)
                {
                    float effective=Mathf.Min(t.life,remaining);t.life-=remaining;
                    Contribution(r.damage,"Player/Rupture").total+=effective;
                    if(s.data.Has(PassiveKeystone.BarbarianRecovery))HealOrConvert(s,t,effective*s.data.wouldBeLifeRegenerationFraction*PassiveKeystoneState.Value(PassiveKeystone.BarbarianRecovery),"Damage-based Recovery",r);
                    Ailment(r,"Rupture").damage+=effective;r.ruptureTriggers++;
                    Trace(r,c,now,CombatEventType.AilmentApply,CombatTraceFilter.Ailment|CombatTraceFilter.Damage,s,t,"Rupture","Consumed remaining mitigated Bleed damage",amount:remaining);
                }
            }
            if(packet.fire>0||darkVoid||packet.physical>0&&s.data.Has(PassiveKeystone.BarbarianFire))
                ignite=ApplyDot("Ignite",s.data.igniteChance+voidChance,skill?.specializedAilment==StatusEffects.AilmentKind.Ignite?skill.guaranteedAilmentApplications:0,
                    DotMagnitude(s.data,packet,"Ignite"),2,s.data.igniteExtraTicks,2,s.data.maximumIgniteStacks,s,t,now,rng,r,c);
            s.auras.RecordDamagingAilments(bleed>0,ignite>0,poison>0,s.data.voidAuraAccess||s.data.subclassId==SubclassIds.PriestDark);
            if(packet.lightning>0||darkVoid||s.data.subclassId==SubclassIds.MageStorm&&packet.Total>0)
            {
                int apps=CombatDeterministicRules.RollOverflowApplications(s.data.shockChance,rng.Value);
                for(int i=0;i<apps;i++)
                {
                    float strength=Mathf.Min(1,.5f*(1+s.data.shockEffect+s.auras.Bonus(3,.2f,AuraEffect(s))))*(1-Mathf.Clamp01(t.data.reducedShockEffect));
                    if(t.shocks.Count>=s.data.maximumShockInstances)t.shocks.RemoveAt(0);
                    t.shocks.Add((strength,5));var m=Ailment(r,"Shock");m.applications++;
                    m.magnitudeTotal+=strength;m.maximumEffect=Math.Max(m.maximumEffect,CombinedShock(t));
                }
                if(apps>0)Trace(r,c,now,CombatEventType.AilmentApply,CombatTraceFilter.Ailment,s,t,
                    "Shock applied","Production chance/instance cap",amount:CombinedShock(t),stackCount:t.shocks.Count);
            }
            if((packet.cold>0||darkVoid)&&CombatDeterministicRules.RollOverflowApplications(s.data.chillChance,rng.Value)+(skill?.guaranteedAdditionalChill??0)>0)
            {
                float eligible=packet.cold>0?packet.cold:packet.voidDamage;
                float slow=BattleManager.CalculateChillSlow(MitigateElement(eligible,packet.cold>0?Element.Cold:Element.Void,s.data,t.data),
                    t.data.maximumLife,s.data.chillEffect+s.auras.Bonus(2,.2f,AuraEffect(s)))*(1-Mathf.Clamp01(t.data.reducedChillEffect));
                t.chill=Mathf.Max(t.chill,slow);t.chillExpiresAt=Mathf.Max(t.chillExpiresAt,now+5);
                var m=Ailment(r,"Chill");m.applications++;m.magnitudeTotal+=slow;m.maximumEffect=Math.Max(m.maximumEffect,slow);
                float uncapped=ClassKeystoneMechanics.UncappedChill(MitigateElement(eligible,packet.cold>0?Element.Cold:Element.Void,s.data,t.data),t.data.maximumLife,s.data.chillEffect+s.auras.Bonus(2,.2f,AuraEffect(s)))*(1-Mathf.Clamp01(t.data.reducedChillEffect));
                bool freeze=!t.fractured&&!t.fractureOnThaw&&(s.data.Has(PassiveKeystone.PriestFracture)?uncapped>=PassiveKeystoneState.Value(PassiveKeystone.PriestFracture):s.data.Has(PassiveKeystone.MageShatter)?uncapped>=PassiveKeystoneState.Value(PassiveKeystone.MageShatter):skill?.effect==WeaponSkillEffect.FrostJudgment&&rng.Value()<slow);
                if(freeze)
                {t.frozen=true;Ailment(r,"Freeze").applications++;Trace(r,c,now,CombatEventType.AilmentApply,CombatTraceFilter.Ailment,s,t,"Freeze applied","Frost Judgment vs Chilled target",amount:slow);}
                if(freeze&&s.data.Has(PassiveKeystone.PriestFracture))t.fractureOnThaw=true;
            }
        }
        static float DotMagnitude(CombatantSnapshot data,CombatDamageSnapshot hit,string id,bool fullBasis=false)
        {
            var factors=id=="Poison"?(fullBasis?data.poisonFullHitFactors:data.poisonHitFactors):id=="Bleed"?data.bleedHitFactors:data.igniteHitFactors;
            if(factors==null||factors.Length!=5)return id=="Poison"?data.poisonMagnitude:id=="Bleed"?data.bleedMagnitude:data.igniteMagnitude;
            return factors[0]*hit.physical+factors[1]*hit.fire+factors[2]*hit.cold+factors[3]*hit.lightning+factors[4]*hit.voidDamage;
        }
        static int ApplyDot(string id,float chance,int guaranteed,float magnitude,int baseTicks,int extraTicks,float interval,int cap,State s,State t,float now,DeterministicCombatRandom rng,CombatSimulationResult r,CombatSimulationConfig c)
        {
            int apps=guaranteed+CombatDeterministicRules.RollOverflowApplications(chance,rng.Value),applied=0;
            if(apps<=0||magnitude<=0)return 0;
            if(s.data.Has(PassiveKeystone.PriestSacrifice))
            {
                float loss=Mathf.Min(Mathf.Max(0,t.life),t.data.maximumLife*PassiveKeystoneState.Value(PassiveKeystone.PriestSacrifice));t.life-=loss;
                Contribution(r.damage,"Player/Ailment Sacrifice").total+=loss;KeyMetric(r,"Distinct ailment conversions",1);KeyMetric(r,"Sacrifice Max-Life damage",loss);
                return apps;
            }
            if(s.data.Has(PassiveKeystone.MageFire)&&id!="Ignite")return 0;
            if(id=="Bleed"&&s.data.Has(PassiveKeystone.WarriorBleed)){cap*=2;magnitude*=PassiveKeystoneState.Value(PassiveKeystone.WarriorBleed);}
            if(id=="Ignite"&&s.data.Has(PassiveKeystone.BarbarianFire))cap++;
            bool infinite=id=="Poison"&&s.data.Has(PassiveKeystone.RangerEndlessPoison);
            if(infinite){float factor=ClassKeystoneMechanics.EndlessPoisonMultiplier(extraTicks);magnitude*=factor;KeyMetric(r,"Endless Poison duration modifier",factor);}
            for(int i=0;i<apps;i++)
            {
                float amount=magnitude;bool criticalAilment=false;
                if(s.data.subclassId==SubclassIds.ThiefAilmentCrit)
                {
                    amount*=SubclassBalanceProfile.AilmentExtraMore(s.data.critMultiplier);
                    criticalAilment=rng.Value()<s.data.critChance;
                    if(criticalAilment)amount*=SubclassBalanceProfile.CriticalAilmentMultiplier(s.data.critMultiplier);
                }
                int ticks=Mathf.Max(1,baseTicks+extraTicks);if(id=="Ignite")ticks+=s.auras.ExtraIgniteTicks(AuraEffect(s));
                if(id=="Poison")ticks=Mathf.Max(1,Mathf.CeilToInt(ticks*(1+Mathf.Max(0,s.data.poisonSpeed))));
                var d=new Dot{id=id,source=s.data.id,playerSource=s.data.player,damage=amount/baseTicks,ticks=infinite?int.MaxValue:ticks,infinite=infinite,stacks=1,interval=interval,next=now+interval};
                var same=t.dots.Where(x=>x.id==id).ToList();
                if(same.Count>=cap){var weakest=same.OrderBy(x=>x.damage*x.ticks).First();if(weakest.damage*weakest.ticks>=d.damage*d.ticks)continue;t.dots.Remove(weakest);}
                t.dots.Add(d);applied++;var metric=Ailment(r,id);metric.applications++;
                if(criticalAilment)metric.criticalApplications++;
                metric.magnitudeTotal+=amount;metric.maxStacks=Math.Max(metric.maxStacks,t.dots.Count(x=>x.id==id));
                Trace(r,c,now,CombatEventType.AilmentApply,CombatTraceFilter.Ailment,s,t,id+" applied",criticalAilment?"Critical ailment":"Chance resolved",amount:amount,stackCount:t.dots.Count(x=>x.id==id));
            }
            return applied;
        }
        static void TickDot(Dot dot,State p,State e,float now,CombatSimulationResult r,CombatSimulationConfig c,Action<float,CombatEventType,bool,int,int,Dot> add)
        {
            if(dot==null||dot.ticks<=0)return;
            State target=dot.playerSource?e:p,source=dot.playerSource?p:e;
            if(!target.dots.Contains(dot)||target.life<=0)return;
            dot.scheduled=false;float before=target.life;
            Element element=dot.id=="Bleed"?Element.Phys:dot.id=="Ignite"?Element.Fire:Element.Void;
            float more=(source.data.Has(PassiveKeystone.BarbarianRecovery)?ClassKeystoneCatalog.Get(PassiveKeystone.BarbarianRecovery).secondary:1)
                *(source.data.Has(PassiveKeystone.ThiefStealth)?PassiveKeystoneState.Value(PassiveKeystone.ThiefStealth):1);
            if(source.data.Has(PassiveKeystone.ThiefOpener))more*=ClassKeystoneMechanics.TargetLifeMultiplier(before>=target.data.maximumLife);
            if(target.fractured)more*=ClassKeystoneCatalog.Get(PassiveKeystone.PriestFracture).secondary;
            if(source.data.Has(PassiveKeystone.MageFire))more*=dot.id=="Ignite"?PassiveKeystoneState.Value(PassiveKeystone.MageFire):0;
            float damage=MitigateElement(dot.damage*more,element,source.data,target.data);
            float lost=Mathf.Min(before,damage);target.life-=damage;
            if(target.data.player)target.revengeFraction+=lost/Mathf.Max(1,target.data.maximumLife);
            if(dot.id=="Poison")HealOrConvert(source,target,GenericPassiveMechanics.PoisonLeech(lost,source.data.poisonLifeLeech),"Poison Leech",r);
            if(source.data.Has(PassiveKeystone.BarbarianRecovery))HealOrConvert(source,target,lost*source.data.wouldBeLifeRegenerationFraction*PassiveKeystoneState.Value(PassiveKeystone.BarbarianRecovery),"Damage-based Recovery",r);
            if(!dot.infinite)dot.ticks--;var m=Ailment(r,dot.id);m.damage+=lost;
            m.maxStacks=Math.Max(m.maxStacks,target.dots.Count(x=>x.id==dot.id));
            Contribution(r.damage,(dot.playerSource?"Player/":"Enemy/")+dot.id).total+=lost;
            Trace(r,c,now,CombatEventType.AilmentTick,CombatTraceFilter.Ailment|CombatTraceFilter.Damage,source,target,dot.id+" tick","Scheduled production-style DOT tick",amount:damage,lifeBefore:before,lifeAfter:target.life,stackCount:m.maxStacks);
            if(dot.ticks<=0)target.dots.Remove(dot);else{dot.next=now+dot.interval;dot.scheduled=true;add(dot.next,CombatEventType.AilmentTick,dot.playerSource,-1,0,dot);}
        }
        static void ScheduleDots(State p,State e,Action<float,CombatEventType,bool,int,int,Dot> add){foreach(var d in p.dots.Concat(e.dots))if(!d.scheduled&&d.ticks>0){d.scheduled=true;add(d.next,CombatEventType.AilmentTick,d.playerSource,-1,0,d);}}
        static void UpdatePhase(State e,float now,CombatSimulationResult r,CombatSimulationConfig c){if(e.data.phases==null)return;var phase=BossPhaseResolver.Resolve(e.data.phases,e.life/e.data.maximumLife);string id=phase?.mechanicId;if(id==e.phase)return;ClosePhase(e,now,r);e.phase=id;e.phaseStartedAt=now;Trace(r,c,now,CombatEventType.BossPhase,CombatTraceFilter.BossPhase,e,null,"Boss phase: "+id,"Authored Life threshold",phase:id);}
        static void ClosePhase(State e,float now,CombatSimulationResult r){if(string.IsNullOrEmpty(e.phase))return;float elapsed=Mathf.Max(0,now-e.phaseStartedAt);var row=Contribution(r.phaseTime,e.phase);row.total+=elapsed;row.count++;e.phaseStartedAt=now;}
        static void AccumulateAilments(State s,float delta,CombatSimulationResult r){foreach(var group in s.dots.GroupBy(x=>x.id)){var m=Ailment(r,group.Key);int count=group.Count();m.uptime+=delta;m.stackTime+=count*delta;m.maxStacks=Math.Max(m.maxStacks,count);}if(s.shocks.Count>0){var m=Ailment(r,"Shock");m.uptime+=delta;m.stackTime+=s.shocks.Count*delta;m.maximumEffect=Math.Max(m.maximumEffect,CombinedShock(s));}if(s.chill>0){var m=Ailment(r,"Chill");m.uptime+=delta;m.stackTime+=delta;m.maximumEffect=Math.Max(m.maximumEffect,s.chill);}}
        static void AddType(CombatSimulationResult r,bool player,string id,float amount,float lifeBefore,float total){if(amount<=0||total<=0)return;Contribution(r.damageByType,(player?"Player/":"Enemy/")+id).total+=Mathf.Min(amount,amount*Mathf.Max(0,lifeBefore)/total);}
        static bool ShouldUseFinisher(State p,State e,int skill,CombatSimulationConfig c){if(!p.data.hasRageFinisher||p.rage<100)return false;return c.ragePolicy switch{RageFinisherPolicy.Never=>false,RageFinisherPolicy.NextSkill=>skill>=0,RageFinisherPolicy.Skill1Only=>skill==0,RageFinisherPolicy.Skill2Only=>skill==1,RageFinisherPolicy.TargetBelowThreshold=>e.life/e.data.maximumLife<=c.rageTargetThreshold,_=>true};}
        static float Mitigate(CombatDamageSnapshot x,CombatantSnapshot a,CombatantSnapshot d)=>MitigateElement(x.physical,Element.Phys,a,d)+MitigateElement(x.fire,Element.Fire,a,d)+MitigateElement(x.cold,Element.Cold,a,d)+MitigateElement(x.lightning,Element.Light,a,d)+MitigateElement(x.voidDamage,Element.Void,a,d);
        static float ResistanceCap(Element element,CombatantSnapshot d)
        {float cap=element switch{Element.Fire=>d.maximumFireResistance,Element.Cold=>d.maximumColdResistance,Element.Light=>d.maximumLightningResistance,_=>d.maximumVoidResistance};return cap>0?cap:d.maximumResistance;}
        static float MitigateElement(float value,Element element,CombatantSnapshot a,CombatantSnapshot d){if(value<=0)return 0;if(element==Element.Phys)return CombatCalculator.ApplyArmourValue(value,d.armour,d.physicalDamageReduction,a.physicalPenetration);float resistance=element switch{Element.Fire=>d.fireResistance,Element.Cold=>d.coldResistance,Element.Light=>d.lightningResistance,_=>d.voidResistance};float pen=element switch{Element.Fire=>a.firePenetration,Element.Cold=>a.coldPenetration,Element.Light=>a.lightningPenetration,_=>a.voidPenetration};return CombatCalculator.ApplyResistanceValue(value,resistance,pen,ResistanceCap(element,d));}
        static void Convert(CombatDamageSnapshot p,Element to,float amount){if(amount<=0)return;float phys=p.physical,fire=p.fire,cold=p.cold,light=p.lightning,vd=p.voidDamage;p.physical*=1-amount;p.fire*=1-amount;p.cold*=1-amount;p.lightning*=1-amount;p.voidDamage*=1-amount;float add=(phys+fire+cold+light+vd)*amount;switch(to){case Element.Phys:p.physical+=add;break;case Element.Fire:p.fire+=add;break;case Element.Cold:p.cold+=add;break;case Element.Light:p.lightning+=add;break;default:p.voidDamage+=add;break;}}
        static float CombinedShock(State s){float product=1;foreach(var x in s.shocks)product*=1+Mathf.Max(0,x.strength);return Mathf.Max(0,product-1);}
        static float PlayerAttackInterval(State source,State target)
        {float bonus=Mathf.Max(-.99f,source.data.attackSpeedBonus);float speed=source.data.attackSpeed*(1+bonus+source.auras.Bonus(0,.1f,source.data.auraEffect))/(1+bonus);return CombatDeterministicRules.AttackInterval(speed,source.chill);}
        static void HealOrConvert(State source,State target,float amount,string reason,CombatSimulationResult result)
        {if(source.data.player&&source.data.subclassId==SubclassIds.PriestDark){if(amount<=0||target.life<=0)return;float damage=MitigateElement(GenericPassiveMechanics.Recovery(amount,source.data.lifeRecoveryEffect),Element.Void,source.data,target.data);float effective=Mathf.Min(target.life,damage);target.life-=damage;Contribution(result.damage,"Player/Healing Converted to Void").total+=effective;Contribution(result.damageByType,"Player/Void").total+=effective;return;}Heal(source,amount,reason,result);}
        static void Heal(State s,float amount,string source,CombatSimulationResult r){if(amount<=0||s.life<=0||s.disableLifeRecovery)return;amount=GenericPassiveMechanics.Recovery(amount,s.data.lifeRecoveryEffect);float missing=Mathf.Max(0,s.data.maximumLife-s.life),effective=Mathf.Min(missing,amount);s.life+=effective;var c=Contribution(r.healing,source);c.total+=amount;c.effective+=effective;c.overheal+=amount-effective;c.count++;}
        static CombatContribution Contribution(List<CombatContribution> list,string id){var x=list.FirstOrDefault(y=>y.id==id);if(x==null){x=new CombatContribution{id=id};list.Add(x);}return x;}
        static CombatAilmentMetric Ailment(CombatSimulationResult r,string id){var x=r.ailments.FirstOrDefault(y=>y.id==id);if(x==null){x=new CombatAilmentMetric{id=id};r.ailments.Add(x);}return x;}
        static CombatSkillMetric Metric(List<CombatSkillMetric> list,string id){var x=list.FirstOrDefault(y=>y.id==id);if(x==null){x=new CombatSkillMetric{id=id};list.Add(x);}return x;}
        static void RecordResolve(CombatSkillMetric metric,float now){if(metric.lastUse>=0)metric.intervalTotal+=now-metric.lastUse;metric.lastUse=now;metric.resolves++;}
        static bool Dead(State p,State e)=>p.life<=0||e.life<=0;static bool Finite(State x)=>float.IsFinite(x.life)&&float.IsFinite(x.mana)&&float.IsFinite(x.rage)&&x.mana>=-Epsilon&&x.rage>=-Epsilon;
        static void Trace(CombatSimulationResult r,CombatSimulationConfig c,float time,CombatEventType type,CombatTraceFilter cat,State source,State target,string action,string reason,string rule=null,string phase=null,float roll=0,float amount=0,float lifeBefore=0,float lifeAfter=0,float manaBefore=0,float manaAfter=0,float rageBefore=0,float rageAfter=0,int projectile=0,int stackCount=0){if(!c.retainTrace||r.trace.Count>=c.traceLimit)return;r.trace.Add(new CombatTraceEvent{index=r.trace.Count,time=time,type=type,category=cat,source=source?.data.name,target=target?.data.name,action=action,reason=reason,ruleId=rule,phase=phase,roll=roll,amount=amount,lifeBefore=lifeBefore,lifeAfter=lifeAfter,manaBefore=manaBefore,manaAfter=manaAfter,rageBefore=rageBefore,rageAfter=rageAfter,projectileIndex=projectile,stackCount=stackCount});}
    }
}
