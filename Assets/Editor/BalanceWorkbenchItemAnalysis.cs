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

    [Serializable] public sealed class LootProgressionRequest
    {public PlayerBuildSnapshot build=new();public OptimizationObjective objective=new();public int combatLevel=60,kills=10000;public long seed=70001;public EnemyAI.EnemyRarity rarity=EnemyAI.EnemyRarity.Normal;public bool boss,progressive,generateActualEnemyGear=true;public string archetypeId="Any";public double minimumImprovement;}
    [Serializable] public sealed class LootUpgrade
    {public int kill;public string slot,rarity;public double magnitude;public GearSnapshot item;}
    [Serializable] public sealed class LootProgressionResult
    {public ExperimentMetadata metadata;public int kills,drops,firstUpgradeKill,killsWithUpgrade;public List<LootUpgrade> upgrades=new();public MetricSummary upgradeIntervals,upgradeMagnitudes;public List<CurrencyStackData> currency=new();public bool Stale=>metadata!=null&&metadata.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint();}
    public static class LootProgressionAnalyzer
    {
        public static LootProgressionResult Run(LootProgressionRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            var result=new LootProgressionResult{metadata=ExperimentMetadata.Create("Loot Progression",request.seed,request.kills,request.combatLevel,request.combatLevel,JsonUtility.ToJson(request))};var rng=new SeededSimulationRandomSource(request.seed);var build=request.build.Clone();var baseline=request.build.Clone();var baselineMetrics=PlayerBuildEvaluator.Evaluate(baseline);var progressiveMetrics=baselineMetrics;var money=new Dictionary<CraftingCurrencyType,int>();float expected=EnemyLootProfile.ExpectedGearScore(request.combatLevel,request.rarity,1);var qualities=new List<float>{1};
            if(request.generateActualEnemyGear){var generated=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{level=request.combatLevel,sampleCount=Math.Min(request.kills,1000),rarity=request.rarity,archetypeId=request.archetypeId,seed=request.seed+37});qualities=generated.samples.Select(x=>Mathf.Clamp((float)x.gearScore/Mathf.Max(.001f,EnemyLootProfile.ExpectedGearScore(request.combatLevel,request.rarity,x.slotCount)),.60f,3f)).ToList();if(qualities.Count==0)throw new InvalidOperationException("No production enemy gear samples were generated.");}
            using var session=new WorkbenchSession();var roller=new GameObject("Loot progression roller"){hideFlags=HideFlags.HideAndDontSave};roller.SetActive(false);var loot=roller.AddComponent<LootManager>();try
            {
                for(int kill=1;kill<=request.kills;kill++)
                {
                    if(cancelled?.Invoke()==true)break;float quality=qualities[(kill-1)%qualities.Count];var power=new EnemyLootPowerSnapshot(EnemyLootProfile.LevelFactor(request.combatLevel),EnemyLootProfile.RarityMultiplier(request.rarity,request.boss),expected*quality,expected,quality);
                    int gearCount=1+EnemyLootProfile.StochasticRound(power.ExtraGearBudget,EnemyLootProfile.MaximumExtraGear,rng);int currencyRolls=EnemyLootProfile.StochasticRound(power.CurrencyRollBudget,EnemyLootProfile.MaximumCurrencyRolls,rng);
                    for(int c=0;c<currencyRolls;c++){var entry=CurrencyLootTable.Choose(request.combatLevel,request.rarity,request.boss,request.combatLevel>=RebirthManager.RequiredZone,rng);if(entry!=null)money[entry.Currency]=money.GetValueOrDefault(entry.Currency)+rng.Range(entry.StackMinimum,entry.StackMaximum+1);}
                    for(int itemIndex=0;itemIndex<gearCount;itemIndex++)
                    {
                        var slot=loot.RollItemType(rng);var element=loot.RollItemElement(slot==LootManager.GearType.Weapons,rng);int level=request.combatLevel+(int)request.rarity;var rarity=loot.RollItemRarity(level,rng);string weapon=slot==LootManager.GearType.Weapons?LootManager.RollWeaponTypeId(rng):null;var gear=session.ReusableItem;gear.Initialize(slot,rarity,level,element,weapon);List<RolledMod> mods=null;for(int attempt=0;attempt<64&&mods==null;attempt++)mods=session.Roller.RollEquipmentModsForItem(slot,rarity,level,element,gear.WeaponTypeId,rng);if(mods==null)continue;gear.ApplyMods(mods);if(slot==LootManager.GearType.Weapons)LootManager.ApplyNaturalWeaponProfile(gear);var snapshot=GearSnapshot.Capture(gear);result.drops++;
                        var reference=request.progressive?build:baseline;var before=request.progressive?progressiveMetrics:baselineMetrics;var candidate=reference.Clone();candidate.equipment.RemoveAll(x=>x.slot==slot);candidate.equipment.Add(snapshot);var after=PlayerBuildEvaluator.Evaluate(candidate);double gain=OptimizationMetricCatalog.Score(after,before,request.objective);if(gain<=Math.Max(0,request.minimumImprovement))continue;result.upgrades.Add(new LootUpgrade{kill=kill,slot=slot.ToString(),rarity=rarity.ToString(),magnitude=gain,item=snapshot});if(request.progressive){build=candidate;progressiveMetrics=after;}
                    }
                    result.kills=kill;if((kill&127)==0)progress?.Invoke(kill/(float)request.kills);
                }
            }finally{UnityEngine.Object.DestroyImmediate(roller);}
            int previous=0;var intervals=new List<double>();foreach(var u in result.upgrades){intervals.Add(u.kill-previous);previous=u.kill;}result.firstUpgradeKill=result.upgrades.FirstOrDefault()?.kill??0;result.killsWithUpgrade=result.upgrades.Select(x=>x.kill).Distinct().Count();result.upgradeIntervals=MetricSummary.From(intervals);result.upgradeMagnitudes=MetricSummary.From(result.upgrades.Select(x=>x.magnitude));result.currency=money.Select(x=>new CurrencyStackData(x.Key,x.Value)).ToList();result.metadata.sampleCount=result.kills;return result;
        }
    }
}
