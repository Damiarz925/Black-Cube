using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Tooling3DataMigration
{
    public const string AssetPath="Assets/Resources/GameData/WorldContentDatabase.asset";

    [MenuItem("Black-Cube/Tooling/Generate Authoritative World Content Asset")]
    public static void GenerateWithConfirmation()
    {
        if(File.Exists(AssetPath)&&!EditorUtility.DisplayDialog("Replace world content asset?","This rebuilds the authoritative asset from the current production catalog. Existing authored changes would be replaced.","Replace","Cancel"))return;
        Generate();
    }

    public static void Generate()
    {
        if(File.Exists(AssetPath))throw new InvalidOperationException("Authoritative world content asset already exists; use the confirmed menu workflow to replace it.");
        Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
        WorldContentDatabase generated=ProductionWorldContent.Build();
        WorldContentDatabase asset=ScriptableObject.CreateInstance<WorldContentDatabase>();
        EditorUtility.CopySerialized(generated,asset);asset.name="WorldContentDatabase";asset.hideFlags=HideFlags.None;
        AssetDatabase.CreateAsset(asset,AssetPath);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        UnityEngine.Object.DestroyImmediate(generated);WorldContentCatalog.Reload();
        Debug.Log($"Created authoritative production world content asset at {AssetPath}.");
    }

    public static void GenerateBatch()
    {
        try{Generate();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }

    public static void UpgradeAuthoritativeAssetBatch()
    {
        try
        {
            var asset=AssetDatabase.LoadAssetAtPath<WorldContentDatabase>(AssetPath);if(asset==null)throw new InvalidOperationException("Missing authoritative world content asset.");
            if(asset.enemyRarityProfiles==null||asset.enemyRarityProfiles.Count==0){var reference=ProductionWorldContent.Build();asset.enemyRarityProfiles=reference.enemyRarityProfiles;EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);UnityEngine.Object.DestroyImmediate(reference);}
            WorldContentCatalog.Reload();EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
