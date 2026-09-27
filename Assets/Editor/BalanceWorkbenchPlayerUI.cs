using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public sealed partial class BalanceWorkbenchWindow
    {
        enum HeatmapMode{ImmediateObjective,PrimaryMetric,SecondaryMetric,PathAdjusted,SelectedPath}
        PlayerBuildSnapshot playerBuild=new();OptimizationObjective objective=new();OptimizationConstraints playerConstraints=new();PlayerBuildMetrics playerMetrics,compareA,compareB;PlayerBuildSnapshot compareABuild,compareBBuild;List<BuildAblationContribution> contributions=new();GearOptimizationResult gearResult;PassiveOptimizationResult passiveResult;List<PassiveMarginalResult> marginal=new();ScenarioSweepResult playerSweep;ScenarioInspection selectedScenarioInspection;PlayerGearProfileSO gearProfile;int passivePoints=50,passiveBeamWidth=100,scenarioCombatLevelOffset;bool passiveBeam,scenarioPassives,scenarioProgressive,scenarioFreeRespec=true,showConstraints,showScenarioWorkload,showInspectionDebug,showProfileSemantics;CombatLevelSweepPolicy scenarioCombatLevelPolicy=CombatLevelSweepPolicy.MatchPlayerLevel;string curveMetric="basic_dps",playerMetricsBuildHash,playerMetricsFingerprint,contributionsObjectiveHash;Vector2 heatPan;float heatZoom=.09f;HeatmapMode heatmapMode;PassiveMarginalResult selectedMarginal;

        void BuildInputs(bool showCombatLevel=true)
        {
            playerBuild.playerLevel=EditorGUILayout.IntSlider("Player Level",playerBuild.playerLevel,1,100);if(showCombatLevel)playerBuild.combatLevel=EditorGUILayout.IntSlider("Combat Level",playerBuild.combatLevel,1,360);playerBuild.seed=EditorGUILayout.LongField("Simulation Seed",playerBuild.seed);
            string[] classes=PlayerClassCatalog.All.Select(x=>x.Id).ToArray();int ci=Mathf.Max(0,Array.IndexOf(classes,playerBuild.classId));playerBuild.classId=classes[EditorGUILayout.Popup("Class",ci,PlayerClassCatalog.All.Select(x=>x.DisplayName).ToArray())];var subs=SubclassCatalog.ForClass(playerBuild.classId);string[] subIds=new[]{""}.Concat(subs.Select(x=>x.Id)).ToArray();string[] subNames=new[]{"None"}.Concat(subs.Select(x=>x.DisplayName)).ToArray();int subi=Mathf.Max(0,Array.IndexOf(subIds,playerBuild.subclassId));playerBuild.subclassId=subIds[EditorGUILayout.Popup("Subclass",subi,subNames)];string[] weapons=WeaponTypeCatalog.All.Select(x=>x.Id).ToArray();int wi=Mathf.Max(0,Array.IndexOf(weapons,playerBuild.weaponTypeId));playerBuild.weaponTypeId=weapons[EditorGUILayout.Popup("Weapon",wi,WeaponTypeCatalog.All.Select(x=>x.DisplayName).ToArray())];if(playerBuild.subclassId==SubclassIds.RangerProjectile)playerBuild.projectileMode=(SubclassProjectileMode)EditorGUILayout.EnumPopup("Projectile Mode",playerBuild.projectileMode);
        }
        void ObjectiveInputs()
        {
            string[] names=OptimizationMetricCatalog.All.Select(x=>x.Name).ToArray();int p=Mathf.Max(0,OptimizationMetricCatalog.All.ToList().FindIndex(x=>x.Id==objective.primary));objective.primary=OptimizationMetricCatalog.All[EditorGUILayout.Popup("Primary Objective",p,names)].Id;int s=Mathf.Max(0,OptimizationMetricCatalog.All.ToList().FindIndex(x=>x.Id==objective.secondary));objective.secondary=OptimizationMetricCatalog.All[EditorGUILayout.Popup("Secondary Objective",s,names)].Id;objective.mode=(OptimizationMode)EditorGUILayout.EnumPopup("Objective Mode",objective.mode);if(objective.mode==OptimizationMode.Weighted)objective.primaryWeight=EditorGUILayout.Slider("Primary Weight",objective.primaryWeight,0,1);EditorGUILayout.HelpBox("Optimization Score is scenario-relative, not a canonical power rating. Weighted mode uses signed log-relative change with a 5% baseline floor; Secondary Weight = 1 − Primary Weight. Lexicographic mode prioritizes Primary and uses Secondary only as the deterministic second priority.",MessageType.None);
        }
        void ConstraintInputs()
        {
            showConstraints=EditorGUILayout.Foldout(showConstraints,"Optional Optimization Constraints",true);if(!showConstraints)return;
            playerConstraints.minimumLifeEnabled=EditorGUILayout.Toggle("Minimum Life",playerConstraints.minimumLifeEnabled);if(playerConstraints.minimumLifeEnabled)playerConstraints.minimumLife=EditorGUILayout.FloatField("Life Threshold",playerConstraints.minimumLife);
            playerConstraints.minimumArmourEnabled=EditorGUILayout.Toggle("Minimum Armour",playerConstraints.minimumArmourEnabled);if(playerConstraints.minimumArmourEnabled)playerConstraints.minimumArmour=EditorGUILayout.FloatField("Armour Threshold",playerConstraints.minimumArmour);
            playerConstraints.minimumFireResistanceEnabled=EditorGUILayout.Toggle("Minimum Fire Resistance",playerConstraints.minimumFireResistanceEnabled);if(playerConstraints.minimumFireResistanceEnabled)playerConstraints.minimumFireResistance=EditorGUILayout.Slider("Fire Threshold",playerConstraints.minimumFireResistance,-.75f,.9f);
            playerConstraints.minimumColdResistanceEnabled=EditorGUILayout.Toggle("Minimum Cold Resistance",playerConstraints.minimumColdResistanceEnabled);if(playerConstraints.minimumColdResistanceEnabled)playerConstraints.minimumColdResistance=EditorGUILayout.Slider("Cold Threshold",playerConstraints.minimumColdResistance,-.75f,.9f);
            playerConstraints.minimumLightningResistanceEnabled=EditorGUILayout.Toggle("Minimum Lightning Resistance",playerConstraints.minimumLightningResistanceEnabled);if(playerConstraints.minimumLightningResistanceEnabled)playerConstraints.minimumLightningResistance=EditorGUILayout.Slider("Lightning Threshold",playerConstraints.minimumLightningResistance,-.75f,.9f);
            playerConstraints.minimumVoidResistanceEnabled=EditorGUILayout.Toggle("Minimum Void Resistance",playerConstraints.minimumVoidResistanceEnabled);if(playerConstraints.minimumVoidResistanceEnabled)playerConstraints.minimumVoidResistance=EditorGUILayout.Slider("Void Threshold",playerConstraints.minimumVoidResistance,-.75f,.9f);
            playerConstraints.maximumOffClassPoints=EditorGUILayout.IntField("Maximum Off-Class Points (-1 disables)",playerConstraints.maximumOffClassPoints);
        }
        void PlayerBuildLab()
        {
            Heading("PLAYER BUILD SNAPSHOT","Generated and manual snapshots evaluate without entering Play Mode.");
            BuildInputs();ObjectiveInputs();ConstraintInputs();
            string currentBuildHash=ScenarioResultIdentity.BuildHash(playerBuild);
            string currentFingerprint=ProductionBalanceAdapters.DataFingerprint();
            bool stale=playerMetrics!=null&&(playerMetricsBuildHash!=currentBuildHash||
                playerMetricsFingerprint!=currentFingerprint);
            if(stale)
            {
                EditorGUILayout.HelpBox("INPUTS CHANGED — RESULT STALE. Evaluate the current build again before using its metrics or transferring it to another tool.",MessageType.Warning);
                playerMetrics=null;contributions.Clear();selectedScenarioInspection=null;
            }
            else if(playerMetrics!=null&&contributionsObjectiveHash!=JsonUtility.ToJson(objective))
            {
                EditorGUILayout.HelpBox("OBJECTIVE CHANGED — CONTRIBUTIONS STALE. Evaluate again to refresh objective-relative analysis.",MessageType.Warning);
                contributions.Clear();
            }
            if(selectedScenarioInspection!=null)DrawScenarioInspectionHeader(selectedScenarioInspection);
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("EVALUATE BUILD",GUILayout.Height(30)))
                    Run("Player build evaluation",_=>SetPlayerEvaluation(playerBuild,
                        PlayerBuildEvaluator.Evaluate(playerBuild),true));
                if(GUILayout.Button("CLEAR GEAR")){playerBuild.equipment.Clear();playerMetrics=null;contributions.Clear();selectedScenarioInspection=null;}
                if(GUILayout.Button("CLEAR PASSIVES")){playerBuild.passiveStableIds.Clear();playerMetrics=null;contributions.Clear();selectedScenarioInspection=null;}
                if(GUILayout.Button("EXPORT JSON"))status=WorkbenchExports.SaveJson("player_build",playerBuild);
                if(playerMetrics!=null&&GUILayout.Button("EXPORT CSV"))
                    status=WorkbenchExports.SaveBuildCsv(playerBuild,playerMetrics);
            }
            Heading("MANUAL / GENERATED EQUIPMENT");
            gearProfile=(PlayerGearProfileSO)EditorGUILayout.ObjectField("Generation Profile",gearProfile,typeof(PlayerGearProfileSO),false);
            if(gearProfile==null)gearProfile=AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>(
                "Assets/Balance/Profiles/SO_PlayerGearProfile_Mid.asset");
            if(GUILayout.Button("GENERATE COMPLETE GEARSET"))Run("Gear optimization",_=>
            {
                gearResult=PlayerGearsetOptimizer.Optimize(playerBuild,gearProfile,objective,
                    gearProfile.selectionStrategy==PlayerGearSelectionStrategy.FullGearsetBeamSearch,
                    playerConstraints,Progress("Gear search"),()=>cancelled);
                playerBuild=gearResult.build.Clone();
                SetPlayerEvaluation(playerBuild,gearResult.metrics,true,true);
            });
            foreach(var gear in playerBuild.equipment.OrderBy(x=>x.slot))
            using(new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(gear.Description,EditorStyles.wordWrappedLabel,GUILayout.MinWidth(280),GUILayout.ExpandWidth(true));
                bool locked=playerConstraints.lockedSlots.Contains(gear.slot);
                if(GUILayout.Button(locked?"Unlock":"Lock",GUILayout.Width(55)))
                {if(locked)playerConstraints.lockedSlots.Remove(gear.slot);else playerConstraints.lockedSlots.Add(gear.slot);}
                if(GUILayout.Button("Clear",GUILayout.Width(55))){playerBuild.equipment.Remove(gear);playerMetrics=null;contributions.Clear();selectedScenarioInspection=null;break;}
                if(GUILayout.Button("Inspect",GUILayout.Width(60)))EditorUtility.DisplayDialog(gear.slot.ToString(),gear.Description,"Close");
            }
            if(playerMetrics!=null)
            {
                MetricsPanel(playerMetrics);
                Heading("BUILD A vs BUILD B");
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(GUILayout.Button("Capture as A")){compareABuild=playerBuild.Clone();compareA=playerMetrics.Clone();}
                    if(GUILayout.Button("Capture as B")){compareBBuild=playerBuild.Clone();compareB=playerMetrics.Clone();}
                }
                if(compareA!=null&&compareB!=null)
                {
                    MetricRow("A / B Build Hash",$"{ScenarioResultIdentity.BuildHash(compareABuild)} / {ScenarioResultIdentity.BuildHash(compareBBuild)}");
                    Compare(compareA,compareB);
                }
            }
            Heading("SEND THIS BUILD TO ANALYSIS");
            using(new EditorGUI.DisabledScope(playerMetrics==null))
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("SENSITIVITY")){sensitivity.build=playerBuild.Clone();tab=Tab.Sensitivity;}
                if(GUILayout.Button("AFFIX ANALYZER")){affixRequest.build=playerBuild.Clone();tab=Tab.AffixAnalyzer;}
                if(GUILayout.Button("LOOT PROGRESSION")){lootRequest.build=playerBuild.Clone();tab=Tab.LootProgression;}
                if(GUILayout.Button("BREAKPOINT FINDER"))tab=Tab.BreakpointFinder;
                if(GUILayout.Button("COMBAT LAB")){CombatLabTransfer.SetPlayer(combatRequest,playerBuild,playerMetrics.selectedSkillPolicy);combatPlayerPinned=true;tab=Tab.CombatLab;}
            }
        }
        void SetPlayerEvaluation(PlayerBuildSnapshot build,PlayerBuildMetrics metrics,bool analyze,
            bool keepGearResult=false)
        {
            playerMetrics=metrics.Clone();
            playerMetricsBuildHash=ScenarioResultIdentity.BuildHash(build);
            playerMetricsFingerprint=ProductionBalanceAdapters.DataFingerprint();
            contributions=analyze?PlayerBuildContributionAnalyzer.Analyze(build,objective):new();
            contributionsObjectiveHash=JsonUtility.ToJson(objective);
            selectedScenarioInspection=null;
            if(!keepGearResult)gearResult=null;
        }
        void DrawScenarioInspectionHeader(ScenarioInspection inspection)
        {
            Heading("RESULT CONTEXT — STORED SCENARIO POINT");
            MetricRow("L / CL / Gear Profile",$"L{inspection.build.playerLevel} / CL{inspection.build.combatLevel} / {inspection.profile}");
            MetricRow("Item Level assumption",inspection.itemLevelAssumption.ToString());
            MetricRow("Class / Subclass / Weapon",$"{inspection.build.classId} / {inspection.build.subclassId} / {inspection.build.weaponTypeId}");
            MetricRow("Seed / Objective",$"{inspection.build.seed} / {OptimizationMetricCatalog.Get(inspection.objective.primary).Name}");
            MetricRow("Passives / Optimizer",$"{inspection.build.passiveStableIds.Count} / {inspection.passiveAlgorithm}");
            MetricRow("Selected Skill Policy",inspection.skillPolicy);
            showInspectionDebug=EditorGUILayout.Foldout(showInspectionDebug,"Advanced Result Integrity",true);
            if(showInspectionDebug)
            {
                MetricRow("Result ID",inspection.resultId);
                MetricRow("Gear Hash",inspection.gearHash);
                MetricRow("Passive Hash",inspection.passiveHash);
                MetricRow("Evaluation Hash",inspection.evaluationHash);
                MetricRow("Fingerprint",inspection.fingerprint);
                MetricRow("Sweep work: gear generation / evaluation",
                    $"{inspection.gearGenerations} / {inspection.gearEvaluationRequests}");
                MetricRow("Sweep work: passive / combat / enemy",
                    $"{inspection.passiveEvaluations} / {inspection.combatSimulations} / {inspection.enemyGenerations}");
                foreach(string id in inspection.build.passiveStableIds)
                    EditorGUILayout.LabelField(id);
            }
        }
        void MetricsPanel(PlayerBuildMetrics m)
        {
            Heading("AUTHORITATIVE ANALYTICAL METRICS");
            MetricRow("Average Hit",m.averageHit.ToString("0.##"));
            MetricRow("Basic DPS",m.basicDps.ToString("0.##"));
            MetricRow("Sustainable Total DPS",m.totalSustainableDps.ToString("0.##"));
            MetricRow("Sustainable Skill DPS",m.sustainableSkillDps.ToString("0.##"));
            MetricRow("Selected Skill Policy",m.selectedSkillPolicy);
            MetricRow("Time at Zero Mana",m.manaStarvationFraction.ToString("P1"));
            MetricRow("APS / Crit / Crit Multi",$"{m.attacksPerSecond:0.###} / {m.critChance:P2} / {m.critMultiplier:0.###}x");
            MetricRow("Physical / Fire / Cold / Lightning / Void",$"{m.physicalOutput:0.#} / {m.fireOutput:0.#} / {m.coldOutput:0.#} / {m.lightningOutput:0.#} / {m.voidOutput:0.#}");
            MetricRow("Poison / Bleed / Ignite potential",$"{m.poisonDps:0.#} / {m.bleedDps:0.#} / {m.igniteDps:0.#}");
            MetricRow("Life / Armour / Mana",$"{m.life:0.#} / {m.armour:0.#} / {m.mana:0.#}");
            MetricRow("EHP Phys / Fire / Cold / Lightning / Void",$"{m.ehpPhysical:0.#} / {m.ehpFire:0.#} / {m.ehpCold:0.#} / {m.ehpLightning:0.#} / {m.ehpVoid:0.#}");
            foreach(var skill in m.skills)
                MetricRow(skill.name,$"{skill.averageDamagePerUse:0.#}/use · {skill.effectiveCooldown:0.##}s · {(skill.sustainable?"sustainable":"Mana pressure")}");
            foreach(string assumption in m.assumptions)EditorGUILayout.HelpBox(assumption,MessageType.None);
            Heading("DAMAGE CONTRIBUTION TRACE");
            MetricRow("Base weapon average",m.baseWeaponAverage.ToString("0.###"));
            MetricRow("Attribute / level increased",$"{m.weaponAttributeIncreased:P2} / {m.playerLevelIncreased:P2}");
            MetricRow("Generic / Physical increased",$"{m.genericIncreased:P2} / {m.physicalIncreased:P2}");
            MetricRow("Generic / Physical more",$"{m.genericMore:P2} / {m.physicalMore:P2}");
            MetricRow("Crit / Hit Twice expectation",$"{m.critContribution:0.###} / {m.hitTwiceContribution:0.###}");
            if(contributions.Count>0)
            {
                Heading("MARGINAL / ABLATION CONTRIBUTION");
                foreach(var x in contributions)
                    MetricRow(x.source,$"Primary {x.primary:+0.###;-0.###;0} · Secondary {x.secondary:+0.###;-0.###;0}");
                EditorGUILayout.HelpBox("Interacting source categories do not necessarily sum to the full metric.",MessageType.None);
            }
            if(gearResult!=null)
            {
                Heading("SCENARIO-RELATIVE ITEM CONTRIBUTIONS");
                foreach(var x in gearResult.selected)
                    MetricRow(x.item.slot.ToString(),$"Objective {x.score:+0.####;-0.####;0} · Primary {x.primaryDelta:+0.##;-0.##;0} · Secondary {x.secondaryDelta:+0.##;-0.##;0}");
                MetricRow("Gear search debug",$"Generated {gearResult.debug.generated}; retained {gearResult.debug.retained}; states {gearResult.debug.statesEvaluated}; pruned {gearResult.debug.statesPruned}; beam {gearResult.debug.beamWidth}");
                if(GUILayout.Button("EVALUATE FINAL GEARSET WITH COMBAT LAB"))
                {playerBuild=gearResult.build.Clone();CombatLabTransfer.SetPlayer(combatRequest,playerBuild,gearResult.metrics.selectedSkillPolicy);combatPlayerPinned=true;tab=Tab.CombatLab;}
            }
        }
        void GearCurves()
        {
            Heading("PLAYER GEAR CURVES");BuildInputs(false);DrawScenarioCombatLevelControls();
            ObjectiveInputs();SweepInputs(100,false);MetricPopup();
            EditorGUILayout.HelpBox("Exactly one deterministic generated gearset is evaluated per profile and Player Level. This is not a Samples / Level distribution.",MessageType.Info);
            var profiles=LoadProfiles();
            DrawProfileSemantics(profiles);
            if(GUILayout.Button("RUN LOW / MID / OPTIMIZED GEAR CURVES",GUILayout.Height(30)))
                Run("Gear curves",_=>
                {
                    var completed=PlayerScenarioSweep.Run(playerBuild,profiles,objective,
                        sweepStart,sweepEnd,sweepIncrement,false,false,true,1,null,()=>cancelled,
                        scenarioCombatLevelPolicy,scenarioCombatLevelOffset,ScenarioProgress("Gear curves"));
                    playerSweep=completed;curves=completed.Curves(curveMetric);
                });
            DrawPlayerCurves(false);
        }
        void PassiveOptimizer()
        {
            Heading("PRODUCTION PASSIVE OPTIMIZER");BuildInputs();ObjectiveInputs();ConstraintInputs();passivePoints=EditorGUILayout.IntSlider("Passive Point Budget",passivePoints,0,100);using(new EditorGUILayout.HorizontalScope()){GUILayout.Label("Search preset",GUILayout.Width(110));if(GUILayout.Button("Fast · Greedy")){passiveBeam=false;passiveBeamWidth=1;}if(GUILayout.Button("Normal · Beam 100")){passiveBeam=true;passiveBeamWidth=100;}if(GUILayout.Button("Deep · Beam 500")){passiveBeam=true;passiveBeamWidth=500;}}passiveBeam=EditorGUILayout.Toggle("Beam Search",passiveBeam);if(passiveBeam)passiveBeamWidth=EditorGUILayout.IntSlider("Beam Width",passiveBeamWidth,10,2000);int remaining=Math.Max(0,passivePoints-playerBuild.passiveStableIds.Count);long estimate=(long)remaining*(passiveBeam?passiveBeamWidth:1)*Math.Max(25,PassiveTreeOptimizer.LegalNext(playerBuild.AllocationRanks(),playerBuild.classId,playerBuild.subclassId,playerConstraints).Count);EditorGUILayout.LabelField("Rough candidate estimate",$"{estimate:N0} upper-range attempts · {(estimate<10000?"LOW":estimate<100000?"MEDIUM":"HIGH")}");if(passiveBeam&&passiveBeamWidth>=500&&passivePoints>=50)EditorGUILayout.HelpBox("Deep searches can evaluate hundreds of thousands of builds. Run in batchmode or allow substantial time; Cancel Simulation stops at the next progress checkpoint.",MessageType.Warning);using(new EditorGUILayout.HorizontalScope()){if(GUILayout.Button("OPTIMIZE",GUILayout.Height(30)))Run("Passive optimization",_=>{passiveResult=PassiveTreeOptimizer.Optimize(playerBuild,passivePoints,objective,passiveBeam,passiveBeamWidth,playerConstraints,Progress("Passive depth"),()=>cancelled);playerBuild=passiveResult.build.Clone();SetPlayerEvaluation(playerBuild,passiveResult.metrics,true);});if(GUILayout.Button("ANALYZE LEGAL NEXT NODES",GUILayout.Height(30)))Run("Marginal analysis",_=>marginal=PassiveTreeOptimizer.Analyze(playerBuild,objective,playerConstraints,false));if(passiveResult!=null&&GUILayout.Button("EXPORT CSV"))status=WorkbenchExports.SavePassiveCsv(passiveResult);}
            if(passiveResult!=null){Heading("OPTIMAL LEGAL ALLOCATION");EditorGUILayout.LabelField("Algorithm / Score",$"{passiveResult.algorithm} / {passiveResult.score:0.######}");EditorGUILayout.LabelField("Points / Stable IDs",$"{passiveResult.build.passiveStableIds.Count} / {string.Join(", ",passiveResult.build.passiveStableIds)}");foreach(var x in passiveResult.sequence)using(new EditorGUILayout.HorizontalScope()){GUILayout.Label($"{x.point}. {x.name}",GUILayout.Width(260));GUILayout.Label($"Primary {x.primaryDelta:+0.###;-0.###;0}",GUILayout.Width(150));GUILayout.Label($"Secondary {x.secondaryDelta:+0.###;-0.###;0}",GUILayout.Width(160));GUILayout.Label($"Objective {x.scoreDelta:+0.#####;-0.#####;0}");}if(passiveResult.debug.Count>0){var d=passiveResult.debug[^1];EditorGUILayout.LabelField("Search debug",$"Depth {d.depth}; candidate {d.candidateStates}; unique {d.uniqueStates}; retained {d.retainedStates}; diversity {d.diversityGroups}");}if(GUILayout.Button("EVALUATE OPTIMIZED PASSIVES WITH COMBAT LAB")){playerBuild=passiveResult.build.Clone();CombatLabTransfer.SetPlayer(combatRequest,playerBuild,passiveResult.metrics.selectedSkillPolicy);combatPlayerPinned=true;tab=Tab.CombatLab;}}
            if(marginal.Count>0){Heading("IMMEDIATE MARGINAL VALUE");foreach(var x in marginal.Take(30))using(new EditorGUILayout.HorizontalScope()){if(GUILayout.Button(x.name,GUILayout.Width(220)))selectedMarginal=x;GUILayout.Label(x.branch,GUILayout.Width(150));GUILayout.Label($"P {x.primaryDelta:+0.##;-0.##;0}",GUILayout.Width(100));GUILayout.Label($"S {x.secondaryDelta:+0.##;-0.##;0}",GUILayout.Width(100));GUILayout.Label($"Obj {x.objectiveDelta:+0.#####;-0.#####;0}");}if(selectedMarginal!=null&&GUILayout.Button("PING NODE DATA"))PingNode(selectedMarginal.nodeId);}
        }
        void PassiveHeatmap()
        {
            Heading("PASSIVE HEATMAP — ACTUAL AUTHORED LAYOUT");EditorGUILayout.HelpBox("Editor-only overlay. It reads PassiveTreeDefinition.LayoutPosition and never changes node sprites, authored colors, or Branch assets.",MessageType.Info);
            heatmapMode=(HeatmapMode)EditorGUILayout.EnumPopup("Heatmap Mode",heatmapMode);if(marginal.Count==0&&GUILayout.Button("CALCULATE CURRENT BUILD HEATMAP"))Run("Heatmap analysis",_=>marginal=PassiveTreeOptimizer.Analyze(playerBuild,objective,playerConstraints,heatmapMode==HeatmapMode.PathAdjusted));
            heatZoom=EditorGUILayout.Slider("Zoom",heatZoom,.03f,.18f);Rect r=GUILayoutUtility.GetRect(700,700,GUILayout.ExpandWidth(true));EditorGUI.DrawRect(r,new Color(.035f,.04f,.055f));DrawTreeHeatmap(r);
            if(selectedMarginal!=null){EditorGUILayout.HelpBox($"{selectedMarginal.name}\n{selectedMarginal.effect}\nLegal now: {selectedMarginal.immediatelyLegal}\nPrimary {selectedMarginal.primaryDelta:+0.###;-0.###;0}; Secondary {selectedMarginal.secondaryDelta:+0.###;-0.###;0}; Objective {selectedMarginal.objectiveDelta:+0.#####;-0.#####;0}; Path cost {selectedMarginal.pathCost}; Path-adjusted {selectedMarginal.pathAdjustedValue:+0.#####;-0.#####;0}",MessageType.None);using(new EditorGUILayout.HorizontalScope()){if(GUILayout.Button("SELECT OWNING BRANCH SO"))PingNode(selectedMarginal.nodeId);if(GUILayout.Button("WHY THIS NODE?"))EditorUtility.DisplayDialog("Why this node?",$"Immediate Primary: {selectedMarginal.primaryDelta:+0.###;-0.###;0}\nImmediate Secondary: {selectedMarginal.secondaryDelta:+0.###;-0.###;0}\nObjective: {selectedMarginal.objectiveDelta:+0.#####;-0.#####;0}\nMinimum legal package: {selectedMarginal.pathCost} point(s)\nPath-adjusted value: {selectedMarginal.pathAdjustedValue:+0.#####;-0.#####;0}","Close");}}
            EditorGUILayout.LabelField("Legend","Blue: no measured value · Yellow: moderate · Red: strongest value · White: optimizer-selected path");
        }
        void ScenarioSweep()
        {
            Heading("END-TO-END PLAYER SCENARIO SWEEP");
            BuildInputs(false);DrawScenarioCombatLevelControls();ObjectiveInputs();
            SweepInputs(100,false);MetricPopup();
            EditorGUILayout.HelpBox("One deterministic generated gearset per profile/level. Progressive Character starts the next point from the prior selected build, but regenerates gear for all unlocked slots; it does not carry inventory or currency. With Free Respec ON, passives are cleared and reoptimized each point. OFF keeps prior allocation and adds legal nodes. No combat fights are run here.",MessageType.Info);
            scenarioPassives=EditorGUILayout.Toggle("Optimize Passives",scenarioPassives);
            scenarioProgressive=EditorGUILayout.Toggle("Progressive Character",scenarioProgressive);
            if(scenarioProgressive)scenarioFreeRespec=EditorGUILayout.Toggle("Allow Free Respec",scenarioFreeRespec);
            passiveBeamWidth=EditorGUILayout.IntSlider("Passive Beam Width",passiveBeamWidth,1,500);
            var profiles=LoadProfiles();
            DrawProfileSemantics(profiles);
            showScenarioWorkload=EditorGUILayout.Foldout(showScenarioWorkload,"ESTIMATED WORKLOAD",true);
            if(showScenarioWorkload)
            {
                int levels=(sweepEnd-sweepStart)/sweepIncrement+1;
                int points=levels*profiles.Count;
                int candidates=levels*8*profiles.Sum(x=>Mathf.Max(1,x.candidatesPerSlot));
                MetricRow("Levels / Profiles",$"{levels} / {profiles.Count}");
                MetricRow("Generated Gearsets / Candidate Items",$"{points} / ~{candidates}");
                MetricRow("Passive Searches / Combat Fights",$"{(scenarioPassives?points:0)} / 0");
                MetricRow("Search Quality",$"{(passiveBeamWidth>1?$"Beam {passiveBeamWidth}":"Greedy")} / profile-specific gear selection");
                EditorGUILayout.HelpBox("Candidate count is an estimate; retries and optimizer states vary. Each gearset can require many build evaluations. Passive optimization runs once per profile/level because there is one gear sample per point.",MessageType.Info);
            }
            if(GUILayout.Button("RUN PROFILE COMPARISON",GUILayout.Height(30)))
                Run("Scenario sweep",_=>
                {
                    var completed=PlayerScenarioSweep.Run(playerBuild,profiles,objective,
                        sweepStart,sweepEnd,sweepIncrement,scenarioPassives,scenarioProgressive,
                        scenarioFreeRespec,passiveBeamWidth,null,()=>cancelled,
                        scenarioCombatLevelPolicy,scenarioCombatLevelOffset,ScenarioProgress("Scenario"));
                    playerSweep=completed;curves=completed.Curves(curveMetric);
                });
            DrawPlayerCurves(true);
        }
        void DrawProfileSemantics(IReadOnlyList<PlayerGearProfileSO> profiles)
        {
            showProfileSemantics=EditorGUILayout.Foldout(showProfileSemantics,
                "GEAR PROFILE ASSUMPTIONS",true);
            if(!showProfileSemantics)return;
            foreach(var profile in profiles)
                MetricRow(profile.name,
                    $"{profile.selectionStrategy} · {profile.candidatesPerSlot} candidates/slot · retain {profile.candidateRetention} · beam {profile.gearsetBeamWidth} · natural rarity {(profile.useNaturalRarity?"ON":"OFF")}");
            EditorGUILayout.HelpBox("Natural rarity ON bypasses profile min/max rarity. Target percentile only applies to Percentile Target; beam width only applies to Full Gearset Beam Search. Selection Imperfection, Allow Legal Crafting and Deep Endgame Implicit Repair are serialized profile fields but are not consumed by this sweep optimizer. Do not interpret them as active acquisition/crafting simulation.",MessageType.Warning);
        }
        void DrawScenarioCombatLevelControls()
        {
            scenarioCombatLevelPolicy=(CombatLevelSweepPolicy)EditorGUILayout.EnumPopup(
                "Combat Level Policy",scenarioCombatLevelPolicy);
            if(scenarioCombatLevelPolicy==CombatLevelSweepPolicy.Fixed||
                scenarioCombatLevelPolicy==CombatLevelSweepPolicy.AdvanceFromStart)
                playerBuild.combatLevel=EditorGUILayout.IntSlider("Selected Combat Level",
                    playerBuild.combatLevel,1,360);
            if(scenarioCombatLevelPolicy==CombatLevelSweepPolicy.OffsetFromPlayerLevel)
                scenarioCombatLevelOffset=EditorGUILayout.IntSlider("Combat Level Offset",
                    scenarioCombatLevelOffset,-99,350);
            EditorGUILayout.HelpBox(ScenarioLevelPolicy.Description(scenarioCombatLevelPolicy),
                MessageType.None);
        }
        Action<string,float> ScenarioProgress(string operation)=>
            (phase,value)=>
            {
                if(EditorUtility.DisplayCancelableProgressBar("Black-Cube "+operation,
                    phase+" · "+value.ToString("P0"),value))cancelled=true;
            };
        void PlayerVsEnemy()
        {
            Heading("PLAYER vs ENEMY — NEUTRAL ANALYTICAL RATIOS");EditorGUILayout.HelpBox("TTK and TTD are neutral analytical estimates, not simulated encounter duration. Combat Lab will own real fight sequencing.",MessageType.Info);if(playerMetrics==null||playerMetricsBuildHash!=ScenarioResultIdentity.BuildHash(playerBuild)||playerMetricsFingerprint!=ProductionBalanceAdapters.DataFingerprint())SetPlayerEvaluation(playerBuild,PlayerBuildEvaluator.Evaluate(playerBuild),false);if(enemyResult==null){EditorGUILayout.HelpBox("Run Enemy Gear Lab first; the comparison reuses that exact generated dataset.",MessageType.Warning);return;}var enemies=enemyResult.samples.OrderBy(x=>x.gearScore).ToList();EnemySample p50=enemies[Mathf.Clamp((int)Math.Round((enemies.Count-1)*.5),0,enemies.Count-1)],p90=enemies[Mathf.Clamp((int)Math.Round((enemies.Count-1)*.9),0,enemies.Count-1)];EditorGUILayout.LabelField("P50 expected TTK",PlayerScenarioSweep.AnalyticalTtk(playerMetrics,p50).ToString("0.###")+" s-equivalent");EditorGUILayout.LabelField("P90 expected TTK",PlayerScenarioSweep.AnalyticalTtk(playerMetrics,p90).ToString("0.###")+" s-equivalent");EditorGUILayout.LabelField("P50 expected TTD",PlayerScenarioSweep.AnalyticalTtd(playerMetrics,p50).ToString("0.###")+" s-equivalent");EditorGUILayout.LabelField("P90 expected TTD",PlayerScenarioSweep.AnalyticalTtd(playerMetrics,p90).ToString("0.###")+" s-equivalent");if(playerSweep!=null){Heading("NORMALIZED CURVE MODE (FIRST POINT = 100)");var normalized=playerSweep.Curves(curveMetric);foreach(var series in normalized){double baseValue=series.points.FirstOrDefault()?.mean??1;if(Math.Abs(baseValue)<1e-9)baseValue=1;foreach(var point in series.points)point.mean=point.mean/baseValue*100;}Rect r=GUILayoutUtility.GetRect(200,420,GUILayout.ExpandWidth(true));chart.Draw(r,normalized,"Player Level","Growth index");}
        }
        ScenarioSweepResult displayedSweep;string displayedMetric;List<CurveSeries> displayedSeries;
        void DrawPlayerCurves(bool scenario)
        {
            if(playerSweep==null)return;
            if(!ReferenceEquals(displayedSweep,playerSweep)||displayedMetric!=curveMetric)
            {
                displayedSweep=playerSweep;displayedMetric=curveMetric;
                displayedSeries=playerSweep.Curves(curveMetric);
            }
            string request=PlayerScenarioSweep.RequestSignature(playerBuild,LoadProfiles(),
                objective,sweepStart,sweepEnd,sweepIncrement,
                scenario&&scenarioPassives,scenario&&scenarioProgressive,
                !scenario||scenarioFreeRespec,scenario?passiveBeamWidth:1,
                scenarioCombatLevelPolicy,scenarioCombatLevelOffset);
            bool stale=request!=playerSweep.requestSignature||
                playerSweep.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint();
            if(stale)EditorGUILayout.HelpBox(
                "INPUTS CHANGED — RESULT STALE. The graph and rows below retain their captured result contexts; rerun before interpreting them under current controls.",
                MessageType.Warning);
            var series=displayedSeries;
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("EXPORT SWEEP CSV"))status=WorkbenchExports.SaveSweepCsv(
                    playerSweep,playerSweep.points[0].objective);
                if(GUILayout.Button("EXPORT CURVE PNG"))status=chart.ExportPng(series,"player_"+curveMetric);
                if(GUILayout.Button("EXPORT RESULT JSON"))status=WorkbenchExports.SaveJson("player_sweep",playerSweep);
            }
            Rect rect=GUILayoutUtility.GetRect(200,440,GUILayout.ExpandWidth(true));
            string axis=playerSweep.combatLevelPolicy==CombatLevelSweepPolicy.Fixed?
                "Player Level (Combat Level fixed)":"Player Level (Combat Level varies; inspect rows)";
            chart.Draw(rect,series,axis,OptimizationMetricCatalog.Get(curveMetric).Name);
            Heading("CLICK-TO-INSPECT STORED DATA");
            foreach(var point in playerSweep.points)
            {
                string caption=$"L{point.playerLevel} / CL{point.combatLevel} / {point.profile}: "+
                    $"{OptimizationMetricCatalog.Get(curveMetric).Value(point.metrics):0.###}";
                string tip=$"Player Level {point.playerLevel}\nCombat Level {point.combatLevel}\n"+
                    $"Gear Profile {point.profile} v{point.profileVersion}\nSeed {point.seed}\n"+
                    $"Metric {OptimizationMetricCatalog.Get(curveMetric).Name}\n"+
                    $"Objective Score {point.objectiveScore:0.######}\nResult {point.resultId}";
                if(GUILayout.Button(new GUIContent(caption,tip)))InspectScenarioPoint(point);
            }
        }
        void InspectScenarioPoint(PlayerCurvePoint point)
        {
            try
            {
                var inspected=ScenarioResultIdentity.Inspect(point);
                playerBuild=inspected.build.Clone();
                objective=JsonUtility.FromJson<OptimizationObjective>(JsonUtility.ToJson(inspected.objective));
                playerMetrics=inspected.metrics.Clone();
                contributions=inspected.contributions.ToList();
                playerMetricsBuildHash=ScenarioResultIdentity.BuildHash(playerBuild);
                playerMetricsFingerprint=inspected.fingerprint;
                contributionsObjectiveHash=JsonUtility.ToJson(objective);
                selectedScenarioInspection=inspected;
                gearResult=null;passiveResult=null;
                tab=Tab.PlayerBuildLab;
            }
            catch(Exception ex)
            {
                status="ERROR inspecting scenario result: "+ex.Message;
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Scenario result integrity failure",ex.Message,"Close");
            }
        }
        void MetricPopup(){string[] names=OptimizationMetricCatalog.All.Select(x=>x.Name).ToArray();int i=Mathf.Max(0,OptimizationMetricCatalog.All.ToList().FindIndex(x=>x.Id==curveMetric));curveMetric=OptimizationMetricCatalog.All[EditorGUILayout.Popup("Curve Metric",i,names)].Id;}
        List<PlayerGearProfileSO> LoadProfiles()=>new[]{"Low","Mid","Optimized"}.Select(x=>AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>($"Assets/Balance/Profiles/SO_PlayerGearProfile_{x}.asset")).Where(x=>x!=null).ToList();
        List<PassiveMarginalResult> heatLookupSource;Dictionary<int,PassiveMarginalResult> heatLookup=new();double heatMaximum;HeatmapMode heatLookupMode;
        void DrawTreeHeatmap(Rect rect)
        {
            if(Event.current.type!=EventType.Repaint&&Event.current.type!=EventType.MouseDown)return;
            double Value(PassiveMarginalResult x)=>heatmapMode switch{HeatmapMode.PrimaryMetric=>x.primaryDelta,HeatmapMode.SecondaryMetric=>x.secondaryDelta,HeatmapMode.PathAdjusted=>x.pathAdjustedValue,HeatmapMode.SelectedPath=>0,_=>x.objectiveDelta};
            if(!ReferenceEquals(heatLookupSource,marginal)||heatLookupMode!=heatmapMode){heatLookupSource=marginal;heatLookupMode=heatmapMode;heatLookup=marginal.ToDictionary(x=>x.nodeId);heatMaximum=Math.Max(1e-9,marginal.Select(x=>Math.Abs(Value(x))).DefaultIfEmpty(0).Max());}var lookup=heatLookup;double max=heatMaximum;var chosen=new HashSet<string>(passiveResult?.build?.passiveStableIds??playerBuild.passiveStableIds??new());Vector2 center=rect.center+heatPan;
            Handles.BeginGUI();foreach(var e in PassiveTreeDefinition.Edges){Vector2 a=center+PassiveTreeDefinition.Node(e.A).LayoutPosition*heatZoom,b=center+PassiveTreeDefinition.Node(e.B).LayoutPosition*heatZoom;Handles.color=new Color(.25f,.28f,.34f,.45f);Handles.DrawLine(a,b);}Handles.EndGUI();
            foreach(var n in PassiveTreeDefinition.Nodes){Vector2 p=center+n.LayoutPosition*heatZoom;if(!rect.Contains(p))continue;Color c=new(.12f,.24f,.42f);string tip=n.DisplayName;if(lookup.TryGetValue(n.Id,out var v)){double value=Value(v);float t=(float)Math.Clamp(Math.Abs(value)/max,0,1);c=Color.Lerp(new Color(.2f,.45f,.75f),Color.Lerp(Color.yellow,Color.red,t),Mathf.Sqrt(t));tip+=$"\n{heatmapMode}: {value:+0.#####;-0.#####;0}\nPath {v.pathCost}";}if(chosen.Contains(n.StableId))c=Color.white;Rect nr=new(p.x-5,p.y-5,10,10);EditorGUI.DrawRect(nr,c);EditorGUI.LabelField(nr,new GUIContent("",tip));if(Event.current.type==EventType.MouseDown&&nr.Contains(Event.current.mousePosition)){lookup.TryGetValue(n.Id,out selectedMarginal);Event.current.Use();Repaint();}}
        }
        static void PingNode(int id){var n=PassiveTreeDefinition.Node(id);UnityEngine.Object asset=n.IsClassRoute?PassiveTreeDefinition.Database.ClassBranches.FirstOrDefault(x=>x.ClassId==n.RouteClassId):PassiveTreeDefinition.Database.WeaponBranches.FirstOrDefault(x=>x.WeaponId==n.RouteWeaponId);if(asset!=null){Selection.activeObject=asset;EditorGUIUtility.PingObject(asset);}}
        static void Compare(PlayerBuildMetrics a,PlayerBuildMetrics b){foreach(var m in OptimizationMetricCatalog.All){double av=m.Value(a),bv=m.Value(b),pct=Math.Abs(av)<1e-9?0:(bv-av)/Math.Abs(av);EditorGUILayout.LabelField(m.Name,$"A {av:0.###} · B {bv:0.###} · Δ {bv-av:+0.###;-0.###;0} ({pct:+0.##%;-0.##%;0%})");}}
    }
}
