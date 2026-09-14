// Developer map: Opt-in Play checks for scrap yields, ownership, equipment modifiers and tooltip content.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class InventoryChecks
{
    const string Key="BlackCube.InventoryChecks";
    const string Report="ReviewCaptures/inventory-check.txt";
    static double ready;
    static bool pending;
    static InventoryChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);ready=EditorApplication.timeSinceStartup+2;pending=true;
        };
        EditorApplication.update += () =>
        {
            if(!pending || EditorApplication.timeSinceStartup<ready)return;
            pending=false;
            try{Run();}catch(Exception e){Write("FAIL: "+e);}
        };
    }
    [MenuItem("Black Cube/Play Checks/Verify Inventory")]
    static void Begin()
    {
        if(EditorApplication.isPlaying)return;
        File.WriteAllText(Report,"Inventory bounded Play diagnostic\n");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Write(string message){File.AppendAllText(Report,message+"\n");Debug.Log(message);}
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);Write("PASS: "+label);}
    static int CurrencyTotal()=>EnemyDropTable.OrdinaryTypes().Sum(type=>CurrencyInventory.Instance.Count(type));
    static Gear Make(LootManager.GearRarity rarity,Element element=Element.Fire)
    {
        var go=new GameObject("Inventory fixture "+rarity);var gear=go.AddComponent<Gear>();
        gear.Initialize(LootManager.GearType.Weapons,rarity,7,element);
        gear.BaseDamage=80;gear.BaseAttackSpeed=1.2f;gear.BaseCritChance=.05f;
        Inventory.Instance.Add(gear);return gear;
    }
    static void Run()
    {
        Time.timeScale=0;
        var inv=Inventory.Instance;var equipment=EquipmentManager.Instance;
        int initial=inv.Items.Count;
        var normal=Make(LootManager.GearRarity.Normal);
        int currency=CurrencyTotal();int reward=Inventory.ScrapYield(normal);
        Check(inv.TryDismantle(normal),"Normal dismantles into the consolidated currency inventory");
        Check(!inv.Items.Contains(normal) && inv.Items.Count==initial && CurrencyTotal()==currency+reward,"Normal is removed and its current currency reward is recorded");
        Check(!inv.TryDismantle(normal),"Repeated callback is rejected before Destroy completes");
        var magic=Make(LootManager.GearRarity.Magic);var rare=Make(LootManager.GearRarity.Rare);
        currency=CurrencyTotal();reward=Inventory.ScrapYield(magic);Check(inv.TryDismantle(magic) && !inv.Items.Contains(magic) && CurrencyTotal()==currency+reward,"Magic dismantles into consolidated currency and is removed");
        currency=CurrencyTotal();reward=Inventory.ScrapYield(rare);Check(inv.TryDismantle(rare) && !inv.Items.Contains(rare) && CurrencyTotal()==currency+reward,"Rare dismantles into consolidated currency and is removed");
        var starter=equipment.GetEquipped(LootManager.GearType.Weapons);
        var weapon=Make(LootManager.GearRarity.Rare,Element.Cold);
        weapon.LocalFlatDamage=12;weapon.LocalIncDamage=.25f;weapon.LocalIncAttackSpeed=.1f;
        weapon.ApplyMods(new System.Collections.Generic.List<RolledMod>{new(StatTypes.Life,1,100),new(StatTypes.PoisonChance,1,17)});
        var stats=Object.FindFirstObjectByType<PlayerController>().GetComponent<StatsComponent>();
        float hp=stats.GetRawStat(StatTypes.Life);
        equipment.Equip(weapon);
        Check(equipment.GetEquipped(LootManager.GearType.Weapons)==weapon && !inv.Items.Contains(weapon) && stats.GetRawStat(StatTypes.Life)==hp+100,"Equip applies item stats and removes inventory item");
        Check(!inv.TryDismantle(weapon) && stats.GetRawStat(StatTypes.Life)==hp+100,"Equipped gear protected without corrupting stats");
        equipment.Equip(starter);
        Check(inv.Items.Contains(weapon) && stats.GetRawStat(StatTypes.Life)==hp,"Swap returns gear and removes only its modifiers");
        string text=ItemTooltipUI.Describe(weapon);
        Check(text.Contains("Cold") && text.Contains("115") && text.Contains("1.32") && text.Contains("Poison Chance: +17%") && text.Contains("Maximum HP: +100"),"Tooltip uses effective weapon stats, base element and actual global/status modifiers");
        Check(inv.TryDismantle(weapon) && !inv.Items.Contains(weapon) && stats.GetRawStat(StatTypes.Life)==hp,"Swapped gear dismantles; stats stay intact");
        var legendary=Make(LootManager.GearRarity.Legendary);
        currency=CurrencyTotal();reward=Inventory.ScrapYield(legendary);Check(inv.TryDismantle(legendary) && !inv.Items.Contains(legendary) && CurrencyTotal()==currency+reward,"Legendary dismantles into consolidated currency and is removed");
        Check(!inv.TryDismantle(legendary),"Legendary repeat callback rejected");
        var preview=Make(LootManager.GearRarity.Rare,Element.Fire);
        preview.LocalFlatDamage=12;preview.LocalIncDamage=.25f;preview.LocalIncAttackSpeed=.1f;
        preview.ApplyMods(new System.Collections.Generic.List<RolledMod>{new(StatTypes.PoisonChance,1,17),new(StatTypes.Life,1,100)});
        Make(LootManager.GearRarity.Normal,Element.Cold);Make(LootManager.GearRarity.Magic,Element.Light);Make(LootManager.GearRarity.Normal,Element.Phys);
        var hud=Object.FindFirstObjectByType<PaperBattleHUD>();hud.inventoryPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        var slot=hud.inventoryPanel.GetComponentsInChildren<ItemSlotUI>().Single(s=>s.Item==preview);
        slot.OnPointerEnter(new PointerEventData(EventSystem.current));
        Write("Visual fixture ready: inventory has Fire/Cold/Lightning/Physical weapons. Hover, equip and dismantle normally. Exit Play afterward; no scene save.");
    }
}
