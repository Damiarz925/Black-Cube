using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BlackCube.BalanceWorkbench;
using BlackCube.CombatSimulation;
using UnityEditor;
using UnityEngine;

public static class Tooling4ValidationRunner
{
    public static void RunValidators()=>Run(() =>
    {
        var errors=BalanceWorkbenchValidation.Validate().ToList();errors.AddRange(WorldContentValidation.Validate(WorldContentCatalog.Reference));errors.AddRange(PassiveTreeV3Validation.Validate());if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        ItemizationValidationRunner.RunStep14_5();WorldContentValidationRunner.ValidateReferenceContent();BaselineVerificationRunner.ValidateReferences();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Tooling4Validation.txt",$"PASS\nschema={GamePersistence.SchemaVersion}\ndata={ProductionBalanceAdapters.DataFingerprint()}\nfocused=BalanceWorkbenchTooling4Tests\nBalanceLab=NOT RUN\n");
    });

    public static void RunSmoke()=>Run(() =>
    {
        Directory.CreateDirectory("Logs");var lines=new List<string>{"TOOLING 4 EDITOR WORKFLOW SMOKE"};var build=WarriorBuild();var db=WorldContentCatalog.Reference;var request=new CombatLabRequest{player=build,enemyArchetypeId=db.enemyArchetypes[0].stableId,enemyLevel=50,rarity=EnemyAI.EnemyRarity.Rare,seed=47001,fightCount=1000,config=new CombatSimulationConfig{maximumDuration=120,maximumEvents=100000,retainTrace=true}};
        var single=CombatLabAdapters.Single(request);var repeat=CombatLabAdapters.Replay(request,single.seed);if(JsonUtility.ToJson(single)!=JsonUtility.ToJson(repeat))throw new InvalidOperationException("Single-fight replay did not reproduce exactly.");if(single.trace.Count==0)throw new InvalidOperationException("Single fight did not retain its event timeline.");lines.Add($"PASS single/replay: {single.outcome}, {single.duration:0.###}s, seed={single.seed}, trace={single.trace.Count}");
        request.config.retainTrace=false;var batch=CombatLabAdapters.Batch(request);if(batch.fights!=1000||batch.errors!=0)throw new InvalidOperationException($"Batch failed: fights={batch.fights}, errors={batch.errors}");lines.Add($"PASS 1,000-fight batch: win={batch.winRate:P2}, P50={batch.duration.p50:0.###}s, P90={batch.duration.p90:0.###}s");
        long replaySeed=batch.worstLossSeed!=0?batch.worstLossSeed:batch.closestWinSeed;var outlier=CombatLabAdapters.Replay(request,replaySeed);if(outlier.seed!=replaySeed||outlier.trace.Count==0)throw new InvalidOperationException("Outlier replay failed.");lines.Add($"PASS outlier replay: seed={replaySeed}, {outlier.outcome}");
        var staff=Fixture(WeaponTypeIds.Staff);staff.maximumMana=5;staff.manaRegeneration=1;staff.skills.Add(new CombatSkillSnapshot{id="staff.starve",name="Staff Starvation",castMode=PlayerSkillCastMode.AutoCooldown,cooldown=.2f,manaCost=20});var starved=HeadlessCombatSimulator.Run(staff,FixtureEnemy(),new CombatSimulationConfig{seed=47002,maximumDuration=4,maximumEvents=10000});if(starved.skills.Single().manaFailures==0)throw new InvalidOperationException("Staff starvation fixture did not delay casts.");lines.Add($"PASS Staff starvation: delays={starved.skills.Single().manaFailures}, ready={starved.skills.Single().readyStarvedTime:0.###}s");
        var axe=Fixture(WeaponTypeIds.TwoHandedAxe);axe.hasRageFinisher=true;axe.basicDamage.physical=100;var durable=FixtureEnemy();durable.maximumLife=10000;durable.attackSpeed=.1f;var never=HeadlessCombatSimulator.Run(axe,durable,new CombatSimulationConfig{seed=47003,maximumDuration=30,ragePolicy=RageFinisherPolicy.Never});var immediate=HeadlessCombatSimulator.Run(axe,durable,new CombatSimulationConfig{seed=47003,maximumDuration=30,ragePolicy=RageFinisherPolicy.Immediately});if(never.rageFinisherUses!=0||immediate.rageFinisherUses==0)throw new InvalidOperationException("Rage policy comparison failed.");lines.Add($"PASS Rage policies: Never={never.rageFinisherUses}, Immediate={immediate.rageFinisherUses}");
        var boss=db.bosses.First();var bossRequest=Clone(request);bossRequest.useBoss=true;bossRequest.bossId=boss.stableId;bossRequest.fightCount=10;var bossBatch=CombatLabAdapters.Batch(bossRequest);if(bossBatch.fights!=10)throw new InvalidOperationException("Boss batch failed.");lines.Add($"PASS Boss Lab: {boss.displayName}, win={bossBatch.winRate:P2}");
        var matrix=new CombatMatrixResult{fingerprint=ProductionBalanceAdapters.DataFingerprint()};foreach(var enemy in db.enemyArchetypes.Take(3)){var cellRequest=Clone(request);cellRequest.enemyArchetypeId=enemy.stableId;cellRequest.fightCount=10;var cell=CombatLabAdapters.Batch(cellRequest);matrix.cells.Add(new CombatMatrixCell{player="Warrior/Sword",enemy=enemy.displayName,winRate=cell.winRate,medianDuration=cell.duration.p50,medianPlayerLife=cell.playerLifeOnWin.p50,timeoutRate=cell.timeoutRate,seed=cellRequest.seed});}if(matrix.cells.Count!=3)throw new InvalidOperationException("Matchup matrix failed.");lines.Add("PASS simple matchup matrix: 3 production enemies");
        string batchCsv=WorkbenchExports.SaveCombatBatchCsv(batch),timelineCsv=WorkbenchExports.SaveCombatTimelineCsv(single),matrixCsv=WorkbenchExports.SaveCombatMatrixCsv(matrix),traceJson=WorkbenchExports.SaveJson("tooling4_smoke_trace",single);foreach(string path in new[]{batchCsv,timelineCsv,matrixCsv,traceJson})if(!File.Exists(path)||new FileInfo(path).Length==0)throw new InvalidOperationException("Missing export: "+path);lines.Add("PASS batch/timeline/matrix CSV and trace JSON exports");
        File.WriteAllLines("Logs/Tooling4EditorSmoke.txt",lines);UnityEngine.Debug.Log(string.Join("\n",lines));
    });

    public static void RunPerformance()=>Run(() =>
    {
        Directory.CreateDirectory("Logs");var lines=new List<string>{"TOOLING 4 PURE BATCH PERFORMANCE"};foreach(int count in new[]{100,1000,10000}){var watch=Stopwatch.StartNew();for(int i=0;i<count;i++)HeadlessCombatSimulator.Run(Fixture(WeaponTypeIds.Sword),FixtureEnemy(),new CombatSimulationConfig{seed=CombatDeterministicRules.DeriveSeed(48001,i),maximumDuration=30,maximumEvents=10000,retainTrace=false});watch.Stop();lines.Add($"{count} fights: {watch.Elapsed.TotalSeconds:0.###}s ({count/Math.Max(.001,watch.Elapsed.TotalSeconds):0.##}/s)");}File.WriteAllLines("Logs/Tooling4Performance.txt",lines);UnityEngine.Debug.Log(string.Join("\n",lines));
    });

    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildTooling4Windows);

    static PlayerBuildSnapshot WarriorBuild()=>new(){playerLevel=50,combatLevel=50,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,seed=47001,equipment=new(){new GearSnapshot{slot=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Rare,itemLevel=50,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,baseMin=35,baseMax=50,baseSpeed=1.2f,baseCrit=.05f}}};
    static CombatantSnapshot Fixture(string weapon)=>new(){id="player",name="Player",player=true,weaponTypeId=weapon,maximumLife=1000,maximumMana=100,attackSpeed=1,critMultiplier=2,basicDamage=new CombatDamageSnapshot{physical=20},projectileTravelTime=.5f,precisionMultiplier=1.5f};
    static CombatantSnapshot FixtureEnemy()=>new(){id="enemy",name="Enemy",maximumLife=1000,attackSpeed=1,critMultiplier=2,basicDamage=new CombatDamageSnapshot{physical=10},maximumResistance=.75f};
    static T Clone<T>(T value)=>JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){UnityEngine.Debug.LogException(ex);EditorApplication.Exit(1);}}
}
