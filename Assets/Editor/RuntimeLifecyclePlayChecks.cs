// Step 4 real-scene transition soak for persistent ownership, run reset and rebinding.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RuntimeLifecyclePlayChecks
{
    const string PendingKey = "BlackCube.RuntimeLifecycle.Pending";
    const string Report = "Logs/RuntimeLifecyclePlayChecks.txt";
    static readonly Type[] PersistentTypes = { typeof(GameManager), typeof(Inventory), typeof(CurrencyInventory),
        typeof(EquipmentManager), typeof(ModManager), typeof(GearStatLists), typeof(RelicInventory), typeof(RebirthManager) };
    static int entry, errors;
    static int previousWeaponInstanceId;
    static bool completed, hadSave;
    static string savedValue;
    static double readyAt;

    static RuntimeLifecyclePlayChecks()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        if (SessionState.GetBool(PendingKey, false))
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }
    }

    [MenuItem("Black Cube/Play Checks/Verify Runtime Lifecycle Soak")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, string.Empty);
        hadSave = PlayerPrefs.HasKey(GamePersistence.SaveKey);
        savedValue = hadSave ? PlayerPrefs.GetString(GamePersistence.SaveKey) : null;
        PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
        PlayerPrefs.Save();
        GamePersistence.RequestNewGame();
        entry = errors = previousWeaponInstanceId = 0;
        completed = false;
        readyAt = 0;
        Application.logMessageReceived += OnLog;
        SessionState.SetBool(PendingKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity");
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(PendingKey, false)) return;
        if (!EditorApplication.isPlaying)
        {
            if (!completed) return;
            RestoreSave();
            Application.logMessageReceived -= OnLog;
            SessionState.EraseBool(PendingKey);
            EditorApplication.Exit(errors == 0 ? 0 : 1);
            return;
        }
        if (completed || EditorApplication.timeSinceStartup < readyAt) return;
        try { RunStep(); }
        catch (Exception exception) { Write("FAIL: " + exception); errors++; Complete(); }
    }

    static void RunStep()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == GameSceneNames.MainMenu)
        {
            // The cold Main Menu intentionally has no application services; the
            // gameplay prefab creates the first authoritative set.
            if (entry > 0) ValidatePersistentServices();
            else Require(GameManager.Instance == null && Inventory.Instance == null,
                "Cold Main Menu unexpectedly created a partial persistent service set");
            Require(BattleManager.Instance == null, "Main Menu retained BattleManager.Instance");
            Require(DamagePopup.Instance == null || DamagePopup.Instance.gameObject.scene == scene,
                "Main Menu retained a DamagePopup from another scene");
            if (entry > 0) ValidateReleasedSceneReferences();
            if (entry >= 6)
            {
                Require(GameManager.Instance.GameplayInitializationCount == 6,
                    $"Expected 6 gameplay initializations, observed {GameManager.Instance.GameplayInitializationCount}");
                Write("PASS: six gameplay entries (five complete return/re-entry cycles) produced one initialization each.");
                Complete();
                return;
            }

            bool load = entry == 2 || entry == 4;
            if (load)
            {
                Require(GamePersistence.HasSave, "Load soak entry has no save fixture");
                ButtonNamed("Load Game Button").onClick.Invoke();
            }
            else
            {
                if (entry == 3) { Require(GamePersistence.RequestLoad(), "Could not create stale request fixture"); }
                ButtonNamed("Start Game Button").onClick.Invoke();
            }
            Delay();
            return;
        }

        if (scene.name != GameSceneNames.Gameplay || GameManager.Instance == null || BattleManager.Instance == null) return;
        ValidatePersistentServices();
        ValidateGameplayReferences(scene);
        Require(!GamePersistence.LoadRequested, "Gameplay retained a one-shot Load Game request");
        bool loaded = entry == 2 || entry == 4;
        int expectedCurrency = loaded ? 23 : 0;
        Require(CurrencyInventory.Instance.Count(CraftingCurrencyType.MagicToRare) == expectedCurrency,
            $"Entry {entry} currency did not match {(loaded ? "loaded" : "fresh-run")} baseline");
        Require(GameManager.Instance.CurrentCombatLevel == 1 && GameManager.Instance.NormalKills == 0,
            "Gameplay entry did not start at combat level 1 with zero kills");
        Require(GameManager.Instance.GetComponent<PlayerProgression>().Level == 1,
            "Gameplay entry did not reset run-local player progression");
        Gear weapon = EquipmentManager.Instance.GetEquipped(LootManager.GearType.Weapons);
        Require(Inventory.Instance.Items.Count == 0 && EquipmentManager.Instance.EquippedItems.Count == 1 && weapon != null,
            $"Gameplay entry expected only its starter/loaded weapon (inventory={Inventory.Instance.Items.Count}, equipped={EquipmentManager.Instance.EquippedItems.Count})");
        if (entry > 0) Require(RuntimeHelpers.GetHashCode(weapon) != previousWeaponInstanceId,
            "Gameplay entry retained the prior scene's run-owned weapon object");
        previousWeaponInstanceId = RuntimeHelpers.GetHashCode(weapon);
        Write($"PASS: gameplay entry {entry + 1}/6 bound the current scene with one service set and the expected {(loaded ? "load" : "New Game")} state.");

        if (entry == 0)
        {
            CurrencyInventory.Instance.Add(CraftingCurrencyType.MagicToRare, 23);
            GamePersistence.Save();
            CurrencyInventory.Instance.Restore(Array.Empty<CurrencyStackData>());
        }
        entry++;
        UnityEngine.Object.FindFirstObjectByType<DeathMenuUI>(FindObjectsInactive.Include).OnReturnToMainMenuClicked();
        Delay();
    }

    static void ValidatePersistentServices()
    {
        foreach (Type type in PersistentTypes)
        {
            int count = UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Require(count == 1, $"Expected one authoritative {type.Name}, found {count}");
        }
        foreach (Component service in new Component[] { GameManager.Instance, Inventory.Instance, CurrencyInventory.Instance,
                     EquipmentManager.Instance, ModManager.Instance, GearStatLists.Instance, RelicInventory.Instance, RebirthManager.Instance })
        {
            Require(service != null && service.transform.root == service.transform, $"{service?.GetType().Name ?? "service"} is not a persistent root");
            Require(service.gameObject.scene.name == "DontDestroyOnLoad", $"{service.GetType().Name} is not in the persistent scene");
        }
    }

    static void ValidateReleasedSceneReferences()
    {
        Require(Field<Component>(GameManager.Instance, "zoneManager") == null, "GameManager retained ZoneManager in Main Menu");
        Require(Field<Component>(GameManager.Instance, "lootManager") == null, "GameManager retained LootManager in Main Menu");
        Require(Field<Component>(GameManager.Instance, "deathMenuUI") == null, "GameManager retained DeathMenuUI in Main Menu");
        Require(Field<Component>(EquipmentManager.Instance, "playerController") == null, "EquipmentManager retained PlayerController in Main Menu");
        Require(Field<Component>(EquipmentManager.Instance, "playerStats") == null, "EquipmentManager retained StatsComponent in Main Menu");
    }

    static void ValidateGameplayReferences(Scene scene)
    {
        Require(BattleManager.Instance.gameObject.scene == scene, "BattleManager does not belong to current gameplay scene");
        Require(UnityEngine.Object.FindObjectsByType<BattleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
            "Gameplay has duplicate BattleManagers");
        foreach (string field in new[] { "zoneManager", "lootManager", "deathMenuUI" })
            Require(Field<Component>(GameManager.Instance, field)?.gameObject.scene == scene, $"GameManager.{field} is not rebound to current scene");
        Require(Field<Component>(EquipmentManager.Instance, "playerController")?.gameObject.scene == scene,
            "EquipmentManager PlayerController is not rebound to current scene");
        Require(Field<Component>(EquipmentManager.Instance, "playerStats")?.gameObject.scene == scene,
            "EquipmentManager StatsComponent is not rebound to current scene");
    }

    static T Field<T>(object target, string name) where T : class =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as T;
    static Button ButtonNamed(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(b => b.name == name);
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void Write(string message) { File.AppendAllText(Report, message + Environment.NewLine); Debug.Log(message); }
    static void Delay() => readyAt = EditorApplication.timeSinceStartup + .9;
    static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
    static void Complete() { completed = true; EditorApplication.delayCall += () => EditorApplication.isPlaying = false; }
    static void RestoreSave()
    {
        GamePersistence.RequestNewGame();
        if (hadSave) PlayerPrefs.SetString(GamePersistence.SaveKey, savedValue); else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
        PlayerPrefs.Save();
    }
}
