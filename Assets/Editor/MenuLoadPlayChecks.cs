// End-to-end Step 3 smoke check for repeated Main Menu and gameplay transitions.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MenuLoadPlayChecks
{
    const string PendingKey="BlackCube.MenuLoadPlayChecks.Pending";
    const string Report="Logs/MenuLoadPlayChecks.txt";
    static string SaveDirectory=>Path.GetFullPath("Temp/MenuLoadPlayChecksSave");
    static int step,errors;
    static double readyAt;
    static bool completed,hadSave;
    static string savedValue,savedFilterValue,savedRunId;
    static bool hadFilter;
    static float checkpointLife,checkpointMana;
    const string FilterPreferenceKey="BlackCube.InventoryFilters.V1";

    static MenuLoadPlayChecks()
    {
        if(SessionState.GetBool(PendingKey,false))GamePersistence.SaveDirectoryOverride=SaveDirectory;
        EditorApplication.update-=Tick;
        EditorApplication.update+=Tick;
    }

    [MenuItem("Black Cube/Play Checks/Verify Main Menu and Load Game")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report,string.Empty);
        if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);
        Directory.CreateDirectory(SaveDirectory);GamePersistence.SaveDirectoryOverride=SaveDirectory;
        hadSave=PlayerPrefs.HasKey(GamePersistence.SaveKey);
        savedValue=hadSave?PlayerPrefs.GetString(GamePersistence.SaveKey):null;
        hadFilter=PlayerPrefs.HasKey(FilterPreferenceKey);
        savedFilterValue=hadFilter?PlayerPrefs.GetString(FilterPreferenceKey):null;
        PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
        PlayerPrefs.Save();
        GamePersistence.RequestNewGame();
        SessionState.SetBool(PendingKey,true);
        step=errors=0;readyAt=0;completed=false;
        Application.logMessageReceived+=OnLog;
        EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity");
        EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        if(!SessionState.GetBool(PendingKey,false))return;
        if(!EditorApplication.isPlaying)
        {
            if(!completed)return;
            RestoreSave();
            Application.logMessageReceived-=OnLog;
            SessionState.EraseBool(PendingKey);
            EditorApplication.Exit(errors==0?0:1);
            return;
        }
        if(completed||EditorApplication.timeSinceStartup<readyAt)return;
        try{RunStep();}
        catch(Exception exception)
        {
            Write("FAIL: "+exception);
            errors++;
            Complete();
        }
    }

    static void RunStep()
    {
        string scene=SceneManager.GetActiveScene().name;
        switch(step)
        {
            case 0:
                if(scene!=GameSceneNames.MainMenu)return;
                var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuUI>();
                if(menu==null)return;
                Button loadGame=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(button=>button.name=="Load Game Button");
                Require(loadGame.gameObject.activeInHierarchy&&!loadGame.interactable,"Load Game must be visible and disabled when no save exists");
                Write("PASS: Main Menu loaded with the authored Load Game button disabled while no save exists.");
                PlayerPrefs.SetString(GamePersistence.SaveKey,JsonUtility.ToJson(new GameSaveData()));
                Require(GamePersistence.RequestLoad()&&GamePersistence.LoadRequested,"Load request fixture was not created");
                Button newGame=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(button=>button.name=="Start Game Button");
                newGame.onClick.Invoke();
                ButtonNamed("Confirm Start New Game").onClick.Invoke();
                step=1;Delay();return;
            case 1:
                if(scene!=GameSceneNames.Gameplay||CurrencyInventory.Instance==null||GameManager.Instance==null)return;
                Require(!GamePersistence.LoadRequested,"New Game left a pending Load Game request");
                Write("PASS: Main Menu -> New Game used the assigned button and cleared a stale load request.");
                var progression=GameManager.Instance.GetComponent<PlayerProgression>();
                int root=Enumerable.Range(0,PassiveTreeDefinition.NodeCount).First(PassiveTreeDefinition.IsRootConnected);
                var ranks=new int[PassiveTreeDefinition.NodeCount];ranks[root]=1;
                Require(progression.RestoreProgression(3,1d,1,ranks),"Could not seed rich progression fixture");
                var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                var skill=player.GetComponent<PlayerSkillController>();
                Require(skill.RestoreSelection(true,PlayerSkillId.Fireball),"Could not seed active skill fixture");
                var ring=new GearSnapshotData{id="integration-ring",type=LootManager.GearType.Rings,rarity=LootManager.GearRarity.Rare,itemLevel=17,element=Element.Fire,
                    mods=new List<RolledMod>{new(StatTypes.GenericDmg,2,11.5f,true)}}.Create("Integration Ring");
                var helmet=new GearSnapshotData{id="integration-helmet",type=LootManager.GearType.Helmets,rarity=LootManager.GearRarity.Magic,itemLevel=12,element=Element.Cold}.Create("Integration Helmet");
                Inventory.Instance.Add(ring);EquipmentManager.Instance.Equip(ring);Inventory.Instance.Add(helmet);
                CurrencyInventory.Instance.Add(CraftingCurrencyType.MagicToRare,23);
                CurrencyInventory.Instance.Add(CraftingCurrencyType.AncientReroll,4);
                var relic1=new RelicData{id="integration-relic-1",cycle=1,rarity=LootManager.GearRarity.Normal,craftableThisCycle=false,
                    modifiers=new List<RelicModifier>{new(RelicModifierType.MoreDamage,6f,true)}};
                var relic2=new RelicData{id="integration-relic-2",cycle=2,rarity=LootManager.GearRarity.Magic,craftableThisCycle=true,
                    modifiers=new List<RelicModifier>{new(RelicModifierType.IncreasedExperience,8f,true)}};
                RelicInventory.Instance.Restore(new List<RelicData>{relic1,relic2},2,new[]{0,-1,1,-1});
                Inventory.Instance.FilterLevelEnabled=true;Inventory.Instance.FilterLevel=31;
                var health=player.GetComponent<HealthComponent>();var mana=player.GetComponent<ManaComponent>();
                checkpointLife=Mathf.Max(1f,health.MaxLife*.72f);checkpointMana=mana.MaxMana*.63f;
                Require(health.RestoreCheckpointLife(checkpointLife)&&mana.RestoreCheckpointMana(checkpointMana),"Could not seed encounter checkpoint resources");
                GamePersistence.RecordEncounterStart(health,mana);health.LoseLife(Mathf.Min(10f,checkpointLife*.25f));mana.SpendUpTo(Mathf.Min(10f,checkpointMana*.25f));
                Require(CurrencyInventory.Instance.Arm(CraftingCurrencyType.AncientReroll),"Could not seed transient armed currency");
                var hud=UnityEngine.Object.FindFirstObjectByType<PaperBattleHUD>();hud.GetButton(TopHUDButtonKind.Pause).onClick.Invoke();
                Require(hud.PauseMenu.IsOpen,"Could not seed transient pause state");
                Require(GamePersistence.TrySave(),GamePersistence.LastError);
                hud.GetButton(TopHUDButtonKind.Play).onClick.Invoke();
                Require(GamePersistence.TryReadFile(GamePersistence.PrimaryPath,out var saved,out var saveError),saveError);savedRunId=saved.runId;
                ReturnToMenu();step=2;Delay();return;
            case 2:
                if(scene!=GameSceneNames.MainMenu)return;
                Write("PASS: gameplay -> Main Menu used DeathMenuUI and the enabled Main Menu scene.");
                Button savedLoadButton=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(button=>button.name=="Load Game Button");
                Require(savedLoadButton.gameObject.activeInHierarchy&&savedLoadButton.interactable,"Authored Load Game button was not available for the saved game");
                savedLoadButton.onClick.Invoke();
                step=3;Delay();return;
            case 3:
                if(scene!=GameSceneNames.Gameplay||CurrencyInventory.Instance==null)return;
                Require(!GamePersistence.LoadRequested,"Load request survived gameplay initialization");
                Require(CurrencyInventory.Instance.Count(CraftingCurrencyType.MagicToRare)==23,"Existing saved currency was not restored through the Load Game route");
                Require(CurrencyInventory.Instance.Count(CraftingCurrencyType.AncientReroll)==4&&!CurrencyInventory.Instance.ArmedCurrency.HasValue,"Ancient currency or transient armed intent restored incorrectly");
                var loadedProgression=GameManager.Instance.GetComponent<PlayerProgression>();
                Require(loadedProgression.Level==3&&loadedProgression.Experience==1d&&loadedProgression.AvailablePoints==1&&loadedProgression.CopyRanks().Count(x=>x==1)==1,"Player progression did not round-trip");
                var loadedPlayer=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                Require(loadedPlayer.GetComponent<PlayerSkillController>().SelectedSkill?.id==PlayerSkillId.Fireball,"Selected skill did not round-trip");
                Require(Inventory.Instance.Items.Any(x=>x.PersistentId=="integration-helmet")&&EquipmentManager.Instance.EquippedItems.Values.Any(x=>x.PersistentId=="integration-ring"),"Stable gear ownership did not round-trip");
                Require(RelicInventory.Instance.CurrentCycle==2&&RelicInventory.Instance.Relics.Count==2&&RelicInventory.Instance.Active(0)?.id=="integration-relic-1"&&RelicInventory.Instance.Active(2)?.id=="integration-relic-2","Relic history/slots did not round-trip");
                var loadedHealth=loadedPlayer.GetComponent<HealthComponent>();var loadedMana=loadedPlayer.GetComponent<ManaComponent>();
                Require(loadedHealth.CurrentLife>=checkpointLife-.01f&&loadedHealth.CurrentLife<checkpointLife+5f&&loadedMana.CurrentMana>=checkpointMana-.01f&&loadedMana.CurrentMana<checkpointMana+5f,"Encounter-start health/mana were not restored");
                Require(!UnityEngine.Object.FindFirstObjectByType<PaperBattleHUD>().PauseMenu.IsOpen&&Mathf.Approximately(Time.timeScale,1f),"Transient pause state was restored");
                Write("PASS: Main Menu -> Load Game restored the rich v2 schema and clean encounter checkpoint while clearing transient intent.");
                ReturnToMenu();step=4;Delay();return;
            case 4:
                if(scene!=GameSceneNames.MainMenu)return;
                string beforeCancel=File.ReadAllText(GamePersistence.PrimaryPath);
                ButtonNamed("Start Game Button").onClick.Invoke();
                Require(UnityEngine.Object.FindFirstObjectByType<MainMenuUI>().NewGameConfirmationVisible,"Existing save did not require overwrite confirmation");
                ButtonNamed("Cancel New Game").onClick.Invoke();
                Require(File.ReadAllText(GamePersistence.PrimaryPath)==beforeCancel,"Cancel changed the existing save");
                ButtonNamed("Start Game Button").onClick.Invoke();
                ButtonNamed("Confirm Start New Game").onClick.Invoke();
                step=5;Delay();return;
            case 5:
                if(scene!=GameSceneNames.Gameplay||GameManager.Instance==null)return;
                Require(!GamePersistence.LoadRequested,"Second New Game inherited a Load Game request");
                Require(GameManager.Instance.GetComponent<PlayerProgression>().Level==1&&Inventory.Instance.Items.Count==0&&CurrencyInventory.Instance.Stacks.Count==0&&RelicInventory.Instance.Relics.Count==0&&RelicInventory.Instance.CurrentCycle==0,"Confirmed New Game retained save-owned progress");
                Require(Inventory.Instance.FilterLevelEnabled&&Inventory.Instance.FilterLevel==31,"Confirmed New Game cleared independent filter preferences");
                Require(GamePersistence.TryReadFile(GamePersistence.PrimaryPath,out var fresh,out var freshError),freshError);
                Require(GamePersistence.TryReadFile(GamePersistence.BackupPath,out var backup,out var backupError),backupError);
                Require(fresh.runId!=savedRunId&&backup.runId==fresh.runId,"Confirmed New Game did not replace primary/backup with one fresh run identity");
                Require(errors==0,"Transition sequence logged runtime errors");
                Write("PASS: overwrite Cancel preserved the old run; Confirm cleared all game state, retained preferences, and replaced primary/backup with the new run.");
                Complete();return;
        }
    }

    static void ReturnToMenu()
    {
        var death=UnityEngine.Object.FindFirstObjectByType<DeathMenuUI>(FindObjectsInactive.Include);
        Require(death!=null,"Gameplay DeathMenuUI is missing");
        death.OnReturnToMainMenuClicked();
    }

    static void Delay()=>readyAt=EditorApplication.timeSinceStartup+.75;
    static Button ButtonNamed(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(button=>button.name==name);
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static void Write(string message){File.AppendAllText(Report,message+Environment.NewLine);Debug.Log(message);}
    static void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    static void Complete(){completed=true;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;}
    static void RestoreSave()
    {
        GamePersistence.RequestNewGame();
        if(hadSave)PlayerPrefs.SetString(GamePersistence.SaveKey,savedValue);
        else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
        if(hadFilter)PlayerPrefs.SetString(FilterPreferenceKey,savedFilterValue);
        else PlayerPrefs.DeleteKey(FilterPreferenceKey);
        PlayerPrefs.Save();
        GamePersistence.SaveDirectoryOverride=null;
        if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);
    }
}
