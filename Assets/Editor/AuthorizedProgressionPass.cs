// Developer map: Opt-in multi-phase Play diagnostics for kill/boss progression, restart and UI transition.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Bounded, explicitly invoked diagnostics. No assets/scenes are saved.
[InitializeOnLoad]
public static class AuthorizedProgressionPass
{
    const string Key = "BlackCube.AuthorizedProgressionPass";
    const string Report = "ReviewCaptures/authorized-progression-pass.txt";
    static IEnumerator routine;
    static double ready;
    static int deaths, rewards, errors;
    static bool counting;
    static AuthorizedProgressionPass()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            int phase = SessionState.GetInt(Key, -1);
            if (phase < 0) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ready = EditorApplication.timeSinceStartup + 2;
                routine = Run(phase);
                Application.logMessageReceived += Log;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (phase >= 4) { SessionState.SetInt(Key, -1); return; }
                SessionState.SetInt(Key, phase + 1);
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            }
        };
        EditorApplication.update += Update;
    }
    [MenuItem("Black Cube/Play Checks/Authorized Progression Pass")]
    static void Begin()
    {
        if (EditorApplication.isPlaying) return;
        File.WriteAllText(Report, "Authorized bounded progression pass " + DateTime.Now.ToString("O") + "\n");
        SessionState.SetInt(Key, 0);
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Black Cube/Play Checks/Authorized UI Transition")]
    static void Transition()
    {
        if (!EditorApplication.isPlaying) return;
        BattleManager.Instance.enabled = false;
        for (int i = GameManager.Instance.NormalKills; i < 9; i++) Enemy.LoseLife(Enemy.MaxLife + 1);
        Enemy.LoseLife(Enemy.MaxLife + 1);
        Record("UI transition fixture: " + GameManager.Instance.CurrentCombatLevel + "/" + GameManager.Instance.NormalKills);
    }
    static HealthComponent Enemy => BattleManager.Instance.CurrentEnemyAI.GetComponent<HealthComponent>();
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    static void Record(string value) { File.AppendAllText(Report, value + "\n"); Debug.Log(value); }
    static void Check(bool result, string label) { Record((result ? "PASS " : "FAIL ") + label); }
    static void Log(string value, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) { errors++; File.AppendAllText(Report, "CONSOLE " + value + "\n"); }
        if (!counting) return;
        if (value.StartsWith("GameManager: Loot generated and added.")) rewards++;
        if (value.StartsWith("GameManager: Enemy killed."))
        {
            deaths++;
            File.AppendAllText(Report, "KILL " + deaths + " " + value + "\n");
            if (deaths >= 30) BattleManager.Instance.enabled = false;
        }
    }
    static void Update()
    {
        if (routine == null || EditorApplication.timeSinceStartup < ready) return;
        try { if (routine.MoveNext()) return; }
        catch (Exception e) { Record("FAIL fixture " + SessionState.GetInt(Key, -1) + ": " + e); }
        routine = null;
        counting = false;
        Time.timeScale = 1;
        if (SessionState.GetInt(Key, -1) != 4) EditorApplication.isPlaying = false;
    }
    static IEnumerator Run(int phase)
    {
        var game = GameManager.Instance;
        var battle = BattleManager.Instance;
        var player = Object.FindFirstObjectByType<PlayerController>();
        var hp = player.GetComponent<HealthComponent>();
        var stats = player.GetComponent<StatsComponent>();
        var zone = Object.FindFirstObjectByType<ZoneManager>();
        battle.enabled = false;
        Time.timeScale = 1;
        if (phase == 0)
        {
            stats.AddModifier(new StatModifier(StatTypes.Life, StatOp.Override, 1000000, typeof(AuthorizedProgressionPass)));
            stats.SetBaseStat(StatTypes.FlatPhys, 1000000);
            stats.SetBaseStat(StatTypes.PoisonChance, 100);
            hp.ReviveToFullLife();
            game.StartZone(1);
            yield return null;
            var poison = AssetDatabase.LoadAssetAtPath<StatusEffects>("Assets/Prefabs/Scriptable Objects/PoisonStatus.asset");
            var previous = Enemy;
            int observed = 0, inherited = 0, cadenceErrors = 0, actorErrors = 0;
            int lootStart = Inventory.Instance.Items.Count;
            bool dotInjected = false;
            counting = true;
            battle.enabled = true;
            Time.timeScale = 5;
            double end = EditorApplication.timeSinceStartup + 60;
            while (EditorApplication.timeSinceStartup < end)
            {
                if (Enemy != previous)
                {
                    observed++;
                    int stage = deaths / 10 + 1, count = deaths % 10;
                    if (game.CurrentCombatLevel != stage || game.NormalKills != count || Enemy.IsBoss != (count == 9)) cadenceErrors++;
                    if (Enemy.GetComponent<StatusController>().GetStatusSummaries().Count != 0) inherited++;
                    if (Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Count(e => e.GetComponent<HealthComponent>().CurrentLife > 0) != 1) actorErrors++;
                    previous = Enemy;
                }
                if (deaths >= 30) break;
                if (!dotInjected && deaths == 2)
                {
                    Enemy.GetComponent<StatusController>().ApplyStatus(poison, 1, 100000000, 1, stats, 1);
                    dotInjected = true;
                }
                yield return null;
            }
            battle.enabled = false;
            Time.timeScale = 1;
            counting = false;
            Check(deaths == 30 && observed == 30 && game.CurrentCombatLevel == 4 && game.NormalKills == 0 && cadenceErrors == 0 && actorErrors == 0 && inherited == 0,
                $"sustained: deaths={deaths}, observed={observed}, stage={game.CurrentCombatLevel}, count={game.NormalKills}, cadenceErrors={cadenceErrors}, actorErrors={actorErrors}, inherited={inherited}, lethalDOTInjected={dotInjected}");
            double settle = EditorApplication.timeSinceStartup + 3;
            while (EditorApplication.timeSinceStartup < settle) yield return null;
            int actors = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length;
            int statuses = Enemy.GetComponent<StatusController>().GetStatusSummaries().Count;
            int badges = Object.FindObjectsByType<StatusBadge>(FindObjectsSortMode.None).Count(b => b.transform.parent.name == "ENEMY EFFECTS");
            int lootDelta = Inventory.Instance.Items.Count - lootStart;
            Check(actors == 1 && statuses == 0 && badges == 0 && errors == 0 && rewards == 30 && lootDelta == 30,
                $"cleanup: enemies={actors}, currentStatuses={statuses}, enemyBadges={badges}, errors={errors}, rewards={rewards}, inventoryDelta={lootDelta}");
        }
        if (phase == 1)
        {
            game.StartZone(5);
            for (int i = 0; i < 9; i++) Enemy.LoseLife(Enemy.MaxLife + 1);
            bool boss = game.BossActive && Enemy.IsBoss;
            hp.LoseLife(hp.MaxLife + 1);
            bool dead = hp.CurrentLife == 0;
            game.RestartCurrentLevelAfterDeath();
            bool reset = game.CurrentCombatLevel == 5 && zone.StageNumber == 5 && zone.ZoneName == "Forest" && game.NormalKills == 0 && !game.BossActive && !Enemy.IsBoss && hp.CurrentLife == hp.MaxLife;
            Enemy.LoseLife(Enemy.MaxLife + 1);
            Check(boss && dead && reset && game.NormalKills == 1 && game.CurrentCombatLevel == 5,
                "boss death/restart: Forest5 boss -> dead -> Forest5 normal0 full revive -> normal1; global5 preserved");
        }
        if (phase == 2)
        {
            Set(zone, "forestBackgrounds", null); // Isolate the legacy theme fallback fixture.
            Set(zone, "zoneNames", new[] { "Alpha", "Beta" }); Set(zone, "stagesPerZone", 3);
            int[] levels = { 1, 3, 4, 6, 7 }, stages = { 1, 3, 1, 3, 1 };
            for (int i = 0; i < levels.Length; i++)
            {
                game.StartZone(levels[i]);
                Check(game.CurrentCombatLevel == levels[i] && zone.StageNumber == stages[i] && zone.ZoneName == (i < 2 ? "Alpha" : "Beta"),
                    $"configuration: global={game.CurrentCombatLevel}, {zone.ZoneName}{zone.StageNumber}");
            }
        }
        if (phase == 3)
        {
            Set(zone, "forestBackgrounds", null); // Isolate fallback from active paper art.
            Set(zone, "zoneNames", new string[0]);
            foreach (int divisor in new[] { 0, -3 })
            {
                Set(zone, "stagesPerZone", divisor); game.StartZone(7);
                Check(zone.StageNumber == 1 && zone.ZoneName == "Forest" && game.CurrentCombatLevel == 7, "invalid configuration: empty names; divisor=" + divisor + " -> Forest1 global7");
            }
            Set(zone, "zoneNames", new[] { " " });
            Check(zone.ZoneName == "Zone 1" && zone.StageNumber == 1, "invalid configuration: blank name -> Zone 1 / stage1");
        }
        if (phase == 4)
        {
            game.StartZone(1);
            Record("UI READY: fresh Play, combat disabled, timeScale1. Open inventory/stats; Authorized UI Transition; reopen panels; UI Death Fixture; native Restart. Exit Play without saving.");
        }
    }
}
