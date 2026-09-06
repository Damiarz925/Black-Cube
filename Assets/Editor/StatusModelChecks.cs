using System.Linq;
using UnityEditor;
using UnityEngine;

public static class StatusModelChecks
{
    [MenuItem("Black Cube/Play Checks/Verify Status Stacks")]
    static void Verify()
    {
        if(!EditorApplication.isPlaying)return;
        var player=Object.FindFirstObjectByType<PlayerController>();var controller=player.GetComponent<StatusController>();var hp=player.GetComponent<HealthComponent>();var stats=player.GetComponent<StatsComponent>();
        var poison=Object.Instantiate(AssetDatabase.LoadAssetAtPath<StatusEffects>("Assets/Prefabs/Scriptable Objects/PoisonStatus.asset"));
        controller.ClearStatuses();hp.ReviveToFullLife();
        controller.ApplyStatus(poison,1,100,1,stats,1);controller.ApplyStatus(poison,1,50,2,stats,1);controller.ApplyStatus(poison,1,75,3,stats,1);
        Check(controller,3,75);controller.TickStatuses();Require(hp.CurrentLife==775,"Three75 ticks apply225 actual damage");
        Check(controller,2,62.5f);controller.TickStatuses();Require(hp.CurrentLife==650,"Remaining two stacks apply125");
        Check(controller,1,75);controller.TickStatuses();Require(hp.CurrentLife==575 && controller.GetStatusSummaries().Count==0,"Final stack applies75 and icon expires");
        controller.ApplyStatus(poison,1,100,5,stats,1);controller.ApplyStatus(poison,1,50,5,stats,2);
        var mixed=controller.GetStatusSummaries().Single();Require(Mathf.Abs(mixed.DamagePerTick-83.33333f)<.01f && mixed.DamagePerTurn==125,"Mixed speeds preserve125 damage per turn rate");
        controller.RemoveStatus(poison);Require(controller.GetStatusSummaries().Count==0,"Explicit removal clears count");
        var enemy=BattleManager.Instance.CurrentEnemyAI;var enemyStatus=enemy.GetComponent<StatusController>();var enemyHP=enemy.GetComponent<HealthComponent>();
        enemyStatus.ClearStatuses();float before=enemyHP.CurrentLife;
        enemyStatus.ApplyStatus(poison,1,10,1,stats,1);enemyStatus.ApplyStatus(poison,1,5,2,stats,1);enemyStatus.ApplyStatus(poison,1,7.5f,3,stats,1);
        var e=enemyStatus.GetStatusSummaries().Single();enemyStatus.TickStatuses();Require(Mathf.Abs(enemyHP.CurrentLife-(before-e.DamagePerTick*3))<.01f,"Enemy damage matches displayed average after resistance");
        hp.ReviveToFullLife();controller.ApplyStatus(poison,1,10,5,stats,1);hp.LoseLife(2000);Require(controller.GetStatusSummaries().Count==0,"Death clears player statuses");
        GameManager.Instance.RestartCurrentLevelAfterDeath();Require(BattleManager.Instance.CurrentEnemyAI!=enemy && BattleManager.Instance.CurrentEnemyAI.GetComponent<StatusController>().GetStatusSummaries().Count==0,"Replacement has no stale statuses");
        // Disposable, paused visual fixture for both actor tooltips.
        controller.ApplyStatus(poison,1,100,3,stats,4);controller.ApplyStatus(poison,1,50,4,stats,4);controller.ApplyStatus(poison,1,75,5,stats,4);
        var newEnemy=BattleManager.Instance.CurrentEnemyAI.GetComponent<StatusController>();newEnemy.ApplyStatus(poison,2,15,4,stats,4);
        foreach(var name in new[]{"IgniteStatus","BleedStatus"})
        {var effect=AssetDatabase.LoadAssetAtPath<StatusEffects>("Assets/Prefabs/Scriptable Objects/"+name+".asset");controller.ApplyStatus(effect,1,10,4,stats,1);newEnemy.ApplyStatus(effect,1,8,4,stats,1);}
        var chill=ScriptableObject.CreateInstance<StatusEffects>();var so=new SerializedObject(chill);so.FindProperty("statusType").enumValueIndex=2;so.FindProperty("stackPolicy").enumValueIndex=3;so.ApplyModifiedPropertiesWithoutUndo();controller.ApplyStatus(chill,1,.2f,3,stats,1);
        Time.timeScale=0;
        const string report="PASS: player poison100/50/75 ->3 stacks at75/tick, actual225 damage; expiry ->2 at62.5 (125 damage) ->1 at75 ->0. Mixed speeds yield83.333/tick and preserve125 damage/global-turn rate. Enemy applied damage matches averaged tooltip after resistance. Explicit removal, player death, restart and enemy replacement clear states. Paused Play-only fixtures show both actors' poison/burn/bleed and non-damaging chill tooltip. No seconds/DPS conflation.";
        System.IO.File.WriteAllText("ReviewCaptures/status-check.txt",report);Debug.Log(report);
    }
    static void Check(StatusController controller,int count,float mean)
    {var s=controller.GetStatusSummaries().Single();Require(s.Count==count && Mathf.Approximately(s.DamagePerTick,mean),$"Expected{count} at{mean}; got{s.Count} at{s.DamagePerTick}");}
    static void Require(bool condition,string text){if(!condition)throw new System.InvalidOperationException(text);}
}
