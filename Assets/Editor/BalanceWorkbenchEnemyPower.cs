using System;
using System.Linq;
using BlackCube.CombatSimulation;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public static class EnemyPowerReferenceAuthoring
    {
        // Explicit designer operation: never regenerate approved references in OnGUI/runtime.
        public static EnemyLootPowerReferenceRow Generate(PlayerBuildSnapshot build,int combatLevel,
            EnemyAI.EnemyRarity rarity,int samples,long seed,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(build==null)throw new ArgumentNullException(nameof(build));
            if(build.combatLevel!=combatLevel)throw new InvalidOperationException(
                "Reference player Combat Level must match the enemy reference level.");
            if(build.equipment==null||build.equipment.Count==0)throw new InvalidOperationException(
                "REFERENCE PLAYER INCOMPLETE: choose an explicit equipped build first.");
            var profile=AssetDatabase.LoadAssetAtPath<EnemyLootPowerReferenceSO>(BalanceWorkbenchAssets.EnemyPowerReferencePath)
                ??throw new InvalidOperationException("Missing EnemyLootPowerReference asset.");
            var player=CombatLabAdapters.PlayerSnapshot(build);
            var population=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest
            {
                archetypeId="Any",level=combatLevel,sampleCount=Mathf.Max(1,samples),
                productionRarity=false,rarity=rarity,seed=seed
            },progress,cancelled);
            if(population.samples.Count==0)throw new InvalidOperationException("No enemies sampled.");
            var average=new CombatantSnapshot
            {
                id="average",name=$"Average {rarity} L{combatLevel}",maximumLife=(float)population.samples.Average(x=>x.life),
                attackSpeed=(float)population.samples.Average(x=>x.attackSpeed),
                armour=(float)population.samples.Average(x=>x.armour),
                fireResistance=(float)population.samples.Average(x=>x.fireResistance),
                coldResistance=(float)population.samples.Average(x=>x.coldResistance),
                lightningResistance=(float)population.samples.Average(x=>x.lightningResistance),
                voidResistance=(float)population.samples.Average(x=>x.voidResistance),
                critChance=(float)population.samples.Average(x=>x.critChance),
                critMultiplier=CombatCalculator.BaseCriticalMultiplier+(float)population.samples.Average(x=>x.critMultiplier)
            };
            foreach(var sample in population.samples)
            {
                float amount=(float)sample.damagePerHit/population.samples.Count;
                switch(sample.primaryDamage)
                {
                    case "Fire":average.basicDamage.fire+=amount;break;
                    case "Cold":average.basicDamage.cold+=amount;break;
                    case "Light":average.basicDamage.lightning+=amount;break;
                    case "Void":average.basicDamage.voidDamage+=amount;break;
                    default:average.basicDamage.physical+=amount;break;
                }
            }
            var row=new EnemyLootPowerReferenceRow
            {
                combatLevel=combatLevel,rarity=rarity,referencePlayer=player,averageEnemy=average,
                sampleCount=population.samples.Count,sourceBuildFingerprint=JsonUtility.ToJson(build)
            };
            Undo.RecordObject(profile,"Generate enemy-power reference");
            profile.rows.RemoveAll(x=>x!=null&&x.combatLevel==combatLevel&&x.rarity==rarity);
            profile.rows.Add(row);
            profile.productionFingerprint=ProductionBalanceAdapters.DataFingerprint();
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
            return row;
        }
    }

    public sealed partial class BalanceWorkbenchWindow
    {
        int enemyPowerLevel=50,enemyPowerSamples=24;
        EnemyAI.EnemyRarity enemyPowerRarity=EnemyAI.EnemyRarity.Rare;
        long enemyPowerSeed=54001;
        EnemyLootPowerReferenceRow enemyPowerRow;
        EnemyPowerResult enemyPowerPreview;
        void EnemyPowerTab()
        {
            Heading("REFERENCE PLAYER AND ENEMY POWER");
            var profile=AssetDatabase.LoadAssetAtPath<EnemyLootPowerReferenceSO>(BalanceWorkbenchAssets.EnemyPowerReferencePath);
            if(profile==null){EditorGUILayout.HelpBox("Missing EnemyLootPowerReference asset. Run Create Missing Production Assets.",MessageType.Error);return;}
            bool stale=profile.IsStale(ProductionBalanceAdapters.DataFingerprint());
            EditorGUILayout.HelpBox(stale?"STALE / INCOMPLETE reference. Loot power is neutral until an explicit matching reference exists.":
                "Reference fingerprint current. Missing levels/rarities still use neutral power and are shown as incomplete.",
                stale?MessageType.Warning:MessageType.Info);
            enemyPowerLevel=EditorGUILayout.IntSlider("Combat Level",enemyPowerLevel,1,360);
            enemyPowerRarity=(EnemyAI.EnemyRarity)EditorGUILayout.EnumPopup("Enemy Rarity",enemyPowerRarity);
            enemyPowerSamples=EditorGUILayout.IntSlider("Enemy Samples",enemyPowerSamples,1,256);
            enemyPowerSeed=EditorGUILayout.LongField("Seed",enemyPowerSeed);
            EditorGUILayout.LabelField("Selected player level",playerBuild.playerLevel.ToString());
            EditorGUILayout.LabelField("Selected player combat level",playerBuild.combatLevel.ToString());
            EditorGUILayout.LabelField("Equipped reference items",(playerBuild.equipment?.Count??0).ToString());
            if(GUILayout.Button("GENERATE EXPLICIT REFERENCE",GUILayout.Height(32))&&
                EditorUtility.DisplayDialog("Generate enemy-power reference?",
                    "This derives an average generated enemy against the currently selected Player Build. It writes one level/rarity row and is Undo-safe.","Generate","Cancel"))
            {
                Run("Generating enemy-power reference",p=>enemyPowerRow=EnemyPowerReferenceAuthoring.Generate(
                    playerBuild,enemyPowerLevel,enemyPowerRarity,enemyPowerSamples,enemyPowerSeed,Progress("Enemies"),()=>cancelled));
            }
            if(profile.TryGet(enemyPowerLevel,enemyPowerRarity,out var found))enemyPowerRow=found;
            if(enemyPowerRow==null||enemyPowerRow.combatLevel!=enemyPowerLevel||enemyPowerRow.rarity!=enemyPowerRarity)return;
            Heading("REFERENCE BASELINE");
            MetricRow("Samples",enemyPowerRow.sampleCount.ToString());
            MetricRow("Average enemy Life",enemyPowerRow.averageEnemy.maximumLife.ToString("0.##"));
            MetricRow("Enemy→player baseline DPS",EnemyLootPowerScorer.ExpectedDps(enemyPowerRow.averageEnemy,enemyPowerRow.referencePlayer).ToString("0.##"));
            MetricRow("Player→enemy baseline DPS",EnemyLootPowerScorer.ExpectedDps(enemyPowerRow.referencePlayer,enemyPowerRow.averageEnemy).ToString("0.##"));
            if(GUILayout.Button("PREVIEW GENERATED ENEMY",GUILayout.Height(30)))
            {
                Run("Previewing enemy power",_=>
                {
                    var id=WorldContentCatalog.Reference.enemyArchetypes.First().stableId;
                    var sample=CombatLabAdapters.EnemySnapshot(id,enemyPowerLevel,enemyPowerRarity,0,enemyPowerSeed);
                    enemyPowerPreview=EnemyLootPowerScorer.Evaluate(sample,enemyPowerRow,profile.offenseWeight);
                });
            }
            if(enemyPowerPreview.available)
            {
                Heading("GENERATED ENEMY POWER");
                MetricRow("Offense pressure",enemyPowerPreview.offensePressure.ToString("0.###"),"×");
                MetricRow("Defense pressure",enemyPowerPreview.defensePressure.ToString("0.###"),"×");
                MetricRow("Power / loot multiplier",enemyPowerPreview.power.ToString("0.###"),"×");
                var drop=LootDropBalanceProfileSO.Current.EvaluateCurrency(CraftingCurrencyType.NormalToMagic,
                    new DropRateContext(enemyPowerLevel,enemyPowerRarity,false,enemyPowerPreview.power));
                MetricRow("Normal→Magic budget",(drop.finalBudget*100f).ToString("0.###"),"%");
            }
            Heading("PRODUCTION EDITING");
            var so=new SerializedObject(profile);so.Update();EditorGUILayout.PropertyField(so.FindProperty("offenseWeight"));
            if(so.hasModifiedProperties&&GUILayout.Button("APPLY TO PRODUCTION")&&
                EditorUtility.DisplayDialog("Apply enemy-power weights?","This changes production loot power scoring.","Apply","Cancel"))
            {Undo.RecordObject(profile,"Change enemy-power weights");so.ApplyModifiedProperties();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();}
        }
    }
}
