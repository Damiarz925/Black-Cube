using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Step16ClassWeaponPlayCheck
{
    const string Active="BlackCube.Step16.Play.Active",State="BlackCube.Step16.Play.State",Exit="BlackCube.Step16.Play.Exit";
    const string Report="Logs/Step16-class-weapon-play-check.txt";
    static string SaveDirectory=>Path.GetFullPath("Temp/Step16ClassWeaponPlayCheckSave");static double readyAt;
    static Step16ClassWeaponPlayCheck(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;if(SessionState.GetBool(Active,false)){GamePersistence.SaveDirectoryOverride=SaveDirectory;if(SessionState.GetInt(State,0)==0){GameLaunchSelection.SelectNewGameClass(PlayerClassIds.Ranger);GamePersistence.RequestConfirmedNewGame();}}}
    public static void Run()
    {
        Directory.CreateDirectory("Logs");File.WriteAllText(Report,"Step 16 real-scene class, starter, weapon-skill, Load, and Rebirth smoke\n");
        if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);Directory.CreateDirectory(SaveDirectory);GamePersistence.SaveDirectoryOverride=SaveDirectory;GamePersistence.ResetStaticStateForTests();
        GameLaunchSelection.SelectNewGameClass(PlayerClassIds.Ranger);GamePersistence.RequestConfirmedNewGame();SessionState.SetBool(Active,true);SessionState.SetInt(State,0);SessionState.SetInt(Exit,1);readyAt=0;EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;
    }
    static void Tick(){if(!SessionState.GetBool(Active,false))return;if(!EditorApplication.isPlaying){if(SessionState.GetInt(State,0)<2)return;Cleanup();EditorApplication.Exit(SessionState.GetInt(Exit,1));return;}if(EditorApplication.timeSinceStartup<readyAt)return;readyAt=EditorApplication.timeSinceStartup+.5;try{RunState();}catch(Exception e){Write("FAIL "+e);SessionState.SetInt(State,2);EditorApplication.isPlaying=false;}}
    static void RunState()
    {
        int state=SessionState.GetInt(State,0);if(SceneManager.GetActiveScene().name!=GameSceneNames.Gameplay||GameManager.Instance==null)return;
        var identity=GameManager.Instance.GetComponent<PlayerIdentityState>();var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();var skills=player?.GetComponent<PlayerSkillController>();
        Require(identity!=null&&identity.BaseClassId==PlayerClassIds.Ranger,"Ranger class was not initialized in the real gameplay scene.");
        if(state==0)
        {
            Require(player?.EquippedWeapon?.WeaponTypeId==WeaponTypeIds.Bow,"Ranger did not receive the Bow signature starter.");
            WeaponSkillBindings.RegisterDeveloperFixture(WeaponTypeIds.Bow,PlayerSkillId.Fireball,PlayerSkillId.LightningStrike);WeaponSkillBindings.RegisterDeveloperFixture(WeaponTypeIds.Sword,PlayerSkillId.HeavyStrike,PlayerSkillId.IceStrike);skills.RefreshWeaponSkills();Require(skills.WeaponSkills.Count==2,"Bow fixture did not expose exactly two skills.");Require(skills.TryQueueWeaponSkill(0),"Could not queue Bow skill in live combat.");
            var go=new GameObject("Step16 Swap Sword");var sword=go.AddComponent<Gear>();sword.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,WeaponTypeIds.Sword);sword.BaseDamageMin=18;sword.BaseDamageMax=27;sword.BaseDamage=22.5f;sword.BaseAttackSpeed=.45f;sword.BaseCritChance=.05f;sword.ApplyMods(new System.Collections.Generic.List<RolledMod>{new(StatTypes.GenericDmg,5,7,true)});EquipmentManager.Instance.Equip(sword);
            Require(!skills.HasQueuedSkill,"Weapon swap retained a queued skill.");Require(skills.WeaponSkills.Select(x=>x.id).SequenceEqual(new[]{PlayerSkillId.HeavyStrike,PlayerSkillId.IceStrike}),"Weapon swap did not refresh the available pair.");Require(GamePersistence.TrySave(),"Step 16 checkpoint failed.");Write("PASS class -> signature starter -> two-skill queue -> weapon swap refresh/clear.");
            SessionState.SetInt(State,1);GamePersistence.RequestLoad();SceneManager.LoadScene(GameSceneNames.Gameplay);readyAt=EditorApplication.timeSinceStartup+1;return;
        }
        if(state==1)
        {
            Require(identity.BaseClassId==PlayerClassIds.Ranger,"Load changed base class.");Require(player?.EquippedWeapon?.WeaponTypeId==WeaponTypeIds.Sword,"Load did not restore the cross-class Sword swap.");GameManager.Instance.StartZone(RebirthManager.RequiredZone);Require(RebirthManager.Instance.RequestRebirth()&&RebirthManager.Instance.ConfirmRebirth(),"Rebirth fixture failed.");
            Require(identity.BaseClassId==PlayerClassIds.Ranger,"Rebirth changed base class.");Require(player.EquippedWeapon?.WeaponTypeId==WeaponTypeIds.Bow,"Rebirth did not recreate the class signature starter.");Write("PASS Load and Rebirth preserved Ranger identity and Rebirth recreated Bow starter.");SessionState.SetInt(Exit,0);SessionState.SetInt(State,2);EditorApplication.isPlaying=false;
        }
    }
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}static void Write(string value){File.AppendAllText(Report,value+Environment.NewLine);Debug.Log(value);}
    static void Cleanup(){GamePersistence.RequestNewGame();GameLaunchSelection.Clear();GamePersistence.SaveDirectoryOverride=null;WeaponSkillBindings.ClearDeveloperFixtures();if(Directory.Exists(SaveDirectory))Directory.Delete(SaveDirectory,true);SessionState.SetBool(Active,false);}
}
