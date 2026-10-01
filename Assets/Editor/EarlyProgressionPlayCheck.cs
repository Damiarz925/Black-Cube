using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Six first encounters only, then one saved Rebirth transaction. No balance sweep.
[InitializeOnLoad]
public static class EarlyProgressionPlayCheck
{
    const string Key="BlackCube.EarlyProgression.Smoke",Report="Logs/EarlyProgression-play-check.txt";
    static double next;
    static EarlyProgressionPlayCheck(){EditorApplication.update+=Tick;if(SessionState.GetBool(Key,false))GamePersistence.SaveDirectoryOverride=SessionState.GetString(Key+".dir","");}
    public static void Run()
    {
        var folder=Path.GetFullPath("Temp/EarlyProgressionSmoke-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);Directory.CreateDirectory("Logs");File.WriteAllText(Report,"Six class first-encounter sanity + saved Rebirth setup\n");
        SessionState.SetString(Key+".dir",folder);SessionState.SetBool(Key+".prefs",PlayerPrefs.HasKey(GamePersistence.SaveKey));SessionState.SetString(Key+".value",PlayerPrefs.GetString(GamePersistence.SaveKey,""));
        GamePersistence.ResetStaticStateForTests();GamePersistence.SaveDirectoryOverride=folder;PlayerPrefs.DeleteKey(GamePersistence.SaveKey);GamePersistence.RequestNewGame();SessionState.SetInt(Key+".state",0);SessionState.SetInt(Key+".class",0);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity");EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;int state=SessionState.GetInt(Key+".state",0);
        if(!EditorApplication.isPlaying){if(state<2)return;if(SessionState.GetBool(Key+".prefs",false))PlayerPrefs.SetString(GamePersistence.SaveKey,SessionState.GetString(Key+".value",""));else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);PlayerPrefs.Save();GamePersistence.SaveDirectoryOverride=null;SessionState.SetBool(Key,false);EditorApplication.Exit(state==2?0:1);return;}
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.25;
        try
        {
            if(state==0&&SceneManager.GetActiveScene().name==GameSceneNames.MainMenu){UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(b=>b.name=="Start Game Button").onClick.Invoke();var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuUI>();Require(menu.SelectClass(PlayerClassIds.Warrior),"Class selection");menu.StartSelectedClass();SessionState.SetInt(Key+".state",1);SessionState.SetFloat(Key+".start",(float)EditorApplication.timeSinceStartup);return;}
            if(state!=1||SceneManager.GetActiveScene().name!=GameSceneNames.Gameplay||BattleManager.Instance==null)return;
            var gm=GameManager.Instance;var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();var hp=player.GetComponent<HealthComponent>();int index=SessionState.GetInt(Key+".class",0);
            if(gm.NormalKills<1)
            {Require(hp.CurrentLife>0,"Starter died: "+gm.GetComponent<PlayerIdentityState>().BaseClassId);Require(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".start",0)<60,"First encounter timeout");return;}
            Write($"PASS {gm.GetComponent<PlayerIdentityState>().BaseClassId} first encounter; level {gm.GetComponent<PlayerProgression>().Level}; remaining Life {hp.CurrentLife:0.##}; XP {gm.GetComponent<PlayerProgression>().Experience:0.##}");
            if(++index<PlayerClassCatalog.All.Count)
            {
                SessionState.SetInt(Key+".class",index);gm.GetComponent<PlayerIdentityState>().BeginNewGame(PlayerClassCatalog.All[index].Id);EquipmentManager.Instance.ResetForNewRun();Inventory.Instance.ResetForNewRun();player.GetComponent<StatusController>().ClearStatuses();hp.ReviveToFullLife();player.GetComponent<ManaComponent>().RestoreFull();player.EnsureStarterWeapon();gm.StartNewRun();SessionState.SetFloat(Key+".start",(float)EditorApplication.timeSinceStartup);return;
            }
            CheckSetup();Write("PASS all targeted live-scene checks");SessionState.SetInt(Key+".state",2);EditorApplication.isPlaying=false;
        }
        catch(Exception ex){Write("FAIL "+ex);SessionState.SetInt(Key+".state",3);EditorApplication.isPlaying=false;}
    }
    static void CheckSetup()
    {
        var gm=GameManager.Instance;var r=RelicInventory.Instance;var cur=CurrencyInventory.Instance;var m=RebirthManager.Instance;var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();var controller=player.GetComponent<PlayerSkillController>();
        Time.timeScale=0;gm.StartZone(100);cur.Add(CraftingCurrencyType.AncientNormalToMagic,7);int before=r.Relics.Count;
        var view=UnityEngine.Object.FindAnyObjectByType<RebirthView>();
        Require(view!=null,"Authored Rebirth view missing");view.openButton.onClick.Invoke();Require(m.ConfirmationPending,"Open confirmation control failed");
        view.confirmButton.onClick.Invoke();
        Require(view.message.text.Contains("CRAFT CURRENT-CYCLE"),"Wizard phase text failed");
        view.confirmationPanel.transform.Find("OPEN RELIC INVENTORY").GetComponent<Button>().onClick.Invoke();
        var inventoryUi=UnityEngine.Object.FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
        Require(inventoryUi.gameObject.activeInHierarchy&&inventoryUi.GetComponent<CurrencyInventoryPanel>().IsShowingRelics,"Relic inventory navigation failed");
        inventoryUi.gameObject.SetActive(false);view.openButton.onClick.Invoke();Require(view.confirmationPanel.activeSelf,"Resume setup control failed");
        Require(m.IsSetup,"Confirmation failed");Require(m.Phase==RebirthPhase.Crafting&&r.Relics.Count==before+2,"Grant/phase mismatch");Require(cur.Count(CraftingCurrencyType.AncientNormalToMagic)==7,"Earned Ancient currency lost");m.Cancel();Require(m.IsSetup&&!m.ConfirmRebirth(),"Cancelled irreversible setup or duplicate grant");
        Require(GamePersistence.TrySave()&&GamePersistence.Load(),GamePersistence.LastError);Require(m.Phase==RebirthPhase.Crafting&&r.Relics.Count==before+2,"Crafting phase reload failed");Require(r.IsCurrentCraftable(r.Relics.Last()),"Current-cycle craft window lost");
        Require(m.AdvanceSetup()&&m.Phase==RebirthPhase.Equipment,"Equipment phase missing");var loadout=r.Relics.Last();loadout.modifiers=new List<RelicModifier>{new(RelicModifierType.RebirthItemReturn,100,true),new(RelicModifierType.TriggerFrostJudgment,1,false)};
        Require(r.Equip(loadout,0),"Setup equip failed");Require(m.AdvanceSetup()&&m.Phase==RebirthPhase.Returns,"Returns phase missing");Require(!r.Equip(loadout,1),"Loadout remained editable");
        var returned=UniqueCatalog.Create("unique.stormcaller",100,fixedRoll:.5f);Inventory.Instance.Add(returned);string id=returned.PersistentId;Require(m.SelectReturnItem(0,returned),"Eligible return rejected");controller.ToggleRelicTrigger(PlayerSkillId.SceptreFrostJudgment);
        Require(GamePersistence.TrySave()&&GamePersistence.Load(),GamePersistence.LastError);Require(m.ReturningItems[0].PersistentId==id,"Return selection lost");Require(!controller.RelicTriggerEnabled(PlayerSkillId.SceptreFrostJudgment),"Trigger setting lost");
        Require(m.AdvanceSetup()&&m.Phase==RebirthPhase.Weapon,"Weapon phase missing");string weapon=gm.GetComponent<PlayerIdentityState>().ClassDefinition.SignatureWeaponTypeId;Require(m.SelectStartingWeapon(weapon),"Default weapon rejected");Require(m.AdvanceSetup()&&m.Phase==RebirthPhase.Review,"Review missing");Require(m.AdvanceSetup(),"Begin next run rejected");
        Require(!m.IsSetup&&gm.CurrentCombatLevel==1,"Old run resumed");Require(Inventory.Instance.Items.Any(g=>g.PersistentId==id),"Returned Unique lost");Require(cur.Count(CraftingCurrencyType.AncientNormalToMagic)==0,"Run currency leaked");Require(r.Relics.All(x=>!x.craftableThisCycle)&&!r.CanChangeLoadout,"Relics reopened during run");Require(player.EquippedWeapon.WeaponTypeId==weapon,"Starting weapon mismatch");Write("PASS saved crafting/returns/trigger state, irreversible confirmation, locked loadout, return item and clean next run");
    }
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static void Write(string message){File.AppendAllText(Report,message+Environment.NewLine);Debug.Log(message);}
}
