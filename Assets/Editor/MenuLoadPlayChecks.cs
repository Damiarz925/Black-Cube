// End-to-end Step 3 smoke check for repeated Main Menu and gameplay transitions.
using System;
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
    static int step,errors;
    static double readyAt;
    static bool completed,hadSave;
    static string savedValue;

    static MenuLoadPlayChecks()
    {
        EditorApplication.update-=Tick;
        EditorApplication.update+=Tick;
    }

    [MenuItem("Black Cube/Play Checks/Verify Main Menu and Load Game")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report,string.Empty);
        hadSave=PlayerPrefs.HasKey(GamePersistence.SaveKey);
        savedValue=hadSave?PlayerPrefs.GetString(GamePersistence.SaveKey):null;
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
                step=1;Delay();return;
            case 1:
                if(scene!=GameSceneNames.Gameplay||CurrencyInventory.Instance==null||GameManager.Instance==null)return;
                Require(!GamePersistence.LoadRequested,"New Game left a pending Load Game request");
                Write("PASS: Main Menu -> New Game used the assigned button and cleared a stale load request.");
                CurrencyInventory.Instance.Restore(Array.Empty<CurrencyStackData>());
                CurrencyInventory.Instance.Add(CraftingCurrencyType.MagicToRare,23);
                GamePersistence.Save();
                CurrencyInventory.Instance.Restore(Array.Empty<CurrencyStackData>());
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
                Write("PASS: Main Menu -> Load Game restored current save data and cleared the one-shot request without prestige.");
                ReturnToMenu();step=4;Delay();return;
            case 4:
                if(scene!=GameSceneNames.MainMenu)return;
                UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(button=>button.name=="Start Game Button").onClick.Invoke();
                step=5;Delay();return;
            case 5:
                if(scene!=GameSceneNames.Gameplay||GameManager.Instance==null)return;
                Require(!GamePersistence.LoadRequested,"Second New Game inherited a Load Game request");
                Require(errors==0,"Transition sequence logged runtime errors");
                Write("PASS: repeated Main Menu -> New Game -> Main Menu -> Load Game -> Main Menu -> New Game sequence completed without a stale request.");
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
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static void Write(string message){File.AppendAllText(Report,message+Environment.NewLine);Debug.Log(message);}
    static void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    static void Complete(){completed=true;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;}
    static void RestoreSave()
    {
        GamePersistence.RequestNewGame();
        if(hadSave)PlayerPrefs.SetString(GamePersistence.SaveKey,savedValue);
        else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
        PlayerPrefs.Save();
    }
}
