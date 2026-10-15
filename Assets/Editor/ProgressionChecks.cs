using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProgressionChecks
{
    [MenuItem("Black Cube/Play Checks/Verify Progression")]
    static void Verify()
    {
        if (!EditorApplication.isPlaying) return;
        var game = GameManager.Instance;
        var zone = UnityEngine.Object.FindFirstObjectByType<ZoneManager>();
        Require(game != null && zone != null && Inventory.Instance != null, "Scene references");
        Time.timeScale = 0;
        try
        {
            game.StartZone(1);
            int lootBefore = Inventory.Instance.Items.Count;
            game.OnEnemyKilled(CurrentHealth(), true);
            Require(game.NormalKills == 0 && Inventory.Instance.Items.Count == lootBefore, "Live callback rejected");
            ReachBoss(game);
            KillAndCheckDuplicate(game);
            Require(game.CurrentCombatLevel == 2 && zone.StageNumber == 2 && !game.BossActive, "Boss advances one stage");
            game.StartZone(10);
            ReachBoss(game);
            KillAndCheckDuplicate(game);
            Require(game.CurrentCombatLevel == 11 && zone.ZoneName == "Desert" && zone.StageNumber == 1, "Forest10 -> Desert1");
            game.StartZone(20);
            ReachBoss(game);
            KillAndCheckDuplicate(game);
            Require(game.CurrentCombatLevel == 21 && zone.ZoneName == "Tundra" && zone.StageNumber == 1, "Desert10 -> Tundra1");
            KillAndCheckDuplicate(game);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>().GetComponent<HealthComponent>();
            player.LoseLife(player.MaxLife + 1);
            game.RestartCurrentLevelAfterDeath();
            Require(game.CurrentCombatLevel == 21 && game.NormalKills == 0 && !game.BossActive && !CurrentHealth().IsBoss && player.CurrentLife == player.MaxLife, "Death/restart retains stage and resets progress");
            // Paused boss fixture for one visual inspection; exiting Play discards it.
            game.StartZone(11);
            ReachBoss(game);
            const string report = "PASS: live callback rejected; ten normals spawn boss without advancing; boss advances exactly once; duplicate callbacks grant no extra loot or progress; Forest10 -> Desert1; Desert10 -> Tundra1; death/restart keeps stage and resets count/boss. Paused Desert1 boss HUD fixture. Broader regressions deferred.";
            Directory.CreateDirectory("ReviewCaptures");
            File.WriteAllText("ReviewCaptures/progression-check.txt", report);
            Debug.Log(report);
        }
        catch (Exception error)
        {
            File.WriteAllText("ReviewCaptures/progression-check.txt", "FAIL: " + error.Message);
            throw;
        }
    }
    static HealthComponent CurrentHealth() => BattleManager.Instance.CurrentEnemyAI.GetComponent<HealthComponent>();
    static void ReachBoss(GameManager game)
    {
        int level = game.CurrentCombatLevel;
        for (int i = 0; i < game.NormalKillsRequired; i++)
        {
            Require(!CurrentHealth().IsBoss, "Normal before threshold");
            KillAndCheckDuplicate(game);
            Require(game.CurrentCombatLevel == level && game.NormalKills == i + 1, "Normal increments only kill count");
        }
        Require(game.BossActive && CurrentHealth().IsBoss, "Boss spawned at threshold");
    }
    static void KillAndCheckDuplicate(GameManager game)
    {
        var dead = CurrentHealth();
        bool boss = dead.IsBoss;
        dead.LoseLife(dead.MaxLife + 1);
        int level = game.CurrentCombatLevel, count = game.NormalKills, loot = Inventory.Instance.Items.Count;
        var replacement = CurrentHealth();
        game.OnEnemyKilled(dead, boss);
        Require(level == game.CurrentCombatLevel && count == game.NormalKills && loot == Inventory.Instance.Items.Count && replacement == CurrentHealth(), "Duplicate must not award loot, count or replace enemy");
    }
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
