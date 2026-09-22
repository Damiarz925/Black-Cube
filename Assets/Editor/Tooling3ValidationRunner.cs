using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlackCube.BalanceWorkbench;
using UnityEditor;
using UnityEngine;

public static class Tooling3ValidationRunner
{
    public static void RunValidators()=>Run(()=>
    {
        var db=WorldContentCatalog.Reference;var errors=WorldContentValidation.Validate(db);if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        var tooling=BalanceWorkbenchValidation.Validate();if(tooling.Length>0)throw new InvalidOperationException(string.Join("\n",tooling));
        ItemizationValidationRunner.RunStep14_5();var passiveErrors=PassiveTreeV3Validation.Validate();if(passiveErrors.Count>0)throw new InvalidOperationException(string.Join("\n",passiveErrors));WorldContentValidationRunner.ValidateReferenceContent();BaselineVerificationRunner.ValidateReferences();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Tooling3Validation.txt",$"PASS\nschema={GamePersistence.SchemaVersion}\narchetypes={db.enemyArchetypes.Count}\nbosses={db.bosses.Count}\nskills={db.enemySkills.Count}\nbehaviors={db.enemyBehaviorProfiles.Count}\nrarities={db.enemyRarityProfiles.Count}\ncorruptionProfiles={db.corruptionMechanicProfiles.Count}\nphaseProfiles={db.bossPhaseProfiles.Count}\ndata={ProductionBalanceAdapters.DataFingerprint()}\nBalanceLab=NOT RUN\n");
    });
    public static void RunSmoke()=>Run(()=>
    {
        Directory.CreateDirectory("Logs");var lines=new List<string>{"TOOLING 3 EDITOR WORKFLOW SMOKE"};var db=WorldContentCatalog.Reference;if(db.enemyRarityProfiles.Count!=4)throw new InvalidOperationException("Authoritative rarity profiles are missing.");var enemy=db.enemyArchetypes[0];var profile=db.BehaviorProfile(enemy.behaviorProfileId);
        var timeline=EnemyAuthoringAdapters.PreviewBehavior(db,profile,20,36001,.25f,.75f,100);if(timeline.timeline.Count!=20||timeline.timeline.Any(x=>x.decision.traces.Count==0))throw new InvalidOperationException("Behavior timeline failed.");lines.Add($"PASS authoring/behavior: {enemy.displayName}, profile={profile.stableId}, 20 explained actions");
        var a=EnemyAuthoringAdapters.PreviewEnemy(enemy.stableId,50,EnemyAI.EnemyRarity.Normal,0,36002);var b=EnemyAuthoringAdapters.PreviewEnemy(enemy.stableId,50,EnemyAI.EnemyRarity.Normal,0,36002);if(a.sample.equipment!=b.sample.equipment||a.sample.dps!=b.sample.dps)throw new InvalidOperationException("Enemy rerun changed.");var fire=EnemyAuthoringAdapters.PreviewEnemy(enemy.stableId,50,EnemyAI.EnemyRarity.Normal,0,36002,true,Element.Fire);lines.Add($"PASS enemy preview: exact rerun DPS={a.sample.dps:0.###}; forced Fire DPS={fire.sample.dps:0.###}");
        var original=EnemyScalingProfile.Default.Capture();var preview=original;preview.lifeGrowth+=.001f;var productionBefore=EnemyScalingMath.Calculate(100,EnemyScalingProfile.Default).LifeFactor;var previewValue=EnemyScalingMath.Calculate(100,preview).LifeFactor;if(previewValue==productionBefore||EnemyScalingProfile.Default.Capture().lifeGrowth!=original.lifeGrowth)throw new InvalidOperationException("Scaling preview isolation failed.");var curves=EnemyAuthoringAdapters.IntrinsicCurves(preview,1,360,10);lines.Add($"PASS scaling preview isolated: production L100={productionBefore:0.###}, preview={previewValue:0.###}");
        var generated=EnemyAuthoringAdapters.GearCurves(new EnemyCurveRequest{archetypeId=enemy.stableId,startLevel=40,endLevel=60,increment=20,samplesPerLevel=3,seed=36004,metric=EnemyCurveMetric.Dps,useScalingOverride=true,scalingOverride=preview});if(generated.Count!=5||generated.Any(x=>x.points.Count!=2||x.points.Any(p=>!double.IsFinite(p.mean))))throw new InvalidOperationException("Generated percentile curves failed.");lines.Add("PASS generated curves: P10/P50/Mean/P90/P99 through production optimizer");
        foreach(var corruption in db.corruptionMechanicProfiles){var resolved=EnemyAuthoringAdapters.PreviewEnemy(enemy.stableId,50,EnemyAI.EnemyRarity.Normal,corruption.percentage,36005);if(!double.IsFinite(resolved.sample.dps))throw new InvalidOperationException($"Corruption {corruption.percentage}% preview failed.");}lines.Add("PASS corruption resolution: 0/20/40/60/80/100 including apex data");
        var boss=db.bosses.First(x=>db.BossPhase(x.phaseProfileId)?.phases.Count>1);var phaseProfile=db.BossPhase(boss.phaseProfileId);if(BossPhaseResolver.Resolve(phaseProfile,.7f)==null||BossPhaseResolver.Resolve(phaseProfile,.4f)==null)throw new InvalidOperationException("Boss phase resolution failed.");lines.Add($"PASS boss phases: {boss.displayName} resolves authored thresholds");
        var batch=EnemyAuthoringAdapters.Batch(db.biomes[0].stableId,50,EnemyAI.EnemyRarity.Rare,2,36003);if(batch.rows.Count==0||batch.rows.Any(x=>!double.IsFinite(x.p90Dps)))throw new InvalidOperationException("Batch analyzer failed.");lines.Add($"PASS biome batch: {batch.rows.Count} archetypes; top P90 DPS={batch.rows.Max(x=>x.p90Dps):0.###}");
        string behaviorCsv=WorkbenchExports.SaveBehaviorCsv(timeline),scalingCsv=WorkbenchExports.SaveScalingCsv(curves),batchCsv=WorkbenchExports.SaveEnemyBatchCsv(batch),comparisonCsv=WorkbenchExports.SaveEnemyComparisonCsv(new[]{a,fire}),png=new BalanceWorkbenchChart().ExportPng(curves,"tooling3_smoke_curve",640,360);foreach(string path in new[]{behaviorCsv,scalingCsv,batchCsv,comparisonCsv,png})if(!File.Exists(path)||new FileInfo(path).Length==0)throw new InvalidOperationException("Missing export: "+path);lines.Add("PASS behavior/scaling/batch/comparison CSV and curve PNG exports");
        File.WriteAllLines("Logs/Tooling3EditorSmoke.txt",lines);Debug.Log(string.Join("\n",lines));
    });
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildTooling3Windows);
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
}
