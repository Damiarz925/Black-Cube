using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public sealed partial class BalanceWorkbenchWindow
    {
        ProgressionHistoryRequest progressionHistoryRequest=new();
        ProgressionHistoryResult progressionHistoryResult;
        RealisticInventoryResult realisticInventoryResult;
        RealisticCraftingResult realisticCraftingResult;
        RealisticGearsetResult realisticGearsetResult;
        RealisticGearsetResult groundComparison,craftedComparison;
        RealisticPlayerResult realisticPlayerResult;
        string realisticCraftingSettings;
        DefenseAdherence realisticAdherence=DefenseAdherence.Soft;
        bool realisticUseCrafting=true;
        RealisticCraftingSearch realisticCraftingSearch=RealisticCraftingSearch.Fast;
        int realisticMaximumCrafts=12;

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
                Run("Production progression history",_=>
                {
                    progressionHistoryResult=ProgressionHistorySimulator.Run(progressionHistoryRequest);
                    realisticInventoryResult=null;realisticCraftingResult=null;
                    realisticGearsetResult=null;realisticPlayerResult=null;
                    groundComparison=null;craftedComparison=null;
                });
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
                Run("Historical production item inventory",_=>
                {
                    realisticInventoryResult=RealisticInventoryGenerator.Generate(result,progressionHistoryRequest.seed+1);
                    realisticCraftingResult=null;realisticGearsetResult=null;realisticPlayerResult=null;
                    groundComparison=null;craftedComparison=null;
                });
            if(realisticInventoryResult==null)return;
            MetricRow("Observed / Generated Items",$"{realisticInventoryResult.observedDrops} / {realisticInventoryResult.generatedItems}");
            foreach(LootManager.GearType slot in System.Enum.GetValues(typeof(LootManager.GearType)))
                MetricRow(slot.ToString(),realisticInventoryResult.items.FindAll(x=>x.slot==slot).Count.ToString());
            foreach(LootManager.GearRarity rarity in System.Enum.GetValues(typeof(LootManager.GearRarity)))
                MetricRow(rarity+" Items",realisticInventoryResult.items.Count(x=>x.rarity==rarity).ToString());
            EditorGUILayout.HelpBox(realisticInventoryResult.warning,MessageType.Info);
            if(GUILayout.Button("EXPORT INVENTORY JSON"))status=WorkbenchExports.SaveJson("historical_inventory",realisticInventoryResult);
            Heading("BUILD AND DEFENSE TARGETS");
            BuildInputs();ObjectiveInputs();ConstraintInputs();
            string settings=JsonUtility.ToJson(playerBuild)+JsonUtility.ToJson(objective)+
                JsonUtility.ToJson(playerConstraints)+realisticInventoryResult.seed;
            if(realisticCraftingResult!=null&&settings!=realisticCraftingSettings)
            {
                realisticCraftingResult=null;realisticGearsetResult=null;
                realisticPlayerResult=null;
                groundComparison=null;craftedComparison=null;
            }
            Heading("SHARED-CURRENCY CRAFTING");
            EditorGUILayout.HelpBox("Ground Loot + Expected Crafting uses one shared historical currency inventory, production Crafting Potential and production RNG. Fast / Serious / Deep bound the number of projects and action-roll samples; all original items remain available for final gear selection.",MessageType.Info);
            realisticCraftingSearch=(RealisticCraftingSearch)EditorGUILayout.EnumPopup("Search Tier",realisticCraftingSearch);
            realisticMaximumCrafts=EditorGUILayout.IntSlider("Maximum Craft Actions",realisticMaximumCrafts,0,100);
            if(GUILayout.Button("RUN EXPECTED CRAFTING",GUILayout.Height(32)))
                Run("Historical shared-currency crafting",_=>
                {
                    var build=playerBuild.Clone();build.playerLevel=result.finalPlayerLevel;
                    build.combatLevel=result.finalCombatLevel;
                    realisticCraftingResult=RealisticCraftingOptimizer.Run(build,
                        realisticInventoryResult,objective,playerConstraints,realisticCraftingSearch,
                        realisticMaximumCrafts,progressionHistoryRequest.seed+2,()=>cancelled,
                        Progress("Crafting"));
                    realisticCraftingSettings=settings;
                    realisticGearsetResult=null;realisticPlayerResult=null;
                    groundComparison=null;craftedComparison=null;
                });
            if(realisticCraftingResult!=null)
            {
                MetricRow("Crafts Applied",realisticCraftingResult.actions.Count.ToString());
                MetricRow("Projects Reevaluated",realisticCraftingResult.projectsReevaluated.ToString());
                foreach(var currency in realisticCraftingResult.spent)
                    MetricRow("Spent "+CurrencyPresentation.Name(currency.currency),currency.count.ToString("0"));
                foreach(var currency in realisticCraftingResult.remaining.Where(x=>x.count>0))
                    MetricRow("Unused "+CurrencyPresentation.Name(currency.currency),currency.count.ToString("0"));
                EditorGUILayout.HelpBox(realisticCraftingResult.stoppingReason,MessageType.Info);
                if(GUILayout.Button("EXPORT CRAFTING JSON"))status=WorkbenchExports.SaveJson("historical_crafting",realisticCraftingResult);
                Heading("TOP INITIAL CRAFTABILITY PROJECTS");
                foreach(var project in realisticCraftingResult.initialCraftability
                    .OrderByDescending(x=>x.score).Take(8))
                    MetricRow($"Item {project.inventoryIndex}: {project.item.Split('\n')[0]}",
                        project.score.ToString("+0.###;-0.###;0"));
                Heading("CRAFT TRACE");
                foreach(var action in realisticCraftingResult.actions)
                    MetricRow($"Craft {action.number}: item {action.inventoryIndex}",
                        CurrencyPresentation.Name(action.currency)+
                        (action.currency==CraftingCurrencyType.EmpowermentCatalyst?$" ({action.targetStat})":"")+
                        (action.focused?$" ({action.focusedSide})":""));
            }
            Heading("WHOLE-INVENTORY GEAR SEARCH");
            EditorGUILayout.HelpBox("Uses all observed items for candidate ranking, then a bounded diverse whole-set search. Joint passive optimization is not applied by this operation.",MessageType.Info);
            bool useCrafting=EditorGUILayout.Toggle("Use Expected Crafting",realisticUseCrafting);
            if(useCrafting!=realisticUseCrafting)
            {realisticUseCrafting=useCrafting;realisticGearsetResult=null;realisticPlayerResult=null;}
            realisticAdherence=(DefenseAdherence)EditorGUILayout.EnumPopup("Defense Adherence",realisticAdherence);
            if(realisticUseCrafting&&realisticCraftingResult==null)
                EditorGUILayout.HelpBox("Run Expected Crafting above, or turn off Use Expected Crafting for the Ground Loot Only comparison.",MessageType.Warning);
            if(GUILayout.Button(realisticUseCrafting?"OPTIMIZE CRAFTED GEARSET":"OPTIMIZE GROUND-LOOT GEARSET",GUILayout.Height(32)))
                Run("Historical inventory gear search",p =>
                {
                    if(realisticUseCrafting&&realisticCraftingResult==null)
                        throw new System.InvalidOperationException("Run Expected Crafting before optimizing the crafted inventory.");
                    var build=playerBuild.Clone();
                    build.playerLevel=result.finalPlayerLevel;
                    build.combatLevel=result.finalCombatLevel;
                    realisticGearsetResult=RealisticGearsetOptimizer.Optimize(build,
                        realisticUseCrafting?realisticCraftingResult.inventory:realisticInventoryResult,
                        objective,playerConstraints,realisticAdherence);
                    realisticPlayerResult=null;
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
            MetricRow("Basic / Skill DPS",$"{gearset.metrics.basicDps:0.##} / {gearset.metrics.sustainableSkillDps:0.##}");
            MetricRow("Skill Policy",gearset.metrics.selectedSkillPolicy);
            MetricRow("Mana Regen / Starvation",$"{gearset.metrics.manaRegen:0.##} / {gearset.metrics.manaStarvationFraction:P1}");
            MetricRow("Life / Armour",$"{gearset.metrics.life:0.#} / {gearset.metrics.armour:0.#}");
            MetricRow("Physical Damage Reduction",gearset.metrics.physicalDamageReduction.ToString("P1"));
            MetricRow("Life Regen / On Hit",$"{gearset.metrics.lifeRegen:0.##} / {gearset.metrics.lifeOnHit:0.##}");
            MetricRow("Fire / Cold / Lightning / Void",$"{gearset.metrics.fireResistance:P0} / {gearset.metrics.coldResistance:P0} / {gearset.metrics.lightningResistance:P0} / {gearset.metrics.voidResistance:P0}");
            var ordinaryMods=gearset.build.equipment.SelectMany(x=>x.mods)
                .Where(x=>!x.implicitMod&&!Gear.IsWeaponBaseStat(x.stat)).ToList();
            int Count(params StatTypes[] stats)=>ordinaryMods.Count(x=>stats.Contains(x.stat));
            Heading("OBSERVED EQUIPPED AFFIX PRESSURE");
            MetricRow("Resistance / Max Resistance Affixes",Count(StatTypes.FireRes,StatTypes.ColdRes,
                StatTypes.LightRes,StatTypes.VoidRes,StatTypes.AllRes,StatTypes.MaxFireRes,
                StatTypes.MaxColdRes,StatTypes.MaxLightRes,StatTypes.MaxVoidRes,
                StatTypes.MaxAllRes).ToString());
            MetricRow("Life / Armour / PDR Affixes",$"{Count(StatTypes.Life):0} / {Count(StatTypes.FlatArmour,StatTypes.ArmourPercent):0} / {Count(StatTypes.PhysicalDamageReduction):0}");
            MetricRow("Recovery Affixes",Count(StatTypes.LifeRegeneration,StatTypes.LifeOnHit,
                StatTypes.LifeOnKill,StatTypes.ManaRegeneration,StatTypes.ManaOnHit,
                StatTypes.ManaOnKill).ToString());
            MetricRow("Defensive Prefix / Suffix",$"{ordinaryMods.Count(x=>IsDefensiveAffix(x.stat)&&AffixPolicy.Side(x.stat)==AffixSide.Prefix)} / {ordinaryMods.Count(x=>IsDefensiveAffix(x.stat)&&AffixPolicy.Side(x.stat)==AffixSide.Suffix)}");
            MetricRow("Offense / Utility Affixes",ordinaryMods.Count(x=>!IsDefensiveAffix(x.stat)).ToString());
            if(realisticUseCrafting&&realisticCraftingResult!=null)
                MetricRow("Equipped crafted items",realisticCraftingResult.actions.Select(x=>x.inventoryIndex)
                    .Distinct().Count(i=>gearset.build.equipment.Any(e=>
                        object.ReferenceEquals(e,realisticCraftingResult.inventory.items[i]))).ToString());
            if(!string.IsNullOrEmpty(gearset.warning))EditorGUILayout.HelpBox(gearset.warning,MessageType.Warning);
            Heading("GROUND VS CRAFTED COMPARISON");
            if(realisticCraftingResult!=null&&GUILayout.Button("COMPARE SAME-HISTORY GEARSETS",GUILayout.Height(30)))
                Run("Ground versus crafted comparison",_=>
                {
                    var build=playerBuild.Clone();build.playerLevel=result.finalPlayerLevel;
                    build.combatLevel=result.finalCombatLevel;
                    groundComparison=RealisticGearsetOptimizer.Optimize(build,realisticInventoryResult,
                        objective,playerConstraints,realisticAdherence);
                    craftedComparison=RealisticGearsetOptimizer.Optimize(build,realisticCraftingResult.inventory,
                        objective,playerConstraints,realisticAdherence);
                });
            if(groundComparison?.metrics!=null&&craftedComparison?.metrics!=null)
            {
                MetricRow("Ground / Crafted Total DPS",$"{groundComparison.metrics.totalSustainableDps:0.##} / {craftedComparison.metrics.totalSustainableDps:0.##}");
                MetricRow("Ground / Crafted Life",$"{groundComparison.metrics.life:0.#} / {craftedComparison.metrics.life:0.#}");
                MetricRow("Ground / Crafted Armour",$"{groundComparison.metrics.armour:0.#} / {craftedComparison.metrics.armour:0.#}");
                MetricRow("Ground / Crafted Mana Regen",$"{groundComparison.metrics.manaRegen:0.##} / {craftedComparison.metrics.manaRegen:0.##}");
            }
            if(GUILayout.Button("SEND GEARSET TO PLAYER BUILD"))
            {playerBuild=gearset.build.Clone();playerMetrics=gearset.metrics;tab=Tab.PlayerBuildLab;}
            Heading("JOINT GEAR + PASSIVE SEARCH");
            EditorGUILayout.HelpBox("Alternates production-legal passive allocations with the whole historical gear inventory. Each passive pass starts from the locked/source allocation; defense deficits are valued dynamically. This is a bounded local search, not proof of a global optimum.",MessageType.Info);
            if(GUILayout.Button("OPTIMIZE GEAR + PASSIVES",GUILayout.Height(32)))
                Run("Joint historical gear and passive search",_=>
                {
                    var build=playerBuild.Clone();build.playerLevel=result.finalPlayerLevel;
                    build.combatLevel=result.finalCombatLevel;
                    realisticPlayerResult=RealisticPlayerOptimizer.Optimize(build,
                        realisticUseCrafting?realisticCraftingResult.inventory:realisticInventoryResult,
                        objective,playerConstraints,realisticAdherence,
                        Mathf.Clamp(result.finalPlayerLevel,1,100),2,()=>cancelled,
                        Progress("Gear + passives"));
                });
            if(realisticPlayerResult==null)return;
            if(!string.IsNullOrEmpty(realisticPlayerResult.warning))
                EditorGUILayout.HelpBox(realisticPlayerResult.warning,MessageType.Warning);
            if(realisticPlayerResult.build==null)return;
            MetricRow("Joint Iterations",realisticPlayerResult.iterations.ToString());
            MetricRow("Passive Points",realisticPlayerResult.build.passiveStableIds.Count.ToString());
            MetricRow("Joint Sustainable Total DPS",realisticPlayerResult.metrics.totalSustainableDps.ToString("0.##"));
            MetricRow("Joint Fire / Cold / Lightning / Void",$"{realisticPlayerResult.metrics.fireResistance:P0} / {realisticPlayerResult.metrics.coldResistance:P0} / {realisticPlayerResult.metrics.lightningResistance:P0} / {realisticPlayerResult.metrics.voidResistance:P0}");
            if(GUILayout.Button("SEND JOINT BUILD TO PLAYER BUILD"))
            {playerBuild=realisticPlayerResult.build.Clone();playerMetrics=realisticPlayerResult.metrics;tab=Tab.PlayerBuildLab;}
        }
        static bool IsDefensiveAffix(StatTypes stat)=>stat is
            StatTypes.FireRes or StatTypes.ColdRes or StatTypes.LightRes or StatTypes.VoidRes or
            StatTypes.AllRes or StatTypes.MaxFireRes or StatTypes.MaxColdRes or
            StatTypes.MaxLightRes or StatTypes.MaxVoidRes or StatTypes.MaxAllRes or
            StatTypes.Life or StatTypes.FlatArmour or StatTypes.ArmourPercent or
            StatTypes.PhysicalDamageReduction or StatTypes.LifeRegeneration or
            StatTypes.LifeOnHit or StatTypes.LifeOnKill or StatTypes.ManaRegeneration or
            StatTypes.ManaOnHit or StatTypes.ManaOnKill or
            StatTypes.ReducedShockEffect or StatTypes.ReducedChillEffect;
    }
}
