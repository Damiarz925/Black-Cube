using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public static class BalanceWorkbenchAssets
    {
        public const string LootProfilePath="Assets/Resources/LootBalanceProfile.asset";
        [MenuItem("Black-Cube/Balance Workbench/Create Missing Production Assets")]
        public static void CreateMissingProductionAssets()
        {
            if(AssetDatabase.LoadAssetAtPath<LootBalanceProfileSO>(LootProfilePath)!=null)return;
            Directory.CreateDirectory("Assets/Resources");var profile=ScriptableObject.CreateInstance<LootBalanceProfileSO>();profile.ResetToProductionDefaults();AssetDatabase.CreateAsset(profile,LootProfilePath);AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("Created authoritative production loot profile: "+LootProfilePath);
        }
    }
}
