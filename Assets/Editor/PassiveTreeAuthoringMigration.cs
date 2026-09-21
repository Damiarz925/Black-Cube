// One-time, explicit migration of the current V3 runtime definitions into authoritative authoring assets.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PassiveTreeAuthoringMigration
{
    public const string ClassFolder = "Assets/GameData/PassiveTree/Branches/Class";
    public const string WeaponFolder = "Assets/GameData/PassiveTree/Branches/Weapon";
    public const string LibraryFolder = "Assets/GameData/UI/Libraries";
    public const string DatabasePath = "Assets/Resources/GameData/PassiveTree/SO_PassiveTreeDatabase.asset";
    public const string IconLibraryPath = LibraryFolder + "/SO_PassiveNodeIconLibrary.asset";
    public const string EffectCatalogPath = LibraryFolder + "/SO_PassiveEffectCatalog.asset";

    static readonly Dictionary<string, string> ClassAssetNames = new()
    {
        [PlayerClassIds.Warrior] = "SO_Warrior_Branch", [PlayerClassIds.Barbarian] = "SO_Barbarian_Branch",
        [PlayerClassIds.Ranger] = "SO_Ranger_Branch", [PlayerClassIds.Thief] = "SO_Thief_Branch",
        [PlayerClassIds.Mage] = "SO_Mage_Branch", [PlayerClassIds.Priest] = "SO_Priest_Branch"
    };
    static readonly Dictionary<string, string> WeaponAssetNames = new()
    {
        [WeaponTypeIds.Sword] = "SO_Sword_Branch", [WeaponTypeIds.TwoHandedAxe] = "SO_TwoHandedAxe_Branch",
        [WeaponTypeIds.Bow] = "SO_Bow_Branch", [WeaponTypeIds.Staff] = "SO_Staff_Branch",
        [WeaponTypeIds.Dagger] = "SO_Dagger_Branch", [WeaponTypeIds.Sceptre] = "SO_Sceptre_Branch"
    };

    [MenuItem("Black-Cube/UI Authoring/Migrate Current Passive V3 To Assets")]
    public static void GenerateCurrentV3Assets()
    {
        EnsureFolder(ClassFolder); EnsureFolder(WeaponFolder); EnsureFolder(LibraryFolder); EnsureFolder("Assets/Resources/GameData/PassiveTree");
        if (AssetDatabase.LoadAssetAtPath<PassiveTreeDatabaseSO>(DatabasePath) != null)
            throw new InvalidOperationException("Passive authoring assets already exist. Migration is intentionally one-shot and will not overwrite authored data.");

        PassiveNodeIconLibrarySO icons = CreateIconLibrary();
        PassiveEffectCatalogSO effects = CreateEffectCatalog();
        var classAssets = new List<PassiveClassBranchSO>();
        var weaponAssets = new List<PassiveWeaponBranchSO>();
        foreach (string classId in PassiveTreeDefinition.ClassIds) classAssets.Add(CreateClassBranch(classId));
        foreach (string weaponId in PassiveTreeDefinition.WeaponIds) weaponAssets.Add(CreateWeaponBranch(weaponId));

        var database = ScriptableObject.CreateInstance<PassiveTreeDatabaseSO>();
        database.Configure(classAssets, weaponAssets, icons, effects);
        AssetDatabase.CreateAsset(database, DatabasePath);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = database;
        Debug.Log("PASSIVE AUTHORING MIGRATION: Created 6 class branches, 6 weapon branches, the icon library, and the runtime database without changing V3 values.");
    }

    static PassiveClassBranchSO CreateClassBranch(string classId)
    {
        string path = $"{ClassFolder}/{ClassAssetNames[classId]}.asset";
        var asset = ScriptableObject.CreateInstance<PassiveClassBranchSO>();
        var subclasses = SubclassCatalog.ForClass(classId);
        if (subclasses.Count != 2) throw new InvalidOperationException(classId + " must have exactly two subclasses before authoring migration.");
        var tiers = new List<PassiveClassTierData>(10);
        for (int tierNumber = 1; tierNumber <= 10; tierNumber++)
        {
            var tier = new PassiveClassTierData(); tier.SetTier(tierNumber);
            PassiveNodeDefinition spine = PassiveTreeDefinition.Node(PassiveTreeDefinition.ClassSpineNode(classId, tierNumber));
            Copy(tier.Spine, spine);
            CopyClassSide(tier.Left, classId, tierNumber, "left", subclasses[0], subclasses[1]);
            CopyClassSide(tier.Right, classId, tierNumber, "right", subclasses[0], subclasses[1]);
            tiers.Add(tier);
        }
        asset.Configure(classId, PassiveTreeDefinition.SignatureWeapon(classId), subclasses[0].Id, subclasses[1].Id, tiers);
        AssetDatabase.CreateAsset(asset, path); EditorUtility.SetDirty(asset); return asset;
    }

    static void CopyClassSide(PassiveChoiceSideData target, string classId, int tier, string side, SubclassDefinition first, SubclassDefinition second)
    {
        PassiveNodeDefinition[] source = PassiveTreeDefinition.RouteNodes(classId).Select(PassiveTreeDefinition.Node)
            .Where(x => x.Tier == tier && x.ChoiceGroupId.EndsWith(side, StringComparison.Ordinal)).OrderBy(x => x.StableId).ToArray();
        PassiveNodeDefinition[] generic = source.Where(x => !x.IsSubclassChoice).ToArray();
        PassiveNodeDefinition slot = source.Single(x => x.IsSubclassChoice);
        if (generic.Length != 3) throw new InvalidOperationException($"{classId} tier {tier} {side} does not contain three generic choices.");
        Copy(target.A, generic[0]); Copy(target.B, generic[1]); Copy(target.C, generic[2]);
        CopySubclass(target.SubclassA, slot, first); CopySubclass(target.SubclassB, slot, second);
    }

    static void CopySubclass(PassiveAuthoredNode target, PassiveNodeDefinition slot, SubclassDefinition subclass)
    {
        string variant = slot.StableId + ".variant." + subclass.Id.Replace("subclass.", string.Empty).Replace('.', '-');
        target.Configure(variant, subclass.DisplayName, slot.Description, slot.Branch, slot.Size, slot.Kind, slot.Keystone,
            PassiveTreeDefinition.SubclassEffects(subclass.Id, slot), slot.StableId);
    }

    static PassiveWeaponBranchSO CreateWeaponBranch(string weaponId)
    {
        string path = $"{WeaponFolder}/{WeaponAssetNames[weaponId]}.asset";
        var asset = ScriptableObject.CreateInstance<PassiveWeaponBranchSO>();
        var tiers = new List<PassiveWeaponTierData>(5);
        for (int tierNumber = 1; tierNumber <= 5; tierNumber++)
        {
            var tier = new PassiveWeaponTierData(); tier.SetTier(tierNumber);
            Copy(tier.Spine, PassiveTreeDefinition.Node(PassiveTreeDefinition.WeaponSpineNode(weaponId, tierNumber)));
            CopyWeaponSide(tier.Left, weaponId, tierNumber, "left"); CopyWeaponSide(tier.Right, weaponId, tierNumber, "right");
            tiers.Add(tier);
        }
        asset.Configure(weaponId, PassiveTreeDefinition.WeaponClass(weaponId), tiers);
        AssetDatabase.CreateAsset(asset, path); EditorUtility.SetDirty(asset); return asset;
    }

    static void CopyWeaponSide(PassiveChoiceSideData target, string weaponId, int tier, string side)
    {
        PassiveNodeDefinition[] source = PassiveTreeDefinition.RouteNodes(null, weaponId).Select(PassiveTreeDefinition.Node)
            .Where(x => x.Tier == tier && x.ChoiceGroupId.EndsWith(side, StringComparison.Ordinal) && x.IsChoice).OrderBy(x => x.StableId).ToArray();
        if (source.Length != 3) throw new InvalidOperationException($"{weaponId} tier {tier} {side} does not contain three choices.");
        Copy(target.A, source[0]); Copy(target.B, source[1]); Copy(target.C, source[2]);
    }

    static void Copy(PassiveAuthoredNode target, PassiveNodeDefinition source)
        => target.Configure(source.StableId, source.DisplayName, source.Description, source.Branch, source.Size, source.Kind, source.Keystone, source.Effects);

    static PassiveNodeIconLibrarySO CreateIconLibrary()
    {
        var library = ScriptableObject.CreateInstance<PassiveNodeIconLibrarySO>();
        AssetDatabase.CreateAsset(library, IconLibraryPath);
        EnsureFolder("Assets/Resources/UI/PassiveIcons");
        var statBranches = new Dictionary<StatTypes, PassiveBranch>();
        foreach (PassiveNodeDefinition node in PassiveTreeDefinition.Nodes)
            foreach (PassiveEffect effect in node.Effects) if (!statBranches.ContainsKey(effect.Stat)) statBranches.Add(effect.Stat, node.Branch);
        foreach (string classId in PassiveTreeDefinition.ClassIds)
        foreach (SubclassDefinition subclass in SubclassCatalog.ForClass(classId))
        foreach (PassiveNodeDefinition node in PassiveTreeDefinition.Nodes.Where(x => x.RouteClassId == classId && x.IsSubclassChoice))
            foreach (PassiveEffect effect in PassiveTreeDefinition.SubclassEffects(subclass.Id, node)) if (!statBranches.ContainsKey(effect.Stat)) statBranches.Add(effect.Stat, node.Branch);

        var cache = new Dictionary<(PassiveBranch, PassiveNodeVisualState), Sprite>();
        Sprite Resolve(PassiveBranch branch, PassiveNodeVisualState state)
        {
            var key = (branch, state); if (cache.TryGetValue(key, out Sprite existing)) return existing;
            Sprite source = PassiveTreeIconAtlas.Get(branch, PassiveNodeSize.Small, state);
            if (source == null) return null;
            string name=$"{branch}-{state}";Sprite result=PassiveIconLibraryExternalizer.WriteSprite(source,$"Assets/Resources/UI/PassiveIcons/{name}.png");cache.Add(key,result);return result;
        }

        var mappings = new List<PassiveIconMapping>();
        foreach (var pair in statBranches.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal))
        {
            var mapping = new PassiveIconMapping(); mapping.Configure(pair.Key, Resolve(pair.Value, PassiveNodeVisualState.Inactive), Resolve(pair.Value, PassiveNodeVisualState.Hover), Resolve(pair.Value, PassiveNodeVisualState.Allocated), Resolve(pair.Value, PassiveNodeVisualState.Unavailable)); mappings.Add(mapping);
        }
        // EmptyTravel is a logical-only branch and has no legacy atlas row. Use the
        // neutral Magic artwork as the explicit visual fallback for unmapped stats.
        library.Configure(Resolve(PassiveBranch.Magic, PassiveNodeVisualState.Inactive), mappings);
        EditorUtility.SetDirty(library); return library;
    }

    static PassiveEffectCatalogSO CreateEffectCatalog()
    {
        var catalog = ScriptableObject.CreateInstance<PassiveEffectCatalogSO>();
        var mechanics = new List<PassiveMechanicDefinition>();
        foreach (SubclassDefinition subclass in SubclassCatalog.All)
        foreach (string hook in subclass.GrantedSystemHooks)
        {
            if (string.IsNullOrWhiteSpace(hook) || mechanics.Any(x => x.StableId == hook)) continue;
            var entry = new PassiveMechanicDefinition();
            string label = string.Join(" ", hook.Split('.').Last().Split('_').Select(x => char.ToUpperInvariant(x[0]) + x.Substring(1)));
            entry.Configure(hook, label, subclass.DisplayName + " mechanic"); mechanics.Add(entry);
        }
        catalog.Configure(mechanics); AssetDatabase.CreateAsset(catalog, EffectCatalogPath); EditorUtility.SetDirty(catalog); return catalog;
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/'); string current = parts[0];
        for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
    }
}
