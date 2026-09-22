using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class EnemyBehaviorTimelineEntry
    {public int action;public string skill,ruleId,reason;public EnemyBehaviorDecision decision;}
    [Serializable] public sealed class EnemyBehaviorPreviewResult
    {public string profileId;public long seed;public List<EnemyBehaviorTimelineEntry> timeline=new();}
    [Serializable] public sealed class EnemyPreviewSnapshot
    {public string archetypeId,displayName,biome,behaviorProfileId,loadoutId;public int level,corruption;public EnemyAI.EnemyRarity rarity;public long seed;public EnemySample sample;public List<string> skills=new(),rules=new();}
    [Serializable] public sealed class EnemyBatchRow
    {public string enemy,stableId,biome,behavior;public double meanLife,p90Life,meanDps,p90Dps,armour,primaryResistance,gearScore;public int skillCount;public List<string> warnings=new();}
    [Serializable] public sealed class EnemyBatchResult{public int level,samplesPerEnemy;public List<EnemyBatchRow> rows=new();}
    public enum EnemyCurveMetric{Life,DamagePerHit,Dps,Armour,GearScore,PrimaryResistance,Regeneration}
    [Serializable] public sealed class EnemyCurveRequest
    {public string archetypeId="Any";public EnemyAI.EnemyRarity rarity=EnemyAI.EnemyRarity.Normal;public int corruption,startLevel=1,endLevel=360,increment=10,samplesPerLevel=25;public long seed=37001;public EnemyCurveMetric metric=EnemyCurveMetric.Dps;public bool forceElement;public Element element=Element.Phys;public bool useScalingOverride;public EnemyScalingValues scalingOverride;public CorruptionMechanicProfile corruptionOverride;public EnemyRarityProfile rarityOverride;}

    public static class EnemyAuthoringAdapters
    {
        public static EnemyBehaviorPreviewResult PreviewBehavior(WorldContentDatabase db,EnemyBehaviorProfileDefinition profile,int steps,long seed,float selfLife=1,float playerLife=1,int corruption=0,string phase=null,EnemyBehaviorAilment playerAilments=EnemyBehaviorAilment.None,EnemyBehaviorAilment selfAilments=EnemyBehaviorAilment.None,int hitsTaken=0,int hitsDealt=0)
        {
            var result=new EnemyBehaviorPreviewResult{profileId=profile?.stableId,seed=seed};var state=new EnemyBehaviorRuntimeState{selfLifeFraction=selfLife,playerLifeFraction=playerLife,corruption=corruption,bossPhaseId=phase,playerAilments=playerAilments,selfAilments=selfAilments,hitsTaken=hitsTaken,hitsDealt=hitsDealt};var rng=new SeededSimulationRandomSource(seed);
            for(int i=0;i<Math.Max(1,steps);i++){state.completedAttacks=i;state.random01=rng.Value();var d=EnemyBehaviorResolver.Resolve(db,profile,state);var skill=db?.EnemySkill(d.selectedSkillId);result.timeline.Add(new EnemyBehaviorTimelineEntry{action=i+1,skill=skill?.displayName??d.selectedSkillId,ruleId=d.selectedRuleId,reason=d.reason,decision=d});}
            return result;
        }

        public static EnemyPreviewSnapshot PreviewEnemy(string archetypeId,int level,EnemyAI.EnemyRarity rarity,int corruption,long seed,bool forceElement=false,Element element=Element.Phys,bool useScalingOverride=false,EnemyScalingValues scalingOverride=default,CorruptionMechanicProfile corruptionOverride=null,EnemyRarityProfile rarityOverride=null)
        {
            var db=WorldContentCatalog.Reference;var archetype=db.Enemy(archetypeId)??db.enemyArchetypes.First();var request=new EnemyLabRequest{archetypeId=archetype.stableId,level=level,sampleCount=1,productionRarity=false,rarity=rarity,corruptionPercentage=corruption,seed=seed,forcePrimaryDamage=forceElement,primaryDamage=element,useScalingOverride=useScalingOverride,scalingOverride=scalingOverride,corruptionOverride=corruptionOverride,rarityOverride=rarityOverride};var result=ProductionBalanceAdapters.RunEnemies(request);var behavior=db.BehaviorProfile(archetype.behaviorProfileId)??db.BehaviorForLoadout(archetype.skillLoadoutId);string biome=BiomeFor(db,archetype.stableId)?.displayName??"Unassigned";
            return new EnemyPreviewSnapshot{archetypeId=archetype.stableId,displayName=archetype.displayName,biome=biome,behaviorProfileId=behavior?.stableId,loadoutId=archetype.skillLoadoutId,level=level,rarity=rarity,corruption=corruption,seed=seed,sample=result.samples.Single(),skills=(db.SkillLoadout(archetype.skillLoadoutId)?.skillIds??new()).ToList(),rules=(behavior?.rules??new()).Select(x=>x.stableId).ToList()};
        }

        public static List<CurveSeries> GearCurves(EnemyCurveRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            CurveSeries p10=new(){name="P10",color=new Color(.55f,.65f,.75f)},p50=new(){name="P50",color=Color.green},mean=new(){name="Mean",color=Color.cyan},p90=new(){name="P90",color=Color.yellow},p99=new(){name="P99",color=new Color(1f,.35f,.2f)};
            int start=Mathf.Max(1,request.startLevel),end=Mathf.Max(start,request.endLevel),step=Mathf.Max(1,request.increment),count=(end-start)/step+1,index=0;
            for(int level=start;level<=end;level+=step)
            {
                if(cancelled?.Invoke()==true)break;var r=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{archetypeId=request.archetypeId,level=level,sampleCount=Mathf.Max(1,request.samplesPerLevel),productionRarity=false,rarity=request.rarity,corruptionPercentage=request.corruption,seed=request.seed+level*7919,forcePrimaryDamage=request.forceElement,primaryDamage=request.element,useScalingOverride=request.useScalingOverride,scalingOverride=request.scalingOverride,corruptionOverride=request.corruptionOverride,rarityOverride=request.rarityOverride});var summary=Summary(r,request.metric);var point=new CurvePoint{x=level,mean=summary.mean,p10=summary.p10,p50=summary.p50,p90=summary.p90,p99=summary.p99};p10.points.Add(new CurvePoint{x=level,mean=point.p10});p50.points.Add(new CurvePoint{x=level,mean=point.p50});mean.points.Add(new CurvePoint{x=level,mean=point.mean});p90.points.Add(new CurvePoint{x=level,mean=point.p90});p99.points.Add(new CurvePoint{x=level,mean=point.p99});progress?.Invoke(++index/(float)count);
            }
            return new(){p10,p50,mean,p90,p99};
        }

        static MetricSummary Summary(EnemyLabResult result,EnemyCurveMetric metric)=>metric switch
        {
            EnemyCurveMetric.Life=>result.life,EnemyCurveMetric.Dps=>result.dps,EnemyCurveMetric.Armour=>result.armour,EnemyCurveMetric.GearScore=>result.gearScore,
            EnemyCurveMetric.DamagePerHit=>MetricSummary.From(result.samples.Select(x=>x.damagePerHit)),
            EnemyCurveMetric.PrimaryResistance=>MetricSummary.From(result.samples.Select(PrimaryResistance)),
            EnemyCurveMetric.Regeneration=>MetricSummary.From(result.samples.Select(x=>x.lifeRegeneration)),_=>result.dps
        };
        public static double PrimaryResistance(EnemySample x)=>x.primaryDamage switch{"Fire"=>x.fireResistance,"Cold"=>x.coldResistance,"Light"=>x.lightningResistance,"Void"=>x.voidResistance,_=>0};

        public static List<CurveSeries> IntrinsicCurves(EnemyScalingValues values,int start=1,int end=360,int step=5)
        {
            CurveSeries life=new(){name="Intrinsic Life Factor",color=Color.green},damage=new(){name="Intrinsic Damage Factor",color=Color.red},armour=new(){name="Intrinsic Armour",color=Color.cyan},resistance=new(){name="Intrinsic Resistance",color=Color.magenta};
            for(int level=Math.Max(1,start);level<=Math.Max(start,end);level+=Math.Max(1,step)){var x=EnemyScalingMath.Calculate(level,values);life.points.Add(new CurvePoint{x=level,mean=x.LifeFactor});damage.points.Add(new CurvePoint{x=level,mean=x.DamageFactor});armour.points.Add(new CurvePoint{x=level,mean=x.Armour});resistance.points.Add(new CurvePoint{x=level,mean=x.ResistancePoints});}return new(){life,damage,armour,resistance};
        }

        public static EnemyBatchResult Batch(string biomeId,int level,EnemyAI.EnemyRarity rarity,int samples,long seed,int corruption=0)
        {
            var db=WorldContentCatalog.Reference;var biome=db.Biome(biomeId);var ids=biome==null?new HashSet<string>(db.enemyArchetypes.Select(x=>x.stableId)):new HashSet<string>(biome.locations.SelectMany(x=>db.EncounterTable(x.encounterTableId)?.normalEnemyPool??new()).Select(x=>x.enemyArchetypeId));var result=new EnemyBatchResult{level=level,samplesPerEnemy=samples};int index=0;
            foreach(string id in ids.OrderBy(x=>x,StringComparer.Ordinal)){var archetype=db.Enemy(id);var r=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{archetypeId=id,level=level,sampleCount=Math.Max(1,samples),productionRarity=false,rarity=rarity,corruptionPercentage=corruption,seed=seed+index++*7919});var behavior=db.BehaviorProfile(archetype.behaviorProfileId)??db.BehaviorForLoadout(archetype.skillLoadoutId);var primary=MetricSummary.From(r.samples.Select(PrimaryResistance));var row=new EnemyBatchRow{enemy=archetype.displayName,stableId=id,biome=BiomeFor(db,id)?.displayName??"Unassigned",behavior=behavior?.displayName,meanLife=r.life.mean,p90Life=r.life.p90,meanDps=r.dps.mean,p90Dps=r.dps.p90,armour=r.armour.mean,primaryResistance=primary.mean,gearScore=r.gearScore.mean,skillCount=db.SkillLoadout(archetype.skillLoadoutId)?.skillIds.Count??0};if(behavior==null)row.warnings.Add("No behavior profile");if(behavior!=null&&!behavior.rules.Any(x=>x.fallbackEligible))row.warnings.Add("No fallback attack");if(row.skillCount==0)row.warnings.Add("No skills");if(!(db.SkillLoadout(archetype.skillLoadoutId)?.skillIds.Any(x=>db.EnemySkill(x)?.kind!=EnemySkillKind.Recover)??false))row.warnings.Add("No damaging skill");if(r.dps.p50>0&&r.dps.p90/r.dps.p50>1.75)row.warnings.Add("High P90/P50 DPS spread");result.rows.Add(row);}
            double medianDps=MetricSummary.From(result.rows.Select(x=>x.meanDps)).median,medianLife=MetricSummary.From(result.rows.Select(x=>x.meanLife)).median,medianRes=MetricSummary.From(result.rows.Select(x=>x.primaryResistance)).median;foreach(var row in result.rows){if(medianDps>0&&row.meanDps>medianDps*2)row.warnings.Add("Mean DPS > 2x group median");if(medianLife>0&&row.meanLife<medianLife*.5)row.warnings.Add("Life < 50% group median");if(row.primaryResistance>medianRes+20)row.warnings.Add("Resistance significantly above peers");}return result;
        }

        public static List<string> ValidateArchetype(WorldContentDatabase db,EnemyArchetypeDefinition enemy)
        {var errors=new List<string>();if(enemy==null){errors.Add("Missing enemy.");return errors;}if(string.IsNullOrWhiteSpace(enemy.stableId))errors.Add("Stable ID missing.");if(db.SkillLoadout(enemy.skillLoadoutId)==null)errors.Add("Missing skill loadout: "+enemy.skillLoadoutId);var behavior=db.BehaviorProfile(enemy.behaviorProfileId)??db.BehaviorForLoadout(enemy.skillLoadoutId);errors.AddRange(EnemyBehaviorValidation.Validate(db,behavior));if(db.BuildPreference(enemy.buildPreferenceId)==null)errors.Add("Missing build preference: "+enemy.buildPreferenceId);return errors;}
        public static BiomeDefinition BiomeFor(WorldContentDatabase db,string enemyId)=>db.biomes.FirstOrDefault(b=>b.locations.Any(l=>(db.EncounterTable(l.encounterTableId)?.normalEnemyPool??new()).Any(x=>x.enemyArchetypeId==enemyId)));
    }
}
