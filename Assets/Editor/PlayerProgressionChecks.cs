using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PlayerProgressionChecks
{
    const string Key = "BlackCube.XPChecks";
    const string Report = "ReviewCaptures/player-progression-check.txt";
    static IEnumerator routine;
    static double ready;
    static int errors;
    static PlayerProgressionChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false)) return;
            SessionState.SetBool(Key,false);
            ready=EditorApplication.timeSinceStartup+2;
            routine=SessionState.GetBool(Key+"UI",false) ? Preview() : Run();
            SessionState.SetBool(Key+"UI",false);
            Application.logMessageReceived += Log;
        };
        EditorApplication.update += () =>
        {
            if(routine==null || EditorApplication.timeSinceStartup<ready)return;
            try { if(routine.MoveNext()) return; }
            catch(Exception e) { Write("FAIL: "+e); }
            routine=null;Time.timeScale=0;
            Write("Fixture paused for UI review; exit Play to discard.");
        };
    }
    [MenuItem("Black Cube/Play Checks/Verify XP Skills and Combat")]
    static void Begin()
    {
        if(EditorApplication.isPlaying)return;
        File.WriteAllText(Report,"Player progression integration\n");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [MenuItem("Black Cube/Play Checks/Preview Skill Screen _F10")]
    static void PreviewBegin()
    {
        if(EditorApplication.isPlaying)return;
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+"UI",true);EditorApplication.isPlaying=true;
    }
    static IEnumerator Preview()
    {
        Time.timeScale=0;
        GameManager.Instance.GetComponent<PlayerProgression>().AddExperience(100);
        var ui=Object.FindFirstObjectByType<SkillTreeUI>();ui.Toggle();
        var player=Object.FindFirstObjectByType<PlayerController>().GetComponent<HealthComponent>();
        float hp=player.CurrentLife,enemyHP=Enemy.CurrentLife;
        Time.timeScale=1;
        double end=EditorApplication.timeSinceStartup+2;
        while(EditorApplication.timeSinceStartup<end)yield return null;
        Check(player.CurrentLife==hp && Enemy.CurrentLife==enemyHP,"Skill panel pauses live combat without time-scale freeze");
        Write("PASS skill pause: 2 seconds at Time.timeScale=1 with no actor damage while screen open.");
    }
    static void Log(string message,string trace,LogType type)
    {if(type==LogType.Error || type==LogType.Exception){errors++;Write("CONSOLE: "+message);}}
    static void Write(string text){File.AppendAllText(Report,text+"\n");Debug.Log(text);}
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
    static HealthComponent Enemy => BattleManager.Instance.CurrentEnemyAI.GetComponent<HealthComponent>();
    static void Turn(string method) => typeof(BattleManager).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(BattleManager.Instance,null);
    static IEnumerator Run()
    {
        Time.timeScale=0;
        var game=GameManager.Instance;var xp=game.GetComponent<PlayerProgression>();
        var player=Object.FindFirstObjectByType<PlayerController>();var hp=player.GetComponent<HealthComponent>();var stats=player.GetComponent<StatsComponent>();
        xp.ResetProgression();hp.RestoreFullLife();game.StartZone(1);
        yield return null;
        Check(hp.MaxLife==1000 && player.EquippedWeaponBaseDamage==80,"Actual baseline health/weapon");
        hp.LoseLife(500);
        var dead=Enemy;dead.LoseLife(dead.MaxLife+1);
        Check(xp.Experience==10 && xp.Level==1 && hp.CurrentLife==500,"Partial XP does not heal");
        game.OnEnemyKilled(dead,dead.IsBoss);
        Check(xp.Experience==10,"Duplicate death XP");
        Enemy.LoseLife(Enemy.MaxLife+1);Enemy.LoseLife(Enemy.MaxLife+1);
        Check(xp.Level==2 && xp.AvailablePoints==1 && xp.Experience==0 && hp.CurrentLife==1000,"Third kill levels and heals");
        Check(!xp.TrySpend(3) && !xp.TrySpend(-1) && !xp.TrySpend(6),"Invalid/locked nodes rejected");
        Check(xp.TrySpend(0) && xp.AvailablePoints==0 && hp.MaxLife==1100 && hp.CurrentLife==1000,"Life node adds max only");
        Check(!xp.TrySpend(0),"No overspending");
        xp.AddExperience(100);
        Check(xp.Level==4 && xp.Experience==34 && xp.AvailablePoints==2 && hp.CurrentLife==1100,"Multi-level carryover");
        Check(xp.TrySpend(1) && xp.TrySpend(2),"Damage/speed node purchase");
        Check(Mathf.Approximately(stats.GetStat(StatTypes.GenericDmg),.15f) && Mathf.Approximately(player.GetFinalAttackSpeed(),1.26f),"Skills feed combat stats");
        double before=xp.Experience;int lvl=xp.Level;
        xp.AddExperience(double.NaN);xp.AddExperience(double.PositiveInfinity);xp.AddExperience(-1);
        Check(xp.Experience==before && xp.Level==lvl,"Invalid XP rejected");
        foreach(int stage in new[]{1,5})
        {
            game.StartZone(stage);for(int i=0;i<10;i++)Enemy.LoseLife(Enemy.MaxLife+1);
            int saved=xp.Level,points=xp.AvailablePoints;double savedXP=xp.Experience;
            hp.LoseLife(hp.MaxLife+1);game.RestartCurrentLevelAfterDeath();Time.timeScale=0;
            Check(game.CurrentCombatLevel==stage && game.NormalKills==0 && !game.BossActive && !Enemy.IsBoss && hp.CurrentLife==hp.MaxLife,"Restart stage "+stage);
            Check(xp.Level==saved && xp.Experience==savedXP && xp.AvailablePoints==points && xp.Rank(0)==1,"Restart preserves XP and skills");
            Enemy.LoseLife(Enemy.MaxLife+1);Check(game.NormalKills==1,"Restart first normal");
        }
        xp.AddExperience(1e12);Check(xp.Level==100 && xp.Experience==0,"Cap and overflow");
        int capPoints=xp.AvailablePoints;xp.AddExperience(1e12);Check(xp.AvailablePoints==capPoints,"No extra cap points");
        for(int i=1;i<5;i++)Check(xp.TrySpend(0),"Rank purchase");
        Check(!xp.TrySpend(0) && xp.Rank(0)==5,"Rank cap");
        Write("PASS XP: one award per death, third kill level/full heal, point spend, prerequisites, max ranks, real combat stat modifiers, multi-level carryover, malformed XP, level100 cap, Forest1/5 boss restart preserves progression and resets normal count0->1.");

        xp.ResetProgression();hp.RestoreFullLife();game.StartZone(1);Time.timeScale=0;yield return null;
        var poison=AssetDatabase.LoadAssetAtPath<StatusEffects>("Assets/Prefabs/Scriptable Objects/PoisonStatus.asset");
        stats.SetBaseStat(StatTypes.PoisonChance,100);
        Enemy.LoseLife(Enemy.CurrentLife-1);
        Turn("ResolvePlayerTurn");
        Check(Enemy.GetComponent<StatusController>().GetStatusSummaries().Count==0 && Enemy.CurrentLife==Enemy.MaxLife,"Lethal hit original-target binding");
        yield return null;
        var old=Enemy;
        old.GetComponent<StatusController>().ApplyStatus(poison,1,100000,1,stats,1);
        Turn("ResolvePlayerTurn");
        Check(Enemy!=old && Enemy.CurrentLife==Enemy.MaxLife && Enemy.GetComponent<StatusController>().GetStatusSummaries().Count==0,"Lethal DOT cannot hit replacement");
        yield return null;
        old=Enemy;float life=hp.CurrentLife;
        old.GetComponent<StatusController>().ApplyStatus(poison,1,100000,1,stats,1);
        Turn("ResolveEnemyTurn");
        Check(Enemy!=old && hp.CurrentLife>=life,"DOT-killed enemy cannot attack");
        player.GetComponent<StatusController>().ApplyStatus(poison,1,100000,1,stats,1);
        old=Enemy;float enemyLife=old.CurrentLife;Turn("ResolvePlayerTurn");
        Check(hp.CurrentLife==0 && Enemy==old && Enemy.CurrentLife==enemyLife,"DOT-killed player cannot attack");
        Write("PASS combat lifecycle: lethal hit has no successor statuses; lethal enemy DOT stops either turn before successor hit/attack; lethal player DOT stops attack.");

        game.RestartCurrentLevelAfterDeath();xp.ResetProgression();game.StartZone(1);hp.RestoreFullLife();
        stats.SetBaseStat(StatTypes.PoisonChance,0);player.GetComponent<StatusController>().ClearStatuses();
        yield return null;
        int loot=Inventory.Instance.Items.Count,kills=0;var previous=Enemy;
        float minLife=hp.CurrentLife;int previousLevel=xp.Level,heals=0;
        double end=EditorApplication.timeSinceStartup+120;
        Time.timeScale=12;
        while(kills<33 && EditorApplication.timeSinceStartup<end)
        {
            if(hp.CurrentLife<=0)throw new Exception("Natural combat death at stage "+game.CurrentCombatLevel);
            minLife=Mathf.Min(minLife,hp.CurrentLife);
            if(xp.Level>previousLevel){heals+=xp.Level-previousLevel;previousLevel=xp.Level;}
            if(Enemy!=previous)
            {
                kills++;previous=Enemy;
                Check(game.CurrentCombatLevel==1+kills/11 && game.NormalKills==kills%11,"Natural cadence "+kills);
                Check(Enemy.IsBoss==(kills%11==10),"Natural boss cadence");
                Check(Enemy.GetComponent<StatusController>().GetStatusSummaries().Count==0,"Natural replacement status");
            }
            yield return null;
        }
        Time.timeScale=0;
        Check(kills==33,"Natural combat timeout");
        Check(Inventory.Instance.Items.Count-loot==33,"One reward per natural kill");
        Write($"PASS natural combat: 33 kills, 3 consecutive stages, zero deaths, {heals} level-up heals; lowest observed HP={minLife:0.0}/1000; level={xp.Level}; no health/damage overrides; 12x wall-time acceleration only.");
        Time.timeScale=1;double settle=EditorApplication.timeSinceStartup+1;
        while(EditorApplication.timeSinceStartup<settle)yield return null;
        Time.timeScale=0;
        Check(Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length==1 && errors==0,"Cleanup actors/errors");
        Write("PASS cleanup: one enemy after delayed destruction, 33 rewards for 33 kills, no new errors.");
        var ui=Object.FindFirstObjectByType<SkillTreeUI>();ui.Toggle();
        yield return null;
        Check(SkillTreeUI.IsOpen && ui.Panel.activeSelf,"Skill screen opens");
        int available=xp.AvailablePoints;ui.NodeButton(0).onClick.Invoke();
        Check(xp.AvailablePoints==available-1 && xp.Rank(0)==1,"UI button spends point");
        Write("PASS skill screen: opens, pauses battle, real button spends a point and updates rank; paused visual fixture ready.");
    }
}
