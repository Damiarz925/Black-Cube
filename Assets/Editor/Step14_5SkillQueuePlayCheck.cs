using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>One real-scene smoke check for next-attack skill replacement.</summary>
[InitializeOnLoad]
public static class Step14_5SkillQueuePlayCheck
{
    const string ActiveKey="BlackCube.Step14_5.SkillQueueCheck";
    const string ExitKey="BlackCube.Step14_5.SkillQueueExit";
    const string Report="Logs/Step14_5-skill-queue-play-check.txt";
    static double readyAt;

    static Step14_5SkillQueuePlayCheck()
    {
        EditorApplication.playModeStateChanged-=OnPlayModeChanged;
        EditorApplication.playModeStateChanged+=OnPlayModeChanged;
    }

    public static void Run()
    {
        Directory.CreateDirectory("Logs");File.WriteAllText(Report,"Real-scene queued-skill check\n");
        SessionState.SetBool(ActiveKey,true);SessionState.SetInt(ExitKey,0);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(ActiveKey,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){readyAt=EditorApplication.timeSinceStartup+2;EditorApplication.update-=RunWhenReady;EditorApplication.update+=RunWhenReady;}
        else if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(ActiveKey,false);EditorApplication.Exit(SessionState.GetInt(ExitKey,1));}
    }

    static void RunWhenReady()
    {
        if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<readyAt)return;
        EditorApplication.update-=RunWhenReady;
        try
        {
            var battle=BattleManager.Instance??throw new InvalidOperationException("BattleManager missing.");
            var skills=UnityEngine.Object.FindAnyObjectByType<PlayerSkillController>()??throw new InvalidOperationException("PlayerSkillController missing.");
            var selected=skills.SelectedSkill??skills.Skills.FirstOrDefault()??throw new InvalidOperationException("No active skill definition.");
            if(skills.SelectedSkill==null&&!skills.TrySelect(selected))throw new InvalidOperationException("Could not select fixture skill.");
            var gauge=typeof(BattleManager).GetField("playerGauge",BindingFlags.Instance|BindingFlags.NonPublic);
            float beforeGauge=(float)gauge.GetValue(battle),beforeMana=skills.Mana.CurrentMana;
            if(!skills.TryQueueSelected())throw new InvalidOperationException("Scene skill did not queue.");
            if(!Mathf.Approximately((float)gauge.GetValue(battle),beforeGauge)||!Mathf.Approximately(skills.Mana.CurrentMana,beforeMana))throw new InvalidOperationException("Queue changed gauge or Mana early.");
            typeof(BattleManager).GetMethod("ResolvePlayerTurn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,null);
            if(skills.HasQueuedSkill||skills.Mana.CurrentMana>=beforeMana)throw new InvalidOperationException("Scheduled resolution did not clear queue and spend Mana.");
            File.AppendAllText(Report,"PASS queue left gauge/Mana untouched and resolved once at the real scene attack authority.\n");
            SessionState.SetInt(ExitKey,0);
        }
        catch(Exception exception){File.AppendAllText(Report,"FAIL "+exception+"\n");SessionState.SetInt(ExitKey,1);}
        EditorApplication.isPlaying=false;
    }
}
