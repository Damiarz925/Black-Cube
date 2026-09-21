using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class UIAuthoringWindow : EditorWindow
{
    enum Tab { Overview, HUD, Inventory, PassiveTree, Menus, Crafting, Challenges, Tooltips, VisualLibraries, Validation }
    static readonly string[] MajorPrefabs =
    {
        "Assets/Prefabs/UI/GameplayHUD.prefab", "Assets/Prefabs/UI/InventoryPanel.prefab", "Assets/Prefabs/UI/PassiveTreePanel.prefab",
        "Assets/Prefabs/UI/PauseMenu.prefab", "Assets/Prefabs/UI/StatsPanel.prefab", "Assets/Prefabs/UI/SkillSelectionPanel.prefab",
        "Assets/Prefabs/UI/SubclassPanel.prefab", "Assets/Prefabs/UI/ChallengePanel.prefab", "Assets/Prefabs/UI/EndgameCraftingPanel.prefab",
        "Assets/Prefabs/UI/ItemTooltip.prefab", "Assets/Prefabs/UI/RebirthPanel.prefab", "Assets/Prefabs/UI/ModListPanel.prefab",
        "Assets/Prefabs/UI/MainMenu.prefab", "Assets/Prefabs/UI/CharacterSlots.prefab", "Assets/Prefabs/UI/EnemyInspectionPanel.prefab",
        "Assets/Prefabs/UI/StatusHUD.prefab", "Assets/Prefabs/UI/StatusBadge.prefab",
        "Assets/Resources/UI/Tooltips/CurrencyTooltip.prefab", "Assets/Resources/UI/Tooltips/RelicTooltip.prefab"
    };
    Tab tab; Vector2 scroll;

    [MenuItem("Black-Cube/UI Authoring")]
    public static void Open() { var window = GetWindow<UIAuthoringWindow>("Black-Cube UI Authoring"); window.minSize = new Vector2(620, 460); window.Show(); }

    void OnGUI()
    {
        tab = (Tab)GUILayout.Toolbar((int)tab, Enum.GetNames(typeof(Tab)), GUILayout.Height(28));
        scroll = EditorGUILayout.BeginScrollView(scroll); EditorGUILayout.Space(8);
        switch (tab)
        {
            case Tab.Overview: DrawOverview(); break;
            case Tab.HUD: DrawScreen("HUD", "Assets/Prefabs/UI/GameplayHUD.prefab"); break;
            case Tab.Inventory: DrawScreen("INVENTORY", "Assets/Prefabs/UI/InventoryPanel.prefab"); break;
            case Tab.PassiveTree: DrawPassiveTree(); break;
            case Tab.Menus: DrawPaths("MENUS", "Assets/Scenes/Main Menu.unity", "Assets/Prefabs/UI/PauseMenu.prefab", "Assets/Prefabs/UI/SkillSelectionPanel.prefab", "Assets/Prefabs/UI/SubclassPanel.prefab", "Assets/Prefabs/UI/RebirthPanel.prefab", "Assets/Prefabs/UI/ModListPanel.prefab"); break;
            case Tab.Crafting: DrawScreen("ENDGAME CRAFTING", "Assets/Prefabs/UI/EndgameCraftingPanel.prefab"); break;
            case Tab.Challenges: DrawScreen("CHALLENGES", "Assets/Prefabs/UI/ChallengePanel.prefab"); break;
            case Tab.Tooltips: DrawPaths("TOOLTIPS", "Assets/Prefabs/UI/ItemTooltip.prefab", "Assets/Resources/UI/Tooltips/CurrencyTooltip.prefab", "Assets/Resources/UI/Tooltips/RelicTooltip.prefab"); DrawSelectedTransform(); DrawPrefabActions(); break;
            case Tab.VisualLibraries: DrawLibraries(); break;
            case Tab.Validation: DrawValidation(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawOverview()
    {
        EditorGUILayout.LabelField("EDITOR-AUTHORED PRESENTATION", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Runtime controllers own behavior and data updates. Scene/prefab RectTransforms own permanent placement, size, scale, rotation, anchors, and hierarchy.", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("OPEN GAMEPLAY SCENE")) OpenScene("Assets/Scenes/SampleScene.unity");
        if (GUILayout.Button("OPEN MAIN MENU SCENE")) OpenScene("Assets/Scenes/Main Menu.unity");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("OPEN HUD PREFAB")) OpenAsset("Assets/Prefabs/UI/GameplayHUD.prefab");
        if (GUILayout.Button("OPEN INVENTORY PREFAB")) OpenAsset("Assets/Prefabs/UI/InventoryPanel.prefab");
        if (GUILayout.Button("OPEN PASSIVE TREE PREFAB")) OpenAsset("Assets/Prefabs/UI/PassiveTreePanel.prefab");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("VALIDATE ALL UI")) UIAuthoringValidation.RunValidation();
        if (GUILayout.Button("GENERATE WIRING REPORT")) UIAuthoringValidation.GenerateReport();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(); EditorGUILayout.LabelField("Configured production assets", EditorStyles.boldLabel);
        foreach (string path in MajorPrefabs) AssetRow(path);
        DrawSelectedTransform();
    }

    void DrawScreen(string title, string path)
    {
        DrawPaths(title, path); DrawSelectedTransform(); DrawPrefabActions();
    }

    void DrawPaths(string title, params string[] paths)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel); foreach (string path in paths) AssetRow(path);
    }

    void DrawPassiveTree()
    {
        DrawPaths("PASSIVE TREE VIEW", "Assets/Prefabs/UI/PassiveTreePanel.prefab");
        PassiveTreeDatabaseSO database = AssetDatabase.LoadAssetAtPath<PassiveTreeDatabaseSO>(PassiveTreeAuthoringMigration.DatabasePath);
        if (database == null) { EditorGUILayout.HelpBox("The authoritative Passive Tree database is missing. Restore the checked-in asset; do not regenerate over authored data.", MessageType.Error); return; }
        EditorGUILayout.LabelField("Class Branch Assets", EditorStyles.boldLabel); foreach (var branch in database.ClassBranches) ObjectRow(branch);
        EditorGUILayout.LabelField("Weapon Branch Assets", EditorStyles.boldLabel); foreach (var branch in database.WeaponBranches) ObjectRow(branch);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("PASSIVE DATABASE")) Select(database);
        if (GUILayout.Button("ICON LIBRARY")) Select(database.IconLibrary);
        if (GUILayout.Button("EFFECT CATALOG")) Select(database.EffectCatalog);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("SHOW/HIDE AUTHORING LABELS IN OPEN PREFAB")) ToggleLabels();
        EditorGUILayout.HelpBox("Default layout is never applied automatically. Any default-layout command must be invoked explicitly from the prefab authoring tools.", MessageType.Info);
        DrawSelectedTransform(); DrawPrefabActions();
    }

    void DrawLibraries()
    {
        DrawPaths("VISUAL LIBRARIES", PassiveTreeAuthoringMigration.IconLibraryPath, PassiveTreeAuthoringMigration.EffectCatalogPath, "Assets/GameData/UI/Libraries/SO_UIVisualLibrary.asset", "Assets/GameData/UI/Libraries/SO_DefaultButtonVisualStyle.asset");
        EditorGUILayout.HelpBox("Node-specific Custom icon → Primary-effect mapping → Generic fallback. Button controllers choose a state; the selected style asset defines its sprite, colors, and scale multiplier.", MessageType.Info);
    }

    void DrawValidation()
    {
        EditorGUILayout.LabelField("VALIDATION AND REPORTS", EditorStyles.boldLabel);
        if (GUILayout.Button("VALIDATE ALL UI")) UIAuthoringValidation.RunValidation();
        if (GUILayout.Button("GENERATE UI AUTHORING REPORT")) UIAuthoringValidation.GenerateReport();
        AssetRow("Docs/UI_RUNTIME_AUTHORING_AUDIT.md"); AssetRow(UIAuthoringValidation.ReportPath);
    }

    void DrawSelectedTransform()
    {
        UIAuthoringElement element = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<UIAuthoringElement>() : null;
        RectTransform rect = element != null ? element.AuthoredRect : Selection.activeTransform as RectTransform;
        EditorGUILayout.Space(); EditorGUILayout.LabelField("SELECTED RECTTRANSFORM", EditorStyles.boldLabel);
        if (rect == null) { EditorGUILayout.HelpBox("Select an authored UI RectTransform to edit its serialized layout here.", MessageType.None); return; }
        EditorGUI.BeginChangeCheck();
        Vector2 position = EditorGUILayout.Vector2Field("Anchored Position", rect.anchoredPosition);
        Vector2 size = EditorGUILayout.Vector2Field("Width / Height", rect.sizeDelta);
        Vector2 scale = EditorGUILayout.Vector2Field("Scale X / Y", new Vector2(rect.localScale.x, rect.localScale.y));
        float rotation = EditorGUILayout.FloatField("Rotation Z", rect.localEulerAngles.z);
        Vector2 anchorMin = EditorGUILayout.Vector2Field("Anchor Min", rect.anchorMin), anchorMax = EditorGUILayout.Vector2Field("Anchor Max", rect.anchorMax), pivot = EditorGUILayout.Vector2Field("Pivot", rect.pivot);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rect, "Edit authored UI transform"); rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = new Vector3(scale.x, scale.y, rect.localScale.z); rect.localRotation = Quaternion.Euler(0, 0, rotation); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; EditorUtility.SetDirty(rect);
            if (rect.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(rect.gameObject.scene);
        }
    }

    void DrawPrefabActions()
    {
        GameObject selected = Selection.activeGameObject; if (selected == null) return;
        EditorGUILayout.Space(); EditorGUILayout.LabelField("PREFAB WORKFLOW", EditorStyles.boldLabel);
        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(selected); UnityEngine.Object source = root != null ? PrefabUtility.GetCorrespondingObjectFromSource(root) : null;
        EditorGUILayout.LabelField("Source", source != null ? AssetDatabase.GetAssetPath(source) : "Not a prefab instance");
        EditorGUILayout.LabelField("Overrides", root != null && PrefabUtility.HasPrefabInstanceAnyOverrides(root, false) ? "YES" : "NO");
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(source == null)) if (GUILayout.Button("OPEN PREFAB")) AssetDatabase.OpenAsset(source);
        using (new EditorGUI.DisabledScope(source == null)) if (GUILayout.Button("PING PREFAB")) EditorGUIUtility.PingObject(source);
        using (new EditorGUI.DisabledScope(root == null)) if (GUILayout.Button("APPLY CURRENT INSTANCE OVERRIDES TO PREFAB")) PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);
        using (new EditorGUI.DisabledScope(root == null)) if (GUILayout.Button("REVERT INSTANCE FROM PREFAB")) PrefabUtility.RevertPrefabInstance(root, InteractionMode.UserAction);
        EditorGUILayout.EndHorizontal();
    }

    static void AssetRow(string path)
    {
        UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path); EditorGUILayout.BeginHorizontal(); EditorGUILayout.LabelField(path, asset != null ? EditorStyles.label : EditorStyles.miniLabel);
        using (new EditorGUI.DisabledScope(asset == null)) { if (GUILayout.Button("OPEN", GUILayout.Width(58))) AssetDatabase.OpenAsset(asset); if (GUILayout.Button("PING", GUILayout.Width(58))) EditorGUIUtility.PingObject(asset); }
        EditorGUILayout.EndHorizontal();
    }
    static void ObjectRow(UnityEngine.Object asset) { EditorGUILayout.BeginHorizontal(); EditorGUILayout.ObjectField(asset, asset != null ? asset.GetType() : typeof(UnityEngine.Object), false); if (GUILayout.Button("SELECT", GUILayout.Width(70))) Select(asset); EditorGUILayout.EndHorizontal(); }
    static void Select(UnityEngine.Object asset) { if (asset == null) return; Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }
    static void OpenAsset(string path) { UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path); if (asset != null) AssetDatabase.OpenAsset(asset); else EditorUtility.DisplayDialog("Missing asset", path + " has not been generated yet.", "OK"); }
    static void OpenScene(string path) { if (!File.Exists(path)) return; if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path); }
    static void ToggleLabels() { foreach (PassiveBranchBinding branch in Resources.FindObjectsOfTypeAll<PassiveBranchBinding>()) if (branch != null && !EditorUtility.IsPersistent(branch)) { Undo.RecordObject(branch, "Toggle passive authoring labels"); branch.ShowAuthoringLabels = !branch.ShowAuthoringLabels; EditorUtility.SetDirty(branch); } }
}
