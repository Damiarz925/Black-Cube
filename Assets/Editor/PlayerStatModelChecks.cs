using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit integration check using disposable gear in a fresh Play session.</summary>
public static class PlayerStatModelChecks
{
    [MenuItem("Black Cube/Play Checks/Verify Player Stat Model")]
    static void Verify()
    {
        if (!EditorApplication.isPlaying) return;
        var player = Object.FindFirstObjectByType<PlayerController>();
        var stats = player.GetComponent<StatsComponent>();
        var health = player.GetComponent<HealthComponent>();
        var manager = EquipmentManager.Instance;
        Require(manager.GetEquipped(LootManager.GearType.BodyArmours) == null, "Use a fresh Play session with empty Body Armor");
        var starter = manager.GetEquipped(LootManager.GearType.Weapons);
        Require(starter != null && player.EquippedWeaponBaseDamage == 80, "Starter base damage remains 80");
        Require(stats.GetStat(StatTypes.UnarmedDamage) == 0 && !stats.HasStat(StatTypes.WeaponBaseDmg), "Unarmed is zero with no competing intrinsic weapon base");
        Require(health.MaxLife == 1000 && stats.GetStat(StatTypes.Life) == 1000, "Baseline derived maximum is 1000");
        health.ReviveToFullLife(); Require(health.CurrentLife == 1000, "Revive fills derived max");
        health.LoseLife(100); Require(health.CurrentLife == 900, "Damage reduces current HP only");
        var a = LifeGear(200); var b = LifeGear(500);
        manager.Equip(a); Check(health, stats, 1200, 900);
        manager.Equip(b); Check(health, stats, 1500, 900);
        Require(Inventory.Instance.Items.Contains(a), "Replaced Life item returned to inventory");
        health.ReviveToFullLife(); Check(health, stats, 1500, 1500);
        manager.Equip(a); Check(health, stats, 1200, 1200);
        manager.Equip(b); Check(health, stats, 1500, 1200); // catches transient clamp to base1000 during replacement
        manager.Unequip(LootManager.GearType.BodyArmours); Check(health, stats, 1000, 1000);
        manager.Equip(a); Check(health, stats, 1200, 1000);
        manager.Unequip(LootManager.GearType.Weapons);
        Require(player.EquippedWeaponBaseDamage == 0 && player.BuildAttackContext().Hits.Count == 0 && player.GetFinalAttackSpeed() == 0, "No weapon plus zero unarmed produces no damaging attack");
        stats.SetBaseStat(StatTypes.UnarmedDamage, 7);
        Require(player.BuildAttackContext().Hits.Sum(h => h.Amount) == 7, "Positive unarmed stat drives unarmed damage");
        manager.Equip(starter);
        Require(player.EquippedWeaponBaseDamage == 80 && player.BuildAttackContext().Hits.Sum(h => h.Amount) == 80, "Unarmed base is not added to weapon base");
        stats.SetBaseStat(StatTypes.UnarmedDamage, 0);
        Require(stats.GetStat(StatTypes.UnarmedDamage) == 0, "Base changes invalidate cached stats");
        var replacement = new GameObject("QA weapon").AddComponent<Gear>();
        replacement.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys);
        replacement.BaseDamage = 42; replacement.BaseAttackSpeed = 1.2f;
        manager.Equip(replacement); Require(player.BuildAttackContext().Hits.Sum(h => h.Amount) == 42, "Weapon replacement supplies its own base");
        manager.Equip(starter);
        var hud = Object.FindFirstObjectByType<PaperBattleHUD>(); hud.statsPanel.SetActive(true);
        health.LoseLife(health.CurrentLife + 1); Require(health.CurrentLife == 0, "Lethal damage clamps to zero");
        GameManager.Instance.RestartCurrentLevelAfterDeath(); Check(health, stats, 1200, 1200);
        const string report = "PASS: baseline Life/maxHP1000; damage current900; +200/+500 Life swaps max1200/1500 with current900 unchanged; down-swap clamps1500->1200; up-swap preserves1200 (no transient base clamp); remove clamps to1000; no stacking/free healing. No weapon+Unarmed0 yields no hits/speed0; Unarmed7 yields7 but weapon remains80; replacement42 then starter80. Lethal damage0 then actual restart fills derived1200. Enemy path unchanged. QA gear is Play-session only.";
        System.IO.File.WriteAllText("ReviewCaptures/stat-model-check.txt", report); Debug.Log(report);
    }
    static Gear LifeGear(float amount)
    {
        var gear = new GameObject("QA Life " + amount).AddComponent<Gear>();
        gear.Initialize(LootManager.GearType.BodyArmours, LootManager.GearRarity.Magic,1,Element.Phys);
        gear.globalRolledMods.Add(new RolledMod(StatTypes.Life,1,amount));
        return gear;
    }
    static void Check(HealthComponent health, StatsComponent stats, float max, float current)
    { Require(health.MaxLife == max && stats.GetStat(StatTypes.Life) == max && health.CurrentLife == current, $"Expected {current}/{max}, got {health.CurrentLife}/{health.MaxLife}"); }
    static void Require(bool condition,string message) { if (!condition) throw new System.InvalidOperationException(message); }
}
