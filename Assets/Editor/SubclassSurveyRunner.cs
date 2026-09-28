using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BlackCube.CombatSimulation;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class SubclassSurveyManifest
    {
        public string fingerprint,commit,startedUtc,updatedUtc;
        public int seriousCraftActions=12,finalistFights=10,finalFights=100;
        public List<SubclassSurveyManifestEntry> entries=new();
    }

    [Serializable] public sealed class SubclassSurveyManifestEntry
    {
        public int playerLevel;public string subclassId,status,resultHash,error;
        public long seed;public string updatedUtc;
    }

    [Serializable] public sealed class SubclassSurveyLevel
    {
        public int targetPlayerLevel,actualPlayerLevel,combatLevel,inventoryItems;
        public long seed,enemySeed;
        public string enemyArchetypeId,enemyHash,historyHash,inventoryHash;
        public float fireTarget,coldTarget,lightningTarget,voidTarget,physicalTarget,referencePhysicalHit;
        public ProgressionHistoryResult history;
        public RealisticInventoryResult ground;
        public EnemyPreviewSnapshot referenceEnemy;
    }

    [Serializable] public sealed class SubclassSurveyRun
    {
        public int targetPlayerLevel,actualPlayerLevel,combatLevel,passivePoints,finalistCount,selectedFinalist,combatRevision;
        public string subclassId,subclassName,classId,weaponTypeId,fingerprint,historyHash,inventoryHash,enemyHash,warning;
        public long seed;public double groundDps,analyticalDps,combatDps,combinedPhysicalReduction;
        public int craftedActions,craftedEquippedItems;
        public PlayerBuildSnapshot finalBuild;
        public PlayerBuildMetrics metrics;
        public RealisticCraftingResult crafting;
        public CombatBatchResult combat;
        public List<SubclassSurveyFinalist> finalists=new();
    }

    [Serializable] public sealed class SubclassSurveyFinalist
    {
        public PlayerBuildSnapshot build;public double analyticalDps,combatDps,winRate;
    }

    // Run with -executeMethod BlackCube.BalanceWorkbench.SubclassSurveyRunner.RunAll.
    // Every completed subclass has an independently checksummed artifact. A later
    // invocation resumes only PENDING/FAILED/RUNNING rows with matching data.
    public static class SubclassSurveyRunner
    {
        const string Root="ReviewCaptures/BalanceWorkbench/SubclassSurvey";
        static readonly int[] Levels={20,50,80};
        static readonly OptimizationObjective Objective=new()
        {primary="total_dps",secondary="average_hit",primaryWeight=1,mode=OptimizationMode.Weighted};

        public static void RunAll()
        {
            try{Execute();EditorApplication.Exit(0);}
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }

        public static void RerankCompleted()
        {
            try
            {
                string manifestPath=Path.Combine(Root,"manifest.json");
                var manifest=JsonUtility.FromJson<SubclassSurveyManifest>(File.ReadAllText(manifestPath));
                foreach(var entry in manifest.entries)
                {
                    string path=RunPath(entry.playerLevel,entry.subclassId);
                    var run=JsonUtility.FromJson<SubclassSurveyRun>(File.ReadAllText(path));
                    if(run.combatRevision>=2&&entry.status=="COMPLETE")continue;
                    entry.status="RUNNING";File.WriteAllText(manifestPath,JsonUtility.ToJson(manifest,true));
                    var shared=JsonUtility.FromJson<SubclassSurveyLevel>(File.ReadAllText(Path.Combine(Root,$"level{entry.playerLevel}_shared.json")));
                    for(int i=0;i<run.finalists.Count;i++)
                    {
                        var candidate=run.finalists[i];var metrics=PlayerBuildEvaluator.Evaluate(candidate.build);
                        var snapshot=CombatLabAdapters.PlayerSnapshot(candidate.build);
                        if(snapshot.skills.Count!=2)throw new InvalidOperationException("Production finalist is missing weapon skills.");
                        var lab=CombatLabAdapters.Batch(Request(shared,candidate.build,metrics,manifest.finalistFights));
                        if(lab.errors>0)throw new InvalidOperationException("Finalist Combat Lab error.");
                        candidate.combatDps=lab.playerDps.mean;candidate.winRate=lab.winRate;
                    }
                    run.selectedFinalist=run.finalists.Select((x,i)=>(x,i)).OrderByDescending(x=>x.x.combatDps).ThenBy(x=>x.i).First().i;
                    run.finalBuild=run.finalists[run.selectedFinalist].build;
                    run.metrics=PlayerBuildEvaluator.Evaluate(run.finalBuild);run.analyticalDps=run.metrics.totalSustainableDps;
                    run.combat=CombatLabAdapters.Batch(Request(shared,run.finalBuild,run.metrics,manifest.finalFights));
                    if(run.combat.errors>0)throw new InvalidOperationException("Final Combat Lab error.");
                    run.combatDps=run.combat.playerDps.mean;
                    var floors=new OptimizationConstraints{referencePhysicalHit=shared.referencePhysicalHit};
                    run.combinedPhysicalReduction=floors.CombinedPhysicalReduction(run.metrics);
                    var craftedIndices=run.crafting.actions.Select(x=>x.inventoryIndex).Distinct().ToHashSet();
                    run.craftedEquippedItems=run.finalBuild.equipment.Count(item=>run.crafting.inventory.items.Select((x,i)=>(x,i)).Any(x=>craftedIndices.Contains(x.i)&&JsonUtility.ToJson(x.x)==JsonUtility.ToJson(item)));
                    run.combatRevision=2;
                    string json=JsonUtility.ToJson(run,true);File.WriteAllText(path,json);
                    entry.resultHash=Hash(json);entry.status="COMPLETE";entry.updatedUtc=DateTime.UtcNow.ToString("O");manifest.updatedUtc=entry.updatedUtc;
                    File.WriteAllText(manifestPath,JsonUtility.ToJson(manifest,true));Export(manifest);
                    Debug.Log($"SUBCLASS SURVEY RERANK L{entry.playerLevel} {run.subclassName}: {run.combatDps:0.##}, skills {run.combat.skills.Count}");
                }
                EditorApplication.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }

        static void Execute()
        {
            Directory.CreateDirectory(Root);
            string fingerprint=ProductionBalanceAdapters.DataFingerprint();
            string manifestPath=Path.Combine(Root,"manifest.json");
            var manifest=File.Exists(manifestPath)?JsonUtility.FromJson<SubclassSurveyManifest>(File.ReadAllText(manifestPath)):new SubclassSurveyManifest
            {fingerprint=fingerprint,commit=ProductionBalanceAdapters.GitCommit(),startedUtc=DateTime.UtcNow.ToString("O")};
            if(manifest.fingerprint!=fingerprint)
                throw new InvalidOperationException("Survey fingerprint changed. Existing results are not comparable; preserve them and start a fresh survey directory.");
            foreach(int level in Levels)
            foreach(var subclass in SubclassCatalog.All)
            {
                if(!manifest.entries.Any(x=>x.playerLevel==level&&x.subclassId==subclass.Id))
                    manifest.entries.Add(new SubclassSurveyManifestEntry
                    {playerLevel=level,subclassId=subclass.Id,seed=level*1000L+1,status="PENDING"});
            }
            SaveManifest();
            foreach(int level in Levels)
            {
                var shared=LoadOrCreateLevel(level);
                foreach(var subclass in SubclassCatalog.All)
                {
                    var entry=manifest.entries.Single(x=>x.playerLevel==level&&x.subclassId==subclass.Id);
                    string artifact=RunPath(level,subclass.Id);
                    if(entry.status=="COMPLETE"&&File.Exists(artifact)&&Hash(File.ReadAllText(artifact))==entry.resultHash)
                        continue;
                    entry.status="RUNNING";entry.error=null;entry.updatedUtc=DateTime.UtcNow.ToString("O");SaveManifest();
                    try
                    {
                        Debug.Log($"SUBCLASS SURVEY START L{level} {subclass.DisplayName}");
                        var result=RunOne(shared,subclass,manifest);
                        string json=JsonUtility.ToJson(result,true);
                        File.WriteAllText(artifact,json);
                        entry.resultHash=Hash(json);entry.status="COMPLETE";
                        Debug.Log($"SUBCLASS SURVEY COMPLETE L{level} {subclass.DisplayName}: analytical={result.analyticalDps:0.##}, lab={result.combatDps:0.##}");
                    }
                    catch(Exception ex)
                    {
                        entry.status="FAILED";entry.error=ex.ToString();
                        Debug.LogError($"SUBCLASS SURVEY FAILED L{level} {subclass.DisplayName}: {ex}");
                    }
                    entry.updatedUtc=DateTime.UtcNow.ToString("O");SaveManifest();
                    Export(manifest);
                }
            }
            Export(manifest);
            if(manifest.entries.Any(x=>x.status!="COMPLETE"))
                throw new InvalidOperationException("Subclass survey has failed or incomplete rows; inspect manifest.json and rerun after fixing the cause.");

            void SaveManifest()
            {manifest.updatedUtc=DateTime.UtcNow.ToString("O");File.WriteAllText(manifestPath,JsonUtility.ToJson(manifest,true));}
        }

        static SubclassSurveyLevel LoadOrCreateLevel(int level)
        {
            string path=Path.Combine(Root,$"level{level}_shared.json");
            if(File.Exists(path))return JsonUtility.FromJson<SubclassSurveyLevel>(File.ReadAllText(path));
            long seed=level*1000L+1;
            var history=ProgressionHistorySimulator.Run(new ProgressionHistoryRequest
            {targetPlayerLevel=level,mode=ProgressionHistoryMode.Straight,stochastic=true,seed=seed});
            if(!history.targetReached)throw new InvalidOperationException($"Straight progression did not reach player level {level}: {history.warning}");
            var ground=RealisticInventoryGenerator.Generate(history,seed+1);
            if(ground.generatedItems!=ground.items.Count)throw new InvalidOperationException("Historical item count changed during generation.");
            var shared=new SubclassSurveyLevel
            {targetPlayerLevel=level,actualPlayerLevel=history.finalPlayerLevel,combatLevel=history.finalCombatLevel,
                inventoryItems=ground.items.Count,seed=seed,history=history,ground=ground,
                historyHash=Hash(JsonUtility.ToJson(history)),inventoryHash=Hash(JsonUtility.ToJson(ground))};
            SetDefenseTargets(shared);
            var archetype=WorldContentCatalog.Reference.enemyArchetypes.First(x=>x.primaryElement==Element.Phys);
            var samples=new List<EnemyPreviewSnapshot>();
            for(int i=0;i<101;i++)samples.Add(EnemyAuthoringAdapters.PreviewEnemy(archetype.stableId,
                shared.combatLevel,EnemyAI.EnemyRarity.Rare,0,seed+10000+i));
            shared.referenceEnemy=samples.OrderBy(x=>x.sample.damagePerHit).ElementAt(samples.Count/2);
            shared.referencePhysicalHit=(float)shared.referenceEnemy.sample.damagePerHit;
            shared.enemyArchetypeId=archetype.stableId;shared.enemySeed=shared.referenceEnemy.seed;
            shared.enemyHash=Hash(JsonUtility.ToJson(shared.referenceEnemy));
            shared.physicalTarget=(shared.fireTarget+shared.coldTarget+shared.lightningTarget+shared.voidTarget)/4;
            File.WriteAllText(path,JsonUtility.ToJson(shared,true));
            Debug.Log($"SUBCLASS SURVEY SHARED L{level}: combat L{shared.combatLevel}, ground items {ground.generatedItems}, Rare hit P50 {shared.referencePhysicalHit:0.##}");
            return shared;
        }

        static void SetDefenseTargets(SubclassSurveyLevel shared)
        {
            using var session=new WorkbenchSession();var db=session.Roller.Database;
            var pressure=ResistancePressureAnalyzer.Analyze(db);
            var totals=new float[4];
            foreach(var investment in pressure.referenceInvestment)
            {
                var tier=ModManager.ApplicableTiers(db.GetDefinition(investment.stat),investment.slot)
                    .Where(x=>x.minItemLevel<=shared.combatLevel).OrderBy(x=>x.tierIndex).FirstOrDefault();
                if(tier==null)continue;
                float value=(tier.minValue+tier.maxValue)*.005f;
                if(investment.stat==StatTypes.AllRes){for(int i=0;i<3;i++)totals[i]+=value;}
                else{int i=investment.stat==StatTypes.FireRes?0:investment.stat==StatTypes.ColdRes?1:
                    investment.stat==StatTypes.LightRes?2:3;totals[i]+=value;}
            }
            shared.fireTarget=Mathf.Min(.75f,totals[0]);shared.coldTarget=Mathf.Min(.75f,totals[1]);
            shared.lightningTarget=Mathf.Min(.75f,totals[2]);shared.voidTarget=Mathf.Min(.75f,totals[3]);
        }

        static SubclassSurveyRun RunOne(SubclassSurveyLevel shared,SubclassDefinition subclass,SubclassSurveyManifest manifest)
        {
            string weapon=subclass.ParentClassId switch
            {PlayerClassIds.Warrior=>WeaponTypeIds.Sword,PlayerClassIds.Barbarian=>WeaponTypeIds.TwoHandedAxe,
                PlayerClassIds.Ranger=>WeaponTypeIds.Bow,PlayerClassIds.Mage=>WeaponTypeIds.Staff,
                PlayerClassIds.Priest=>WeaponTypeIds.Sceptre,_=>WeaponTypeIds.Dagger};
            var build=new PlayerBuildSnapshot
            {playerLevel=shared.actualPlayerLevel,combatLevel=shared.combatLevel,
                classId=subclass.ParentClassId,subclassId=subclass.Id,weaponTypeId=weapon,
                projectileMode=SubclassProjectileMode.Volley,seed=shared.seed,
                dataFingerprint=manifest.fingerprint};
            var floors=new OptimizationConstraints
            {minimumFireResistanceEnabled=true,minimumFireResistance=shared.fireTarget,
                minimumColdResistanceEnabled=true,minimumColdResistance=shared.coldTarget,
                minimumLightningResistanceEnabled=true,minimumLightningResistance=shared.lightningTarget,
                minimumVoidResistanceEnabled=true,minimumVoidResistance=shared.voidTarget,
                minimumCombinedPhysicalReductionEnabled=true,
                minimumCombinedPhysicalReduction=shared.physicalTarget,
                referencePhysicalHit=shared.referencePhysicalHit};
            // The optimizer clones all historical items before each subclass's
            // independent craft. The canonical shared artifact is never mutated.
            var crafting=RealisticCraftingOptimizer.Run(build,shared.ground,Objective,floors,
                RealisticCraftingSearch.Serious,manifest.seriousCraftActions,shared.seed+2);
            var joint=RealisticPlayerOptimizer.Optimize(build,crafting.inventory,Objective,floors,
                DefenseAdherence.Soft,shared.actualPlayerLevel,2);
            if(joint.build==null)throw new InvalidOperationException("Joint gear/passive search found no build: "+joint.warning);
            if(!PlayerProgression.ValidateAllocationState(joint.build.AllocationRanks(),build.classId,build.subclassId))
                throw new InvalidOperationException("Joint optimizer returned an illegal passive allocation.");
            var groundSource=joint.build.Clone();groundSource.equipment.Clear();
            var ground=RealisticGearsetOptimizer.Optimize(groundSource,shared.ground,Objective,floors);
            var alternatives=RealisticGearsetOptimizer.Optimize(joint.build,crafting.inventory,Objective,floors);
            var candidates=new List<PlayerBuildSnapshot>{joint.build};
            foreach(var candidate in alternatives.finalists)
                if(!candidates.Any(x=>EquipmentKey(x)==EquipmentKey(candidate)))
                    candidates.Add(candidate);
            var run=new SubclassSurveyRun
            {targetPlayerLevel=shared.targetPlayerLevel,actualPlayerLevel=shared.actualPlayerLevel,
                combatLevel=shared.combatLevel,passivePoints=joint.build.passiveStableIds.Count,
                subclassId=subclass.Id,subclassName=subclass.DisplayName,classId=subclass.ParentClassId,
                weaponTypeId=weapon,seed=shared.seed,fingerprint=manifest.fingerprint,
                historyHash=shared.historyHash,inventoryHash=shared.inventoryHash,enemyHash=shared.enemyHash,
                groundDps=ground.metrics?.totalSustainableDps??0,crafting=crafting,
                craftedActions=crafting.actions.Count,warning=joint.warning};
            for(int i=0;i<Math.Min(10,candidates.Count);i++)
            {
                var candidate=candidates[i];var metrics=PlayerBuildEvaluator.Evaluate(candidate);
                var lab=CombatLabAdapters.Batch(Request(shared,candidate,metrics,manifest.finalistFights));
                if(lab.errors>0)throw new InvalidOperationException($"Finalist {i} produced {lab.errors} Combat Lab errors.");
                run.finalists.Add(new SubclassSurveyFinalist
                {build=candidate,analyticalDps=metrics.totalSustainableDps,
                    combatDps=lab.playerDps.mean,winRate=lab.winRate});
            }
            if(run.finalists.Count==0)throw new InvalidOperationException("No Combat Lab finalists were retained.");
            run.selectedFinalist=run.finalists.Select((x,i)=>(x,i))
                .OrderByDescending(x=>x.x.combatDps).ThenBy(x=>x.i).First().i;
            run.finalBuild=run.finalists[run.selectedFinalist].build;
            run.metrics=PlayerBuildEvaluator.Evaluate(run.finalBuild);
            run.analyticalDps=run.metrics.totalSustainableDps;
            run.combat=CombatLabAdapters.Batch(Request(shared,run.finalBuild,run.metrics,manifest.finalFights));
            if(run.combat.errors>0)throw new InvalidOperationException($"Final build produced {run.combat.errors} Combat Lab errors.");
            run.combatDps=run.combat.playerDps.mean;
            run.combinedPhysicalReduction=floors.CombinedPhysicalReduction(run.metrics);
            var craftedIndices=crafting.actions.Select(x=>x.inventoryIndex).Distinct().ToHashSet();
            run.craftedEquippedItems=run.finalBuild.equipment.Count(item=>
                crafting.inventory.items.Select((x,i)=>(x,i)).Any(x=>craftedIndices.Contains(x.i)&&
                    JsonUtility.ToJson(x.x)==JsonUtility.ToJson(item)));
            run.finalistCount=run.finalists.Count;
            run.combatRevision=2;
            return run;
        }

        static CombatLabRequest Request(SubclassSurveyLevel shared,PlayerBuildSnapshot build,
            PlayerBuildMetrics metrics,int fights)
        {
            var request=new CombatLabRequest
            {player=build,analyticalSelectedSkillPolicy=metrics.selectedSkillPolicy,
                enemyArchetypeId=shared.enemyArchetypeId,enemyLevel=shared.combatLevel,
                rarity=EnemyAI.EnemyRarity.Rare,seed=shared.enemySeed,fightCount=fights,
                samplingMode=EnemySamplingMode.Representative};
            string policy=metrics.selectedSkillPolicy??"";
            if(policy=="No Skills")request.config.actionPolicy=PlayerActionPolicy.BasicOnly;
            else if(policy=="Skill 1 Only")request.config.enableSkill2=false;
            else if(policy=="Skill 2 Only")request.config.enableSkill1=false;
            else if(policy.StartsWith("Skill 2",StringComparison.Ordinal))request.config.actionPolicy=PlayerActionPolicy.Skill2Priority;
            request.config.retainTrace=false;
            return request;
        }

        static void Export(SubclassSurveyManifest manifest)
        {
            var completed=new List<SubclassSurveyRun>();
            foreach(var entry in manifest.entries.Where(x=>x.status=="COMPLETE"))
            {
                string path=RunPath(entry.playerLevel,entry.subclassId);
                if(File.Exists(path)&&Hash(File.ReadAllText(path))==entry.resultHash)
                    completed.Add(JsonUtility.FromJson<SubclassSurveyRun>(File.ReadAllText(path)));
            }
            string header="Level,Subclass,Class,CombatLevel,AnalyticalDPS,CombatDPS,WinRate,Life,Armour,ExplicitPDR,CombinedPDR,FireRes,ColdRes,LightningRes,VoidRes,ManaStarvation,Crafts,EquippedCrafted,PassivePoints,Finalists";
            string Row(SubclassSurveyRun r)=>string.Join(",",new[]{r.targetPlayerLevel.ToString(),Csv(r.subclassName),Csv(r.classId),r.combatLevel.ToString(),N(r.analyticalDps),N(r.combatDps),N(r.combat.winRate),N(r.metrics.life),N(r.metrics.armour),N(r.metrics.physicalDamageReduction),N(r.combinedPhysicalReduction),N(r.metrics.fireResistance),N(r.metrics.coldResistance),N(r.metrics.lightningResistance),N(r.metrics.voidResistance),N(r.metrics.manaStarvationFraction),r.craftedActions.ToString(),r.craftedEquippedItems.ToString(),r.passivePoints.ToString(),r.finalistCount.ToString()});
            foreach(int level in Levels)
                File.WriteAllLines(Path.Combine(Root,$"subclass_survey_level{level}.csv"),
                    new[]{header}.Concat(completed.Where(x=>x.targetPlayerLevel==level).Select(Row)));
            File.WriteAllLines(Path.Combine(Root,"subclass_survey_cross_level.csv"),
                new[]{header}.Concat(completed.OrderBy(x=>x.targetPlayerLevel).ThenBy(x=>x.subclassName).Select(Row)));
            File.WriteAllText(Path.Combine(Root,"subclass_survey_raw.json"),
                JsonUtility.ToJson(new SubclassSurveyRaw{runs=completed},true));
            var text=new StringBuilder("# Subclass survey\n\n");
            text.AppendLine($"Status: {completed.Count}/36 complete. Fingerprint: `{manifest.fingerprint}`. Commit: `{manifest.commit}`.");
            text.AppendLine($"Settings: straight progression; shared level seed; Serious crafting ({manifest.seriousCraftActions} actions); soft derived resistance and combined-PDR targets; {manifest.finalistFights} fights/finalist and {manifest.finalFights} fights/final build.\n");
            foreach(int level in Levels)
            {
                text.AppendLine($"## Player level {level}\n\n| Subclass | Combat level | Analytical DPS | Combat DPS | Win rate |\n|---|---:|---:|---:|---:|");
                foreach(var r in completed.Where(x=>x.targetPlayerLevel==level).OrderByDescending(x=>x.combatDps))
                    text.AppendLine($"| {r.subclassName} | {r.combatLevel} | {r.analyticalDps:0.##} | {r.combatDps:0.##} | {r.combat.winRate:P1} |");
                text.AppendLine();
            }
            foreach(var failure in manifest.entries.Where(x=>x.status=="FAILED"))
                text.AppendLine($"- FAILED L{failure.playerLevel} {failure.subclassId}: {failure.error?.Split('\n')[0]}");
            File.WriteAllText(Path.Combine(Root,"subclass_survey_summary.md"),text.ToString());
        }

        [Serializable] sealed class SubclassSurveyRaw{public List<SubclassSurveyRun> runs=new();}
        static string EquipmentKey(PlayerBuildSnapshot build)=>string.Join("|",build.equipment.Select(JsonUtility.ToJson));
        static string RunPath(int level,string id)=>Path.Combine(Root,$"level{level}_{id.Replace('.','_')}.json");
        static string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static string Csv(string value)=>"\""+(value??"").Replace("\"","\"\"")+"\"";
        static string Hash(string value)
        {using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value??""))).Replace("-","");}
    }

    [Serializable] public sealed class SubclassSurveyAblation
    {
        public int level,allocatedPoints;
        public string subclassId;
        public double analyticalDps,withoutPassivesDps,withoutSubclassDps;
        public double passiveDelta,subclassDelta;
        public CombatBatchResult withoutPassivesCombat,withoutSubclassCombat;
        public CombatSimulationResult longerTarget;
        public GearSnapshot bestGroundPhysicalAxe;
        public double bestGroundPhysicalAxeWeaponDps,selectedWeaponDps;
    }

    [Serializable] public sealed class SubclassSurveyAblationSet
    {
        public List<SubclassSurveyAblation> runs=new();
    }

    // Postprocess the sealed, checksummed runs without changing their results.
    // These are analytical one-factor ablations, not Combat Lab fight reruns.
    public static class SubclassSurveyAblationRunner
    {
        public static void Run()
        {
            try
            {
                const string root="ReviewCaptures/BalanceWorkbench/SubclassSurvey";
                var manifest=JsonUtility.FromJson<SubclassSurveyManifest>(File.ReadAllText(Path.Combine(root,"manifest.json")));
                if(manifest.entries.Count!=36||manifest.entries.Any(x=>x.status!="COMPLETE"))
                    throw new InvalidOperationException("All 36 survey runs must be complete before ablation analysis.");
                var result=new SubclassSurveyAblationSet();
                var originals=new List<SubclassSurveyRun>();
                foreach(var entry in manifest.entries)
                {
                    string path=Path.Combine(root,$"level{entry.playerLevel}_{entry.subclassId.Replace('.','_')}.json");
                    string json=File.ReadAllText(path);
                    using(var sha=SHA256.Create())
                    {
                        string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-","");
                        if(hash!=entry.resultHash)throw new InvalidOperationException("Survey run checksum mismatch: "+path);
                    }
                    var run=JsonUtility.FromJson<SubclassSurveyRun>(json);
                    originals.Add(run);
                    var noPassives=run.finalBuild.Clone();noPassives.passiveStableIds.Clear();
                    var noSubclass=run.finalBuild.Clone();noSubclass.subclassId=string.Empty;
                    double passiveDps=PlayerBuildEvaluator.Evaluate(noPassives).totalSustainableDps;
                    double subclassDps=PlayerBuildEvaluator.Evaluate(noSubclass).totalSustainableDps;
                    var noPassivesRequest=JsonUtility.FromJson<CombatLabRequest>(JsonUtility.ToJson(run.combat.request));
                    noPassivesRequest.player=noPassives;
                    var noSubclassRequest=JsonUtility.FromJson<CombatLabRequest>(JsonUtility.ToJson(run.combat.request));
                    noSubclassRequest.player=noSubclass;
                    var passivesLab=CombatLabAdapters.Batch(noPassivesRequest);
                    var subclassLab=CombatLabAdapters.Batch(noSubclassRequest);
                    if(passivesLab.errors>0||subclassLab.errors>0)throw new InvalidOperationException("Combat ablation produced an error.");
                    var shared=JsonUtility.FromJson<SubclassSurveyLevel>(File.ReadAllText(Path.Combine(root,$"level{entry.playerLevel}_shared.json")));
                    var longEnemy=CombatLabAdapters.EnemySnapshot(shared.referenceEnemy,null);
                    // Diagnostic only: extend the same target's Life tenfold;
                    // no enemy production values or canonical runs are changed.
                    longEnemy.maximumLife*=10;
                    var longConfig=JsonUtility.FromJson<CombatSimulationConfig>(JsonUtility.ToJson(run.combat.request.config));
                    longConfig.seed=run.combat.request.seed;longConfig.retainTrace=false;
                    var longFight=HeadlessCombatSimulator.Run(CombatLabAdapters.PlayerSnapshot(run.finalBuild),longEnemy,longConfig);
                    if(longFight.outcome==CombatOutcome.SimulationError)throw new InvalidOperationException(longFight.error);
                    GearSnapshot bestAxe=null;double axeDps=0,selectedWeaponDps=0;
                    var rootObject=new GameObject("Survey weapon provenance"){hideFlags=HideFlags.HideAndDontSave};
                    try
                    {
                        double WeaponDps(GearSnapshot item)
                        {var gear=item.Materialize(rootObject.transform,"Survey weapon");try{return gear.GetAverageWeaponDps();}finally{UnityEngine.Object.DestroyImmediate(gear.gameObject);}}
                        var selectedWeapon=run.finalBuild.equipment.FirstOrDefault(x=>x.slot==LootManager.GearType.Weapons);
                        if(selectedWeapon!=null)selectedWeaponDps=WeaponDps(selectedWeapon);
                        if(run.weaponTypeId==WeaponTypeIds.TwoHandedAxe)
                            foreach(var item in shared.ground.items.Where(x=>x.slot==LootManager.GearType.Weapons&&x.weaponTypeId==WeaponTypeIds.TwoHandedAxe&&x.element==Element.Phys))
                            {double dps=WeaponDps(item);if(bestAxe==null||dps>axeDps){bestAxe=item;axeDps=dps;}}
                    }
                    finally{UnityEngine.Object.DestroyImmediate(rootObject);}
                    result.runs.Add(new SubclassSurveyAblation
                    {level=run.targetPlayerLevel,subclassId=run.subclassId,allocatedPoints=run.passivePoints,
                        analyticalDps=run.analyticalDps,withoutPassivesDps=passiveDps,
                        withoutSubclassDps=subclassDps,passiveDelta=run.analyticalDps-passiveDps,
                        subclassDelta=run.analyticalDps-subclassDps,
                        withoutPassivesCombat=passivesLab,withoutSubclassCombat=subclassLab,longerTarget=longFight,
                        bestGroundPhysicalAxe=bestAxe,bestGroundPhysicalAxeWeaponDps=axeDps,selectedWeaponDps=selectedWeaponDps});
                    Debug.Log($"SUBCLASS SURVEY ABLATION L{entry.playerLevel} {run.subclassName}");
                }
                File.WriteAllText(Path.Combine(root,"subclass_survey_ablations.json"),JsonUtility.ToJson(result,true));
                SubclassSurveyReportExporter.Export(root,originals,result.runs);
                Debug.Log("SUBCLASS SURVEY ABLATIONS COMPLETE: "+result.runs.Count);
                EditorApplication.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }

    public static class SubclassSurveyReportExporter
    {
        static string N(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
        static string Q(string x)=>"\""+(x??"").Replace("\"","\"\"")+"\"";
        public static void Export(string root,List<SubclassSurveyRun> runs,List<SubclassSurveyAblation> ablations)
        {
            const string header="Class,Subclass,Weapon,PlayerLevel,CombatLevel,AnalyticalDPS,CombatDPS,AnalyticalCombatDeltaFraction,AverageHit,BasicDPS,Life,Armour,ReferencePhysicalHit,ArmourPDR,ExplicitPDR,CombinedPDR,FireRes,ColdRes,LightningRes,VoidRes,FireTarget,ColdTarget,LightningTarget,VoidTarget,PDRTarget,SkillPolicy,MainDamageType,MainAilment,ApproxLandedHitsPerSecond,PassivePoints,NativePoints,WeaponPoints,OffClassPoints,CombatDPSWithoutTree,PassiveCombatDeltaFraction,CombatDPSWithoutSubclass,SubclassCombatDeltaFraction,AnalyticalDPSWithoutTree,AnalyticalDPSWithoutSubclass,CoreMechanicActive,Warnings,WinRate,MeanDuration,P50Duration,Mana,ManaRegen,ManaSpentPerSecond,TimeAtZeroMana,FinalProjectilesBeforeVolley,DoubleVolleyProjectiles,FlurryHitsPerUse,ComboMax,Ruptures,RuptureDamage,Finishers,FinisherDamage,Eruptions,ShockBarrageHitsPerUse,AilmentCriticalApplications,CraftedEquipped,AnalyticalGroundDPS,CraftingPipelineDeltaFraction,Equipment";
            foreach(int level in new[]{20,50,80})
            {
                var shared=JsonUtility.FromJson<SubclassSurveyLevel>(File.ReadAllText(Path.Combine(root,$"level{level}_shared.json")));
                var rows=new List<string>{header};
                foreach(var r in runs.Where(x=>x.targetPlayerLevel==level).OrderByDescending(x=>x.combatDps))
                {
                    var a=ablations.Single(x=>x.level==level&&x.subclassId==r.subclassId);
                    double seconds=Math.Max(.001,r.combat.duration.mean*r.combat.fights);
                    double landed=r.weaponTypeId==WeaponTypeIds.Bow?r.combat.projectilesImpacted:r.combat.rapidFlurryUses>0?r.combat.rapidFlurryHits+r.combat.hitTwiceCount:r.combat.playerAttacks+r.combat.hitTwiceCount;
                    int native=0,weapon=0,off=0;
                    foreach(var id in r.finalBuild.passiveStableIds)if(PassiveTreeDefinition.TryNode(id,out var node)){if(node.IsWeaponRoute)weapon++;else if(node.RouteClassId==r.classId)native++;else off++;}
                    bool active=r.subclassId switch
                    {
                        SubclassIds.WarriorBleed=>r.combat.ruptureTriggers>0,
                        SubclassIds.WarriorMultihit=>r.combat.maximumCombo>0,
                        SubclassIds.BarbarianBigHit=>r.combat.fullLifeBonusHits+r.combat.injuredBonusHits>0,
                        SubclassIds.BarbarianFire=>r.combat.eruptionTriggers>0,
                        SubclassIds.RangerPoison=>r.combat.ailments.Any(x=>x.id=="Poison"&&x.applications>0),
                        SubclassIds.RangerProjectile=>r.combat.projectilesLaunched>r.combat.playerAttacks,
                        SubclassIds.MageCooldown=>r.combat.skills.Any(x=>x.cooldownBypasses>0),
                        SubclassIds.MageStorm=>r.combat.shockBarrageHits>r.combat.shockBarrageUses,
                        SubclassIds.PriestDark=>r.combat.damage.Any(x=>x.id=="Player/Healing Converted to Void"&&x.total>0),
                        SubclassIds.PriestLight=>r.combat.physicalAuraIntensity+r.combat.fireAuraIntensity+r.combat.coldAuraIntensity+r.combat.lightningAuraIntensity>0,
                        SubclassIds.ThiefAssassin=>r.combat.openingDamage>0||r.combat.executionTriggers>0,
                        _=>r.combat.ailments.Any(x=>x.criticalApplications>0)
                    };
                    string warning=r.warning??"";
                    if(Math.Abs(r.analyticalDps/Math.Max(.001,r.combatDps)-1)>.2)warning+=" HIGH DIVERGENCE.";
                    if(!active)warning+=" CORE MECHANIC INACTIVE / OFF-THEME OPTIMUM.";
                    if(r.weaponTypeId==WeaponTypeIds.TwoHandedAxe&&r.combat.rageFinisherUses==0)warning+=" Finisher inactive (not selected).";
                    var typed=new Dictionary<string,double>{{"Physical",0},{"Fire",0},{"Cold",0},{"Lightning",0},{"Void",0}};
                    foreach(var t in r.combat.damageByType.Where(x=>x.id.StartsWith("Player/")))if(typed.ContainsKey(t.id.Substring(7)))typed[t.id.Substring(7)]+=t.total;
                    foreach(string dot in new[]{"Bleed","Rupture","Ignite","Poison"})typed[dot=="Poison"?"Void":dot=="Ignite"?"Fire":"Physical"]+=r.combat.damage.Where(x=>x.id=="Player/"+dot).Sum(x=>x.total);
                    string main=typed.OrderByDescending(x=>x.Value).First().Key;
                    var ailment=r.combat.ailments.Where(x=>x.id is "Poison" or "Bleed" or "Ignite").OrderByDescending(x=>x.damage).FirstOrDefault();
                    string gear=string.Join(" | ",r.finalBuild.equipment.Select(x=>$"{x.slot}: L{x.itemLevel} {x.rarity} {x.element}; "+string.Join("; ",x.mods.Select(m=>$"{m.stat} T{m.tier} {m.value:R}"))));
                    var values=new List<string>{Q(r.classId),Q(r.subclassName),Q(r.weaponTypeId),N(level),N(r.combatLevel),N(r.analyticalDps),N(r.combatDps),N(r.analyticalDps/Math.Max(.001,r.combatDps)-1),N(r.metrics.averageHit),N(r.metrics.basicDps),N(r.metrics.life),N(r.metrics.armour),N(shared.referencePhysicalHit),N(r.combinedPhysicalReduction-r.metrics.physicalDamageReduction),N(r.metrics.physicalDamageReduction),N(r.combinedPhysicalReduction),N(r.metrics.fireResistance),N(r.metrics.coldResistance),N(r.metrics.lightningResistance),N(r.metrics.voidResistance),N(shared.fireTarget),N(shared.coldTarget),N(shared.lightningTarget),N(shared.voidTarget),N(shared.physicalTarget),Q(r.metrics.selectedSkillPolicy),Q(main),Q(ailment?.damage>0?ailment.id:"None"),N(landed/seconds),N(r.passivePoints),N(native),N(weapon),N(off),N(a.withoutPassivesCombat.playerDps.mean),N(1-a.withoutPassivesCombat.playerDps.mean/Math.Max(.001,r.combatDps)),N(a.withoutSubclassCombat.playerDps.mean),N(1-a.withoutSubclassCombat.playerDps.mean/Math.Max(.001,r.combatDps)),N(a.withoutPassivesDps),N(a.withoutSubclassDps),active?"true":"false",Q(warning),N(r.combat.winRate),N(r.combat.duration.mean),N(r.combat.duration.p50),N(r.metrics.mana),N(r.metrics.manaRegen),N(r.combat.manaSpent/seconds),N(r.combat.timeAtZeroMana),N(r.metrics.projectileCount),N(r.metrics.projectileCount*2),N(r.combat.rapidFlurryUses>0?(double)r.combat.rapidFlurryHits/r.combat.rapidFlurryUses:0),N(r.combat.maximumCombo),N(r.combat.ruptureTriggers),N(r.combat.ailments.Where(x=>x.id=="Rupture").Sum(x=>x.damage)),N(r.combat.rageFinisherUses),N(r.combat.rageFinisherDamage),N(r.combat.eruptionTriggers),N(r.combat.shockBarrageUses>0?(double)r.combat.shockBarrageHits/r.combat.shockBarrageUses:0),N(r.combat.ailments.Sum(x=>x.criticalApplications)),N(r.craftedEquippedItems),N(r.groundDps),N(r.analyticalDps/Math.Max(.001,r.groundDps)-1),Q(gear)};
                    rows.Add(string.Join(",",values));
                }
                File.WriteAllLines(Path.Combine(root,$"subclass_survey_level{level}.csv"),rows);
            }
            var cross=new List<string>{"Subclass,L20CombatDPS,L50CombatDPS,L80CombatDPS,Multiplier20To50,Multiplier50To80"};
            foreach(var group in runs.GroupBy(x=>x.subclassId))
            {var r20=group.Single(x=>x.targetPlayerLevel==20);var r50=group.Single(x=>x.targetPlayerLevel==50);var r80=group.Single(x=>x.targetPlayerLevel==80);cross.Add(string.Join(",",Q(r20.subclassName),N(r20.combatDps),N(r50.combatDps),N(r80.combatDps),N(r50.combatDps/r20.combatDps),N(r80.combatDps/r50.combatDps)));}
            File.WriteAllLines(Path.Combine(root,"subclass_survey_cross_level.csv"),cross);
        }
    }
}
