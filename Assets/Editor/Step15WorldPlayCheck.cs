using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Step15WorldPlayCheck
{
    const string ActiveKey="BlackCube.Step15.WorldSmoke.Active";
    const string StateKey="BlackCube.Step15.WorldSmoke.State";
    const string ExitKey="BlackCube.Step15.WorldSmoke.Exit";
    const string HadPrefsKey="BlackCube.Step15.WorldSmoke.HadPrefs";
    const string PrefsValueKey="BlackCube.Step15.WorldSmoke.PrefsValue";
    const string Report="Logs/Step15-world-play-check.txt";
    static string SaveDirectory=>Path.GetFullPath("Temp/Step15WorldPlayCheckSave");
    static double readyAt;

    static Step15WorldPlayCheck()
    {
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        if(SessionState.GetBool(ActiveKey,false))GamePersistence.SaveDirectoryOverride=SaveDirectory;
    }

    public static void Run()
    {
        Directory.CreateDirectory("Logs");File.WriteAllText(Report,"Step 15 gameplay progression and menu/load smoke\n");
        if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);Directory.CreateDirectory(SaveDirectory);
        GamePersistence.SaveDirectoryOverride=SaveDirectory;GamePersistence.ResetStaticStateForTests();
        bool had=PlayerPrefs.HasKey(GamePersistence.SaveKey);SessionState.SetBool(HadPrefsKey,had);
        SessionState.SetString(PrefsValueKey,had?PlayerPrefs.GetString(GamePersistence.SaveKey):string.Empty);
        PlayerPrefs.DeleteKey(GamePersistence.SaveKey);PlayerPrefs.Save();GamePersistence.RequestNewGame();
        SessionState.SetBool(ActiveKey,true);SessionState.SetInt(StateKey,0);SessionState.SetInt(ExitKey,1);
        readyAt=0;EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity");EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        if(!SessionState.GetBool(ActiveKey,false))return;
        if(!EditorApplication.isPlaying)
        {
            if(SessionState.GetInt(StateKey,0)<4)return;
            Cleanup();EditorApplication.Exit(SessionState.GetInt(ExitKey,1));return;
        }
        if(EditorApplication.timeSinceStartup<readyAt)return;
        readyAt=EditorApplication.timeSinceStartup+.5;
        try{RunState();}catch(Exception exception){Write("FAIL "+exception);SessionState.SetInt(StateKey,4);EditorApplication.isPlaying=false;}
    }

    static void RunState()
    {
        int state=SessionState.GetInt(StateKey,0);Scene scene=SceneManager.GetActiveScene();
        if(state==0&&scene.name==GameSceneNames.MainMenu)
        {
            Button load=ButtonNamed("Load Game Button");Require(!load.interactable,"Cold menu unexpectedly enabled Load.");
            Write("PASS cold Main Menu exposed New Game and correctly disabled Load without a save.");
            SessionState.SetInt(StateKey,1);ButtonNamed("Start Game Button").onClick.Invoke();readyAt=EditorApplication.timeSinceStartup+1;return;
        }
        if(state==1&&scene.name==GameSceneNames.Gameplay&&GameManager.Instance!=null&&BattleManager.Instance!=null)
        {
            ValidateWorld(1,0,0,0);GameManager.Instance.StartZone(61);ValidateWorld(61,1,0,0);
            Require(GamePersistence.TrySave(),"Could not save level-61 fixture.");
            Write("PASS gameplay scene resolved levels 1 and 61 through definitions and saved level 61.");
            SessionState.SetInt(StateKey,2);SceneManager.LoadScene(GameSceneNames.MainMenu);readyAt=EditorApplication.timeSinceStartup+1;return;
        }
        if(state==2&&scene.name==GameSceneNames.MainMenu)
        {
            Button load=ButtonNamed("Load Game Button");Require(load.interactable,"Menu did not enable Load for Step 15 fixture.");
            Write("PASS returned Main Menu recognized the new save.");SessionState.SetInt(StateKey,3);
            load.onClick.Invoke();readyAt=EditorApplication.timeSinceStartup+1;return;
        }
        if(state==3&&scene.name==GameSceneNames.Gameplay&&GameManager.Instance!=null&&BattleManager.Instance!=null)
        {
            Require(GameManager.Instance.CurrentCombatLevel==61,"Load did not restore combat level 61.");ValidateWorld(61,1,0,0);
            Write("PASS Load restored combat level 61 and derived the same biome/location/corruption encounter.");
            SessionState.SetInt(ExitKey,0);SessionState.SetInt(StateKey,4);EditorApplication.isPlaying=false;
        }
    }

    static void ValidateWorld(int level,int biome,int location,int corruption)
    {
        ZoneManager zone=UnityEngine.Object.FindFirstObjectByType<ZoneManager>()??throw new InvalidOperationException("ZoneManager missing.");
        WorldPosition world=zone.CurrentWorldPosition;
        Require(GameManager.Instance.CurrentCombatLevel==level,$"Expected combat level {level}.");
        Require(world.BiomeIndex==biome&&world.LocationIndex==location&&world.CorruptionIndex==corruption,"World position mismatch.");
        Require(BattleManager.Instance.CurrentEncounter?.kind==EncounterKind.Normal,"Scene did not spawn the resolved normal encounter.");
        var hud=UnityEngine.Object.FindFirstObjectByType<PaperBattleHUD>()??throw new InvalidOperationException("HUD missing.");
        typeof(PaperBattleHUD).GetMethod("RefreshRunText",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(hud,null);
        TMP_Text text=hud.runText;
        Require(text!=null&&text.text.Contains(world.BiomeLabel.ToUpperInvariant())&&text.text.Contains(world.CorruptionLabel.ToUpperInvariant()),
            $"HUD lacks authoritative biome/corruption labels: '{text?.text ?? "missing"}'.");
    }

    static Button ButtonNamed(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(x=>x.name==name);
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static void Write(string message){File.AppendAllText(Report,message+Environment.NewLine);Debug.Log(message);}
    static void Cleanup()
    {
        if(SessionState.GetBool(HadPrefsKey,false))PlayerPrefs.SetString(GamePersistence.SaveKey,SessionState.GetString(PrefsValueKey,string.Empty));
        else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);PlayerPrefs.Save();
        GamePersistence.RequestNewGame();GamePersistence.SaveDirectoryOverride=null;
        if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);
        SessionState.SetBool(ActiveKey,false);
    }
}
