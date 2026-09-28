using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in real-scene check. Saves are isolated; no real character files are touched.
[InitializeOnLoad]
public static class SystemsRedesignPlayCheck
{
    const string Key="BlackCube.SystemsRedesign.Smoke",Report="Logs/SystemsRedesign-play-check.txt";
    static double next;
    static SystemsRedesignPlayCheck(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;if(SessionState.GetBool(Key,false))GamePersistence.SaveDirectoryOverride=SessionState.GetString(Key+".dir","");}
    public static void Run()
    {
        string folder=Path.GetFullPath("Temp/SystemsRedesignSmoke-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);Directory.CreateDirectory("Logs");File.WriteAllText(Report,"Systems redesign real-scene smoke\n");
        SessionState.SetString(Key+".dir",folder);SessionState.SetBool(Key+".prefs",PlayerPrefs.HasKey(GamePersistence.SaveKey));SessionState.SetString(Key+".value",PlayerPrefs.GetString(GamePersistence.SaveKey,""));
        GamePersistence.ResetStaticStateForTests();GamePersistence.SaveDirectoryOverride=folder;PlayerPrefs.DeleteKey(GamePersistence.SaveKey);GamePersistence.RequestNewGame();
        SessionState.SetInt(Key+".state",0);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity");EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(!EditorApplication.isPlaying){int state=SessionState.GetInt(Key+".state",0);if(state<2)return;if(SessionState.GetBool(Key+".prefs",false))PlayerPrefs.SetString(GamePersistence.SaveKey,SessionState.GetString(Key+".value",""));else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);PlayerPrefs.Save();GamePersistence.SaveDirectoryOverride=null;GamePersistence.RequestNewGame();SessionState.SetBool(Key,false);EditorApplication.Exit(state==2?0:1);return;}
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;
        try
        {
            int state=SessionState.GetInt(Key+".state",0);
            if(state==0&&SceneManager.GetActiveScene().name==GameSceneNames.MainMenu)
            {
                UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(x=>x.name=="Start Game Button").onClick.Invoke();
                var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuUI>();Require(menu.SelectClass(PlayerClassIds.Warrior),"Class selection");menu.StartSelectedClass();SessionState.SetInt(Key+".state",1);return;
            }
            if(state!=1||SceneManager.GetActiveScene().name!=GameSceneNames.Gameplay||BattleManager.Instance==null)return;
            RunGameplay();Write("PASS all targeted real-scene checks");SessionState.SetInt(Key+".state",2);EditorApplication.isPlaying=false;
        }
        catch(Exception ex){Write("FAIL "+ex);SessionState.SetInt(Key+".state",3);EditorApplication.isPlaying=false;}
    }
    static void RunGameplay()
    {
        var inventory=Inventory.Instance;var relics=RelicInventory.Instance;
        Require(UnityEngine.Object.FindObjectsByType<UniqueRelicForgeUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length>0,"Authored forge UI missing");
        GameManager.Instance.StartZone(341);Require(BattleManager.Instance.CurrentEnemyAI.GetComponent<HealthComponent>().IsBoss,"Gauntlet spawned ordinary enemy");Require(BattleManager.Instance.CurrentEnemyAI.CurrentRarity==EnemyAI.EnemyRarity.Legendary,"Gauntlet boss not Legendary");
        Require(GameManager.Instance.RestoreRunState(342,0,false),"Gauntlet legacy checkpoint restore");Require(BattleManager.Instance.CurrentEnemyAI.GetComponent<HealthComponent>().IsBoss,"Legacy save reintroduced ordinary encounter");Write("PASS gauntlet start and legacy checkpoint normalization");
        var first=UniqueCatalog.Create("unique.heart-berserker",100,fixedRoll:.5f);var second=UniqueCatalog.Create("unique.prism-voices",100,fixedRoll:1);inventory.Add(first);inventory.Add(second);
        var legendary1=Legendary();var legendary2=Legendary();inventory.Add(legendary1);inventory.Add(legendary2);relics.AwardForgeOpportunity();
        var forgeUi=UnityEngine.Object.FindObjectsByType<UniqueRelicForgeUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
        // Exercise the authored button/dropdown wiring, not just backend authority.
        forgeUi.gameObject.SetActive(true);
        var forgeSerialized=new SerializedObject(forgeUi);
        ((Button)forgeSerialized.FindProperty("open").objectReferenceValue).onClick.Invoke();
        ((Button)forgeSerialized.FindProperty("close").objectReferenceValue).onClick.Invoke();
        Require(!UniqueRelicForge.TryForge(relics,first,first,legendary1,legendary2,0,0,()=>.25f,out _,out _),"Duplicate ingredients accepted");Require(relics.ForgeOpportunities==1&&inventory.Items.Contains(first),"Rejected recipe consumed resources");
        Require(UniqueRelicForge.TryForge(relics,first,second,legendary1,legendary2,0,0,()=>.25f,out var forged,out var error),error);
        Require(relics.ForgeOpportunities==0&&new[]{first,second,legendary1,legendary2}.All(x=>!inventory.Items.Contains(x)),"Recipe did not consume exactly four items and one opportunity");Require(forged.forgedPowers.Count==2&&forged.forgedStats.Count==2,"Four extracted modifiers missing");Require(relics.Equip(forged,0),"Unique Relic equip failed");
        var other=JsonUtility.FromJson<RelicData>(JsonUtility.ToJson(forged));other.id=Guid.NewGuid().ToString("N");relics.Grant(other);Require(!relics.Equip(other,1),"Second Unique Relic equipped");
        var saved=UniqueCatalog.Create("unique.stormcaller",100,fixedRoll:.5f);inventory.Add(saved);string savedId=saved.PersistentId,forgedId=forged.id;string savedRolls=JsonUtility.ToJson(saved.UniqueData);relics.AwardForgeOpportunity();
        Require(GamePersistence.TrySave(),GamePersistence.LastError);Require(GamePersistence.Load(),GamePersistence.LastError);Require(relics.ForgeOpportunities==1&&relics.Active(0)?.id==forgedId,"Forge state did not survive save/load");Require(JsonUtility.ToJson(inventory.Items.Single(x=>x.PersistentId==savedId).UniqueData)==savedRolls,"Unique rerolled on load");Write("PASS authored forge controls, atomic forge, chosen/retained rolls, one-equipped limit and schema-15 save/load");
        GameManager.Instance.StartZone(350);int previousRelics=relics.Relics.Count,previousCycle=relics.CurrentCycle;
        Require(RebirthManager.Instance.RequestRebirth()&&RebirthManager.Instance.ConfirmRebirth(),"Stage350 Rebirth failed");
        Require(relics.ForgeOpportunities==2&&relics.Relics.Count==previousRelics+8&&relics.CurrentCycle==previousCycle+1,"Rebirth did not award exactly eight Relics and one forge opportunity");
        Require(GameManager.Instance.CurrentCombatLevel==1,"Rebirth did not restart run");Write("PASS actual stage350 Rebirth -> eight rewards + one opportunity -> clean new run");
    }
    static Gear Legendary()
    {
        var gear=new GameObject("Forge Legendary fixture").AddComponent<Gear>();gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Legendary,100,Element.Phys,WeaponTypeIds.Sword);
        List<RolledMod> mods=null;for(int i=0;i<40;i++){mods=ModManager.Instance.RollEquipmentModsForItem(gear.ItemType,gear.ItemRarity,100,gear.BaseElement,gear.WeaponTypeId,new SequenceLootRandomSource(100+i,.05f,.21f,.37f,.53f,.69f,.85f));if(mods!=null&&mods.Count(x=>!x.lockedOriginal&&!Gear.IsWeaponBaseStat(x.statType))==6)break;}
        Require(mods!=null&&mods.Count(x=>!x.lockedOriginal&&!Gear.IsWeaponBaseStat(x.statType))==6,"Six-explicit Legendary fixture unavailable");gear.ApplyMods(mods);return gear;
    }
    static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
    static void Write(string message){File.AppendAllText(Report,message+Environment.NewLine);Debug.Log(message);}
}
