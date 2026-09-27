using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public sealed partial class BalanceWorkbenchWindow
    {
        ProgressionHistoryRequest progressionHistoryRequest=new();
        ProgressionHistoryResult progressionHistoryResult;
        RealisticInventoryResult realisticInventoryResult;
        RealisticGearsetResult realisticGearsetResult;
        DefenseAdherence realisticAdherence=DefenseAdherence.Soft;

        void ProgressionHistoryTab()
        {
            Heading("PROGRESSION HISTORY");
            EditorGUILayout.HelpBox("Models production XP and ten encounters per combat level. Assumes encounters are won; this is not a survival simulation. Enemy power is an explicit assumption until reference power distributions are generated.",MessageType.Info);
            progressionHistoryRequest.targetPlayerLevel=EditorGUILayout.IntSlider("Target Player Level",progressionHistoryRequest.targetPlayerLevel,1,100);
            progressionHistoryRequest.mode=(ProgressionHistoryMode)EditorGUILayout.EnumPopup("History Mode",progressionHistoryRequest.mode);
            progressionHistoryRequest.stochastic=EditorGUILayout.Toggle("Seeded Stochastic",progressionHistoryRequest.stochastic);
            progressionHistoryRequest.seed=EditorGUILayout.LongField("Seed",progressionHistoryRequest.seed);
            progressionHistoryRequest.assumedEnemyPower=EditorGUILayout.Slider("Assumed Enemy Power",progressionHistoryRequest.assumedEnemyPower,.1f,5f);
            if(progressionHistoryRequest.mode==ProgressionHistoryMode.TargetFarming)
                progressionHistoryRequest.extraClears=EditorGUILayout.IntSlider("Extra Full Clears",progressionHistoryRequest.extraClears,0,100);
            if(progressionHistoryRequest.mode==ProgressionHistoryMode.Manual)
            {
                progressionHistoryRequest.manualCombatLevel=EditorGUILayout.IntSlider("Combat Level",progressionHistoryRequest.manualCombatLevel,1,360);
                progressionHistoryRequest.manualNormalKills=EditorGUILayout.IntField("Normal Kills",progressionHistoryRequest.manualNormalKills);
                progressionHistoryRequest.manualMagicKills=EditorGUILayout.IntField("Magic Kills",progressionHistoryRequest.manualMagicKills);
                progressionHistoryRequest.manualRareKills=EditorGUILayout.IntField("Rare Kills",progressionHistoryRequest.manualRareKills);
                progressionHistoryRequest.manualLegendaryKills=EditorGUILayout.IntField("Legendary Kills",progressionHistoryRequest.manualLegendaryKills);
                progressionHistoryRequest.manualBossKills=EditorGUILayout.IntField("Boss Kills",progressionHistoryRequest.manualBossKills);
            }
            if(GUILayout.Button("RUN PROGRESSION",GUILayout.Height(32)))
                Run("Production progression history",_=>progressionHistoryResult=ProgressionHistorySimulator.Run(progressionHistoryRequest));
            var result=progressionHistoryResult;
            if(result==null)return;
            if(!string.IsNullOrEmpty(result.warning))EditorGUILayout.HelpBox(result.warning,MessageType.Warning);
            Heading("SUMMARY");
            MetricRow("Target Reached",result.targetReached?"Yes":"No");
            MetricRow("Player / Combat Level",$"{result.finalPlayerLevel} / {result.finalCombatLevel}");
            MetricRow("Straight / Farm Encounters",$"{result.straightEncounters} / {result.farmingEncounters}");
            MetricRow("Normal / Magic Kills",$"{result.normalKills:0.##} / {result.magicKills:0.##}");
            MetricRow("Rare / Legendary Kills",$"{result.rareKills:0.##} / {result.legendaryKills:0.##}");
            MetricRow("Boss Kills",result.bossKills.ToString("0.##"));
            MetricRow("Expected Gear Drops",result.gearDrops.ToString("0.##"));
            Heading("CURRENCY ACQUIRED");
            foreach(var row in result.currencies)
                MetricRow(CurrencyPresentation.Name(row.currency),row.count.ToString("0.###"));
            if(GUILayout.Button("EXPORT HISTORY JSON"))status=WorkbenchExports.SaveJson("progression_history",result);
            Heading("REALISTIC HISTORICAL INVENTORY");
            if(GUILayout.Button("GENERATE GROUND-LOOT INVENTORY",GUILayout.Height(32)))
                Run("Historical production item inventory",_=>realisticInventoryResult=
                    RealisticInventoryGenerator.Generate(result,progressionHistoryRequest.seed+1));
            if(realisticInventoryResult==null)return;
            MetricRow("Observed / Generated Items",$"{realisticInventoryResult.observedDrops} / {realisticInventoryResult.generatedItems}");
            foreach(LootManager.GearType slot in System.Enum.GetValues(typeof(LootManager.GearType)))
                MetricRow(slot.ToString(),realisticInventoryResult.items.FindAll(x=>x.slot==slot).Count.ToString());
            EditorGUILayout.HelpBox(realisticInventoryResult.warning,MessageType.Info);
            if(GUILayout.Button("EXPORT INVENTORY JSON"))status=WorkbenchExports.SaveJson("historical_inventory",realisticInventoryResult);
            Heading("WHOLE-INVENTORY GEAR SEARCH");
            EditorGUILayout.HelpBox("Uses all observed items for candidate ranking, then a bounded diverse whole-set search. Crafting and joint passive optimization are not applied by this operation.",MessageType.Info);
            BuildInputs();ObjectiveInputs();ConstraintInputs();
            realisticAdherence=(DefenseAdherence)EditorGUILayout.EnumPopup("Defense Adherence",realisticAdherence);
            if(GUILayout.Button("OPTIMIZE GROUND-LOOT GEARSET",GUILayout.Height(32)))
                Run("Historical inventory gear search",p =>
                {
                    var build=playerBuild.Clone();
                    build.playerLevel=result.finalPlayerLevel;
                    build.combatLevel=result.finalCombatLevel;
                    realisticGearsetResult=RealisticGearsetOptimizer.Optimize(build,
                        realisticInventoryResult,objective,playerConstraints,realisticAdherence);
                });
            if(realisticGearsetResult?.build==null)
            {
                if(!string.IsNullOrEmpty(realisticGearsetResult?.warning))
                    EditorGUILayout.HelpBox(realisticGearsetResult.warning,MessageType.Warning);
                return;
            }
            var gearset=realisticGearsetResult;
            MetricRow("Items Evaluated",gearset.candidatesEvaluated.ToString());
            MetricRow("Shortlisted",gearset.shortlisted.ToString());
            MetricRow("Gear States Evaluated",gearset.completeSetsEvaluated.ToString());
            MetricRow("Sustainable Total DPS",gearset.metrics.totalSustainableDps.ToString("0.##"));
            MetricRow("Life / Armour",$"{gearset.metrics.life:0.#} / {gearset.metrics.armour:0.#}");
            MetricRow("Fire / Cold / Lightning / Void",$"{gearset.metrics.fireResistance:P0} / {gearset.metrics.coldResistance:P0} / {gearset.metrics.lightningResistance:P0} / {gearset.metrics.voidResistance:P0}");
            if(!string.IsNullOrEmpty(gearset.warning))EditorGUILayout.HelpBox(gearset.warning,MessageType.Warning);
            if(GUILayout.Button("SEND GEARSET TO PLAYER BUILD"))
            {playerBuild=gearset.build.Clone();playerMetrics=gearset.metrics;tab=Tab.PlayerBuildLab;}
        }
    }
}
