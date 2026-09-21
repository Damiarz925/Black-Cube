using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PassiveBranchDataSO), true)]
public sealed class PassiveBranchAuthoringEditor : Editor
{
    const string ClipboardKey = "BlackCube.PassiveTierClipboard";
    SerializedProperty tiers, visualStyle;
    PassiveTreeDatabaseSO database;

    void OnEnable()
    {
        tiers = serializedObject.FindProperty("tiers"); visualStyle = serializedObject.FindProperty("visualStyle");
        database = AssetDatabase.LoadAssetAtPath<PassiveTreeDatabaseSO>(PassiveTreeAuthoringMigration.DatabasePath);
        Undo.undoRedoPerformed += Repaint;
    }
    void OnDisable() => Undo.undoRedoPerformed -= Repaint;

    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();
        PassiveBranchDataSO branch = (PassiveBranchDataSO)target;
        EditorGUILayout.LabelField(branch.RouteId.ToUpperInvariant(), EditorStyles.boldLabel);
        if (EditorApplication.isPlaying) EditorGUILayout.HelpBox("PLAY MODE — ASSET CHANGES ARE PERSISTENT", MessageType.Warning);
        DrawIdentity(branch);
        EditorGUILayout.PropertyField(visualStyle, new GUIContent("Visual Configuration"), true);
        EditorGUILayout.Space(); DrawToolbar(branch);
        for (int i = 0; i < tiers.arraySize; i++) DrawTier(branch, i, tiers.GetArrayElementAtIndex(i));
        if (serializedObject.ApplyModifiedProperties()) QueueSave(branch);
    }

    void DrawIdentity(PassiveBranchDataSO branch)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            if (branch is PassiveClassBranchSO)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("classId")); EditorGUILayout.PropertyField(serializedObject.FindProperty("signatureWeaponId"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("subclassAId")); EditorGUILayout.PropertyField(serializedObject.FindProperty("subclassBId"));
            }
            else { EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponId")); EditorGUILayout.PropertyField(serializedObject.FindProperty("owningClassId")); }
        }
    }

    void DrawToolbar(PassiveBranchDataSO branch)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("VALIDATE BRANCH")) ShowValidation(branch);
        if (GUILayout.Button("PING BRANCH SO")) EditorGUIUtility.PingObject(branch);
        if (GUILayout.Button("SELECT BRANCH VIEW")) SelectBranchView(branch.RouteId);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("COLLAPSE ALL TIERS")) SetAllFoldouts(branch, false);
        if (GUILayout.Button("EXPAND ALL TIERS")) SetAllFoldouts(branch, true);
        EditorGUILayout.EndHorizontal();
    }

    void DrawTier(PassiveBranchDataSO branch, int index, SerializedProperty tier)
    {
        string key = FoldoutKey(branch, index, "tier"); bool open = SessionState.GetBool(key, index == 0);
        open = EditorGUILayout.Foldout(open, $"NODE {index + 1} / TIER {index + 1}", true, EditorStyles.foldoutHeader); SessionState.SetBool(key, open);
        if (!open) return;
        EditorGUI.indentLevel++;
        DrawNestedNode(branch, index, tier.FindPropertyRelative("spine"), "SPINE", "spine");
        DrawSide(branch, index, tier.FindPropertyRelative("right"), "RIGHT CHOICES", "right", branch is PassiveClassBranchSO);
        DrawSide(branch, index, tier.FindPropertyRelative("left"), "LEFT CHOICES", "left", branch is PassiveClassBranchSO);
        DrawTierTools(branch, index);
        EditorGUI.indentLevel--; EditorGUILayout.Space(5);
    }

    void DrawSide(PassiveBranchDataSO branch, int tierIndex, SerializedProperty side, string label, string token, bool includeSubclass)
    {
        string key = FoldoutKey(branch, tierIndex, token); bool open = SessionState.GetBool(key, tierIndex == 0);
        open = EditorGUILayout.Foldout(open, label, true); SessionState.SetBool(key, open); if (!open) return;
        EditorGUI.indentLevel++;
        DrawNode(side.FindPropertyRelative("a"), "A"); DrawNode(side.FindPropertyRelative("b"), "B"); DrawNode(side.FindPropertyRelative("c"), "C");
        if (includeSubclass)
        {
            EditorGUILayout.LabelField("SUBCLASS SLOT", EditorStyles.boldLabel);
            DrawNode(side.FindPropertyRelative("subclassA"), "Subclass A"); DrawNode(side.FindPropertyRelative("subclassB"), "Subclass B");
        }
        EditorGUI.indentLevel--;
    }

    void DrawNestedNode(PassiveBranchDataSO branch, int tier, SerializedProperty node, string label, string token)
    {
        string key = FoldoutKey(branch, tier, token); bool open = SessionState.GetBool(key, tier == 0);
        open = EditorGUILayout.Foldout(open, label, true); SessionState.SetBool(key, open); if (!open) return;
        EditorGUI.indentLevel++; DrawNodeBody(node); EditorGUI.indentLevel--;
    }

    void DrawNode(SerializedProperty node, string label)
    {
        node.isExpanded = EditorGUILayout.Foldout(node.isExpanded, label, true); if (!node.isExpanded) return;
        EditorGUI.indentLevel++; DrawNodeBody(node); EditorGUI.indentLevel--;
    }

    void DrawNodeBody(SerializedProperty node)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(node.FindPropertyRelative("stableId"), new GUIContent("Stable Data ID"));
            EditorGUILayout.PropertyField(node.FindPropertyRelative("logicalSlotId"), new GUIContent("Logical Slot ID"));
        }
        EditorGUILayout.PropertyField(node.FindPropertyRelative("displayName")); EditorGUILayout.PropertyField(node.FindPropertyRelative("description"));
        EditorGUILayout.PropertyField(node.FindPropertyRelative("branch")); EditorGUILayout.PropertyField(node.FindPropertyRelative("size"));
        SerializedProperty effects = node.FindPropertyRelative("effects");
        for (int i = 0; i < effects.arraySize; i++) DrawEffect(effects.GetArrayElementAtIndex(i), i == 0 ? "Primary Effect" : $"Additional Effect {i}");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ ADD EFFECT")) effects.InsertArrayElementAtIndex(effects.arraySize);
        if (effects.arraySize > 0 && GUILayout.Button("CLEAR NODE EFFECT")) effects.arraySize = 0;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(node.FindPropertyRelative("iconMode"));
        if ((PassiveIconMode)node.FindPropertyRelative("iconMode").enumValueIndex == PassiveIconMode.Custom)
        {
            EditorGUILayout.PropertyField(node.FindPropertyRelative("iconOverride"));
            if (GUILayout.Button("RESET ICON TO AUTO")) { node.FindPropertyRelative("iconMode").enumValueIndex = (int)PassiveIconMode.Auto; node.FindPropertyRelative("iconOverride").objectReferenceValue = null; }
        }
        DrawPreview(node);
    }

    void DrawEffect(SerializedProperty effect, string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        SerializedProperty kind = effect.FindPropertyRelative("kind"), value = effect.FindPropertyRelative("value");
        EditorGUILayout.PropertyField(kind);
        if ((PassiveEffectKind)kind.enumValueIndex == PassiveEffectKind.Stat) EditorGUILayout.PropertyField(effect.FindPropertyRelative("stat"), new GUIContent("Effect"));
        else DrawMechanic(effect.FindPropertyRelative("mechanicId"));
        EditorGUILayout.PropertyField(value, new GUIContent("Value"));
    }

    void DrawMechanic(SerializedProperty mechanicId)
    {
        var catalog = database != null ? database.EffectCatalog : AssetDatabase.LoadAssetAtPath<PassiveEffectCatalogSO>(PassiveTreeAuthoringMigration.EffectCatalogPath);
        if (catalog == null || catalog.Mechanics.Count == 0) { EditorGUILayout.HelpBox("No Passive Effect Catalog is configured.", MessageType.Error); return; }
        string[] labels = catalog.Mechanics.Select(x => x.DisplayName).ToArray(); int current = 0;
        for (int i = 0; i < catalog.Mechanics.Count; i++) if (catalog.Mechanics[i].StableId == mechanicId.stringValue) { current = i; break; }
        int next = EditorGUILayout.Popup("Mechanic", current, labels); mechanicId.stringValue = catalog.Mechanics[next].StableId;
        EditorGUILayout.LabelField("Stable ID", mechanicId.stringValue, EditorStyles.miniLabel);
    }

    void DrawPreview(SerializedProperty node)
    {
        SerializedProperty effects = node.FindPropertyRelative("effects"); string preview = string.Empty;
        for (int i = 0; i < effects.arraySize; i++)
        {
            SerializedProperty effect = effects.GetArrayElementAtIndex(i); float value = effect.FindPropertyRelative("value").floatValue;
            string label = (PassiveEffectKind)effect.FindPropertyRelative("kind").enumValueIndex == PassiveEffectKind.Stat
                ? ObjectNames.NicifyVariableName(((StatTypes)effect.FindPropertyRelative("stat").enumValueIndex).ToString())
                : effect.FindPropertyRelative("mechanicId").stringValue.Split('.').Last();
            preview += (i > 0 ? "\n" : string.Empty) + $"+{value:0.##} {label}";
        }
        EditorGUILayout.HelpBox(string.IsNullOrEmpty(preview) ? "RESOLVED TOOLTIP PREVIEW\nNo effect" : "RESOLVED TOOLTIP PREVIEW\n" + preview, MessageType.None);
        Sprite icon = node.FindPropertyRelative("iconOverride").objectReferenceValue as Sprite;
        if (icon != null) GUILayout.Label(icon.texture, GUILayout.Width(64), GUILayout.Height(64));
    }

    void DrawTierTools(PassiveBranchDataSO branch, int index)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("COPY TIER")) SessionState.SetString(ClipboardKey, JsonUtility.ToJson(TierObject(branch, index)));
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(SessionState.GetString(ClipboardKey, string.Empty))))
            if (GUILayout.Button("PASTE TIER")) PasteTier(branch, index);
        using (new EditorGUI.DisabledScope(index + 1 >= branch.TierCount)) if (GUILayout.Button("DUPLICATE INTO NEXT")) DuplicateTier(branch, index, index + 1);
        EditorGUILayout.EndHorizontal();
    }

    object TierObject(PassiveBranchDataSO branch, int index) => branch is PassiveClassBranchSO c ? c.Tiers[index] : ((PassiveWeaponBranchSO)branch).Tiers[index];
    void PasteTier(PassiveBranchDataSO branch, int index) { Undo.RecordObject(branch, "Paste passive tier"); JsonUtility.FromJsonOverwrite(SessionState.GetString(ClipboardKey, string.Empty), TierObject(branch, index)); EditorUtility.SetDirty(branch); serializedObject.UpdateIfRequiredOrScript(); QueueSave(branch); }
    void DuplicateTier(PassiveBranchDataSO branch, int source, int destination) { SessionState.SetString(ClipboardKey, JsonUtility.ToJson(TierObject(branch, source))); PasteTier(branch, destination); }
    void SetAllFoldouts(PassiveBranchDataSO branch, bool value) { for (int i = 0; i < branch.TierCount; i++) SessionState.SetBool(FoldoutKey(branch, i, "tier"), value); Repaint(); }
    static string FoldoutKey(PassiveBranchDataSO branch, int tier, string part) => $"BlackCube.PassiveFoldout.{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(branch))}.{tier}.{part}";
    static void QueueSave(UnityEngine.Object asset) { EditorUtility.SetDirty(asset); EditorApplication.delayCall += () => { if (asset != null) AssetDatabase.SaveAssetIfDirty(asset); }; }
    static void ShowValidation(PassiveBranchDataSO branch) { var errors = UIAuthoringValidation.ValidatePassiveBranch(branch); EditorUtility.DisplayDialog(errors.Length == 0 ? "Branch Valid" : "Branch Validation", errors.Length == 0 ? "No authoring errors found." : string.Join("\n", errors), "OK"); }
    static void SelectBranchView(string routeId) { PassiveBranchBinding[] views = Resources.FindObjectsOfTypeAll<PassiveBranchBinding>(); foreach (var view in views) if (view != null && view.RouteId == routeId) { Selection.activeObject = view.gameObject; EditorGUIUtility.PingObject(view.gameObject); return; } EditorUtility.DisplayDialog("Branch View", "Open the Passive Tree prefab first, then use SELECT BRANCH VIEW.", "OK"); }
}
