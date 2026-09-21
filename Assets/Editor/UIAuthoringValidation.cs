using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class UIAuthoringValidation
{
    public const string ReportPath = "ReviewCaptures/UIAuthoringReport.md";
    static readonly string[] RequiredClassIds = { PlayerClassIds.Warrior, PlayerClassIds.Barbarian, PlayerClassIds.Ranger, PlayerClassIds.Thief, PlayerClassIds.Mage, PlayerClassIds.Priest };
    static readonly string[] RequiredWeaponIds = { WeaponTypeIds.Sword, WeaponTypeIds.TwoHandedAxe, WeaponTypeIds.Bow, WeaponTypeIds.Staff, WeaponTypeIds.Dagger, WeaponTypeIds.Sceptre };

    public static string[] ValidatePassiveBranch(PassiveBranchDataSO branch)
    {
        var errors = new List<string>(); if (branch == null) return new[] { "Branch asset is missing." };
        int expected = branch is PassiveClassBranchSO ? 10 : 5; if (branch.TierCount != expected) errors.Add($"{branch.name}: expected {expected} tiers, found {branch.TierCount}.");
        if (string.IsNullOrWhiteSpace(branch.RouteId)) errors.Add(branch.name + ": route ID is missing.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (PassiveAuthoredNode node in branch.AllAuthoredNodes())
        {
            if (node == null) { errors.Add(branch.name + ": a node definition is null."); continue; }
            if (string.IsNullOrWhiteSpace(node.StableId)) { if (branch is PassiveClassBranchSO || !node.IsUnusedWeaponSubclassPlaceholder()) errors.Add(branch.name + ": a node stable ID is missing."); continue; }
            if (!ids.Add(node.StableId)) errors.Add(branch.name + ": duplicate data ID " + node.StableId);
            if (string.IsNullOrWhiteSpace(node.LogicalSlotId)) errors.Add(node.StableId + ": logical slot ID is missing.");
            foreach (PassiveAuthoredEffect effect in node.Effects)
            {
                if (effect == null) errors.Add(node.StableId + ": null effect line.");
                else if (effect.Kind == PassiveEffectKind.Mechanic && string.IsNullOrWhiteSpace(effect.MechanicId)) errors.Add(node.StableId + ": mechanic is missing.");
            }
        }
        if (branch is PassiveClassBranchSO c)
        {
            if (!PlayerClassCatalog.IsValid(c.ClassId)) errors.Add(c.name + ": invalid class identity.");
            if (!WeaponTypeCatalog.IsValid(c.SignatureWeaponId)) errors.Add(c.name + ": invalid signature weapon.");
            if (!SubclassCatalog.TryGet(c.SubclassAId, out var a) || a.ParentClassId != c.ClassId) errors.Add(c.name + ": invalid Subclass A.");
            if (!SubclassCatalog.TryGet(c.SubclassBId, out var b) || b.ParentClassId != c.ClassId) errors.Add(c.name + ": invalid Subclass B.");
        }
        else if (branch is PassiveWeaponBranchSO w)
        {
            if (!WeaponTypeCatalog.IsValid(w.WeaponId)) errors.Add(w.name + ": invalid weapon identity.");
            if (!PlayerClassCatalog.IsValid(w.OwningClassId)) errors.Add(w.name + ": invalid owning class.");
        }
        return errors.ToArray();
    }

    static bool IsUnusedWeaponSubclassPlaceholder(this PassiveAuthoredNode node) => string.IsNullOrEmpty(node.StableId) && node.Effects.Count == 0;

    public static string[] ValidateAll()
    {
        var errors = new List<string>(); PassiveTreeDatabaseSO database = AssetDatabase.LoadAssetAtPath<PassiveTreeDatabaseSO>(PassiveTreeAuthoringMigration.DatabasePath);
        if (database == null) return new[] { "Passive Tree database is missing: " + PassiveTreeAuthoringMigration.DatabasePath };
        if (database.IconLibrary == null || database.IconLibrary.GenericFallback == null) errors.Add("Passive icon library or generic fallback is missing.");
        if (database.EffectCatalog == null) errors.Add("Passive effect catalog is missing.");
        foreach (string id in RequiredClassIds) { PassiveClassBranchSO branch = database.ClassBranches.FirstOrDefault(x => x != null && x.ClassId == id); if (branch == null) errors.Add("Missing class branch " + id); else errors.AddRange(ValidatePassiveBranch(branch)); }
        foreach (string id in RequiredWeaponIds) { PassiveWeaponBranchSO branch = database.WeaponBranches.FirstOrDefault(x => x != null && x.WeaponId == id); if (branch == null) errors.Add("Missing weapon branch " + id); else errors.AddRange(ValidatePassiveBranch(branch)); }
        var globalIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var branch in database.ClassBranches.Cast<PassiveBranchDataSO>().Concat(database.WeaponBranches)) foreach (var node in branch.AllAuthoredNodes())
            if (node != null && !string.IsNullOrEmpty(node.StableId) && !globalIds.Add(node.StableId)) errors.Add("Duplicate global passive data ID: " + node.StableId);
        ValidatePrefabBindings(errors);
        return errors.Distinct().ToArray();
    }

    static void ValidatePrefabBindings(List<string> errors)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid); GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (prefab == null) continue;
            // IDs identify controls within one authored screen. Reusable prefab assets and their
            // production instances intentionally carry the same IDs, so uniqueness is scoped to
            // each prefab rather than the entire AssetDatabase.
            var ids = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (UIAuthoringElement item in prefab.GetComponentsInChildren<UIAuthoringElement>(true)) AddId(item.StableUiId, path, errors, ids);
            foreach (PassiveNodeBinding item in prefab.GetComponentsInChildren<PassiveNodeBinding>(true)) { AddId(item.StableUiId, path, errors, ids); if (string.IsNullOrWhiteSpace(item.LogicalSlotId)) errors.Add(path + ": passive node binding has no logical slot ID."); }
            foreach (PassiveBranchBinding branch in prefab.GetComponentsInChildren<PassiveBranchBinding>(true))
            {
                if (branch.Data == null) errors.Add(path + ": branch view " + branch.RouteId + " has no branch data asset.");
                else if (branch.RouteId != branch.Data.RouteId) errors.Add(path + ": branch view/data route mismatch for " + branch.RouteId);
                foreach (PassiveTierViewBinding tier in branch.Tiers)
                {
                    ValidateVariant(path, branch.RouteId, tier.spine == null ? Array.Empty<PassiveNodeBinding>() : new[] { tier.spine }, errors);
                    ValidateVariant(path, branch.RouteId, tier.left == null ? Array.Empty<PassiveNodeBinding>() : tier.left.RuntimeNodes(true), errors);
                    ValidateVariant(path, branch.RouteId, tier.left == null ? Array.Empty<PassiveNodeBinding>() : tier.left.RuntimeNodes(false), errors);
                    ValidateVariant(path, branch.RouteId, tier.right == null ? Array.Empty<PassiveNodeBinding>() : tier.right.RuntimeNodes(true), errors);
                    ValidateVariant(path, branch.RouteId, tier.right == null ? Array.Empty<PassiveNodeBinding>() : tier.right.RuntimeNodes(false), errors);
                }
            }
        }
    }

    static void ValidateVariant(string path, string routeId, IEnumerable<PassiveNodeBinding> nodes, List<string> errors)
    {
        var slots = new HashSet<string>(StringComparer.Ordinal);
        foreach (PassiveNodeBinding node in nodes)
            if (node != null && !slots.Add(node.LogicalSlotId)) errors.Add(path + ": duplicate passive slot in one layout variant " + routeId + "/" + node.LogicalSlotId);
    }

    static void AddId(string id, string path, List<string> errors, Dictionary<string, string> ids)
    { if (string.IsNullOrWhiteSpace(id)) { errors.Add(path + ": UI authoring ID is missing."); return; } if (ids.TryGetValue(id, out string first)) errors.Add($"Duplicate UI authoring ID {id}: {first} and {path}"); else ids.Add(id, path); }

    [MenuItem("Black-Cube/UI Authoring/Validate All UI")]
    public static void RunValidation()
    {
        string[] errors = ValidateAll(); Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/UIAuthoringValidation.txt", errors.Length == 0 ? new[] { "PASS: UI authoring assets, passive data, IDs, icons, and bindings are valid." } : errors);
        AssetDatabase.Refresh(); if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors)); Debug.Log("UI AUTHORING VALIDATION: PASS");
    }

    [MenuItem("Black-Cube/UI Authoring/Generate UI Authoring Report")]
    public static void GenerateReport()
    {
        string[] errors = ValidateAll(); Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        var lines = new List<string> { "# Black-Cube UI Authoring Report", "", "Generated: " + DateTime.UtcNow.ToString("O"), "", "## Production scenes", "", "- `Assets/Scenes/SampleScene.unity`", "- `Assets/Scenes/Main Menu.unity`", "", "## Authoring assets", "", "- Passive database: `" + PassiveTreeAuthoringMigration.DatabasePath + "`", "- Passive icon library: `" + PassiveTreeAuthoringMigration.IconLibraryPath + "`", "- Passive effect catalog: `" + PassiveTreeAuthoringMigration.EffectCatalogPath + "`", "- Class branches: `" + PassiveTreeAuthoringMigration.ClassFolder + "`", "- Weapon branches: `" + PassiveTreeAuthoringMigration.WeaponFolder + "`", "", "## Major authored prefabs", "" };
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" })) lines.Add("- `" + AssetDatabase.GUIDToAssetPath(guid) + "`");
        lines.AddRange(new[] { "", "## Runtime placement exceptions", "", "- Connection-line RectTransforms derive from assigned node/junction RectTransforms in edit mode and play mode.", "- Pointer-following tooltips/crafting cursor may change temporary screen position.", "- Variable inventory/stat/mod/relic rows may instantiate authored row prefabs.", "- Floating combat text, projectiles, enemies, and world drops are transient prefab instances.", "", "## Validation", "", errors.Length == 0 ? "PASS" : "FAIL", "" });
        if (errors.Length > 0) foreach (string error in errors) lines.Add("- " + error);
        File.WriteAllLines(ReportPath, lines); AssetDatabase.Refresh(); Debug.Log("UI AUTHORING REPORT: " + Path.GetFullPath(ReportPath));
    }
}
