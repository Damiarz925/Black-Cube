using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public static class BalanceWorkbenchAssets
    {
        public const string LootProfilePath="Assets/Resources/LootBalanceProfile.asset";
        public const string LootDropProfilePath="Assets/Resources/LootDropBalanceProfile.asset";
        public const string EnemyPowerReferencePath="Assets/Resources/EnemyLootPowerReference.asset";
        [MenuItem("Black-Cube/Balance Workbench/Create Missing Production Assets")]
        public static void CreateMissingProductionAssets()
        {
            Directory.CreateDirectory("Assets/Resources");
            if(AssetDatabase.LoadAssetAtPath<LootBalanceProfileSO>(LootProfilePath)==null)
            {
                var profile=ScriptableObject.CreateInstance<LootBalanceProfileSO>();profile.ResetToProductionDefaults();
                AssetDatabase.CreateAsset(profile,LootProfilePath);
                Debug.Log("Created authoritative production loot profile: "+LootProfilePath);
            }
            if(AssetDatabase.LoadAssetAtPath<LootDropBalanceProfileSO>(LootDropProfilePath)==null)
            {
                var profile=ScriptableObject.CreateInstance<LootDropBalanceProfileSO>();profile.ResetToInitialDefaults();
                AssetDatabase.CreateAsset(profile,LootDropProfilePath);
                Debug.Log("Created editable drop balance profile: "+LootDropProfilePath);
            }
            if(AssetDatabase.LoadAssetAtPath<EnemyLootPowerReferenceSO>(EnemyPowerReferencePath)==null)
            {
                var profile=ScriptableObject.CreateInstance<EnemyLootPowerReferenceSO>();
                AssetDatabase.CreateAsset(profile,EnemyPowerReferencePath);
                Debug.Log("Created empty enemy-power reference; choose an explicit player build before authoring curves: "+EnemyPowerReferencePath);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
    }
}
