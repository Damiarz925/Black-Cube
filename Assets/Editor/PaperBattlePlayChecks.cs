using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit checks in a disposable Play session; no scene or asset gameplay data is saved.</summary>
public static class PaperBattlePlayChecks
{
    [MenuItem("Black Cube/Play Checks/Verify Equipment Stats")]
    static void VerifyEquipment()
    {
        if(!EditorApplication.isPlaying) {Debug.LogError("Enter Play mode first.");return;}
        var player=Object.FindFirstObjectByType<PlayerController>();
        var stats=player.GetComponent<StatsComponent>();
        var manager=EquipmentManager.Instance;
        var managerStats=(StatsComponent)new SerializedObject(manager).FindProperty("playerStats").objectReferenceValue;
        Require(managerStats==stats,"Equipment manager must modify the active player's stats");
        var hud=Object.FindFirstObjectByType<PaperBattleHUD>();
        hud.inventoryPanel.SetActive(true);
        hud.statsPanel.SetActive(true);
        var display=hud.statsPanel.GetComponent<EquipmentStatsUI>();
        Require(display.slots.Length==8,"All eight equipment categories must be displayed");
        Require(manager.GetEquipped(LootManager.GearType.Weapons)!=null,"Starter weapon must be tracked as equipped");
        float before=stats.GetRawStat(StatTypes.FlatPhys);
        var a=Make("QA Glove +10",10);var b=Make("QA Glove +25",25);
        Inventory.Instance.Add(a);Inventory.Instance.Add(b);
        ClickSlot(hud,a);float first=stats.GetRawStat(StatTypes.FlatPhys);
        CheckDisplay(display,a);
        Require(Mathf.Approximately(first,before+10),$"First equip: expected {before+10}, got {first}");
        Require(!Inventory.Instance.Items.Contains(a),"Equipped item must leave inventory");
        ClickSlot(hud,b);float replacement=stats.GetRawStat(StatTypes.FlatPhys);
        CheckDisplay(display,b);
        Require(Mathf.Approximately(replacement,before+25),$"Replacement must remove old modifier: expected {before+25}, got {replacement}");
        Require(Inventory.Instance.Items.Contains(a) && !Inventory.Instance.Items.Contains(b),"Replaced item must return to inventory");
        ClickSlot(hud,a);float back=stats.GetRawStat(StatTypes.FlatPhys);
        CheckDisplay(display,a);
        Require(Mathf.Approximately(back,before+10),"Re-equipping must not stack modifiers");
        string report=$"PAPER2D CHECK PASS: actual inventory button -> EquipmentManager -> active player FlatPhys {before} -> {first} -> {replacement} -> {back}; replacement returned old gear, equipped gear removed, no modifier stacking. Eight stats categories and starter weapon present; Glove glyph, border and label updated through Magic -> Rare -> Magic. QA gear is Play-session only.";
        Debug.Log(report);
        System.IO.Directory.CreateDirectory("ReviewCaptures");System.IO.File.WriteAllText("ReviewCaptures/equipment-check.txt",report);
    }
    static Gear Make(string name,float value)
    {
        var g=new GameObject(name).AddComponent<Gear>();g.Initialize(LootManager.GearType.Gloves,value==25?LootManager.GearRarity.Rare:LootManager.GearRarity.Magic,1,Element.Phys);
        g.ApplyMods(new System.Collections.Generic.List<RolledMod>{new RolledMod(StatTypes.FlatPhys,1,value)});return g;
    }
    static void CheckDisplay(EquipmentStatsUI display,Gear gear)
    {
        var slot=display.slots.Single(s=>s.type==gear.ItemType);
        Require(EquipmentManager.Instance.GetEquipped(gear.ItemType)==gear,"Slot must reference equipped item");
        Require(slot.glyph.gearType==gear.ItemType && slot.glyph.color==ItemSlotUI.RarityColor(gear.ItemRarity),"Icon category and line color must match equipped item");
        Require(slot.border.color==slot.glyph.color && slot.detail.text.Contains(gear.ItemRarity.ToString()),"Rarity border and detail must update after replacement");
        Require(slot.label.text=="Glove","Category label must be singular");
    }
    static void ClickSlot(PaperBattleHUD hud,Gear gear)
    {
        var slot=hud.inventoryPanel.GetComponentsInChildren<ItemSlotUI>().Last(s=>
            typeof(ItemSlotUI).GetField("gear",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(s)==gear);
        slot.GetComponent<Button>().onClick.Invoke();
    }
    static void Require(bool condition,string description) {if(!condition)throw new System.InvalidOperationException(description);}
    [MenuItem("Black Cube/Play Checks/Trigger Player Death")]
    static void Death()
    {
        if(!EditorApplication.isPlaying)return;
        var player=Object.FindFirstObjectByType<PlayerController>().GetComponent<HealthComponent>();
        player.LoseLife(player.CurrentLife+1);
    }
}
