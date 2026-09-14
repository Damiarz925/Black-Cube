// Developer map: Opt-in multi-phase progression and UI fixtures, including F6/F7/F8 menu shortcuts.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// All mutations are Play-only; each fixture gets a fresh scene/domain.
[InitializeOnLoad]
public static class DeferredProgressionChecks
{
    const string Key = "BlackCube.DeferredPhase";
    const string Report = "ReviewCaptures/deferred-progression.txt";
    static IEnumerator routine;
    static double deadline;
    static int errors;
    static DeferredProgressionChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            int phase = SessionState.GetInt(Key, -1);
            if (phase < 0) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                deadline = EditorApplication.timeSinceStartup + 2;
                routine = Run(phase);
                Application.logMessageReceived += Log;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (phase >= 3) { SessionState.SetInt(Key, -1); return; }
                SessionState.SetInt(Key, phase + 1);
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            }
        };
        EditorApplication.update += Update;
    }
    [MenuItem("Black Cube/Play Checks/Run Deferred Pass _F8")]
    static void Begin()
    {
        if (EditorApplication.isPlaying) return;
        File.WriteAllText(Report, "Deferred progression bounded pass\n");
        SessionState.SetInt(Key, 0);
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Black Cube/Play Checks/UI Transition Fixture _F7")]
    static void UITransition()
    {
        if (!EditorApplication.isPlaying) return;
        Time.timeScale = 0;
        var game = GameManager.Instance;
        for (int i = game.NormalKills; i < 9; i++) Enemy.LoseLife(Enemy.MaxLife + 1);
        Enemy.LoseLife(Enemy.MaxLife + 1);
    }
    [MenuItem("Black Cube/Play Checks/UI Death Fixture _F6")]
    static void UIDeath()
    {
        if (!EditorApplication.isPlaying) return;
        var hp = Object.FindFirstObjectByType<PlayerController>().GetComponent<HealthComponent>();
        hp.LoseLife(hp.MaxLife + 1);
    }
    static void Log(string message, string trace, LogType type)
    { if (type == LogType.Error || type == LogType.Exception) { errors++; File.AppendAllText(Report, "CONSOLE: " + message + "\n"); } }
    static void Update()
    {
        if (routine == null || EditorApplication.timeSinceStartup < deadline) return;
        try { if (routine.MoveNext()) return; }
        catch (Exception e) { Record("FAIL fixture " + SessionState.GetInt(Key, -1) + ": " + e); }
        routine = null;
        Time.timeScale = 1;
        EditorApplication.isPlaying = false;
    }
    static void Record(string message) { File.AppendAllText(Report, message + "\n"); Debug.Log(message); }
    static void Require(bool ok, string label) { if (!ok) throw new Exception(label); }
    static HealthComponent Enemy => BattleManager.Instance.CurrentEnemyAI.GetComponent<HealthComponent>();
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    static IEnumerator Run(int phase)
    {
        var game = GameManager.Instance;
        var player = Object.FindFirstObjectByType<PlayerController>();
        var hp = player.GetComponent<HealthComponent>();
        var stats = player.GetComponent<StatsComponent>();
        var zone = Object.FindFirstObjectByType<ZoneManager>();
        Time.timeScale = 0;
        if (phase == 0)
        {
            stats.AddModifier(new StatModifier(StatTypes.Life, StatOp.Override, 1000000, typeof(DeferredProgressionChecks)));
            hp.ReviveToFullLife();
            game.StartZone(1);
            yield return null;
            var poison = AssetDatabase.LoadAssetAtPath<StatusEffects>("Assets/Prefabs/Scriptable Objects/PoisonStatus.asset");
            stats.SetBaseStat(StatTypes.PoisonChance, 100);
            var previous = Enemy;
            int level = 1, count = 0, kills = 0, inherited = 0;
            int loot = Inventory.Instance.Items.Count;
            bool dot = false;
            UnityEngine.Random.InitState(20260913);
            double end = EditorApplication.timeSinceStartup + 100;
            Time.timeScale = 20;
            while (kills < 30 && EditorApplication.timeSinceStartup < end)
            {
                if (Enemy != previous)
                {
                    kills++;
                    if (previous != null && previous.IsBoss) { level++; count = 0; }
                    else if (count == 9) { level++; count = 0; }
                    else count++;
                    Require(game.CurrentCombatLevel == level && game.NormalKills == count, "Skipped/double stage or cadence");
                    Require(Enemy.IsBoss == (count == 9), "Boss cadence");
                    if (Enemy.GetComponent<StatusController>().GetStatusSummaries().Count != 0) inherited++;
                    previous = Enemy;
                }
                if (!dot && game.NormalKills == 2)
                {
                    Enemy.GetComponent<StatusController>().ApplyStatus(poison, 1, 100000, 1, stats, 1);
                    dot = true;
                }
                yield return null;
            }
            Time.timeScale = 0;
            Require(game.CurrentCombatLevel == 4, "Three-stage timeout");
            Record($"{(inherited == 0 ? "PASS" : "FAIL")} sustained: 3 stages, {kills} kills, correct cadence; replacement status inheritance observed {inherited} times; lethal DOT injected={dot}; loot delta={Inventory.Instance.Items.Count-loot}.");
            Time.timeScale = 1;
            double settle = EditorApplication.timeSinceStartup + 1;
            while (EditorApplication.timeSinceStartup < settle) yield return null;
            Time.timeScale = 0;
            int actors = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length;
            int lootDelta=Inventory.Instance.Items.Count-loot;
            Record($"{(actors == 1 && errors == 0 && inherited == 0 && lootDelta > 0 && lootDelta < kills ? "PASS" : "FAIL")} cleanup: active enemies={actors}; new errors={errors}; probabilistic equipment rewards={lootDelta} for {kills} kills; inherited statuses={inherited}.");
        }
        if (phase == 1)
        {
            game.StartZone(5);
            for (int i = 0; i < 9; i++) Enemy.LoseLife(Enemy.MaxLife + 1);
            Require(game.BossActive, "Boss fixture");
            hp.LoseLife(hp.MaxLife + 1);
            game.RestartCurrentLevelAfterDeath();
            Require(game.CurrentCombatLevel == 5 && zone.StageNumber == 5 && zone.ZoneName == "Forest 1" && game.NormalKills == 0 && !game.BossActive && !Enemy.IsBoss && hp.CurrentLife == hp.MaxLife, "Boss restart state");
            Enemy.LoseLife(Enemy.MaxLife + 1);
            Require(game.NormalKills == 1 && game.CurrentCombatLevel == 5, "First normal after restart");
            Record("PASS boss death/restart: Forest5 boss -> death -> restart at Forest5 normal0 -> kill -> normal1; full revive.");
        }
        if (phase == 2)
        {
            Set(zone, "forestBackgrounds", null); // Isolate the legacy theme fallback fixture.
            Set(zone, "zoneNames", new[] { "Alpha", "Beta" }); Set(zone, "stagesPerZone", 3);
            int[] levels = { 1, 3, 4, 6, 7 }, stages = { 1, 3, 1, 3, 1 };
            for (int i = 0; i < levels.Length; i++)
            {
                game.StartZone(levels[i]);
                Require(game.CurrentCombatLevel == levels[i] && zone.StageNumber == stages[i] && zone.ZoneName == (i < 2 ? "Alpha" : "Beta"), "Custom mapping " + levels[i]);
            }
            Record("PASS configuration: Alpha/Beta,3; levels1,3,4,6,7 map to Alpha1,Alpha3,Beta1,Beta3,Beta1; combat levels retained.");
        }
        if (phase == 3)
        {
            Set(zone, "forestBackgrounds", null); // Isolate fallback from active paper art.
            Set(zone, "zoneNames", new string[0]);
            foreach (int divisor in new[] { 0, -3 })
            {
                Set(zone, "stagesPerZone", divisor); game.StartZone(7);
                Require(zone.StageNumber == 1 && zone.ZoneName == "Forest", "Empty fallback/clamp");
            }
            Set(zone, "zoneNames", new[] { " " });
            Require(zone.ZoneName == "Zone 1" && zone.StageNumber == 1, "Blank fallback");
            Record("PASS invalid configuration: empty -> Forest; blank -> Zone1; stages0/-3 clamp to1. Play exit restores defaults.");
        }
    }
}
