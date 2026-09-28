using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using BlackCube.CombatSimulation;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class ClassKeystoneActorRecord
    {public int level;public string classId,keystone,policy;public CombatantSnapshot actor;}
    [Serializable] public sealed class ClassKeystoneFinalRecord
    {public string optimizationFingerprint,combatFingerprint;public int builds,fights,errors;public List<ClassKeystoneActorRecord> actors=new();}

    // Revalidate sealed builds; never rerun crafting, optimize points or tune values.
    public static class ClassKeystoneSurveyReport
    {
        const string Notice="PROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE";
        static string N(double x)=>x.ToString("0.##",CultureInfo.InvariantCulture);
        static string P(double x)=>N(x*100)+"%";
        static string Name(ClassKeystoneSurveyRow row)=>row.keystone=="None"?"Pre-keystone":ClassKeystoneCatalog.Get((PassiveKeystone)Enum.Parse(typeof(PassiveKeystone),row.keystone)).name;
        static double Counter(CombatBatchResult result,string name)=>result.keystoneTelemetry.Where(x=>x.id==name).Sum(x=>x.total);
        static double Time(CombatBatchResult result)=>Math.Max(.0001,result.duration.mean*result.fights);
        static string Warning(ClassKeystoneSurveyRow row)
        {
            var warnings=new List<string>();var c=row.combat;
            if(row.keystone=="MageShatter"&&Counter(c,"Lightning detonations")==0)warnings.Add("No Shatter");
            if(row.keystone=="PriestFracture"&&Counter(c,"Fracture uptime seconds")==0)warnings.Add("No Fracture");
            if(row.keystone=="BarbarianFullRage"&&Counter(c,"Full-Rage uptime seconds")==0)warnings.Add("No cap uptime");
            if(row.keystone=="BarbarianFire"&&Counter(c,"Converted Physical")==0)warnings.Add("No Physical to convert");
            if(row.keystone=="RangerEndlessPoison"&&!c.ailments.Any(x=>x.id=="Poison"))warnings.Add("No Poison");
            if(row.keystone=="ThiefAilmentCrit"&&Counter(c,"Ailment base Crit gained")==0)warnings.Add("No ailment Crit gained");
            if(row.keystone=="PriestAura"&&Counter(c,"Active aura count × seconds")==0)warnings.Add("No active auras");
            if(row.keystone=="ThiefStealth")warnings.Add("Next-enemy consumption not sampled");
            if(c.duration.mean<2)warnings.Add("Short fight");
            return warnings.Count==0?"—":string.Join("; ",warnings);
        }
        public static void FinalChecks()
        {
            ExportAndRevalidate();
            ClassKeystoneContractExport.VerifyAndCapture();
            GenericClassPassiveVerification.BuildWindows();
            Debug.Log("CLASS KEYSTONE FINAL REVALIDATION / CAPTURES / WINDOWS BUILD: PASS");
        }
        public static void ExportAndRevalidate()
        {
            string originalRoot=Directory.GetDirectories("Logs/ClassKeystones/Survey")
                .Where(d=>Directory.GetFiles(d,"L*.json").Count(p=>!p.EndsWith("_shared.json")&&!p.EndsWith("_native_stage.json"))==42)
                .OrderByDescending(Directory.GetLastWriteTimeUtc).FirstOrDefault();
            if(originalRoot==null)throw new InvalidOperationException("No sealed 42-build optimization survey.");
            var rows=Directory.GetFiles(originalRoot,"L*.json").Where(p=>!p.EndsWith("_shared.json")&&!p.EndsWith("_native_stage.json"))
                .Select(p=>JsonUtility.FromJson<ClassKeystoneSurveyRow>(File.ReadAllText(p))).OrderBy(r=>r.playerLevel).ThenBy(r=>r.classId).ThenBy(r=>r.keystone).ToList();
            string inputFingerprint=rows[0].fingerprint,current=ClassKeystoneSurveyRunner.CurrentFingerprint();
            if(rows.Any(r=>r.fingerprint!=inputFingerprint)||rows.Count(r=>r.playerLevel==30)!=6||rows.Count(r=>r.playerLevel==31)!=18||rows.Count(r=>r.playerLevel==70)!=18)
                throw new InvalidOperationException("Mixed or incomplete optimization survey.");
            string output=Path.Combine(originalRoot,"Validated",current.Substring(0,16));Directory.CreateDirectory(output);
            var record=new ClassKeystoneFinalRecord{optimizationFingerprint=inputFingerprint,combatFingerprint=current,builds=42};
            foreach(var row in rows)
            {
                if(row.build.passiveStableIds.Count!=row.playerLevel||row.classPoints+row.offClassPoints+row.weaponPoints!=row.playerLevel||
                    !PlayerProgression.ValidateAllocationState(row.build.AllocationRanks(),row.classId,null,row.build.selectedClassRoutes,row.build.selectedWeaponTreeId))
                    throw new InvalidOperationException("Invalid sealed allocation: "+row.classId+" "+row.keystone);
                var shared=JsonUtility.FromJson<SubclassSurveyLevel>(File.ReadAllText(Path.Combine(originalRoot,$"L{row.playerLevel}_shared.json")));
                if(row.inventoryHash!=shared.inventoryHash||row.historyHash!=shared.historyHash)throw new InvalidOperationException("Checkpoint provenance mismatch.");
                var actor=CombatLabAdapters.PlayerSnapshot(row.build);
                if(Math.Abs(actor.maximumLife-row.metrics.life)>.01)throw new InvalidOperationException("Static actor changed; optimize a new survey.");
                row.combat=CombatLabAdapters.Batch(ClassKeystoneSurveyRunner.Request(shared,row.build,row.metrics,30));Check(row.combat);record.fights+=row.combat.fights;
                record.actors.Add(new ClassKeystoneActorRecord{level=row.playerLevel,classId=row.classId,keystone=row.keystone,policy=row.combat.request.analyticalSelectedSkillPolicy,actor=actor});
                if(row.playerLevel==70)
                {
                    var ablated=row.build.Clone();ablated.passiveStableIds.RemoveAll(id=>!string.IsNullOrEmpty(PassiveTreeDefinition.Node(PassiveTreeDefinition.NodeId(id)).RouteWeaponId));
                    row.weaponAblation=CombatLabAdapters.Batch(ClassKeystoneSurveyRunner.Request(shared,ablated,row.metrics,30));Check(row.weaponAblation);record.fights+=row.weaponAblation.fights;
                }
                File.WriteAllText(Path.Combine(output,$"L{row.playerLevel}_{row.classId}_{row.keystone}.json"),JsonUtility.ToJson(row,true));
                Debug.Log($"CLASS KEYSTONE FINAL FIGHTS COMPLETE L{row.playerLevel} {row.classId} {row.keystone}: {row.combat.playerDps.mean:0.##} DPS");
            }
            if(record.fights!=1800)throw new InvalidOperationException("Final fight count is not 1800.");
            File.WriteAllText(Path.Combine(output,"ACTOR_INPUTS.json"),JsonUtility.ToJson(record,true));
            ExportMarkdown(rows,record,originalRoot,output);
            Debug.Log("CLASS KEYSTONE FINAL FIGHTS: 42/42 BUILDS, 18/18 ABLATIONS, 1800 FIGHTS, ZERO ERRORS");
        }
        static void Check(CombatBatchResult result){if(result.errors!=0||result.fights!=30)throw new InvalidOperationException("Final fight validation failed.");}
        static void ExportMarkdown(List<ClassKeystoneSurveyRow> rows,ClassKeystoneFinalRecord record,string source,string output)
        {
            var text=new StringBuilder("# Class keystone survey — final production-policy fights\n\n"+Notice+"\n\n");
            text.AppendLine($"42 builds plus 18 weapon ablations; 30 fights each, **1800 fights and zero errors**. No auto-tuning. Optimization provenance: {record.optimizationFingerprint}. Final combat/actor provenance: {record.combatFingerprint}.\n");
            text.AppendLine($"Original bounded-search artifacts remain in {source}. Revalidated per-build JSON and ACTOR_INPUTS.json are in {output}. The new fights use the exact saved allocations, gear and seeds; no crafting/passive searches were repeated. Automatic-only skill bindings always use both skills, matching live Staff behavior; the analytical manual-skill policy cannot disable them. Secondary damage/recovery parity was regression-tested before this replay.\n");
            text.AppendLine("Search: straight progression/no farming, signature weapons, no subclasses, identical checkpoint history/ground-loot seeds, independent cloned inventories, Serious crafting capped at 12 actions, passive beam 12, gear shortlist 20/beam 48, soft current defensive targets. These are bounded heuristic builds, not proven global optima. Level 30 and 31 have different checkpoint inventories; do not read their DPS difference as an isolated keystone multiplier. Analytical ailment DPS is uncapped search potential, not encounter DPS; short fights, mitigation, stack timing and overkill materially change the final Lab result.\n");
            foreach(int level in new[]{30,31,70})
            {
                text.AppendLine($"## Level {level}\n\n| Class | Keystone | Weapon | Combat level | Native/off-class/weapon points | Lab DPS | Analytical DPS | Without weapon DPS | Mean seconds | Win rate | Life | Armour | Combined PDR | F/C/L/V resist | Actual skill policy | Warnings |\n|---|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|");
                foreach(var row in rows.Where(r=>r.playerLevel==level))
                    text.AppendLine($"| {row.classId} | {Name(row)} | {row.weaponId} | {row.combatLevel} | {row.classPoints}/{row.offClassPoints}/{row.weaponPoints} | {N(row.combat.playerDps.mean)} | {N(row.metrics.totalSustainableDps)} | {(row.playerLevel!=70||row.weaponAblation==null?"—":N(row.weaponAblation.playerDps.mean))} | {N(row.combat.duration.mean)} | {P(row.combat.winRate)} | {N(row.metrics.life)} | {N(row.metrics.armour)} | {P(row.combinedPhysicalReduction)} | {P(row.metrics.fireResistance)}/{P(row.metrics.coldResistance)}/{P(row.metrics.lightningResistance)}/{P(row.metrics.voidResistance)} | {row.combat.request.analyticalSelectedSkillPolicy} | {Warning(row)} |");
                text.AppendLine();
            }
            text.AppendLine("## Resource/attack telemetry\n\nCounts and amounts are totals across each build's 30 fights; percentage inputs are configured actor values, not uptime claims. Full snapshots include every ailment coefficient, cooldown, Mana cost, penetration, resistance cap, Rage-retention and recovery input.\n\n| Level | Class | Keystone | APS | Configured Crit | Crit/Multistrike/Precision events | Projectiles launched/impacted | Would-be regen / Blood Engine recovery | Revenge / Aura Effect | Mana spent/regen/on-hit; zero-Mana seconds | Rage generated/spent/decayed |\n|---:|---|---|---:|---:|---|---|---|---|---|---|");
            foreach(var row in rows)
            {
                var a=record.actors.Single(x=>x.level==row.playerLevel&&x.classId==row.classId&&x.keystone==row.keystone).actor;var c=row.combat;
                text.AppendLine($"| {row.playerLevel} | {row.classId} | {Name(row)} | {N(a.attackSpeed)} | {P(a.critChance)} | {c.criticalCount}/{c.hitTwiceCount}/{c.precisionCount} | {c.projectilesLaunched}/{c.projectilesImpacted} | {P(a.wouldBeLifeRegenerationFraction)}/{P(a.Has(PassiveKeystone.BarbarianRecovery)?a.wouldBeLifeRegenerationFraction*PassiveKeystoneState.Value(PassiveKeystone.BarbarianRecovery):0)} | {P(a.revengeEffect)}/{P(a.auraEffect)} | {N(c.manaSpent)}/{N(c.manaRegenerated)}/{N(c.manaOnHit)}; {N(c.timeAtZeroMana)} | {N(c.rageGenerated)}/{N(c.rageSpent)}/{N(c.rageLostToDecay)} |");
            }
            text.AppendLine("\n## Damage, ailments, healing and keystone activity\n\nAilment entries show damage per simulated second and maximum/average stacks (stack-time divided by all fight-time). Keystone entries show summed counter values and observation counts. No entry means no observed activity, not proof the mechanic is unavailable.\n\n| Level | Class | Keystone | Player damage by type | Ailment DPS, max/avg stacks | Healing effective / overheal | Keystone counters: total [count] |\n|---:|---|---|---|---|---|---|");
            foreach(var row in rows)
            {
                var c=row.combat;double time=Time(c);
                string types=string.Join("; ",c.damageByType.Where(x=>x.id.StartsWith("Player/")).Select(x=>x.id.Substring(7)+": "+N(x.total)));
                string ailments=string.Join("; ",c.ailments.Select(x=>$"{x.id}: {N(x.damage/time)}, {x.maxStacks}/{N(x.stackTime/time)}"));
                string heals=string.Join("; ",c.healing.Select(x=>$"{x.id}: {N(x.effective)}/{N(x.overheal)}"));
                string keys=string.Join("; ",c.keystoneTelemetry.Select(x=>$"{x.id}: {N(x.total)} [{x.count}]"));
                text.AppendLine($"| {row.playerLevel} | {row.classId} | {Name(row)} | {types} | {ailments} | {heals} | {keys} |");
            }
            text.AppendLine("\n## Interpretation limits\n\n- Weapon ablation keeps final gear, class allocations and actual skill policy; points are refunded and never reallocated. It estimates these **provisional** packages, not final weapon balance.\n- Soft defenses can miss targets. Wins against one sampled rare archetype, especially short fights, do not establish boss survival or balanced endgame difficulty.\n- Threshold/conditional keystones can be inactive in the selected build. The warning/counter columns distinguish that from focused mechanic regression tests.\n- Independent one-enemy fights cannot verify Vanishing Blade's next-enemy first-attack consumption. Native manual encounter-chain smoke remains separate.\n- Automatic Staff casting increases Mana pressure and self-bolt opportunities. Neither Staff auto-cast was disabled in these final fights.\n");
            File.WriteAllText("Docs/CLASS_KEYSTONE_SURVEY_RESULTS.md",text.ToString());
        }
    }
}
