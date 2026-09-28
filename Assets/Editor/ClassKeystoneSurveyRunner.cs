using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using BlackCube.CombatSimulation;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class ClassKeystoneSurveyNativeStage
    {public string fingerprint,inventoryHash;public PlayerBuildSnapshot build;public RealisticCraftingResult crafting;}
    [Serializable] public sealed class ClassKeystoneSurveyRow
    {
        public int playerLevel,combatLevel,classPoints,offClassPoints,weaponPoints,craftActions;
        public string classId,keystone,weaponId,fingerprint,inventoryHash,historyHash;
        public PlayerBuildSnapshot build;
        public PlayerBuildMetrics metrics;
        public RealisticCraftingResult crafting;
        public CombatBatchResult combat,weaponAblation;
        public double combinedPhysicalReduction;
    }

    // Bounded architecture/economy survey, not automatic balance tuning.
    // Resume only artifacts with the exact current production-data fingerprint.
    public static class ClassKeystoneSurveyRunner
    {
        static string Root="Logs/ClassKeystones/Survey";
        const string Notice="PROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE";
        const int BeamWidth=12,CraftActions=12,FinalFights=30;
        static readonly OptimizationObjective Objective=new(){primary="total_dps",secondary="average_hit",primaryWeight=1,mode=OptimizationMode.Weighted};
        public static void RunAll()
        {
            try{Execute();Debug.Log("CLASS KEYSTONE SURVEY: 42/42 COMPLETE");EditorApplication.Exit(0);}
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        static void Execute()
        {
            // Include code and tuning, not just affix/branch assets: a mechanics
            // fix must never silently reuse an older apparently COMPLETE row.
            string fingerprint=CurrentFingerprint();
            Root=Path.Combine("Logs/ClassKeystones/Survey",fingerprint.Substring(0,16));
            Directory.CreateDirectory(Root);
            var rows=new List<ClassKeystoneSurveyRow>();
            foreach(int level in new[]{30,31,70})
            {
                var shared=Shared(level,fingerprint);
                foreach(string cls in PassiveTreeDefinition.ClassIds)
                foreach(var key in level==30?new[]{PassiveKeystone.None}:ClassKeystoneCatalog.ForClass(cls).Select(x=>x.id))
                {
                    string path=Path.Combine(Root,$"L{level}_{cls}_{key}.json");
                    ClassKeystoneSurveyRow row=null;
                    if(File.Exists(path))
                    {
                        row=JsonUtility.FromJson<ClassKeystoneSurveyRow>(File.ReadAllText(path));
                        if(row.fingerprint!=fingerprint)throw new InvalidOperationException("Survey fingerprint changed. Preserve prior artifacts and use a fresh survey directory.");
                        if(row.combat==null||row.combat.fights!=FinalFights||row.combat.errors!=0)row=null;
                    }
                    if(row==null)
                    {
                        Debug.Log($"CLASS KEYSTONE SURVEY START L{level} {cls} {key}");
                        row=Run(shared,cls,key,fingerprint);
                        File.WriteAllText(path,JsonUtility.ToJson(row,true));
                        Debug.Log($"CLASS KEYSTONE SURVEY COMPLETE L{level} {cls} {key}: {row.combat.playerDps.mean:0.##} DPS");
                    }
                    rows.Add(row);Export(rows);
                }
            }
            if(rows.Count!=42)throw new InvalidOperationException("Survey did not produce all 42 builds.");
        }
        static SubclassSurveyLevel Shared(int level,string fingerprint)
        {
            string path=Path.Combine(Root,$"L{level}_shared.json");
            if(File.Exists(path))return JsonUtility.FromJson<SubclassSurveyLevel>(File.ReadAllText(path));
            long seed=level*1000L+1701;
            var history=ProgressionHistorySimulator.Run(new ProgressionHistoryRequest{targetPlayerLevel=level,mode=ProgressionHistoryMode.Straight,stochastic=true,seed=seed});
            if(!history.targetReached||history.finalPlayerLevel!=level)throw new InvalidOperationException("Straight progression checkpoint unavailable: "+history.warning);
            var ground=RealisticInventoryGenerator.Generate(history,seed+1);
            var shared=new SubclassSurveyLevel{targetPlayerLevel=level,actualPlayerLevel=level,combatLevel=history.finalCombatLevel,seed=seed,history=history,ground=ground,inventoryItems=ground.items.Count,historyHash=Hash(JsonUtility.ToJson(history)),inventoryHash=Hash(JsonUtility.ToJson(ground))};
            SubclassSurveyRunner.SetDefenseTargets(shared);
            var archetype=WorldContentCatalog.Reference.enemyArchetypes.First(x=>x.primaryElement==Element.Phys);
            var samples=Enumerable.Range(0,31).Select(i=>EnemyAuthoringAdapters.PreviewEnemy(archetype.stableId,shared.combatLevel,EnemyAI.EnemyRarity.Rare,0,seed+10000+i)).ToArray();
            shared.referenceEnemy=samples.OrderBy(x=>x.sample.damagePerHit).ElementAt(15);
            shared.referencePhysicalHit=(float)shared.referenceEnemy.sample.damagePerHit;
            shared.enemyArchetypeId=archetype.stableId;shared.enemySeed=shared.referenceEnemy.seed;
            shared.enemyHash=Hash(JsonUtility.ToJson(shared.referenceEnemy));
            shared.physicalTarget=(shared.fireTarget+shared.coldTarget+shared.lightningTarget+shared.voidTarget)/4;
            File.WriteAllText(path,JsonUtility.ToJson(shared,true));return shared;
        }
        static ClassKeystoneSurveyRow Run(SubclassSurveyLevel shared,string cls,PassiveKeystone key,string fingerprint)
        {
            string weapon=cls switch{PlayerClassIds.Warrior=>WeaponTypeIds.Sword,PlayerClassIds.Barbarian=>WeaponTypeIds.TwoHandedAxe,PlayerClassIds.Ranger=>WeaponTypeIds.Bow,PlayerClassIds.Mage=>WeaponTypeIds.Staff,PlayerClassIds.Priest=>WeaponTypeIds.Sceptre,_=>WeaponTypeIds.Dagger};
            var source=new PlayerBuildSnapshot{classId=cls,subclassId=null,weaponTypeId=weapon,playerLevel=shared.actualPlayerLevel,combatLevel=shared.combatLevel,seed=shared.seed,dataFingerprint=fingerprint};
            var floors=new OptimizationConstraints{minimumFireResistanceEnabled=true,minimumFireResistance=shared.fireTarget,minimumColdResistanceEnabled=true,minimumColdResistance=shared.coldTarget,minimumLightningResistanceEnabled=true,minimumLightningResistance=shared.lightningTarget,minimumVoidResistanceEnabled=true,minimumVoidResistance=shared.voidTarget,minimumCombinedPhysicalReductionEnabled=true,minimumCombinedPhysicalReduction=shared.physicalTarget,referencePhysicalHit=shared.referencePhysicalHit};
            // Never let the search substitute another keystone for the requested variant.
            floors.excludedPassiveIds=PassiveTreeDefinition.Nodes.Where(n=>n.Kind==PassiveNodeKind.Keystone&&n.Keystone!=key).Select(n=>n.StableId).ToList();
            // All three variants share the identical pre-keystone state. Cache
            // only that deterministic stage, then deep-clone it independently:
            // no post-keystone allocation, inventory mutation or fight is reused.
            string stagePath=Path.Combine(Root,$"L{shared.actualPlayerLevel}_{cls}_native_stage.json");
            var stage=File.Exists(stagePath)?JsonUtility.FromJson<ClassKeystoneSurveyNativeStage>(File.ReadAllText(stagePath)):null;
            if(stage==null)
            {
                var inventory=JsonUtility.FromJson<RealisticInventoryResult>(JsonUtility.ToJson(shared.ground));
                var crafted=RealisticCraftingOptimizer.Run(source,inventory,Objective,floors,RealisticCraftingSearch.Serious,CraftActions,shared.seed+2);
                var gear=RealisticGearsetOptimizer.Optimize(source,crafted.inventory,Objective,floors,DefenseAdherence.Soft,20,48);
                if(gear.build==null)throw new InvalidOperationException("No legal ground-loot/crafted gearset.");
                var native=PassiveTreeOptimizer.Optimize(gear.build,30,Objective,true,BeamWidth,floors,dynamicDefense:true);
                stage=new ClassKeystoneSurveyNativeStage{fingerprint=fingerprint,inventoryHash=shared.inventoryHash,build=native.build,crafting=crafted};
                File.WriteAllText(stagePath,JsonUtility.ToJson(stage,true));
                Debug.Log($"CLASS KEYSTONE SURVEY NATIVE STAGE COMPLETE L{shared.actualPlayerLevel} {cls}");
            }
            if(stage.fingerprint!=fingerprint||stage.inventoryHash!=shared.inventoryHash)throw new InvalidOperationException("Stale native survey stage.");
            var crafting=JsonUtility.FromJson<RealisticCraftingResult>(JsonUtility.ToJson(stage.crafting));
            var build=stage.build.Clone();
            if(build.passiveStableIds.Count!=30)throw new InvalidOperationException("Pre-keystone build did not spend exactly 30 native points.");
            if(key!=PassiveKeystone.None)
            {
                int keyId=ClassPassiveProgressionRules.Keystones(cls).Single(id=>PassiveTreeDefinition.Node(id).Keystone==key);
                build.passiveStableIds.Add(PassiveTreeDefinition.Node(keyId).StableId);
                if(shared.actualPlayerLevel==70)
                {
                    build.selectedWeaponTreeId=weapon;
                    floors.lockedPassiveIds=build.passiveStableIds.ToList();
                    build=PassiveTreeOptimizer.Optimize(build,70,Objective,true,BeamWidth,floors,dynamicDefense:true).build;
                }
            }
            // One bounded gear re-evaluation after passives; retain the final allocation.
            build=RealisticGearsetOptimizer.Optimize(build,crafting.inventory,Objective,floors,DefenseAdherence.Soft,20,48).build;
            if(build==null||!PlayerProgression.ValidateAllocationState(build.AllocationRanks(),cls,null,build.selectedClassRoutes,build.selectedWeaponTreeId))throw new InvalidOperationException("Final survey allocation is not production-legal.");
            if(build.passiveStableIds.Count!=shared.actualPlayerLevel)throw new InvalidOperationException("Unexpected survey point count.");
            var metrics=PlayerBuildEvaluator.Evaluate(build);
            var combat=CombatLabAdapters.Batch(Request(shared,build,metrics,FinalFights));
            if(combat.errors!=0||combat.fights!=FinalFights)throw new InvalidOperationException("Final Combat Lab validation failed.");
            var row=new ClassKeystoneSurveyRow{playerLevel=shared.actualPlayerLevel,combatLevel=shared.combatLevel,classId=cls,keystone=key.ToString(),weaponId=weapon,fingerprint=fingerprint,inventoryHash=shared.inventoryHash,historyHash=shared.historyHash,build=build,metrics=metrics,crafting=crafting,craftActions=crafting.actions.Count,combat=combat,combinedPhysicalReduction=floors.CombinedPhysicalReduction(metrics)};
            foreach(int id in build.passiveStableIds.Select(PassiveTreeDefinition.NodeId))
            {var n=PassiveTreeDefinition.Node(id);if(!string.IsNullOrEmpty(n.RouteWeaponId))row.weaponPoints++;else if(n.RouteClassId==cls)row.classPoints++;else row.offClassPoints++;}
            if(shared.actualPlayerLevel==70)
            {
                var ablated=build.Clone();ablated.passiveStableIds.RemoveAll(id=>!string.IsNullOrEmpty(PassiveTreeDefinition.Node(PassiveTreeDefinition.NodeId(id)).RouteWeaponId));
                // Keep the final build's action policy; this is an effect ablation,
                // not a second optimization of skills or refunded passive points.
                row.weaponAblation=CombatLabAdapters.Batch(Request(shared,ablated,metrics,FinalFights));
                if(row.weaponAblation.errors!=0)throw new InvalidOperationException("Weapon ablation produced Combat Lab errors.");
            }
            if(Hash(JsonUtility.ToJson(shared.ground))!=shared.inventoryHash)throw new InvalidOperationException("Canonical shared inventory was mutated.");
            return row;
        }
        static void Export(List<ClassKeystoneSurveyRow> rows)
        {
            var text=new StringBuilder("# Class keystone architecture survey\n\n"+Notice+"\n\n");
            text.AppendLine($"Completed {rows.Count}/42. Straight progression, no farming; shared checkpoint history/inventory seeds; independent inventory clones; no subclasses. Serious crafting bounded to {CraftActions} actions, passive beam {BeamWidth}, soft current defensive targets, sustainable total DPS, {FinalFights} final fights. No automatic tuning.\n");
            foreach(int level in new[]{30,31,70})
            {
                text.AppendLine($"## Level {level}\n\n| Class | Keystone | Combat level | Class/off-class/weapon points | Lab DPS | Analytical DPS | Ablated DPS | Win rate | Life | Armour | PDR | F/C/L/V resist |\n|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---|");
                foreach(var r in rows.Where(x=>x.playerLevel==level).OrderBy(x=>x.classId).ThenBy(x=>x.keystone))
                    text.AppendLine($"| {r.classId} | {r.keystone} | {r.combatLevel} | {r.classPoints}/{r.offClassPoints}/{r.weaponPoints} | {r.combat.playerDps.mean:0.##} | {r.metrics.totalSustainableDps:0.##} | {(r.weaponAblation==null?"—":r.weaponAblation.playerDps.mean.ToString("0.##"))} | {r.combat.winRate:P1} | {r.metrics.life:0.##} | {r.metrics.armour:0.##} | {r.combinedPhysicalReduction:P1} | {r.metrics.fireResistance:P0}/{r.metrics.coldResistance:P0}/{r.metrics.lightningResistance:P0}/{r.metrics.voidResistance:P0} |");
                text.AppendLine();
            }
            text.AppendLine("Raw per-build JSON includes all damage types, ailments, healing, skill policy, Mana/Rage/crit/projectile counters and keystone telemetry. Ablation removes weapon allocations without reallocating refunded points. Analytical DPS is an approximation; the final Combat Lab owns sequencing, stack caps, target conditions and self-hits.");
            File.WriteAllText(Path.Combine(Root,"SURVEY.md"),text.ToString());
        }
        public static string CurrentFingerprint()=>Hash(ProductionBalanceAdapters.DataFingerprint()+File.ReadAllText("Assets/Resources/GameData/PassiveTree/SO_ClassKeystoneTuning.asset")+
            string.Join("\n",Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/Editor","*.cs",SearchOption.AllDirectories)).OrderBy(x=>x,StringComparer.Ordinal).Select(File.ReadAllText)));
        public static CombatLabRequest Request(SubclassSurveyLevel shared,PlayerBuildSnapshot build,PlayerBuildMetrics metrics,int fights)
        {
            var request=SubclassSurveyRunner.Request(shared,build,metrics,fights);
            EnforceAutomaticCooldownPolicy(request,CombatLabAdapters.PlayerSnapshot(build));return request;
        }
        public static void EnforceAutomaticCooldownPolicy(CombatLabRequest request,CombatantSnapshot actor)
        {
            // Automatic-only weapon bindings are not optional manual queues.
            // This is cast-mode driven, not a weapon-name check scattered in combat.
            if(actor.skills.Count==0||actor.skills.Any(s=>s.castMode!=PlayerSkillCastMode.AutoCooldown))return;
            request.config.actionPolicy=PlayerActionPolicy.SkillsWhenAvailable;
            request.config.enableSkill1=request.config.enableSkill2=true;
            request.analyticalSelectedSkillPolicy="Both skills — automatic cooldown";
        }
        static string Hash(string value){using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");}
    }
}
