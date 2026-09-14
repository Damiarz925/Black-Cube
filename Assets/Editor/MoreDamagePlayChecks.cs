using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Opt-in disposable Play check; always exits Play without saving fixtures.</summary>
[InitializeOnLoad]
public static class MoreDamagePlayChecks
{
    const string Key="BlackCube.MoreDamageChecks",Report="ReviewCaptures/more-damage-play-check.txt";
    static bool pending;static double ready;
    static MoreDamagePlayChecks()
    {
        EditorApplication.playModeStateChanged+=s=>
        {
            if(s==PlayModeStateChange.ExitingPlayMode)pending=false;
            if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);ready=EditorApplication.timeSinceStartup+2;pending=true;
        };
        EditorApplication.update+=()=>
        {
            if(!pending || EditorApplication.timeSinceStartup<ready)return;pending=false;
            try {Run();Write("COMPLETE: all scoped Play assertions passed.");}
            catch(Exception e){Write("FAIL: "+e);}
            finally{EditorApplication.isPlaying=false;}
        };
    }
    [MenuItem("Black Cube/Play Checks/Verify Independent More Damage %&m")]
    static void Begin()
    {if(EditorApplication.isPlaying)return;File.WriteAllText(Report,"Independent more damage Play checks\n");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static void Write(string s){File.AppendAllText(Report,s+"\n");Debug.Log(s);}
    static void Near(float a,float b,string s){if(Mathf.Abs(a-b)>.002f)throw new Exception(s+$": {a} != {b}");Write("PASS: "+s+$" = {a:0.####}");}
    static Gear Item(LootManager.GearType type,params RolledMod[] mods)
    {var g=new GameObject("More damage fixture").AddComponent<Gear>();g.Initialize(type,LootManager.GearRarity.Magic,1,Element.Cold);g.BaseDamage=100;g.BaseAttackSpeed=1;g.ApplyMods(new List<RolledMod>(mods));Inventory.Instance.Add(g);return g;}
    static float Display(string label)=>float.Parse(Object.FindFirstObjectByType<PlayerStatsPanelUI>().GetComponentsInChildren<StatRowUI>()
        .Single(r=>r.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==label)).GetComponentsInChildren<TMP_Text>().First(t=>t.text!=label).text);
    static void Run()
    {
        Time.timeScale=0;
        var p=Object.FindFirstObjectByType<PlayerController>();var s=p.GetComponent<StatsComponent>();var eq=EquipmentManager.Instance;
        foreach(LootManager.GearType type in Enum.GetValues(typeof(LootManager.GearType)))eq.Unequip(type);
        foreach(var stat in s.GetTrackedStats().ToArray())if(stat!=StatTypes.Life)s.SetBaseStat(stat,0);
        var hud=Object.FindFirstObjectByType<PaperBattleHUD>();hud.statsPanel.SetActive(true);
        var w=Item(LootManager.GearType.Weapons);eq.Equip(w);
        var cold=Item(LootManager.GearType.Rings,new RolledMod(StatTypes.ColdMult,1,20));
        var generic=Item(LootManager.GearType.Helmets,new RolledMod(StatTypes.GenericMult,1,10));eq.Equip(cold);eq.Equip(generic);
        s.SetBaseStat(StatTypes.ColdDmg,40);s.SetBaseStat(StatTypes.GenericDmg,30);
        Near(p.BasicAttackDamage,224.4f,"Actual equipped Cold20 and generic10 with additive increased40+30");
        Near(Display("Cold / Hit (Noncritical)"),224.4f,"Immediate typed UI matches224.4");
        var enemyObject=new GameObject("More damage enemy fixture");enemyObject.AddComponent<StatsComponent>();var enemy=enemyObject.AddComponent<EnemyAI>();
        var es=enemy.GetComponent<StatsComponent>();typeof(EnemyAI).GetField("equippedWeapon",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,w);
        var apply=typeof(EnemyAI).GetMethod("ApplyGlobalModsFromGear",BindingFlags.Instance|BindingFlags.NonPublic);
        apply.Invoke(enemy,new object[]{cold});apply.Invoke(enemy,new object[]{generic});es.SetBaseStat(StatTypes.ColdDmg,40);es.SetBaseStat(StatTypes.GenericDmg,30);
        Near(enemy.BuildNonCriticalAttackContext().Hits.Sum(h=>h.Amount),224.4f,"Enemy actual gear ingestion agrees");
        s.SetBaseStat(StatTypes.FireMult,900);Near(p.BasicAttackDamage,224.4f,"Nonmatching Fire more excluded");s.SetBaseStat(StatTypes.FireMult,0);
        s.SetBaseStat(StatTypes.FlatFire,10);s.SetBaseStat(StatTypes.FireDmg,40);s.SetBaseStat(StatTypes.FireMult,20);
        Near(p.BuildNonCriticalAttackContext().Hits.Single(h=>h.Element==Element.Fire).Amount,22.44f,"Existing off-element flat uses same product");
        Near(Display("Fire / Hit (Noncritical)"),22.44f,"Off-element UI matches");
        eq.Unequip(LootManager.GearType.Rings);eq.Unequip(LootManager.GearType.Helmets);
        foreach(var stat in s.GetTrackedStats().ToArray())if(stat!=StatTypes.Life)s.SetBaseStat(stat,0);
        var a=Item(LootManager.GearType.Rings,new RolledMod(StatTypes.GenericMult,1,20));var b=Item(LootManager.GearType.Helmets,new RolledMod(StatTypes.GenericMult,1,20));
        eq.Equip(a);eq.Equip(b);Near(p.BasicAttackDamage,144,"Two separate gear20 rolls yield144");Near(Display("Cold / Hit (Noncritical)"),144,"Duplicate-roll UI refresh");
        eq.Unequip(LootManager.GearType.Rings);Near(p.BasicAttackDamage,120,"Removing one source leaves120");eq.Equip(a);Near(p.BasicAttackDamage,144,"Reequip preserves roll identity");
        eq.Unequip(LootManager.GearType.Rings);eq.Unequip(LootManager.GearType.Helmets);
        w.BaseElement=Element.Phys;p.EquipWeapon(w);
        File.AppendAllText("ReviewCaptures/inventory-grid-check.txt","\nINDEPENDENT-MORE regression rerun "+DateTime.Now.ToString("s")+"\n");
        typeof(InventoryGridChecks).GetMethod("VerifyDamageTypesAndAilments",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,s,w});
        Write("PASS: existing six-element and production DOT tick/health-loss regressions passed under independent-more expectations");
        w.BaseCritChance=.05f;w.LocalIncCrit=1;s.SetBaseStat(StatTypes.BaseCritChance,2);s.SetBaseStat(StatTypes.CritChance,100);
        Near(p.GetFinalCritChance(),.28f,"Crit formula remains28%");
        Object.Destroy(enemyObject);
    }
}
