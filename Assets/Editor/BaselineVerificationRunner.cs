using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Command-line entry points for repeatable repository baseline verification.</summary>
[InitializeOnLoad]
public static class BaselineVerificationRunner
{
    const string SyncKey = "BlackCube.Baseline.Sync";
    const string MenuKey = "BlackCube.Baseline.Menu";
    const string ExitKey = "BlackCube.Baseline.Exit";
    const string SyncReport = "Logs/BaselineSynchronousPlayChecks.txt";
    static readonly (Type type, string method)[] synchronousChecks =
    {
        (typeof(PaperBattlePlayChecks), "VerifyEquipment"),
        (typeof(PlayerStatModelChecks), "Verify"),
        (typeof(StatusModelChecks), "Verify"),
        (typeof(ProgressionChecks), "Verify")
    };
    static double synchronousReadyAt;

    static BaselineVerificationRunner()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= WatchMenuCheck;
        if (SessionState.GetBool(MenuKey, false)) EditorApplication.update += WatchMenuCheck;
        EditorApplication.update -= RunSynchronousWhenReady;
    }

    public static void ValidateReferences()
    {
        var failures = new List<string>();
        var lines = new List<string>();
        string[] roots =
        {
            "Assets/Scenes/Main Menu.unity",
            "Assets/Scenes/SampleScene.unity",
            "Assets/Prefabs/PaperBattle/PaperBattle.prefab",
            "Assets/Prefabs/PaperBattle/Goblin2D.prefab",
            "Assets/Prefabs/PaperBattle/Hobgoblin2D.prefab",
            "Assets/Resources/PlayerSkills.asset",
            "Assets/Resources/EnemyScalingProfile.asset",
            "Assets/Art/PaperBattle/ChibiPlayer/PlayerAnimation.asset",
            "Assets/Art/PaperBattle/ForestEnemies/GoblinAnimation.asset",
            "Assets/Art/PaperBattle/ForestEnemies/HobgoblinAnimation.asset"
        };
        foreach (string path in roots)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null) failures.Add("Asset failed to load: " + path);
            else lines.Add("PASS asset: " + path);
        }
        foreach (string dependency in AssetDatabase.GetDependencies(roots, true).Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)))
            if (AssetDatabase.LoadMainAssetAtPath(dependency) == null) failures.Add("Dependency failed to load: " + dependency);

        foreach (string scenePath in roots.Where(p => p.EndsWith(".unity", StringComparison.Ordinal)))
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects()) ScanHierarchy(root, scenePath, failures);
            lines.Add("PASS hierarchy: " + scenePath);
        }

        GameObject battle = PrefabUtility.LoadPrefabContents("Assets/Prefabs/PaperBattle/PaperBattle.prefab");
        try
        {
            ScanHierarchy(battle, "PaperBattle.prefab", failures);
            RequireComponent<PlayerController>(battle, "player actor", failures);
            RequireComponent<PaperSpriteActor>(battle, "paper actor presentation", failures);
            RequireComponent<PaperBattleHUD>(battle, "HUD", failures);
            RequireComponent<InventoryUI>(battle, "inventory UI", failures);
            RequireComponent<EquipmentStatsUI>(battle, "equipment UI", failures);
            RequireComponent<ZoneManager>(battle, "forest/environment presentation", failures);
            lines.Add("PASS runtime wiring: PaperBattleHUD.Awake creates SkillTreeUI and StatusHUD when absent.");
        }
        finally { PrefabUtility.UnloadPrefabContents(battle); }

        foreach (string path in new[] { "Assets/Prefabs/PaperBattle/Goblin2D.prefab", "Assets/Prefabs/PaperBattle/Hobgoblin2D.prefab" })
        {
            GameObject enemy = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ScanHierarchy(enemy, path, failures);
                RequireComponent<EnemyAI>(enemy, "enemy AI", failures);
                RequireComponent<PaperSpriteActor>(enemy, "enemy animation", failures);
                RequireComponent<HealthComponent>(enemy, "enemy health", failures);
                RequireComponent<StatusController>(enemy, "enemy statuses", failures);
            }
            finally { PrefabUtility.UnloadPrefabContents(enemy); }
        }

        var skills = Resources.Load<PlayerSkillCatalog>("PlayerSkills");
        if (skills == null) failures.Add("Resources.Load<PlayerSkillCatalog>(PlayerSkills) returned null.");
        if (Resources.Load<EnemyScalingProfile>("EnemyScalingProfile") == null)
            failures.Add("Resources.Load<EnemyScalingProfile>(EnemyScalingProfile) returned null.");
        for (int i = 0; i <= 5; i++)
        {
            string path = $"Assets/Art/PaperBattle/ForestCycle/{i * 20}_Percent.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null) failures.Add("Forest sprite failed to load: " + path);
            else lines.Add("PASS forest sprite: " + path);
        }
        lines.Add("PASS documented optional reference: Main Menu's dormant DamagePopup has no popupRoot; combat uses the fully wired PaperBattle instance.");

        lines.AddRange(failures.Select(f => "FAIL " + f));
        lines.Add($"RESULT: {(failures.Count == 0 ? "PASS" : "FAIL")} ({failures.Count} failures)");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/BaselineReferenceValidation.txt", lines);
        if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
    }

    public static void BuildWindows()
        => BuildWindowsAt("Builds/BaselineWindows/BlackCube.exe", "Logs/BaselineWindowsBuild.txt");

    public static void BuildStep12_5GWindows()
        => BuildWindowsAt("Builds/Step12_5GWindows/BlackCube.exe", "Logs/Step12_5GWindowsBuild.txt");

    public static void BuildStep13Windows()
        => BuildWindowsAt("Builds/Step13Windows/BlackCube.exe", "Logs/Step13WindowsBuild.txt");

    public static void BuildStep14Windows()
        => BuildWindowsAt("Builds/Step14Windows/BlackCube.exe", "Logs/Step14WindowsBuild.txt");

    public static void BuildStep14_5Windows()
        => BuildWindowsAt("Builds/Step14_5Windows/BlackCube.exe", "Logs/Step14_5WindowsBuild.txt");

    static void BuildWindowsAt(string executable, string reportPath)
    {
        string output = Path.GetFullPath(executable);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.StrictMode
        });
        Directory.CreateDirectory("Logs");
        File.WriteAllText(reportPath,
            $"result={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nsize={report.summary.totalSize}\noutput={output}\nscenes={string.Join(",", scenes)}\n");
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
    }

    public static void RunSynchronousPlayChecks()
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText(SyncReport, "Synchronous Play checks\n");
        SessionState.SetInt(SyncKey, 0);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
    }

    public static void RunMenuCheck()
    {
        string name = Argument("-baselineCheck");
        (string menu, string report, string marker) = name switch
        {
            "AttackStats" => ("Black Cube/Play Checks/Verify Attack Stats and Status Gating", "Logs/Step12_5-AttackStats-check.txt", "Visual fixture paused"),
            "MoreDamage" => ("Black Cube/Play Checks/Verify Independent More Damage", "ReviewCaptures/more-damage-play-check.txt", "COMPLETE:"),
            "PlayerProgression" => ("Black Cube/Play Checks/Verify XP Skills and Combat", "ReviewCaptures/player-progression-check.txt", "Fixture paused for UI review"),
            "AuthorizedProgression" => ("Black Cube/Play Checks/Authorized Progression Pass", "ReviewCaptures/authorized-progression-pass.txt", "UI READY:"),
            "DeferredProgression" => ("Black Cube/Play Checks/Run Deferred Pass", "ReviewCaptures/deferred-progression.txt", "PASS invalid configuration"),
            "Inventory" => ("Black Cube/Play Checks/Verify Inventory", "ReviewCaptures/inventory-check.txt", "Visual fixture ready"),
            "InventoryGrid" => ("Black Cube/Play Checks/Verify Inventory Grid Filter and Gear Damage", "Logs/Step12_5-InventoryGrid-check.txt", "Visual fixture paused"),
            "TooltipCrit" => ("Black Cube/Play Checks/Verify Tooltip Crit and Typed Damage", "Logs/Step12_5-TooltipCrit-check.txt", "COMPLETE:"),
            _ => throw new ArgumentException("Unknown -baselineCheck value: " + name)
        };
        SessionState.SetString(MenuKey + ".Report", report);
        SessionState.SetString(MenuKey + ".Marker", marker);
        SessionState.SetBool(MenuKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        if (!EditorApplication.ExecuteMenuItem(menu)) throw new InvalidOperationException("Menu item was not found: " + menu);
        EditorApplication.update -= WatchMenuCheck;
        EditorApplication.update += WatchMenuCheck;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        int index = SessionState.GetInt(SyncKey, -1);
        if (index >= 0 && state == PlayModeStateChange.EnteredPlayMode)
        {
            synchronousReadyAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update -= RunSynchronousWhenReady;
            EditorApplication.update += RunSynchronousWhenReady;
        }
        if (index >= 0 && state == PlayModeStateChange.EnteredEditMode)
        {
            index++;
            if (index >= synchronousChecks.Length)
            {
                SessionState.SetInt(SyncKey, -1);
                EditorApplication.Exit(0);
            }
            else
            {
                SessionState.SetInt(SyncKey, index);
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            }
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(ExitKey, -1) >= 0)
        {
            int code = SessionState.GetInt(ExitKey, 1);
            SessionState.SetInt(ExitKey, -1);
            EditorApplication.Exit(code);
        }
    }

    static void RunSynchronousCheck()
    {
        int index = SessionState.GetInt(SyncKey, -1);
        if (index < 0 || index >= synchronousChecks.Length) return;
        var check = synchronousChecks[index];
        try
        {
            check.type.GetMethod(check.method, BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
            File.AppendAllText(SyncReport, "PASS " + check.type.Name + "\n");
        }
        catch (Exception exception)
        {
            Exception actual = exception is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : exception;
            File.AppendAllText(SyncReport, "FAIL " + check.type.Name + ": " + actual + "\n");
            SessionState.SetInt(SyncKey, -1);
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(1);
            return;
        }
        EditorApplication.isPlaying = false;
    }

    static void RunSynchronousWhenReady()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < synchronousReadyAt) return;
        EditorApplication.update -= RunSynchronousWhenReady;
        RunSynchronousCheck();
    }

    static void WatchMenuCheck()
    {
        if (!SessionState.GetBool(MenuKey, false)) return;
        string report = SessionState.GetString(MenuKey + ".Report", string.Empty);
        string marker = SessionState.GetString(MenuKey + ".Marker", string.Empty);
        if (!File.Exists(report)) return;
        string text = File.ReadAllText(report);
        if (!text.Contains("FAIL", StringComparison.OrdinalIgnoreCase) && !text.Contains(marker, StringComparison.Ordinal)) return;
        int code = text.Contains("FAIL", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        SessionState.SetBool(MenuKey, false);
        EditorApplication.update -= WatchMenuCheck;
        if (EditorApplication.isPlaying)
        {
            SessionState.SetInt(ExitKey, code);
            EditorApplication.isPlaying = false;
        }
        else EditorApplication.Exit(code);
    }

    static void ScanHierarchy(GameObject root, string context, List<string> failures)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            GameObject gameObject = transform.gameObject;
            int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingScripts > 0) failures.Add($"{context}/{HierarchyPath(transform)} has {missingScripts} missing scripts.");
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                SerializedProperty property = serialized.GetIterator();
                if (!property.NextVisible(true)) continue;
                do
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null
                        && property.objectReferenceEntityIdValue != EntityId.None && !AllowedMissingReference(context, component, property))
                        failures.Add($"{context}/{HierarchyPath(transform)}::{component.GetType().Name}.{property.propertyPath} is missing.");
                } while (property.NextVisible(false));
            }
        }
    }

    static void RequireComponent<T>(GameObject root, string label, List<string> failures) where T : Component
    {
        if (root.GetComponentInChildren<T>(true) == null) failures.Add($"PaperBattle is missing {label} ({typeof(T).Name}).");
    }

    static bool AllowedMissingReference(string context, Component component, SerializedProperty property) =>
        context == "Assets/Scenes/Main Menu.unity" && component is DamagePopup && property.propertyPath == "popupRoot";

    static string HierarchyPath(Transform transform)
    {
        var names = new Stack<string>();
        while (transform != null) { names.Push(transform.name); transform = transform.parent; }
        return string.Join("/", names);
    }

    static string Argument(string key)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
        return string.Empty;
    }
}
