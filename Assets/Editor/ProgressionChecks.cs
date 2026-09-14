// Developer map: Editor checks for encounter progression and role/death-reward guards.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
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
            Require(game.CurrentCombatLevel == 11 && zone.ZoneName == "Forest 2" && game.EncounterStage == 1, "Level10 -> level11 / Forest2");
            game.StartZone(20);
            ReachBoss(game);
            KillAndCheckDuplicate(game);
            Require(game.CurrentCombatLevel == 21 && zone.ZoneName == "Forest 3" && game.EncounterStage == 1, "Level20 -> level21 / Forest3");
            KillAndCheckDuplicate(game);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>().GetComponent<HealthComponent>();
            player.LoseLife(player.MaxLife + 1);
            game.RestartCurrentLevelAfterDeath();
            Require(game.CurrentCombatLevel == 21 && game.NormalKills == 0 && !game.BossActive && !CurrentHealth().IsBoss && player.CurrentLife == player.MaxLife, "Death/restart retains stage and resets progress");
            // Paused boss fixture for one visual inspection; exiting Play discards it.
            game.StartZone(11);
            ReachBoss(game);
            const string report = "PASS: live callback rejected; nine normals spawn stage10 boss; boss advances level exactly once; duplicate callbacks grant no extra loot or progress; forest boundaries10/11 and20/21; death/restart keeps level and resets encounter. Paused Forest2 level11 boss HUD fixture.";
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
        Require(game.NormalKillsRequired == 9, "Nine normals precede tenth-stage boss");
        for (int i = 0; i < game.NormalKillsRequired; i++)
        {
            Require(!CurrentHealth().IsBoss, "Normal before threshold");
            KillAndCheckDuplicate(game);
            Require(game.CurrentCombatLevel == level && game.NormalKills == i + 1, "Normal increments only kill count");
        }
        Require(game.BossActive && CurrentHealth().IsBoss && game.EncounterStage == 10, "Boss spawned at stage10");
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
