using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Step19WorldPlayCheck
{
    const string Active="BlackCube.Step19.WorldSmoke.Active",State="BlackCube.Step19.WorldSmoke.State";
    const string HadPrefs="BlackCube.Step19.WorldSmoke.HadPrefs",PrefsValue="BlackCube.Step19.WorldSmoke.PrefsValue";
    const string Report="Logs/Step19-world-play-check.txt";static double readyAt;
    static string SaveDirectory=>Path.GetFullPath("Temp/Step19WorldPlayCheckSave");
    static Step19WorldPlayCheck(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;if(SessionState.GetBool(Active,false))GamePersistence.SaveDirectoryOverride=SaveDirectory;}
    public static void Run()
    {
        Directory.CreateDirectory("Logs");File.WriteAllText(Report,"Step 19 production world real-scene smoke\n");
        if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);Directory.CreateDirectory(SaveDirectory);GamePersistence.SaveDirectoryOverride=SaveDirectory;GamePersistence.ResetStaticStateForTests();
        bool had=PlayerPrefs.HasKey(GamePersistence.SaveKey);SessionState.SetBool(HadPrefs,had);SessionState.SetString(PrefsValue,had?PlayerPrefs.GetString(GamePersistence.SaveKey):string.Empty);
        GamePersistence.RequestNewGame();PlayerPrefs.DeleteKey(GamePersistence.SaveKey);PlayerPrefs.Save();
        SessionState.SetBool(Active,true);SessionState.SetInt(State,0);readyAt=0;
        EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity");EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false))return;if(!EditorApplication.isPlaying){if(SessionState.GetInt(State,0)>=2){int exit=SessionState.GetInt(State,0)==2?0:1;Cleanup();EditorApplication.Exit(exit);}return;}
        if(EditorApplication.timeSinceStartup<readyAt)return;readyAt=EditorApplication.timeSinceStartup+.5;
        try{RunState();}catch(Exception ex){Write("FAIL "+ex);SessionState.SetInt(State,3);EditorApplication.isPlaying=false;}
    }
    static void RunState()
    {
        int state=SessionState.GetInt(State,0);var scene=SceneManager.GetActiveScene();
        if(state==0&&scene.name==GameSceneNames.MainMenu)
        {
            var start=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(x=>x.name=="Start Game Button");start.onClick.Invoke();
            var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuUI>();Require(menu!=null&&menu.SelectClass(PlayerClassIds.Warrior),"Class selection failed.");menu.StartSelectedClass();SessionState.SetInt(State,1);readyAt=EditorApplication.timeSinceStartup+1;return;
        }
        if(state!=1||scene.name!=GameSceneNames.Gameplay||GameManager.Instance==null||BattleManager.Instance==null)return;
        var db=WorldContentCatalog.Reference;int[] levels={1,61,121,181,241,301};
        foreach(int level in levels){GameManager.Instance.StartZone(level);var actor=BattleManager.Instance.CurrentEnemyAI;var world=WorldProgression.Resolve(level,1,db);Require(actor!=null&&actor.ContentId==world.Encounter.enemyArchetypeId,$"Biome spawn mismatch at {level}.");Require(actor.BeginAuthoredTurn()!=null,$"No authored skill at {level}.");Write($"PASS biome {world.BiomeIndex+1}: {actor.ContentDisplayName} / {actor.ActiveAuthoredSkill.displayName}");}
        int eliteLevel=Enumerable.Range(1,360).First(x=>db.Enemy(WorldProgression.Resolve(x,1,db).Encounter.enemyArchetypeId)?.rank==EnemyContentRank.Elite);GameManager.Instance.StartZone(eliteLevel);Require(BattleManager.Instance.CurrentEnemyAI.ContentId==WorldProgression.Resolve(eliteLevel,1,db).Encounter.enemyArchetypeId,"Elite spawn mismatch.");Write("PASS elite production actor spawned in real scene.");
        foreach(int level in new[]{1,50,60,100}){Require(GameManager.Instance.RestoreRunState(level,9,true),$"Boss restore failed at {level}.");var world=WorldProgression.Resolve(level,10,db);var boss=db.Boss(world.Encounter.bossId);Require(BattleManager.Instance.CurrentEnemyAI.ContentId==boss.stableId,"Boss identity mismatch.");Require(db.BossPhase(boss.phaseProfileId)?.phases.Count>=2,"Boss phase profile missing.");Write($"PASS boss smoke level={level} corruption={world.Corruption.percentage}% boss={boss.displayName}");}
        var story=db.Boss(WorldProgression.Resolve(100,10,db).Encounter.bossId);Require(story.futureStoryFlags.Contains(PlayerIdentityState.StoryCompletionMilestoneId),"Story boss milestone missing.");var identity=GameManager.Instance.GetComponent<PlayerIdentityState>();Require(identity.CompleteMilestone(PlayerIdentityState.StoryCompletionMilestoneId)&&identity.SubclassChoiceUnlocked,"Story/subclass unlock failed.");
        var challenge=db.challengeEncounters[0];var service=new ChallengeContentService();Require(service.RollEntryResource(db,challenge.minimumCombatLevel,new SequenceLootRandomSource(19,0f,0f),1f)==challenge.entryResourceId,"Challenge key drop failed.");Require(service.TryEnter(challenge)&&service.Complete(challenge),"Challenge entry/reward failed.");service.Add(challenge.entryResourceId,1);Require(service.TryEnter(challenge),"Challenge repeatability failed.");Write("PASS challenge entry, reward and repeatability smoke.");
        Require(GamePersistence.TrySave(),"Scene save failed.");Require(GameManager.Instance.RestoreRunState(301,0,false),"Clean encounter restore failed.");Write("PASS scene save and clean encounter restoration.");
        Write("PASS Step 19 representative real-scene smoke.");SessionState.SetInt(State,2);EditorApplication.isPlaying=false;
    }
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static void Write(string message){File.AppendAllText(Report,message+Environment.NewLine);Debug.Log(message);}
    static void Cleanup()
    {
        if(SessionState.GetBool(HadPrefs,false))PlayerPrefs.SetString(GamePersistence.SaveKey,SessionState.GetString(PrefsValue,string.Empty));else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);PlayerPrefs.Save();
        GamePersistence.RequestNewGame();GamePersistence.SaveDirectoryOverride=null;if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);SessionState.SetBool(Active,false);
    }
}
