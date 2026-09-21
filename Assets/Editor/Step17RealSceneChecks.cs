// Focused Step 17 production-scene smoke. It intentionally avoids balance simulation.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Step17RealSceneChecks
{
    const string Pending="BlackCube.Step17.RealScene.Pending";
    const string Report="Logs/Step17RealSceneSmoke.txt";
    static string SaveDirectory=>Path.GetFullPath("Temp/Step17RealSceneSave");
    static int step,errors;static double readyAt;static bool complete;static HealthComponent projectileTarget;static float targetLife;
    static Step17RealSceneChecks(){if(SessionState.GetBool(Pending,false))GamePersistence.SaveDirectoryOverride=SaveDirectory;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}

    [MenuItem("Black Cube/Play Checks/Step 17 Real Scene Smoke")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs");File.WriteAllText(Report,string.Empty);if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);Directory.CreateDirectory(SaveDirectory);
        GamePersistence.SaveDirectoryOverride=SaveDirectory;GamePersistence.ResetStaticStateForTests();GamePersistence.RequestConfirmedNewGame(1);GameLaunchSelection.SelectNewGameClass(PlayerClassIds.Ranger);
        step=errors=0;complete=false;readyAt=0;SessionState.SetBool(Pending,true);Application.logMessageReceived+=OnLog;EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        if(!EditorApplication.isPlaying){if(!complete)return;Cleanup();EditorApplication.Exit(errors==0?0:1);return;}
        if(complete||EditorApplication.timeSinceStartup<readyAt)return;
        try{RunStep();}catch(Exception ex){Write("FAIL: "+ex);errors++;Finish();}
    }

    static void RunStep()
    {
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();var manager=BattleManager.Instance;var game=GameManager.Instance;if(player==null||manager==null||game==null)return;
        if(step==0)
        {
            var identity=game.GetComponent<PlayerIdentityState>();var progression=game.GetComponent<PlayerProgression>();Require(identity.BeginNewGame(PlayerClassIds.Ranger)&&identity.BaseClassId==PlayerClassIds.Ranger,"Ranger class start could not be selected in the production scene fixture");
            int target=PassiveTreeDefinition.Nodes.First(x=>x.WeaponTypeRestriction==WeaponTypeIds.Bow&&x.Effects.Length>0).Id;var path=PathFromStart(PlayerClassIds.Ranger,target);var ranks=new int[PassiveTreeDefinition.NodeCount];foreach(int id in path)ranks[id]=1;Require(path.Count<100&&progression.RestoreProgression(100,0,100-path.Count,ranks),"Could not allocate the production Ranger-to-Bow passive route");Require(progression.IsAllocated(target),"Bow district target was not allocated");
            var stats=player.GetComponent<StatsComponent>();var sword=Weapon(player,WeaponTypeIds.Sword);var bow=Weapon(player,WeaponTypeIds.Bow);player.EquipWeapon(sword);float inactive=stats.GetRawStat(PassiveTreeDefinition.Node(target).Effects[0].Stat);player.EquipWeapon(bow);float active=stats.GetRawStat(PassiveTreeDefinition.Node(target).Effects[0].Stat);Require(active>inactive,"Weapon-specific Bow passive did not activate on weapon swap");
            projectileTarget=(HealthComponent)typeof(BattleManager).GetField("enemyHealth",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(manager);Require(projectileTarget!=null,"Production enemy target is unavailable");targetLife=projectileTarget.CurrentLife;
            var projectile=new PlayerSkillDefinition{id=PlayerSkillId.Fireball,displayName="Step 17 Bow Fixture",projectile=true,supportsPrecision=true,hitDamageMultiplier=1,conversionElement=Element.Phys};Require(manager.TryCastPlayerSkill(projectile),"Production Bow projectile could not launch");Require(Mathf.Approximately(projectileTarget.CurrentLife,targetLife),"Projectile damaged the target before travel latency");
            Write($"PASS: Ranger start, {path.Count}-point passive route, and Bow-only node activation work in SampleScene.");step=1;Delay(2.25);return;
        }
        if(step==1)
        {
            Require(projectileTarget==null||projectileTarget.CurrentLife<targetLife,"Bow projectile did not impact after travel time");
            var controller=player.GetComponent<PlayerSkillController>();player.EquipWeapon(Weapon(player,WeaponTypeIds.Staff));var first=new PlayerSkillDefinition{id=PlayerSkillId.IceStrike,displayName="Staff Fixture One",castMode=PlayerSkillCastMode.AutoCooldown,baseCooldown=.2f,manaCost=0};var second=new PlayerSkillDefinition{id=PlayerSkillId.LightningStrike,displayName="Staff Fixture Two",castMode=PlayerSkillCastMode.AutoCooldown,baseCooldown=.4f,manaCost=0};controller.ConfigureDeveloperAutoSkills(first,second);var order=new List<PlayerSkillId>();controller.TickAutoCooldowns(.4f,skill=>{order.Add(skill.id);return manager.TryCastPlayerSkill(skill);});Require(order.SequenceEqual(new[]{first.id,second.id}),"Staff auto-casts did not resolve independently in stable slot order");
            player.EquipWeapon(Weapon(player,WeaponTypeIds.TwoHandedAxe));var rage=player.GetComponent<RageState>();Require(rage!=null,"Rage authority was not installed in the production scene");rage.GainFromDamageDealt(20,100);rage.GainFromDamageTaken(10,100);Require(rage.Rage>0&&rage.SustainedDamageMultiplier>1&&rage.IncomingDamageMultiplier<1,"Axe Rage did not gain or apply its sustained benefits");
            Write("PASS: Bow travel impact, Staff independent auto-casts, weapon swapping, and Axe Rage work in SampleScene.");Finish();
        }
    }

    static List<int> PathFromStart(string classId,int target)
    {
        int start=PassiveTreeDefinition.StartNodeId(classId);var parent=Enumerable.Repeat(-2,PassiveTreeDefinition.NodeCount).ToArray();var queue=new Queue<int>();queue.Enqueue(start);parent[start]=-1;
        while(queue.Count>0&&parent[target]==-2){int current=queue.Dequeue();foreach(int next in PassiveTreeDefinition.AdjacentNodeIds(current)){if(PassiveTreeDefinition.IsClassStart(next)&&next!=start||parent[next]!=-2)continue;parent[next]=current;queue.Enqueue(next);}}
        Require(parent[target]!=-2,"Bow district is unreachable from Ranger start");var path=new List<int>();for(int id=target;id!=start;id=parent[id])path.Add(id);path.Add(start);path.Reverse();return path;
    }
    static Gear Weapon(PlayerController player,string id){var profile=WeaponTypeCatalog.Get(id);var go=new GameObject("Step17 "+profile.DisplayName);go.transform.SetParent(player.transform);var gear=go.AddComponent<Gear>();gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,id);gear.BaseDamageMin=profile.BaseDamageMin;gear.BaseDamageMax=profile.BaseDamageMax;gear.BaseDamage=(profile.BaseDamageMin+profile.BaseDamageMax)*.5f;gear.BaseAttackSpeed=profile.AttacksPerSecond;gear.BaseCritChance=profile.BaseCritChance;return gear;}
    static void Delay(double seconds)=>readyAt=EditorApplication.timeSinceStartup+seconds;
    static void Finish(){complete=true;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;}
    static void Cleanup(){Application.logMessageReceived-=OnLog;SessionState.EraseBool(Pending);GamePersistence.RequestNewGame();GamePersistence.ResetStaticStateForTests();GamePersistence.SaveDirectoryOverride=null;if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);}
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static void Write(string value){File.AppendAllText(Report,value+Environment.NewLine);Debug.Log(value);}
    static void OnLog(string message,string trace,LogType type){if(type is LogType.Error or LogType.Exception or LogType.Assert)errors++;}
}
