using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class AffixAnalysisRequest
    { public PlayerBuildSnapshot build=new();public OptimizationObjective objective=new();public LootManager.GearType slot=LootManager.GearType.Weapons;public int itemLevel=60;public LootManager.GearRarity rarity=LootManager.GearRarity.Rare;public Element element=Element.Phys;public string weaponTypeId=WeaponTypeIds.Sword;public bool allTiers;public float rollPercent=.5f;public int pairTop=5;public bool replaceCurrentMod;public StatTypes replacedStat; }
    [Serializable] public sealed class AffixValueRow
    { public StatTypes stat;public string name,side;public int tier,itemLevel;public float roll,high;public bool paired;public double primaryDelta,secondaryDelta,objectiveDelta; }
    [Serializable] public sealed class AffixPairRow
    { public StatTypes a,b;public double gainA,gainB,combined,interaction; }
    [Serializable] public sealed class AffixAnalysisResult
    { public ExperimentMetadata metadata;public List<AffixValueRow> rows=new();public List<AffixPairRow> pairs=new();public bool Stale=>metadata!=null&&metadata.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint(); }
    [Serializable] public sealed class AffixCombatRow
    {public string affix;public double winRateDelta,p50DurationDelta,dpsDelta;}
    public static class AffixAnalyzer
    {
        public static AffixAnalysisResult Run(AffixAnalysisRequest request,Func<bool> cancelled=null)
        {
            var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");if(db==null)throw new InvalidOperationException("Production ModDatabase is missing.");db.Initialize();
            var result=new AffixAnalysisResult{metadata=ExperimentMetadata.Create("Affix Analyzer",request.build.seed,0,request.itemLevel,request.itemLevel,JsonUtility.ToJson(request))};var originalMetrics=PlayerBuildEvaluator.Evaluate(request.build);var baseBuild=request.build.Clone();var current=baseBuild.equipment.FirstOrDefault(x=>x.slot==request.slot);if(request.replaceCurrentMod){if(current==null||!current.mods.Any(x=>x.stat==request.replacedStat&&!x.implicitMod))throw new InvalidOperationException("Choose an existing replaceable mod on the current item.");current.mods.RemoveAll(x=>x.stat==request.replacedStat&&!x.implicitMod);}var baseMetrics=PlayerBuildEvaluator.Evaluate(baseBuild);var primary=OptimizationMetricCatalog.Get(request.objective.primary);var secondary=OptimizationMetricCatalog.Get(request.objective.secondary);
            foreach(var stat in GearStatLists.GetCanonicalStatPoolForType(request.slot).Distinct())
            {
                if(cancelled?.Invoke()==true)break;var def=db.GetDefinition(stat);if(def==null||def.allowedSlots==null||!def.allowedSlots.Contains(request.slot)||!def.AllowsWeaponType(request.weaponTypeId)||request.slot==LootManager.GearType.Weapons&&!ModManager.IsWeaponAffixEligible(stat,request.element))continue;
                var tiers=ModManager.ApplicableTiers(def,request.slot,request.weaponTypeId).Where(t=>t.minItemLevel<=request.itemLevel).OrderBy(t=>t.tierIndex).ToList();if(!request.allTiers&&tiers.Count>0)tiers=tiers.Take(1).ToList();
                foreach(var tier in tiers)
                {
                    // Controlled comparison keeps all other build sources unchanged.
                    var b=baseBuild.Clone();var gear=b.equipment.FirstOrDefault(x=>x.slot==request.slot);if(gear==null){gear=new GearSnapshot{slot=request.slot,rarity=request.rarity,itemLevel=request.itemLevel,element=request.element,weaponTypeId=request.weaponTypeId};b.equipment.Add(gear);}if(gear.mods.Any(x=>x.stat==stat)||!AffixPolicy.CanAdd(gear.mods.Select(x=>x.Restore()).ToList(),gear.rarity,def.side))continue;
                    if(def.groups!=null&&gear.mods.Where(x=>!x.implicitMod).Select(x=>db.GetDefinition(x.stat)).Where(x=>x?.groups!=null).Any(x=>x.groups.Intersect(def.groups).Any()))continue;
                    float roll=Mathf.Lerp(tier.minValue,tier.maxValue,Mathf.Clamp01(request.rollPercent));var mod=new RolledModSnapshot{stat=stat,tier=tier.tierIndex,value=roll,paired=tier.pairedDamage,high=tier.pairedDamage?Mathf.Lerp(tier.minHighValue,tier.maxHighValue,Mathf.Clamp01(request.rollPercent)):0};gear.mods.Add(mod);
                    var metrics=PlayerBuildEvaluator.Evaluate(b);result.rows.Add(new AffixValueRow{stat=stat,name=string.IsNullOrEmpty(def.displayName)?stat.ToString():def.displayName,side=def.side.ToString(),tier=tier.tierIndex,itemLevel=tier.minItemLevel,roll=roll,high=mod.high,paired=mod.paired,primaryDelta=primary.Value(metrics)-primary.Value(originalMetrics),secondaryDelta=secondary.Value(metrics)-secondary.Value(originalMetrics),objectiveDelta=OptimizationMetricCatalog.Score(metrics,originalMetrics,request.objective)});
                }
            }
            result.rows=result.rows.OrderByDescending(x=>x.objectiveDelta).ThenBy(x=>x.name,StringComparer.Ordinal).ToList();result.metadata.sampleCount=result.rows.Count;
            // Replacements consume the only freed affix slot on a full item; a two-affix addition is not the same experiment.
            if(request.replaceCurrentMod)return result;
            var top=result.rows.GroupBy(x=>x.stat).Select(g=>g.First()).Take(Mathf.Clamp(request.pairTop,0,20)).ToList();
            for(int i=0;i<top.Count;i++)for(int j=i+1;j<top.Count;j++)
            {
                if(cancelled?.Invoke()==true)return result;var b=baseBuild.Clone();var gear=b.equipment.FirstOrDefault(x=>x.slot==request.slot);if(gear==null){gear=new GearSnapshot{slot=request.slot,rarity=request.rarity,itemLevel=request.itemLevel,element=request.element,weaponTypeId=request.weaponTypeId};b.equipment.Add(gear);}if(gear.mods.Any(m=>m.stat==top[i].stat||m.stat==top[j].stat))continue;
                var first=db.GetDefinition(top[i].stat);var second=db.GetDefinition(top[j].stat);if(first.groups!=null&&second.groups!=null&&first.groups.Intersect(second.groups).Any())continue;
                var existing=gear.mods.Select(x=>x.Restore()).ToList();if(!AffixPolicy.CanAdd(existing,gear.rarity,first.side))continue;existing.Add(new RolledMod(top[i].stat,top[i].tier,top[i].roll));if(!AffixPolicy.CanAdd(existing,gear.rarity,second.side))continue;
                gear.mods.Add(new RolledModSnapshot{stat=top[i].stat,tier=top[i].tier,value=top[i].roll});gear.mods.Add(new RolledModSnapshot{stat=top[j].stat,tier=top[j].tier,value=top[j].roll});var metrics=PlayerBuildEvaluator.Evaluate(b);double both=OptimizationMetricCatalog.Score(metrics,baseMetrics,request.objective);result.pairs.Add(new AffixPairRow{a=top[i].stat,b=top[j].stat,gainA=top[i].objectiveDelta,gainB=top[j].objectiveDelta,combined=both,interaction=both-top[i].objectiveDelta-top[j].objectiveDelta});
            }
            result.pairs=result.pairs.OrderByDescending(x=>x.interaction).ToList();return result;
        }
        public static List<AffixCombatRow> CombatValidateTop(AffixAnalysisRequest request,AffixAnalysisResult ranking,CombatLabRequest combat,int count,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(ranking.Stale)throw new InvalidOperationException("Affix ranking is stale; rerun before combat validation.");var result=new List<AffixCombatRow>();var baselineRequest=JsonUtility.FromJson<CombatLabRequest>(JsonUtility.ToJson(combat));baselineRequest.player=request.build.Clone();var baseline=CombatLabAdapters.Batch(baselineRequest);var top=ranking.rows.Take(Mathf.Clamp(count,1,20)).ToList();
            for(int i=0;i<top.Count;i++)
            {
                if(cancelled?.Invoke()==true)break;var row=top[i];var build=request.build.Clone();var gear=build.equipment.FirstOrDefault(x=>x.slot==request.slot);if(gear==null)continue;if(request.replaceCurrentMod)gear.mods.RemoveAll(x=>x.stat==request.replacedStat&&!x.implicitMod);gear.mods.Add(new RolledModSnapshot{stat=row.stat,tier=row.tier,value=row.roll,high=row.high,paired=row.paired});var variation=JsonUtility.FromJson<CombatLabRequest>(JsonUtility.ToJson(combat));variation.player=build;var batch=CombatLabAdapters.Batch(variation);result.Add(new AffixCombatRow{affix=row.name+" T"+row.tier,winRateDelta=batch.winRate-baseline.winRate,p50DurationDelta=batch.duration.p50-baseline.duration.p50,dpsDelta=batch.playerDps.mean-baseline.playerDps.mean});progress?.Invoke((i+1f)/top.Count);
            }
            return result;
        }
    }

    public enum LootSourceMode { FixedRarity, SpecificEnemy, BiomePopulation, SpecificLocation, SpecificCombatLevel, StageRange, FullCombatLevelLoop, CustomWorldRange }
    [Serializable] public sealed class LootProgressionRequest
    {public PlayerBuildSnapshot build=new();public OptimizationObjective objective=new();public int combatLevel=60,kills=10000;public long seed=70001;public EnemyAI.EnemyRarity rarity=EnemyAI.EnemyRarity.Normal;public bool boss,progressive,generateActualEnemyGear=true,stopAtFirstUpgrade;public string archetypeId="Any",biomeId="",locationId="";public double minimumImprovement;public LootSourceMode sourceMode=LootSourceMode.FixedRarity;public int firstStage=1,lastStage=10,firstCombatLevel=1,lastCombatLevel=360;}
    [Serializable] public sealed class LootSourceBreakdown
    {public string biome,location,archetype,rarity,slot,itemRarity;public int stage,kills,drops,upgrades;public bool boss;}
    [Serializable] public sealed class LootUpgrade
    {public int kill;public string slot,rarity,biome,location,archetype,enemyRarity;public int stage;public bool boss;public double magnitude;public GearSnapshot item;}
    [Serializable] public sealed class LootProgressionResult
    {public ExperimentMetadata metadata;public int kills,drops,firstUpgradeKill,killsWithUpgrade;public List<LootUpgrade> upgrades=new();public MetricSummary upgradeIntervals,upgradeMagnitudes,firstInterval,secondInterval,thirdInterval,laterIntervals;public List<CurrencyStackData> currency=new();public List<LootSourceBreakdown> sources=new();public bool Stale=>metadata!=null&&metadata.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint();}
    [Serializable] public sealed class FirstUpgradeTrial { public int trial,kills;public bool censored;public double magnitude; }
    [Serializable] public sealed class FirstUpgradeCurvePoint { public int kills,found,censored;public double probabilityFound,probabilityNotFound; }
    [Serializable] public sealed class FirstUpgradeDistribution
    {public ExperimentMetadata metadata;public int trials,successes,censored,maxKills;public MetricSummary successfulKills;public List<FirstUpgradeTrial> observations=new();public List<FirstUpgradeCurvePoint> curve=new();}
    public enum LootSweepBuildSource { Low, Mid, Optimized, Tooling2Profile, CustomBuild }
    [Serializable] public sealed class LootProgressionSweepRow
    {public int level,kills;public double gearPerKill,currencyPerKill,upgradeChancePerKill,expectedKillsPerUpgrade,firstUpgradeMedian,averageUpgradeMagnitude,bossUpgradeContribution,rareLegendaryContribution;}
    public static class LootProgressionAnalyzer
    {
        public static List<LootProgressionSweepRow> RunWorldSweep(LootProgressionRequest template,int start,int end,int step,int loopsPerLevel,int firstUpgradeTrials,LootSweepBuildSource buildSource,PlayerGearProfileSO toolingProfile,Action<float> progress=null,Func<bool> cancelled=null)
        {
            var rows=new List<LootProgressionSweepRow>();start=Mathf.Clamp(start,1,360);end=Mathf.Clamp(end,start,360);step=Math.Max(1,step);loopsPerLevel=Math.Max(1,loopsPerLevel);
            for(int level=start;level<=end;level+=step)
            {
                if(cancelled?.Invoke()==true)break;var request=JsonUtility.FromJson<LootProgressionRequest>(JsonUtility.ToJson(template));request.combatLevel=level;request.sourceMode=LootSourceMode.FullCombatLevelLoop;request.kills=loopsPerLevel*10;request.progressive=false;
                var build=request.build.Clone();build.playerLevel=level;build.combatLevel=level;
                PlayerGearProfileSO profile=buildSource switch
                {
                    LootSweepBuildSource.Low=>AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>("Assets/Balance/Profiles/SO_PlayerGearProfile_Low.asset"),
                    LootSweepBuildSource.Mid=>AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>("Assets/Balance/Profiles/SO_PlayerGearProfile_Mid.asset"),
                    LootSweepBuildSource.Optimized=>AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>("Assets/Balance/Profiles/SO_PlayerGearProfile_Optimized.asset"),
                    LootSweepBuildSource.Tooling2Profile=>toolingProfile,
                    _=>null
                };
                if(buildSource!=LootSweepBuildSource.CustomBuild){if(profile==null)throw new InvalidOperationException("Selected production gear profile is missing.");build=PlayerGearsetOptimizer.Optimize(build,profile,request.objective,false).build;}
                request.build=build;var r=Run(request,null,cancelled);if(cancelled?.Invoke()==true)break;
                var first=firstUpgradeTrials>0?RunFirstUpgradeTrials(request,firstUpgradeTrials,Math.Max(10,request.kills),null,null,cancelled):null;
                var row=new LootProgressionSweepRow{level=level,kills=r.kills,gearPerKill=r.drops/(double)Math.Max(1,r.kills),currencyPerKill=r.currency.Sum(x=>x.amount)/(double)Math.Max(1,r.kills),upgradeChancePerKill=r.killsWithUpgrade/(double)Math.Max(1,r.kills),expectedKillsPerUpgrade=r.upgrades.Count==0?double.PositiveInfinity:r.kills/(double)r.upgrades.Count,firstUpgradeMedian=first?.successfulKills.p50??0,averageUpgradeMagnitude=r.upgradeMagnitudes.mean,bossUpgradeContribution=r.upgrades.Count==0?0:r.upgrades.Count(x=>x.boss)/(double)r.upgrades.Count,rareLegendaryContribution=r.upgrades.Count==0?0:r.upgrades.Count(x=>x.rarity is "Rare" or "Legendary")/(double)r.upgrades.Count};
                rows.Add(row);progress?.Invoke((level-start+step)/(float)Math.Max(step,end-start+step));
            }
            return rows;
        }
        public static FirstUpgradeDistribution RunFirstUpgradeTrials(LootProgressionRequest request,int trials,int maxKills,IEnumerable<int> thresholds=null,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(request.build==null)throw new ArgumentException("Select a starting build.");
            trials=Math.Max(1,trials);maxKills=Math.Max(1,maxKills);
            var distribution=new FirstUpgradeDistribution{metadata=ExperimentMetadata.Create("First Upgrade Trials",request.seed,trials,request.combatLevel,request.combatLevel,JsonUtility.ToJson(request)),maxKills=maxKills};
            for(int i=0;i<trials;i++)
            {
                if(cancelled?.Invoke()==true)break;
                var trial=JsonUtility.FromJson<LootProgressionRequest>(JsonUtility.ToJson(request));trial.seed=unchecked(request.seed+7919L*i);trial.kills=maxKills;trial.progressive=false;trial.stopAtFirstUpgrade=true;
                var result=Run(trial,null,cancelled,true);if(cancelled?.Invoke()==true)break;
                distribution.observations.Add(new FirstUpgradeTrial{trial=i+1,kills=result.firstUpgradeKill>0?result.firstUpgradeKill:maxKills,censored=result.firstUpgradeKill==0,magnitude=result.upgrades.FirstOrDefault()?.magnitude??0});
                progress?.Invoke((i+1f)/trials);
            }
            SummarizeFirstUpgradeObservations(distribution,thresholds);distribution.metadata.sampleCount=distribution.trials;return distribution;
        }
        public static void SummarizeFirstUpgradeObservations(FirstUpgradeDistribution distribution,IEnumerable<int> thresholds=null)
        {
            distribution.trials=distribution.observations.Count;distribution.censored=distribution.observations.Count(x=>x.censored);distribution.successes=distribution.trials-distribution.censored;
            distribution.successfulKills=MetricSummary.From(distribution.observations.Where(x=>!x.censored).Select(x=>(double)x.kills));
            var checkpoints=new SortedSet<int>(new[]{10,25,50,100,250,500,1000,distribution.maxKills});if(thresholds!=null)foreach(int t in thresholds)if(t>0)checkpoints.Add(t);
            distribution.curve.Clear();foreach(int kill in checkpoints.Where(x=>x<=distribution.maxKills))
            {int found=distribution.observations.Count(x=>!x.censored&&x.kills<=kill);distribution.curve.Add(new FirstUpgradeCurvePoint{kills=kill,found=found,censored=distribution.censored,probabilityFound=distribution.trials==0?0:found/(double)distribution.trials,probabilityNotFound=distribution.trials==0?1:1-found/(double)distribution.trials});}
        }
        public static WorldPosition ResolveWorldSource(LootProgressionRequest request,int kill,int trialSeed)
        {
            int level=Mathf.Clamp(request.combatLevel,1,WorldProgression.MaximumAuthoredCombatLevel);
            int stage=Mathf.Clamp(request.firstStage+(kill-1)%Mathf.Max(1,request.lastStage-request.firstStage+1),1,10);
            if(request.sourceMode==LootSourceMode.FullCombatLevelLoop)stage=1+(kill-1)%10;
            if(request.sourceMode==LootSourceMode.SpecificCombatLevel)stage=1+(kill-1)%10;
            if(request.sourceMode==LootSourceMode.CustomWorldRange)level=Mathf.Clamp(request.firstCombatLevel+(kill-1)%Mathf.Max(1,request.lastCombatLevel-request.firstCombatLevel+1),1,360);
            if(request.sourceMode is LootSourceMode.BiomePopulation or LootSourceMode.SpecificLocation)
            {
                var db=WorldContentCatalog.Reference;int index=db.biomes.FindIndex(x=>x?.stableId==request.biomeId);if(index<0)index=Mathf.Clamp((level-1)/60,0,5);
                int first=index*60+1,last=Mathf.Min(360,first+59);if(request.sourceMode==LootSourceMode.SpecificLocation){int location=db.biomes[index].locations.FindIndex(x=>x?.stableId==request.locationId);if(location<0)location=0;level=first+((kill-1)%6)*10+location;}else level=first+(kill-1)%Mathf.Max(1,last-first+1);
            }
            return WorldProgression.Resolve(level,stage,WorldContentCatalog.Reference,trialSeed+(kill-1)/10);
        }
        public static LootProgressionResult Run(LootProgressionRequest request,Action<float> progress=null,Func<bool> cancelled=null,bool skipMetadata=false)
        {
            var result=new LootProgressionResult{metadata=skipMetadata?null:ExperimentMetadata.Create("Loot Progression",request.seed,request.kills,request.combatLevel,request.combatLevel,JsonUtility.ToJson(request))};var rng=new SeededSimulationRandomSource(request.seed);var build=request.build.Clone();var baseline=request.build.Clone();var baselineMetrics=PlayerBuildEvaluator.Evaluate(baseline);var progressiveMetrics=baselineMetrics;var money=new Dictionary<CraftingCurrencyType,int>();var qualities=new Dictionary<string,List<float>>();var sources=new Dictionary<string,LootSourceBreakdown>();
            using var session=new WorkbenchSession();var roller=new GameObject("Loot progression roller"){hideFlags=HideFlags.HideAndDontSave};roller.SetActive(false);var loot=roller.AddComponent<LootManager>();try
            {
                for(int kill=1;kill<=request.kills;kill++)
                {
                    if(cancelled?.Invoke()==true)break;bool world=request.sourceMode is not (LootSourceMode.FixedRarity or LootSourceMode.SpecificEnemy);var position=world?ResolveWorldSource(request,kill,unchecked((int)request.seed)):default;int level=world?position.CombatLevel:request.combatLevel;bool boss=world?position.Encounter?.kind==EncounterKind.Boss:request.boss;string archetype=world?position.Encounter?.enemyArchetypeId:request.archetypeId;var rarity=request.rarity;
                    if(world){var db=WorldContentCatalog.Reference;var profiles=db.enemyRarityProfiles.Where(x=>x!=null&&x.spawnWeight>0).ToList();int total=profiles.Sum(x=>x.spawnWeight);if(total>0){int choice=rng.Range(0,total);foreach(var profile in profiles){if(choice<profile.spawnWeight){rarity=profile.rarity;break;}choice-=profile.spawnWeight;}}}
                    string qualityKey=$"{level}:{rarity}:{archetype}:{boss}";if(!qualities.TryGetValue(qualityKey,out var qualitySamples)){qualitySamples=new List<float>{1};if(request.generateActualEnemyGear&&!boss){var generated=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{level=level,sampleCount=Math.Min(request.kills,world?4:1000),rarity=rarity,archetypeId=string.IsNullOrEmpty(archetype)?"Any":archetype,seed=request.seed+37+qualities.Count});qualitySamples=generated.samples.Select(x=>Mathf.Clamp((float)x.gearScore/Mathf.Max(.001f,EnemyLootProfile.ExpectedGearScore(level,rarity,x.slotCount)),.60f,3f)).ToList();if(qualitySamples.Count==0)throw new InvalidOperationException("No production enemy gear samples were generated.");}qualities[qualityKey]=qualitySamples;}float quality=qualitySamples[(kill-1)%qualitySamples.Count];float expected=EnemyLootProfile.ExpectedGearScore(level,rarity,1);var power=new EnemyLootPowerSnapshot(EnemyLootProfile.LevelFactor(level),EnemyLootProfile.RarityMultiplier(rarity,boss),expected*quality,expected,quality);
                    string sourceKey=$"{position.BiomeLabel}|{position.LocationLabel}|{position.Stage}|{archetype}|{rarity}|{boss}";if(!sources.TryGetValue(sourceKey,out var source)){source=new LootSourceBreakdown{biome=world?position.BiomeLabel:"Fixed",location=world?position.LocationLabel:"Fixed",stage=world?position.Stage:0,archetype=archetype,rarity=rarity.ToString(),boss=boss};sources[sourceKey]=source;}source.kills++;
                    int gearCount=1+EnemyLootProfile.StochasticRound(power.ExtraGearBudget,EnemyLootProfile.MaximumExtraGear,rng);int currencyRolls=EnemyLootProfile.StochasticRound(power.CurrencyRollBudget,EnemyLootProfile.MaximumCurrencyRolls,rng);
                    for(int c=0;c<currencyRolls;c++){var entry=CurrencyLootTable.Choose(level,rarity,boss,level>=RebirthManager.RequiredZone,rng);if(entry!=null)money[entry.Currency]=money.GetValueOrDefault(entry.Currency)+rng.Range(entry.StackMinimum,entry.StackMaximum+1);}
                    for(int itemIndex=0;itemIndex<gearCount;itemIndex++)
                    {
                        var slot=loot.RollItemType(rng);var element=loot.RollItemElement(slot==LootManager.GearType.Weapons,rng);int itemLevel=level+(int)rarity;var itemRarity=loot.RollItemRarity(itemLevel,rng);string weapon=slot==LootManager.GearType.Weapons?LootManager.RollWeaponTypeId(rng):null;var gear=session.ReusableItem;gear.Initialize(slot,itemRarity,itemLevel,element,weapon);List<RolledMod> mods=null;for(int attempt=0;attempt<64&&mods==null;attempt++)mods=session.Roller.RollEquipmentModsForItem(slot,itemRarity,itemLevel,element,gear.WeaponTypeId,rng);if(mods==null)continue;gear.ApplyMods(mods);if(slot==LootManager.GearType.Weapons)LootManager.ApplyNaturalWeaponProfile(gear);var snapshot=GearSnapshot.Capture(gear);result.drops++;source.drops++;
                        var reference=request.progressive?build:baseline;var before=request.progressive?progressiveMetrics:baselineMetrics;var candidate=reference.Clone();candidate.equipment.RemoveAll(x=>x.slot==slot);candidate.equipment.Add(snapshot);var after=PlayerBuildEvaluator.Evaluate(candidate);double gain=OptimizationMetricCatalog.Score(after,before,request.objective);if(gain<=Math.Max(0,request.minimumImprovement))continue;result.upgrades.Add(new LootUpgrade{kill=kill,slot=slot.ToString(),rarity=itemRarity.ToString(),biome=source.biome,location=source.location,stage=source.stage,archetype=archetype,enemyRarity=rarity.ToString(),boss=boss,magnitude=gain,item=snapshot});source.upgrades++;if(request.progressive){build=candidate;progressiveMetrics=after;}
                    }
                    result.kills=kill;if(request.stopAtFirstUpgrade&&result.upgrades.Count>0)break;if((kill&127)==0)progress?.Invoke(kill/(float)request.kills);
                }
            }finally{UnityEngine.Object.DestroyImmediate(roller);}
            int previous=0;var intervals=new List<double>();foreach(var u in result.upgrades){intervals.Add(u.kill-previous);previous=u.kill;}result.firstUpgradeKill=result.upgrades.FirstOrDefault()?.kill??0;result.killsWithUpgrade=result.upgrades.Select(x=>x.kill).Distinct().Count();result.upgradeIntervals=MetricSummary.From(intervals);result.firstInterval=MetricSummary.From(intervals.Take(1));result.secondInterval=MetricSummary.From(intervals.Skip(1).Take(1));result.thirdInterval=MetricSummary.From(intervals.Skip(2).Take(1));result.laterIntervals=MetricSummary.From(intervals.Skip(3));result.upgradeMagnitudes=MetricSummary.From(result.upgrades.Select(x=>x.magnitude));result.currency=money.Select(x=>new CurrencyStackData(x.Key,x.Value)).ToList();result.sources=sources.Values.ToList();if(result.metadata!=null)result.metadata.sampleCount=result.kills;return result;
        }
    }
}
