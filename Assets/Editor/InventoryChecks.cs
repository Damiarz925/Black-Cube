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
        Check(inv.TryDismantle(normal),"Normal dismantles");
        var scrap=inv.Items.Single(g=>g.IsScrap);
        Check(scrap.StackCount==1 && inv.Items.Count==initial+1,"Normal yields1; removed slot replaced by one Scrap slot");
        Check(!inv.TryDismantle(normal) && scrap.StackCount==1,"Repeated callback gives no second reward before Destroy completes");
        var magic=Make(LootManager.GearRarity.Magic);var rare=Make(LootManager.GearRarity.Rare);
        Check(inv.TryDismantle(magic) && scrap.StackCount==3,"Magic yields2 into same stack");
        Check(inv.TryDismantle(rare) && scrap.StackCount==6 && inv.Items.Count(g=>g.IsScrap)==1,"Rare yields3 into same visible slot");
        Check(!inv.TryDismantle(scrap),"Scrap cannot dismantle itself");
        var starter=equipment.GetEquipped(LootManager.GearType.Weapons);
        equipment.Equip(scrap);Check(equipment.GetEquipped(LootManager.GearType.Weapons)==starter,"Scrap cannot equip");
        var weapon=Make(LootManager.GearRarity.Rare,Element.Cold);
        weapon.LocalFlatDamage=12;weapon.LocalIncDamage=.25f;weapon.LocalIncAttackSpeed=.1f;
        weapon.globalRolledMods.Add(new RolledMod(StatTypes.Life,1,100));
        weapon.globalRolledMods.Add(new RolledMod(StatTypes.PoisonChance,1,17));
        var stats=Object.FindFirstObjectByType<PlayerController>().GetComponent<StatsComponent>();
        float hp=stats.GetRawStat(StatTypes.Life);
        equipment.Equip(weapon);
        Check(equipment.GetEquipped(LootManager.GearType.Weapons)==weapon && !inv.Items.Contains(weapon) && stats.GetRawStat(StatTypes.Life)==hp+100,"Equip applies item stats and removes inventory item");
        Check(!inv.TryDismantle(weapon) && stats.GetRawStat(StatTypes.Life)==hp+100,"Equipped gear protected without corrupting stats");
        equipment.Equip(starter);
        Check(inv.Items.Contains(weapon) && stats.GetRawStat(StatTypes.Life)==hp,"Swap returns gear and removes only its modifiers");
        string text=ItemTooltipUI.Describe(weapon);
        Check(text.Contains("Cold") && text.Contains("115") && text.Contains("1.32") && text.Contains("Poison Chance: +17%") && text.Contains("Maximum HP: +100"),"Tooltip uses effective weapon stats, base element and actual global/status modifiers");
        Check(inv.TryDismantle(weapon) && scrap.StackCount==9 && stats.GetRawStat(StatTypes.Life)==hp,"Swapped gear dismantles with correct yield; stats stay intact");
        var legendary=Make(LootManager.GearRarity.Legendary);
        Check(inv.TryDismantle(legendary) && scrap.StackCount==14,"Legendary yields5 into same stack");
        Check(!inv.TryDismantle(legendary) && scrap.StackCount==14,"Legendary repeat callback rejected");
        var preview=Make(LootManager.GearRarity.Rare,Element.Fire);
        preview.LocalFlatDamage=12;preview.LocalIncDamage=.25f;preview.LocalIncAttackSpeed=.1f;
        preview.globalRolledMods.Add(new RolledMod(StatTypes.PoisonChance,1,17));
        preview.globalRolledMods.Add(new RolledMod(StatTypes.Life,1,100));
        Make(LootManager.GearRarity.Normal,Element.Cold);Make(LootManager.GearRarity.Magic,Element.Light);Make(LootManager.GearRarity.Normal,Element.Phys);
        var hud=Object.FindFirstObjectByType<PaperBattleHUD>();hud.inventoryPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        var slot=hud.inventoryPanel.GetComponentsInChildren<ItemSlotUI>().Single(s=>s.Item==preview);
        slot.OnPointerEnter(new PointerEventData(EventSystem.current));
        Write("Visual fixture ready: inventory has Scrap x14 and Fire/Cold/Lightning/Physical weapons. Hover/click equip normally; enter Fire tooltip and Scrap ->x17. Exit Play afterward; no scene save.");
    }
}
