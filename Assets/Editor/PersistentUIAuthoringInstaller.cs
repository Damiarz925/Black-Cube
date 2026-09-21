using System;
using UnityEditor;
using UnityEngine;

public static class PersistentUIAuthoringInstaller
{
    public const string GameplayPrefabPath = "Assets/Prefabs/PaperBattle/PaperBattle.prefab";

    [MenuItem("Black-Cube/UI Authoring/Install Authored UI In Production Prefabs")]
    public static void Install()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);
        try
        {
            PaperBattleHUD hud = root.GetComponentInChildren<PaperBattleHUD>(true);
            if (hud == null) throw new InvalidOperationException(GameplayPrefabPath + " does not contain PaperBattleHUD.");
            Canvas canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("PaperBattleHUD does not have a parent Canvas in " + GameplayPrefabPath + ".");
            SkillTreeUI controller = hud.GetComponent<SkillTreeUI>();
            if (controller == null) controller = hud.gameObject.AddComponent<SkillTreeUI>();
            PassiveTreeView view = canvas.GetComponentInChildren<PassiveTreeView>(true);
            if (view == null)
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PassiveTreePrefabBuilder.PrefabPath);
                if (source == null) throw new InvalidOperationException("Missing authored prefab: " + PassiveTreePrefabBuilder.PrefabPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(source, canvas.transform) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate " + PassiveTreePrefabBuilder.PrefabPath);
                instance.name = "Passive Tree Panel";
                view = instance.GetComponent<PassiveTreeView>();
            }
            controller.SetAuthoredView(view);
            EditorUtility.SetDirty(controller);
            PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("UI AUTHORING INSTALL: Authored Passive Tree view is installed in " + GameplayPrefabPath + ". Existing authored layout was preserved.");
    }
}
