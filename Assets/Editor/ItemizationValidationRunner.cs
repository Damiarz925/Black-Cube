using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Read-only deterministic audit of authored definitions, production loot, crafting
// additions and the same forEnemy roll path consumed by generated enemy equipment.
public static class ItemizationValidationRunner
{
    [MenuItem("Black Cube/Validation/Validate Itemization")]
    public static void Run()=>RunAt("Logs/Step12_5G-itemization-validation.txt");

    public static void RunStep13()=>RunAt("Logs/Step13-itemization-validation.txt");
    public static void RunStep14()=>RunAt("Logs/Step14-itemization-validation.txt");
    public static void RunStep14_5()=>RunAt("Logs/Step14_5-itemization-validation.txt");

    static void RunAt(string reportPath)
    {
        const string path = "Assets/Prefabs/Scriptable Objects/ModDatabase.asset";
        var database = AssetDatabase.LoadAssetAtPath<ModDatabase>(path);
        var errors = new List<string>();
        ItemizationValidator.ValidateCatalog(database, errors);
        if (database == null) throw new InvalidOperationException(string.Join("\n", errors));
        var state = UnityEngine.Random.state;
        var host = new GameObject("Isolated itemization audit");
        host.SetActive(false);
        try
        {
            UnityEngine.Random.InitState(125125);
            var roller = host.AddComponent<ModManager>();
            roller.ConfigureForIsolatedRolling(database);
            int cases = 0;
            foreach (LootManager.GearType slot in Enum.GetValues(typeof(LootManager.GearType)))
                foreach (LootManager.GearRarity rarity in Enum.GetValues(typeof(LootManager.GearRarity)))
                    foreach (int level in new[] { 1, 10, 50, 100 })
                        for (int sample = 0; sample < 5; sample++)
                        {
                            var element = slot == LootManager.GearType.Weapons
                                ? new[] { Element.Phys, Element.Fire, Element.Cold, Element.Light, Element.Void }[sample]
                                : Element.Phys;
                            int count = rarity switch
                            {
                                LootManager.GearRarity.Normal => 1,
                                LootManager.GearRarity.Magic => 2,
                                LootManager.GearRarity.Rare => 3 + sample % 2,
                                _ => 5 + sample % 2
                            };
                            List<RolledMod> playerMods=null;
                            for(int attempt=0;attempt<64&&playerMods==null;attempt++)
                                playerMods=roller.RollEquipmentModsForItem(slot,rarity,level,element);
                            if(playerMods==null)errors.Add($"Cannot construct {rarity} {slot} at ilvl {level}.");
                            else ItemizationValidator.ValidateRolledMods(slot, rarity, level, playerMods,
                                database, errors, requireImplicit:true);
                            var enemyMods = roller.RollModsForItem(slot, rarity, level, count, element, true);
                            ItemizationValidator.ValidateRolledMods(slot, rarity, level, enemyMods,
                                database, errors, enforceCapacity:false);
                            cases += 2;
                            if (rarity != LootManager.GearRarity.Rare || sample != 0) continue;
                            var gearHost = new GameObject("Isolated crafting audit", typeof(Gear));
                            try
                            {
                                var gear = gearHost.GetComponent<Gear>();
                                gear.Initialize(slot, LootManager.GearRarity.Normal, level, element);
                                gear.ApplyMods(roller.RollEquipmentModsForItem(slot,
                                    LootManager.GearRarity.Normal, level, element));
                                gear.SetRarity(LootManager.GearRarity.Rare);
                                var added = roller.RollAdditionalMod(gear, LootManager.GearRarity.Rare);
                                if (added != null)
                                {
                                    var crafted = gear.rolledMods.ToList(); crafted.Add(added);
                                    ItemizationValidator.ValidateRolledMods(slot,
                                        LootManager.GearRarity.Rare, level, crafted, database, errors);
                                    cases++;
                                }
                            }
                            finally { UnityEngine.Object.DestroyImmediate(gearHost); }
                        }
            string output = Path.GetFullPath(reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, $"Itemization audit: {cases} deterministic cases, "
                + $"{errors.Count} errors\n" + string.Join("\n", errors));
            if (errors.Count > 0) throw new InvalidOperationException($"Itemization audit found {errors.Count} errors; see {output}");
            Debug.Log($"Itemization audit passed: {cases} cases; {output}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Random.state = state;
        }
    }
}
