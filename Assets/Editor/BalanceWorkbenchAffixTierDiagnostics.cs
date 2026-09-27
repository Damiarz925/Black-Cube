using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public sealed partial class BalanceWorkbenchWindow
    {
        StatTypes affixTierStat=StatTypes.CritChance;
        bool showAffixTierDiagnostics;

        void DrawAffixTierDiagnostics()
        {
            showAffixTierDiagnostics=EditorGUILayout.Foldout(showAffixTierDiagnostics,
                "Production Affix Tier Cliff Diagnostic (read-only)",true);
            if(!showAffixTierDiagnostics)return;
            var db=AssetDatabase.LoadAssetAtPath<ModDatabase>(
                "Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
            if(db==null){EditorGUILayout.HelpBox("Production ModDatabase is missing.",MessageType.Error);return;}
            EditorGUILayout.HelpBox("Ranges and unlocks come from legal production tiers for the selected slot. REVIEW only flags a large numerical jump; it is not a balance verdict.",MessageType.Info);
            if(affixRequest.slot==LootManager.GearType.Weapons)
            {
                DrawAffixLadder(db,StatTypes.WeaponBaseDmg,LootManager.GearType.Weapons);
                DrawAffixLadder(db,StatTypes.WeaponBaseAttackSpeed,LootManager.GearType.Weapons);
                DrawAffixLadder(db,StatTypes.WeaponBaseCrit,LootManager.GearType.Weapons);
            }
            affixTierStat=(StatTypes)EditorGUILayout.EnumPopup("Selected Affix Family",affixTierStat);
            DrawAffixLadder(db,affixTierStat,affixRequest.slot);
        }

        static void DrawAffixLadder(ModDatabase db,StatTypes stat,LootManager.GearType slot)
        {
            var pools=GearStatLists.BuildDefaultStatPools();
            if(!pools.TryGetValue(slot,out var allowed)||!allowed.Contains(stat))
            {EditorGUILayout.HelpBox($"{stat} is not in the production {slot} pool.",MessageType.None);return;}
            var definition=db.GetDefinition(stat);
            if(definition==null)
            {EditorGUILayout.HelpBox($"{stat} has no production affix definition.",MessageType.Warning);return;}
            var tiers=ModManager.ApplicableTiers(definition,slot)
                .OrderBy(x=>x.minItemLevel).ThenByDescending(x=>x.tierIndex).ToArray();
            Heading($"{stat} — {slot} ({AffixPolicy.Side(stat)})");
            if(tiers.Length==0)
            {EditorGUILayout.HelpBox("No legal tiers for this slot.",MessageType.Info);return;}
            float previous=0;
            foreach(var tier in tiers)
            {
                float midpoint=(tier.minValue+tier.maxValue)*.5f;
                string jump=previous>0?$" · Δ {(midpoint/previous-1):P0}":"";
                string review=previous>0&&midpoint/previous>=2?" · REVIEW jump":"";
                string paired=tier.pairedDamage?
                    $" · paired high {tier.minHighValue:0.###}–{tier.maxHighValue:0.###}":"";
                MetricRow($"T{tier.tierIndex} · unlock item L{tier.minItemLevel}",
                    $"{tier.minValue:0.###}–{tier.maxValue:0.###} · mid {midpoint:0.###}{paired}{jump}{review}");
                previous=midpoint;
            }
        }
    }
}
