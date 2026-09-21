using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class RolledModSnapshot
    {
        public StatTypes stat; public int tier; public float value,high; public bool paired,implicitMod,empowered,bossSpecial; public string pool,id;
        public static RolledModSnapshot Capture(RolledMod x)=>new(){stat=x.statType,tier=x.tierIndex,value=x.value,high=x.secondaryValue,paired=x.hasSecondaryValue,implicitMod=x.lockedOriginal,empowered=x.isEmpowered,bossSpecial=x.isBossSpecial,pool=x.specialPoolId,id=x.specialModifierId};
        public RolledMod Restore()=>paired?new RolledMod(stat,tier,value,high,implicitMod):new RolledMod(stat,tier,value,implicitMod){isEmpowered=empowered,isBossSpecial=bossSpecial,specialPoolId=pool,specialModifierId=id};
    }

    [Serializable] public sealed class GearSnapshot
    {
        public LootManager.GearType slot;public LootManager.GearRarity rarity;public int itemLevel;public Element element;public string weaponTypeId;public float baseMin,baseMax,baseSpeed,baseCrit;public List<RolledModSnapshot> mods=new();
        public string Description=>$"{rarity} {slot} L{itemLevel}"+(slot==LootManager.GearType.Weapons?$" {weaponTypeId}":"")+"\n"+string.Join("\n",mods.Select(x=>$"{x.stat} T{x.tier}: {x.value:0.##}"+(x.paired?$"–{x.high:0.##}":"")));
        public static GearSnapshot Capture(Gear g)=>new(){slot=g.ItemType,rarity=g.ItemRarity,itemLevel=g.ItemLevel,element=g.BaseElement,weaponTypeId=g.WeaponTypeId,baseMin=g.BaseDamageMin,baseMax=g.BaseDamageMax,baseSpeed=g.BaseAttackSpeed,baseCrit=g.BaseCritChance,mods=g.rolledMods.Where(x=>x!=null).Select(RolledModSnapshot.Capture).ToList()};
        public Gear Materialize(Transform parent,string name)
        {
            var go=new GameObject(name){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(parent);var g=go.AddComponent<Gear>();g.Initialize(slot,rarity,itemLevel,element,weaponTypeId);g.ApplyMods(mods.Select(x=>x.Restore()).ToList());g.BaseDamageMin=baseMin;g.BaseDamageMax=baseMax;g.BaseDamage=(baseMin+baseMax)*.5f;g.BaseAttackSpeed=baseSpeed;g.BaseCritChance=baseCrit;return g;
        }
    }

    [Serializable] public sealed class PlayerBuildSnapshot
    {
        public int playerLevel=50,combatLevel=50;public string classId=PlayerClassIds.Warrior,subclassId="",weaponTypeId=WeaponTypeIds.Sword;public SubclassProjectileMode projectileMode;
        public List<GearSnapshot> equipment=new();public List<string> passiveStableIds=new();public long seed=41001;public string dataFingerprint,gearProfileGuid;public int gearProfileVersion;
        public PlayerBuildSnapshot Clone()=>JsonUtility.FromJson<PlayerBuildSnapshot>(JsonUtility.ToJson(this));
        public int[] AllocationRanks(){var r=new int[PassiveTreeDefinition.NodeCount];foreach(string id in passiveStableIds??new())if(PassiveTreeDefinition.TryNode(id,out var n))r[n.Id]=1;return r;}
    }

    [Serializable] public sealed class SkillAnalyticalMetrics
    {public string name,castMode,assumption;public double averageDamagePerUse,averageDirectHit,expectedHits,effectiveCooldown,idealCooldownDps,manaCost;public bool sustainable;}

    [Serializable] public sealed class PlayerBuildMetrics
    {
        public double averageHit,minimumHit,maximumHit,attacksPerSecond,basicDps,critChance,critMultiplier,critContribution,hitTwiceChance,hitTwiceContribution;
        public double physicalOutput,fireOutput,coldOutput,lightningOutput,voidOutput,projectileCount,projectileTravelTime,precisionChance,precisionMultiplier,cooldownReduction;
        public double poisonChance,poisonMagnitude,poisonDps,bleedChance,bleedMagnitude,bleedDps,igniteChance,igniteMagnitude,igniteDps,shockChance,shockEffect,chillChance,chillEffect;
        public double life,armour,fireResistance,coldResistance,lightningResistance,voidResistance,lifeRegen,lifeOnHit,lifeOnKill,mana,manaRegen,manaOnHit,manaOnKill,strength,dexterity,intelligence,auraEffect,rageGeneration,rageEffect,totalGearScore;
        public double ehpPhysical,ehpFire,ehpCold,ehpLightning,ehpVoid,recoveryPerSecond;
        public double baseWeaponAverage,weaponAttributeIncreased,playerLevelIncreased,genericIncreased,physicalIncreased,genericMore,physicalMore;
        public List<SkillAnalyticalMetrics> skills=new();public List<string> assumptions=new();
    }

    public sealed class PlayerBuildEvaluation:IDisposable
    {
        readonly GameObject root;readonly List<Gear> gear=new();public readonly StatsComponent Stats;public readonly PlayerController Player;public readonly HealthComponent Health;public readonly ManaComponent Mana;public readonly PlayerBuildMetrics Metrics;
        public PlayerBuildEvaluation(PlayerBuildSnapshot build)
        {
            root=new GameObject("Balance Workbench Player"){hideFlags=HideFlags.HideAndDontSave};Stats=root.AddComponent<StatsComponent>();root.AddComponent<PlayerStatSetup>();PlayerStatSetup.ApplyBaseline(Stats);Health=root.AddComponent<HealthComponent>();Health.ConfigureIsolatedStats(Stats);Mana=root.AddComponent<ManaComponent>();Mana.ConfigureIsolatedStats(Stats);Player=root.AddComponent<PlayerController>();Player.ConfigureIsolatedBuildLevel(build.playerLevel);
            Stats.BeginUpdate();try
            {
                Stats.AddModifier(new StatModifier(StatTypes.Life,StatOp.Flat,PlayerProgression.LevelLifeBonus(build.playerLevel),this));
                int[] ranks=build.AllocationRanks();foreach(var node in PassiveTreeDefinition.Nodes)if(ranks[node.Id]!=0&&(string.IsNullOrEmpty(node.WeaponTypeRestriction)||node.WeaponTypeRestriction==build.weaponTypeId))
                {var effects=node.IsSubclassChoice?PassiveTreeDefinition.SubclassEffects(build.subclassId,node):node.Effects;foreach(var e in effects)Stats.AddModifier(new StatModifier(e.Stat,StatOp.Flat,e.Amount,this));}
                SubclassStatPackage.Apply(build.subclassId,(s,v)=>Stats.AddModifier(new StatModifier(s,StatOp.Flat,v,this)));
                foreach(var snapshot in build.equipment??new()){var g=snapshot.Materialize(root.transform,"Workbench "+snapshot.slot);gear.Add(g);foreach(var m in g.globalRolledMods)Stats.AddModifier(new StatModifier(m.statType,StatMappings.GetRolledModifierOperation(m.statType),m.value,g));if(g.ItemType==LootManager.GearType.Weapons)Player.EquipWeapon(g);}
            }finally{Stats.EndUpdate();}
            Metrics=Capture(build);
        }
        PlayerBuildMetrics Capture(PlayerBuildSnapshot b)
        {
            var m=new PlayerBuildMetrics();var avg=Player.BuildNonCriticalAttackContext();var lo=Player.BuildNonCriticalAttackContextAtRangeEnd(false);var hi=Player.BuildNonCriticalAttackContextAtRangeEnd(true);m.averageHit=Sum(avg);m.minimumHit=Sum(lo);m.maximumHit=Sum(hi);m.attacksPerSecond=Player.GetFinalAttackSpeed();m.critChance=Player.GetFinalCritChance();m.critMultiplier=CombatCalculator.BaseCriticalMultiplier+Stats.GetStat(StatTypes.CritMult);m.critContribution=m.averageHit*m.critChance*(m.critMultiplier-1);m.hitTwiceChance=Mathf.Clamp01(Stats.GetStat(StatTypes.ChanceToHitTwice));m.hitTwiceContribution=m.averageHit*m.hitTwiceChance;m.basicDps=(m.averageHit+m.critContribution+m.hitTwiceContribution)*m.attacksPerSecond;
            var weapon=gear.FirstOrDefault(x=>x.ItemType==LootManager.GearType.Weapons);m.baseWeaponAverage=weapon?.GetEffectiveBaseDamage()??0;m.weaponAttributeIncreased=Player.WeaponAttributeDamageBonus;m.playerLevelIncreased=Player.LevelDamageBonus;m.genericIncreased=Stats.GetStat(StatTypes.GenericDmg);m.physicalIncreased=Stats.GetStat(StatTypes.PhysDmg);m.genericMore=Stats.GetStat(StatTypes.GenericMult);m.physicalMore=Stats.GetStat(StatTypes.PhysMult);
            foreach(var h in avg.Hits){double expected=h.Amount*(1+m.critChance*(m.critMultiplier-1))*(1+m.hitTwiceChance)*m.attacksPerSecond;switch(h.Element){case Element.Phys:m.physicalOutput+=expected;break;case Element.Fire:m.fireOutput+=expected;break;case Element.Cold:m.coldOutput+=expected;break;case Element.Light:m.lightningOutput+=expected;break;case Element.Void:case Element.Poison:m.voidOutput+=expected;break;}}
            m.projectileCount=Math.Max(1,1+Stats.GetRawStat(StatTypes.ProjectileAmount));m.projectileTravelTime=WeaponMechanicProfile.ProjectileTravelTime(Stats.GetStat(StatTypes.ProjectileSpeed));m.precisionChance=WeaponMechanicProfile.PrecisionChance(Stats.GetStat(StatTypes.ProjectilePrecisionChance));m.precisionMultiplier=WeaponMechanicProfile.PrecisionMultiplier(Stats.GetStat(StatTypes.ProjectilePrecisionMultiplier));m.cooldownReduction=Stats.GetStat(StatTypes.CooldownReduction);
            m.poisonChance=Mathf.Clamp01(Stats.GetStat(StatTypes.PoisonChance));m.bleedChance=Mathf.Clamp01(Stats.GetStat(StatTypes.BleedChance));m.igniteChance=Mathf.Clamp01(Stats.GetStat(StatTypes.IgniteChance));m.shockChance=Mathf.Clamp01(Stats.GetStat(StatTypes.ShockChance));m.chillChance=Mathf.Clamp01(Stats.GetStat(StatTypes.ChillChance));m.shockEffect=Stats.GetStat(StatTypes.ShockEffect);m.chillEffect=Stats.GetStat(StatTypes.ChillEffect);
            // Neutral, zero-resistance single-hit ailment basis. Combat Lab owns sequence/stack simulation.
            m.poisonMagnitude=m.averageHit*(1+Stats.GetStat(StatTypes.PoisonDmg))*(1+Stats.GetStat(StatTypes.PoisonMult));m.bleedMagnitude=m.averageHit*(1+Stats.GetStat(StatTypes.BleedDmg))*(1+Stats.GetStat(StatTypes.BleedMult));m.igniteMagnitude=m.fireOutput/Math.Max(.0001,m.attacksPerSecond)*(1+Stats.GetStat(StatTypes.IgniteDmg))*(1+Stats.GetStat(StatTypes.IgniteMult));m.poisonDps=m.poisonMagnitude*m.poisonChance*m.attacksPerSecond;m.bleedDps=m.bleedMagnitude*m.bleedChance*m.attacksPerSecond;m.igniteDps=m.igniteMagnitude*m.igniteChance*m.attacksPerSecond;m.assumptions.Add("Ailment DPS is a neutral zero-resistance, single-application analytical basis; stacking and encounter timing are deferred to Combat Lab.");
            m.life=Health.MaxLife;m.mana=Mana.MaxMana;m.armour=Stats.GetStat(StatTypes.FlatArmour)*(1+Stats.GetStat(StatTypes.ArmourPercent));m.fireResistance=Resistance(StatTypes.FireRes);m.coldResistance=Resistance(StatTypes.ColdRes);m.lightningResistance=Resistance(StatTypes.LightRes);m.voidResistance=Resistance(StatTypes.VoidRes);m.lifeRegen=Stats.GetStat(StatTypes.LifeRegeneration);m.lifeOnHit=Stats.GetStat(StatTypes.LifeOnHit);m.lifeOnKill=Stats.GetStat(StatTypes.LifeOnKill);m.manaRegen=Stats.GetStat(StatTypes.ManaRegeneration);m.manaOnHit=Stats.GetStat(StatTypes.ManaOnHit);m.manaOnKill=Stats.GetStat(StatTypes.ManaOnKill);m.strength=DerivedStatCalculator.Strength(Stats);m.dexterity=DerivedStatCalculator.Dexterity(Stats);m.intelligence=DerivedStatCalculator.Intelligence(Stats);m.auraEffect=Stats.GetStat(StatTypes.AuraEffect);m.rageGeneration=Stats.GetStat(StatTypes.RageGeneration);m.rageEffect=Stats.GetStat(StatTypes.RageEffect);m.recoveryPerSecond=m.lifeRegen+m.lifeOnHit*m.attacksPerSecond;
            m.ehpPhysical=m.life/Math.Max(.01,CombatCalculator.ApplyArmourValue(100,(float)m.armour,0)/100);m.ehpFire=Ehp(m.life,m.fireResistance);m.ehpCold=Ehp(m.life,m.coldResistance);m.ehpLightning=Ehp(m.life,m.lightningResistance);m.ehpVoid=Ehp(m.life,m.voidResistance);m.totalGearScore=gear.Sum(x=>x.ItemType==LootManager.GearType.Weapons?x.GetAverageWeaponDps():x.rolledMods.Sum(y=>Math.Abs(y.value)+(y.hasSecondaryValue?Math.Abs(y.secondaryValue):0)));
            AddSkills(m,b);return m;
        }
        void AddSkills(PlayerBuildMetrics m,PlayerBuildSnapshot b)
        {
            var catalog=Resources.Load<PlayerSkillCatalog>("PlayerSkills");
            var definitions=catalog?.skills??PlayerSkillDefinition.CreateProductionDefaults();
            var bound=WeaponSkillBindings.For(b.weaponTypeId);
            if(!bound.All(id=>definitions.Any(x=>x!=null&&x.id==id)))definitions=PlayerSkillDefinition.CreateProductionDefaults();
            foreach(var sid in bound)
            {
                var s=definitions.FirstOrDefault(x=>x!=null&&x.id==sid);if(s==null)continue;
                double hits=Math.Max(1,s.baseHitCount);if(s.effect==WeaponSkillEffect.RapidFlurry)hits=WeaponMechanicProfile.RapidFlurryHits(Stats.GetStat(StatTypes.AttackSpeed));if(s.effect==WeaponSkillEffect.DoubleProjectiles)hits=m.projectileCount*2;
                double direct=m.averageHit*s.hitDamageMultiplier,use=direct*hits;double cd=s.castMode==PlayerSkillCastMode.QueuedAttackReplacement?0:Math.Max(PlayerSkillController.MinimumAutoCooldown,s.baseCooldown/(1+Math.Max(0,m.cooldownReduction)));
                var x=new SkillAnalyticalMetrics{name=s.displayName,castMode=s.castMode.ToString(),averageDirectHit=direct,expectedHits=hits,averageDamagePerUse=use,effectiveCooldown=cd,idealCooldownDps=cd>0?use/cd:0,manaCost=s.manaCost,sustainable=cd<=0||m.manaRegen*cd>=s.manaCost,assumption=cd>0?"Ideal cooldown DPS assumes sufficient Mana and uninterrupted eligible casts.":"Damage per use only; queued attack timing is not modeled as a rotation."};m.skills.Add(x);m.assumptions.Add(s.displayName+": "+x.assumption);
            }
        }
        double Resistance(StatTypes t)=>Mathf.Clamp(Stats.GetStat(t)+Stats.GetStat(StatTypes.AllRes),-.75f,.75f);static double Ehp(double life,double resistance)=>life/Math.Max(.01,1-resistance);static double Sum(DamageContext x)=>x.Hits?.Sum(h=>(double)h.Amount)??0;
        public void Dispose(){if(root!=null)UnityEngine.Object.DestroyImmediate(root);}
    }

    public static class PlayerBuildEvaluator
    {public static PlayerBuildMetrics Evaluate(PlayerBuildSnapshot build){if(build==null)throw new ArgumentNullException(nameof(build));using var e=new PlayerBuildEvaluation(build);return e.Metrics;}}

    [Serializable] public sealed class BuildAblationContribution{public string source;public double primary,secondary;}
    public static class PlayerBuildContributionAnalyzer
    {
        public static List<BuildAblationContribution> Analyze(PlayerBuildSnapshot build,OptimizationObjective objective)
        {
            var full=PlayerBuildEvaluator.Evaluate(build);var primary=OptimizationMetricCatalog.Get(objective.primary);var secondary=OptimizationMetricCatalog.Get(objective.secondary);var result=new List<BuildAblationContribution>();
            void Add(string source,Action<PlayerBuildSnapshot> remove){var copy=build.Clone();remove(copy);var metrics=PlayerBuildEvaluator.Evaluate(copy);result.Add(new BuildAblationContribution{source=source,primary=primary.Value(full)-primary.Value(metrics),secondary=secondary.Value(full)-secondary.Value(metrics)});}
            Add("Level Scaling",x=>x.playerLevel=1);Add("Gear",x=>x.equipment.Clear());Add("Weapon Local",x=>x.equipment.RemoveAll(g=>g.slot==LootManager.GearType.Weapons));Add("Passive Tree",x=>x.passiveStableIds.Clear());Add("Subclass",x=>x.subclassId=string.Empty);return result;
        }
    }

    public enum OptimizationMode{Weighted,Lexicographic}
    [Serializable] public sealed class OptimizationObjective{public string primary="basic_dps",secondary="life";[Range(0,1)]public float primaryWeight=.7f;public OptimizationMode mode;}
    public sealed class OptimizationMetric
    {public readonly string Id,Name;readonly Func<PlayerBuildMetrics,double> read;public OptimizationMetric(string id,string name,Func<PlayerBuildMetrics,double> f){Id=id;Name=name;read=f;}public double Value(PlayerBuildMetrics m)=>m==null?0:read(m);}
    public static class OptimizationMetricCatalog
    {
        public static readonly IReadOnlyList<OptimizationMetric> All=new[]{
            M("basic_dps","Expected Basic DPS",x=>x.basicDps),M("average_hit","Average Hit",x=>x.averageHit),M("physical","Physical Hit DPS",x=>x.physicalOutput),M("fire","Fire Hit DPS",x=>x.fireOutput),M("cold","Cold Hit DPS",x=>x.coldOutput),M("lightning","Lightning Hit DPS",x=>x.lightningOutput),M("void","Void Hit DPS",x=>x.voidOutput),M("attack_speed","Attacks per Second",x=>x.attacksPerSecond),M("cooldown","Cooldown Reduction",x=>x.cooldownReduction),M("crit_chance","Critical Chance",x=>x.critChance),M("crit_multiplier","Critical Multiplier",x=>x.critMultiplier),M("hit_twice","Hit Twice",x=>x.hitTwiceChance),M("poison_dps","Expected Poison DPS",x=>x.poisonDps),M("bleed_dps","Expected Bleed DPS",x=>x.bleedDps),M("ignite_dps","Expected Ignite DPS",x=>x.igniteDps),M("life","Maximum Life",x=>x.life),M("armour","Armour",x=>x.armour),M("ehp_physical","Effective Life vs Physical",x=>x.ehpPhysical),M("ehp_fire","Effective Life vs Fire",x=>x.ehpFire),M("ehp_cold","Effective Life vs Cold",x=>x.ehpCold),M("ehp_lightning","Effective Life vs Lightning",x=>x.ehpLightning),M("ehp_void","Effective Life vs Void",x=>x.ehpVoid),M("life_regen","Life Regeneration",x=>x.lifeRegen),M("mana","Maximum Mana",x=>x.mana),M("mana_regen","Mana Regeneration",x=>x.manaRegen),M("projectiles","Projectile Count",x=>x.projectileCount),M("precision","Precision Chance",x=>x.precisionChance),M("aura","Aura Effect",x=>x.auraEffect),M("rage_generation","Rage Generation",x=>x.rageGeneration),M("rage_effect","Rage Effect",x=>x.rageEffect),M("skill1","Skill 1 Damage per Use",x=>x.skills.Count>0?x.skills[0].averageDamagePerUse:0),M("skill2","Skill 2 Damage per Use",x=>x.skills.Count>1?x.skills[1].averageDamagePerUse:0)};
        static OptimizationMetric M(string id,string name,Func<PlayerBuildMetrics,double> f)=>new(id,name,f);public static OptimizationMetric Get(string id)=>All.FirstOrDefault(x=>x.Id==id)??All[0];
        // Signed log-relative improvement. The 5% reference floor keeps zero baselines finite and symmetric.
        public static double Normalized(double value,double baseline){double floor=Math.Max(1e-6,Math.Abs(baseline)*.05);return Math.Log((Math.Max(0,value)+floor)/(Math.Max(0,baseline)+floor));}
        public static double Score(PlayerBuildMetrics value,PlayerBuildMetrics baseline,OptimizationObjective objective){var p=Get(objective.primary);var s=Get(objective.secondary);double a=Normalized(p.Value(value),p.Value(baseline)),b=Normalized(s.Value(value),s.Value(baseline));return objective.mode==OptimizationMode.Lexicographic?a*1e6+b:a*Math.Clamp(objective.primaryWeight,0,1)+b*(1-Math.Clamp(objective.primaryWeight,0,1));}
    }
}
