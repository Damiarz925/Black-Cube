using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public sealed partial class BalanceWorkbenchWindow
    {
        bool dropAuthoringOpen;
        void DrawDropAuthoring()
        {
            var profile=AssetDatabase.LoadAssetAtPath<LootDropBalanceProfileSO>(BalanceWorkbenchAssets.LootDropProfilePath);
            if(profile==null){EditorGUILayout.HelpBox("Missing production LootDropBalanceProfile asset.",MessageType.Error);return;}
            var world=WorldProgression.Resolve(drop.level,drop.stage,WorldContentCatalog.Reference);
            var context=new DropRateContext(drop.level,drop.rarity,drop.boss,drop.fixedEnemyPower,
                world.Encounter?.stableId,world.Location?.stableId,
                drop.archetypeId=="Any"?null:drop.archetypeId,null);
            Heading("DROP BUDGET PREVIEW");
            var gear=profile.EvaluateGear(context);
            MetricRow("Gear budget",gear.finalBudget.ToString("0.###"),"copies");
            foreach(var rule in profile.currencies)
            {
                if(rule==null)continue;
                if(rule.useLegacyWeightedRoll)
                {
                    MetricRow(CurrencyPresentation.Name(rule.currency),"Legacy weighted path");
                    continue;
                }
                var budget=profile.EvaluateCurrency(rule.currency,context);
                MetricRow(CurrencyPresentation.Name(rule.currency),
                    (budget.finalBudget*100f).ToString("0.###"),"%");
            }
            if(GUILayout.Button((dropAuthoringOpen?"▾":"▸")+" PRODUCTION EDITING — DROP PROFILE"))
                dropAuthoringOpen=!dropAuthoringOpen;
            if(!dropAuthoringOpen)return;
            EditorGUILayout.HelpBox("These are editable starting assumptions. Preview/simulation does not change production data. Apply is explicit and Undo-safe.",MessageType.Info);
            var so=new SerializedObject(profile);so.Update();
            foreach(string field in new[]{"gearBaseBudget","normalRarityAdditive","magicRarityAdditive",
                "rareRarityAdditive","legendaryRarityAdditive","bossAdditive",
                "normalGearAdditive","magicGearAdditive","rareGearAdditive",
                "legendaryGearAdditive","bossGearAdditive","currencies","stages","locations",
                "archetypes","enemies","bosses"})
            {
                var property=so.FindProperty(field);
                if(property!=null)EditorGUILayout.PropertyField(property,true);
            }
            if(so.hasModifiedProperties)
            {
                EditorGUILayout.HelpBox("Pending drop-profile changes have not been applied.",MessageType.Warning);
                if(GUILayout.Button("APPLY TO PRODUCTION",GUILayout.Height(30))&&
                    EditorUtility.DisplayDialog("Apply drop-profile changes?",
                        "This writes the authoritative loot-drop asset. Undo is available.","Apply","Cancel"))
                {
                    Undo.RecordObject(profile,"Apply drop-profile changes");
                    so.ApplyModifiedProperties();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
                    ProductionBalanceAdapters.InvalidateFingerprint();status="Production drop profile updated.";
                }
            }
        }
    }
}
