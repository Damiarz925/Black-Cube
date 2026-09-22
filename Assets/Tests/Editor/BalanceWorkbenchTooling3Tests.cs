using System;
using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BalanceWorkbenchTooling3Tests
{
    WorldContentDatabase db;
    [SetUp]public void SetUp(){WorldContentCatalog.Reload();db=WorldContentCatalog.Reference;}

    [Test]public void AuthoritativeAsset_OwnsCurrentProductionCatalog()
    {
        Assert.That(AssetDatabase.GetAssetPath(db),Is.EqualTo("Assets/Resources/GameData/WorldContentDatabase.asset"));
        Assert.That(db.enemyArchetypes,Has.Count.EqualTo(48));Assert.That(db.bosses,Has.Count.EqualTo(66));Assert.That(db.enemySkills,Has.Count.EqualTo(25));Assert.That(db.enemySkillLoadouts,Has.Count.EqualTo(12));Assert.That(db.enemyBehaviorProfiles,Has.Count.EqualTo(12));Assert.That(db.enemyRarityProfiles,Has.Count.EqualTo(4));
        Assert.That(WorldContentValidation.Validate(db),Is.Empty);
    }

    [Test]public void ScalingSnapshot_ExactlyMatchesProductionAndPreviewIsIsolated()
    {
        var profile=EnemyScalingProfile.Default;var original=profile.Capture();foreach(int level in new[]{1,50,100,101,240,360}){var a=EnemyScalingMath.Calculate(level,profile);var b=EnemyScalingMath.Calculate(level,original);Assert.That(b.LifeFactor,Is.EqualTo(a.LifeFactor));Assert.That(b.DamageFactor,Is.EqualTo(a.DamageFactor));Assert.That(b.Armour,Is.EqualTo(a.Armour));Assert.That(b.ResistancePoints,Is.EqualTo(a.ResistancePoints));}
        var preview=original;preview.lifeGrowth+=.01f;Assert.That(EnemyScalingMath.Calculate(80,preview).LifeFactor,Is.Not.EqualTo(EnemyScalingMath.Calculate(80,profile).LifeFactor));Assert.That(profile.Capture().lifeGrowth,Is.EqualTo(original.lifeGrowth));
    }

    [Test]public void BehaviorMigration_PreservesLegacyActionSequence()
    {
        foreach(var loadout in db.enemySkillLoadouts){var profile=db.BehaviorForLoadout(loadout.stableId);Assert.That(profile,Is.Not.Null);for(int turn=0;turn<30;turn++){var expected=Legacy(loadout,turn);var decision=EnemyBehaviorResolver.Resolve(db,profile,new EnemyBehaviorRuntimeState{completedAttacks=turn});Assert.That(decision.selectedSkillId,Is.EqualTo(expected),$"{loadout.stableId} turn {turn}");}}
    }

    [Test]public void BehaviorPreview_IsDeterministicAndExplainsEveryAction()
    {
        var profile=db.enemyBehaviorProfiles[0];var a=EnemyAuthoringAdapters.PreviewBehavior(db,profile,20,33001,.2f,.8f,100);var b=EnemyAuthoringAdapters.PreviewBehavior(db,profile,20,33001,.2f,.8f,100);Assert.That(a.timeline.Select(x=>x.skill),Is.EqualTo(b.timeline.Select(x=>x.skill)));Assert.That(a.timeline,Has.Count.EqualTo(20));Assert.That(a.timeline.All(x=>x.decision.traces.Count>0&&!string.IsNullOrWhiteSpace(x.reason)),Is.True);
    }

    [Test]public void EnemyPreview_UsesExactProductionGenerationAndSeed()
    {
        string id=db.enemyArchetypes[0].stableId;var a=EnemyAuthoringAdapters.PreviewEnemy(id,50,EnemyAI.EnemyRarity.Normal,0,34001);var b=EnemyAuthoringAdapters.PreviewEnemy(id,50,EnemyAI.EnemyRarity.Normal,0,34001);Assert.That(a.sample.equipment,Is.EqualTo(b.sample.equipment));Assert.That(a.sample.dps,Is.EqualTo(b.sample.dps));Assert.That(a.sample.life,Is.EqualTo(b.sample.life));Assert.That(double.IsFinite(a.sample.gearScore),Is.True);
    }

    [Test]public void BossAndCorruptionProfiles_ResolveCurrentProductionValues()
    {
        foreach(var boss in db.bosses){var phase=db.BossPhase(boss.phaseProfileId);Assert.That(phase,Is.Not.Null);Assert.That(phase.phases.Any(x=>x.beginsAtLifeFraction>=.7f),Is.True);Assert.That(phase.phases.Any(x=>x.beginsAtLifeFraction<=.5f),Is.True);}
        Assert.That(db.corruptionMechanicProfiles.Select(x=>x.percentage),Is.EqualTo(new[]{0,20,40,60,80,100}));Assert.That(db.corruptionMechanicProfiles.All(x=>x.damageMultiplier>0&&x.speedMultiplier>0&&x.recoveryMultiplier>0),Is.True);
    }

    [Test]public void BatchAnalyzer_ProducesFiniteSortableRows()
    {
        var r=EnemyAuthoringAdapters.Batch(db.biomes[0].stableId,20,EnemyAI.EnemyRarity.Normal,1,35001);Assert.That(r.rows,Is.Not.Empty);Assert.That(r.rows.All(x=>double.IsFinite(x.meanLife)&&double.IsFinite(x.meanDps)&&double.IsFinite(x.gearScore)&&double.IsFinite(x.primaryResistance)),Is.True);Assert.That(r.rows.OrderByDescending(x=>x.p90Dps).Count(),Is.EqualTo(r.rows.Count));
    }

    [Test]public void ExactPreview_ForwardsCorruptionIntoProductionGeneration()
    {
        string id=db.enemyArchetypes[0].stableId;var zero=EnemyAuthoringAdapters.PreviewEnemy(id,50,EnemyAI.EnemyRarity.Normal,0,36101);var apex=EnemyAuthoringAdapters.PreviewEnemy(id,50,EnemyAI.EnemyRarity.Normal,100,36101);Assert.That(apex.sample.dps,Is.GreaterThan(zero.sample.dps));Assert.That(apex.corruption,Is.EqualTo(100));
        var direct=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{archetypeId=id,level=50,sampleCount=1,rarity=EnemyAI.EnemyRarity.Normal,corruptionPercentage=100,seed=36101});Assert.That(apex.sample.equipment,Is.EqualTo(direct.samples[0].equipment));Assert.That(apex.sample.dps,Is.EqualTo(direct.samples[0].dps));
    }

    [Test]public void ScalingOverride_ChangesGearedPreviewWithoutMutatingProduction()
    {
        var original=EnemyScalingProfile.Default.Capture();var preview=original;preview.lifeGrowth+=.002f;string id=db.enemyArchetypes[0].stableId;var production=EnemyAuthoringAdapters.PreviewEnemy(id,80,EnemyAI.EnemyRarity.Normal,0,36201);var changed=EnemyAuthoringAdapters.PreviewEnemy(id,80,EnemyAI.EnemyRarity.Normal,0,36201,false,Element.Phys,true,preview);Assert.That(changed.sample.life,Is.Not.EqualTo(production.sample.life));Assert.That(EnemyScalingProfile.Default.Capture().lifeGrowth,Is.EqualTo(original.lifeGrowth));
    }

    [Test]public void ScalingApplyAndUndo_ControlledFixtureRestoresProduction()
    {
        var profile=EnemyScalingProfile.Default;var original=profile.Capture();var changed=original;changed.lifeGrowth+=.0001f;try{Undo.RecordObject(profile,"Tooling 3 scaling apply test");profile.Apply(changed);Assert.That(EnemyScalingMath.Calculate(50).LifeFactor,Is.Not.EqualTo(EnemyScalingMath.Calculate(50,original).LifeFactor));Undo.PerformUndo();Assert.That(profile.Capture().lifeGrowth,Is.EqualTo(original.lifeGrowth));}finally{profile.Apply(original);EditorUtility.ClearDirty(profile);}
    }

    [Test]public void BehaviorEdit_ChangesPreviewAndSharedResolverTogether()
    {
        var profile=JsonUtility.FromJson<EnemyBehaviorProfileDefinition>(JsonUtility.ToJson(db.enemyBehaviorProfiles[0]));profile.preserveLegacyRotatingCadence=false;var rule=profile.rules[0];rule.condition=EnemyBehaviorCondition.EveryNAttacks;rule.everyNAttacks=3;rule.priority=100;foreach(var other in profile.rules.Skip(1)){other.condition=EnemyBehaviorCondition.Always;other.priority=0;}
        var before=EnemyAuthoringAdapters.PreviewBehavior(db,profile,2,36301);rule.everyNAttacks=2;var after=EnemyAuthoringAdapters.PreviewBehavior(db,profile,2,36301);var runtime=EnemyBehaviorResolver.Resolve(db,profile,new EnemyBehaviorRuntimeState{completedAttacks=1,random01=.5f});Assert.That(before.timeline[1].ruleId,Is.Not.EqualTo(rule.stableId));Assert.That(after.timeline[1].ruleId,Is.EqualTo(rule.stableId));Assert.That(runtime.selectedRuleId,Is.EqualTo(after.timeline[1].ruleId));
    }

    [Test]public void ExtendedBehaviorConditions_AreDeterministicAndValidated()
    {
        var source=db.enemyBehaviorProfiles[0].rules[0];var profile=new EnemyBehaviorProfileDefinition{stableId="test.behavior",displayName="Test",preserveLegacyRotatingCadence=false,rules=new(){new EnemyActionRule{stableId="test.low",skillId=source.skillId,condition=EnemyBehaviorCondition.PlayerHasAilment,ailment=EnemyBehaviorAilment.Poison,priority=10,fallbackEligible=false},new EnemyActionRule{stableId="test.fallback",skillId=source.skillId,condition=EnemyBehaviorCondition.Always,priority=0,fallbackEligible=true}}};Assert.That(EnemyBehaviorValidation.Validate(db,profile),Is.Empty);var state=new EnemyBehaviorRuntimeState{playerAilments=EnemyBehaviorAilment.Poison,random01=.25f};Assert.That(EnemyBehaviorResolver.Resolve(db,profile,state).selectedRuleId,Is.EqualTo("test.low"));
    }

    [Test]public void BossPhaseResolver_UsesAuthoredThresholds()
    {
        var boss=db.bosses.First(x=>!x.challengeBoss);var profile=db.BossPhase(boss.phaseProfileId);var opening=BossPhaseResolver.Resolve(profile,.70f);var desperation=BossPhaseResolver.Resolve(profile,.40f);Assert.That(opening,Is.SameAs(profile.phases.OrderByDescending(x=>x.beginsAtLifeFraction).First()));Assert.That(desperation.beginsAtLifeFraction,Is.LessThan(opening.beginsAtLifeFraction));
    }

    [Test]public void GeneratedCurves_ExposeAllRequiredPercentilesAndOverrides()
    {
        var values=EnemyScalingProfile.Default.Capture();values.lifeGrowth+=.001f;var curves=EnemyAuthoringAdapters.GearCurves(new EnemyCurveRequest{archetypeId=db.enemyArchetypes[0].stableId,rarity=EnemyAI.EnemyRarity.Rare,corruption=100,startLevel=10,endLevel=20,increment=10,samplesPerLevel=2,seed=36401,metric=EnemyCurveMetric.Dps,useScalingOverride=true,scalingOverride=values});Assert.That(curves.Select(x=>x.name),Is.EqualTo(new[]{"P10","P50","Mean","P90","P99"}));Assert.That(curves.All(x=>x.points.Count==2&&x.points.All(p=>double.IsFinite(p.mean))),Is.True);
    }

    [Test]public void RarityProfiles_PreserveLegacyWeightsAndMappings()
    {
        Assert.That(db.enemyRarityProfiles.OrderBy(x=>x.rarity).Select(x=>x.spawnWeight),Is.EqualTo(new[]{40,20,10,1}));Assert.That(db.enemyRarityProfiles.All(x=>(int)x.gearRarity==(int)x.rarity&&x.lifeMultiplier==1&&x.damageMultiplier==1&&x.speedMultiplier==1),Is.True);
    }

    [Test]public void EveryCorruptionTier_UsesItsProductionProfileAndApexHooks()
    {
        string id=db.enemyArchetypes[0].stableId;double previous=0;foreach(int corruption in new[]{0,20,40,60,80,100}){var preview=EnemyAuthoringAdapters.PreviewEnemy(id,30,EnemyAI.EnemyRarity.Normal,corruption,36501);var profile=db.corruptionMechanicProfiles.Single(x=>x.percentage==corruption);Assert.That(preview.sample.dps,Is.GreaterThanOrEqualTo(previous));Assert.That(profile.damageMultiplier,Is.GreaterThanOrEqualTo(1));previous=preview.sample.dps;}Assert.That(db.corruptionMechanicProfiles.Single(x=>x.percentage==100).mechanicIds,Does.Contain("corruption.apex-modifier"));Assert.That(db.bosses.Count(x=>x.challengeBoss),Is.EqualTo(6));
    }

    string Legacy(EnemySkillLoadoutDefinition loadout,int turn)
    {
        for(int offset=0;offset<loadout.skillIds.Count;offset++){var skill=db.EnemySkill(loadout.skillIds[(turn+offset)%loadout.skillIds.Count]);if(skill!=null&&skill.cadence>0&&(turn+1)%skill.cadence==0)return skill.stableId;}return loadout.skillIds[turn%loadout.skillIds.Count];
    }
}
