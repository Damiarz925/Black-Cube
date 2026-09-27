using System;
using System.Collections.Generic;
using System.Linq;
using BlackCube.CombatSimulation;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class FullLevelCalibrationRequest
    {
        public PlayerBuildSnapshot player=new();
        public int combatLevel=50,trials=20;
        public long seed=61001;
        public float maximumFightDuration=120;
        public bool noRecovery=true;
    }
    [Serializable] public sealed class FullLevelCalibrationResult
    {
        public PlayerBuildSnapshot runBuild;
        public string buildHash,dataFingerprint;
        public int combatLevel;
        public long seed;
        public bool noRecovery;
        public float maximumFightDuration;
        public int requestedTrials;
        public int trials,clears,bossReached,bossKilled;
        public double clearRate,bossReachRate,bossKillRate,averageRemainingLife,
            averageDamageDealt,averageDamageTaken;
        public List<int> failedAtStage=new();
        public string warning;
    }

    public static class FullLevelCalibrationSimulator
    {
        public static FullLevelCalibrationResult Run(FullLevelCalibrationRequest request,
            Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(request?.player==null)throw new ArgumentNullException(nameof(request));
            int level=Mathf.Clamp(request.combatLevel,1,360);
            int count=Mathf.Clamp(request.trials,1,100);
            var result=new FullLevelCalibrationResult{runBuild=request.player.Clone(),
                buildHash=ScenarioResultIdentity.BuildHash(request.player),
                dataFingerprint=ProductionBalanceAdapters.DataFingerprint(),
                combatLevel=level,seed=request.seed,noRecovery=request.noRecovery,
                maximumFightDuration=request.maximumFightDuration,requestedTrials=count};
            var player=CombatLabAdapters.PlayerSnapshot(request.player);
            var db=WorldContentCatalog.Reference;
            var rarityProfiles=db.enemyRarityProfiles.Where(x=>x!=null&&x.spawnWeight>0).ToArray();
            if(rarityProfiles.Length==0)throw new InvalidOperationException("No production enemy rarity weights.");
            int rarityTotal=rarityProfiles.Sum(x=>x.spawnWeight);
            double remaining=0,damageDealt=0,damageTaken=0;
            for(int trial=0;trial<count;trial++)
            {
                if(cancelled?.Invoke()==true)break;
                float life=player.maximumLife,mana=player.maximumMana;
                var rng=new SeededSimulationRandomSource(CombatDeterministicRules.DeriveSeed(request.seed,trial));
                bool survived=true;
                result.trials++;
                for(int stage=1;stage<=WorldProgression.BossStage;stage++)
                {
                    var position=WorldProgression.Resolve(level,stage,db,(int)request.seed);
                    bool boss=stage==WorldProgression.BossStage;
                    if(boss)result.bossReached++;
                    EnemyAI.EnemyRarity rarity=EnemyAI.EnemyRarity.Legendary;
                    if(!boss)
                    {
                        int roll=rng.Range(0,rarityTotal);
                        foreach(var profile in rarityProfiles)
                        {if(roll<profile.spawnWeight){rarity=profile.rarity;break;}roll-=profile.spawnWeight;}
                    }
                    CombatantSnapshot enemy;
                    long seed=CombatDeterministicRules.DeriveSeed(request.seed+trial*31L,stage);
                    if(boss)
                    {
                        string bossId=position.Encounter?.bossId??throw new InvalidOperationException("Production boss stage has no boss ID.");
                        var preview=CombatLabAdapters.BossPreview(bossId,level,0,seed);
                        enemy=CombatLabAdapters.EnemySnapshot(preview,db.Boss(bossId));
                    }
                    else
                    {
                        string id=position.Encounter?.enemyArchetypeId??throw new InvalidOperationException("Production stage has no enemy archetype ID.");
                        enemy=CombatLabAdapters.EnemySnapshot(id,level,rarity,0,seed);
                    }
                    var config=new CombatSimulationConfig{seed=seed,
                        maximumDuration=Mathf.Max(1,request.maximumFightDuration),retainTrace=false,
                        disablePlayerLifeRecovery=request.noRecovery,
                        startingPlayerLife=life,startingPlayerMana=mana};
                    var fight=HeadlessCombatSimulator.Run(player,enemy,config);
                    damageDealt+=fight.damage.Where(x=>x.id.StartsWith("Player/",StringComparison.Ordinal)).Sum(x=>x.total);
                    damageTaken+=fight.damage.Where(x=>x.id.StartsWith("Enemy/",StringComparison.Ordinal)).Sum(x=>x.total);
                    life=fight.playerLife;mana=fight.playerMana;
                    if(fight.outcome!=CombatOutcome.PlayerWin)
                    {result.failedAtStage.Add(stage);survived=false;break;}
                    if(boss)result.bossKilled++;
                }
                if(survived)result.clears++;
                remaining+=Math.Max(0,life);
                progress?.Invoke((trial+1f)/count);
            }
            if(result.trials>0)
            {
                result.clearRate=result.clears/(double)result.trials;
                result.bossReachRate=result.bossReached/(double)result.trials;
                result.bossKillRate=result.bossKilled/(double)result.trials;
                result.averageRemainingLife=remaining/result.trials;
                result.averageDamageDealt=damageDealt/result.trials;
                result.averageDamageTaken=damageTaken/result.trials;
            }
            result.warning="Carries Life and Mana between production stages. Encounter-local Rage, buffs, and enemy state reset. Results calibrate a supplied build; they do not define final Life, Armour, or recovery targets.";
            return result;
        }
    }

    public sealed partial class BalanceWorkbenchWindow
    {
        FullLevelCalibrationRequest fullLevelRequest=new();
        FullLevelCalibrationResult fullLevelResult;
        ResistancePressureResult resistancePressure;
        void DefenseCalibrationTab()
        {
            Heading("PRODUCTION T1 RESISTANCE INVESTMENT PRESSURE");
            EditorGUILayout.HelpBox("Read-only reference derived from the live ModDatabase and legal item pools. It does not reserve actual gear affixes or set final Life/Armour targets.",MessageType.Info);
            if(GUILayout.Button("AUDIT RESISTANCE TIERS",GUILayout.Height(30)))
                Run("Resistance tier investment audit",_=>
                {
                    using var session=new WorkbenchSession();
                    resistancePressure=ResistancePressureAnalyzer.Analyze(session.Roller.Database);
                });
            if(resistancePressure!=null)
            {
                MetricRow("All direct T1 options available at item level",resistancePressure.allT1AvailableAt.ToString());
                MetricRow("Selected T1 investment available at item level",resistancePressure.referenceInvestmentAvailableAt.ToString());
                MetricRow("Reference resistance suffix count",resistancePressure.referenceInvestment.Count.ToString());
                MetricRow("75% all four covered by reference",resistancePressure.reachesTarget?"Yes":"No");
                foreach(var stat in new[]{StatTypes.FireRes,StatTypes.ColdRes,StatTypes.LightRes,StatTypes.VoidRes})
                {
                    var options=resistancePressure.t1Options.Where(x=>x.stat==stat).ToList();
                    if(options.Count>0)MetricRow($"{stat} T1 unlock / midpoint",
                        $"L{options.Min(x=>x.itemLevel)} / {options.Max(x=>x.midpoint):P0}");
                }
                Heading("SAME INVESTMENT AT EARLIER LEGAL TIERS");
                foreach(var row in resistancePressure.earlierTiers)
                    MetricRow($"Item Level {row.itemLevel}",
                        $"{row.fire:P0} / {row.cold:P0} / {row.lightning:P0} / {row.voidResistance:P0}");
                EditorGUILayout.HelpBox(resistancePressure.assumptions,MessageType.Info);
                if(!string.IsNullOrEmpty(resistancePressure.warning))
                    EditorGUILayout.HelpBox(resistancePressure.warning,MessageType.Warning);
                if(GUILayout.Button("EXPORT RESISTANCE PRESSURE JSON"))
                    status=WorkbenchExports.SaveJson("resistance_pressure",resistancePressure);
            }
            Heading("FULL-LEVEL DEFENSE CALIBRATION");
            EditorGUILayout.HelpBox("Runs the production nine normal encounters plus boss. No-Recovery disables renewable player Life healing only; Mana and offense remain active.",MessageType.Info);
            BuildInputs(false);
            fullLevelRequest.combatLevel=EditorGUILayout.IntSlider("Combat Level",fullLevelRequest.combatLevel,1,360);
            fullLevelRequest.trials=EditorGUILayout.IntSlider("Trials",fullLevelRequest.trials,1,100);
            fullLevelRequest.seed=EditorGUILayout.LongField("Seed",fullLevelRequest.seed);
            fullLevelRequest.maximumFightDuration=EditorGUILayout.Slider("Maximum Fight Duration",fullLevelRequest.maximumFightDuration,5,300);
            fullLevelRequest.noRecovery=EditorGUILayout.Toggle("No Renewable Life Recovery",fullLevelRequest.noRecovery);
            if(GUILayout.Button("RUN FULL LEVEL SEQUENCE",GUILayout.Height(32)))
                Run("Full-level survival calibration",p=>
                {
                    var request=new FullLevelCalibrationRequest{player=playerBuild.Clone(),
                        combatLevel=fullLevelRequest.combatLevel,trials=fullLevelRequest.trials,
                        seed=fullLevelRequest.seed,noRecovery=fullLevelRequest.noRecovery,
                        maximumFightDuration=fullLevelRequest.maximumFightDuration};
                    request.player.combatLevel=request.combatLevel;
                    fullLevelResult=null;
                    fullLevelResult=FullLevelCalibrationSimulator.Run(request,
                        Progress("Full level"),()=>cancelled);
                });
            if(fullLevelResult==null)return;
            var currentRunBuild=playerBuild.Clone();
            currentRunBuild.combatLevel=fullLevelRequest.combatLevel;
            bool stale=fullLevelResult.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint()||
                fullLevelResult.combatLevel!=fullLevelRequest.combatLevel||
                fullLevelResult.requestedTrials!=fullLevelRequest.trials||
                fullLevelResult.seed!=fullLevelRequest.seed||
                fullLevelResult.noRecovery!=fullLevelRequest.noRecovery||
                fullLevelResult.maximumFightDuration!=fullLevelRequest.maximumFightDuration||
                fullLevelResult.buildHash!=ScenarioResultIdentity.BuildHash(currentRunBuild);
            if(stale)EditorGUILayout.HelpBox("STALE RESULT — inputs or production data changed after this run. Values below retain their original run context.",MessageType.Warning);
            Heading("RUN CONTEXT");
            MetricRow("Player Level / Combat Level",$"{fullLevelResult.runBuild.playerLevel} / {fullLevelResult.combatLevel}");
            MetricRow("Class / Subclass / Weapon",$"{fullLevelResult.runBuild.classId} / {fullLevelResult.runBuild.subclassId} / {fullLevelResult.runBuild.weaponTypeId}");
            MetricRow("Trials / Seed",$"{fullLevelResult.trials} of {fullLevelResult.requestedTrials} / {fullLevelResult.seed}");
            MetricRow("No Renewable Life Recovery",fullLevelResult.noRecovery?"ON":"OFF");
            MetricRow("Build Hash / Fingerprint",$"{fullLevelResult.buildHash} / {fullLevelResult.dataFingerprint}");
            MetricRow("Full-Level Clear Rate",fullLevelResult.clearRate.ToString("P1"));
            MetricRow("Boss Reach / Kill",$"{fullLevelResult.bossReachRate:P1} / {fullLevelResult.bossKillRate:P1}");
            MetricRow("Average Remaining Life",fullLevelResult.averageRemainingLife.ToString("0.#"));
            MetricRow("Average Damage Dealt",fullLevelResult.averageDamageDealt.ToString("0.#"));
            MetricRow("Average Damage Taken",fullLevelResult.averageDamageTaken.ToString("0.#"));
            for(int stage=1;stage<=WorldProgression.BossStage;stage++)
            {
                int failures=fullLevelResult.failedAtStage.Count(x=>x==stage);
                if(failures>0)MetricRow($"Failed at Stage {stage}",failures.ToString());
            }
            EditorGUILayout.HelpBox(fullLevelResult.warning,MessageType.Info);
            if(GUILayout.Button("EXPORT FULL-LEVEL RESULT"))status=WorkbenchExports.SaveJson("full_level_calibration",fullLevelResult);
        }
    }
}
