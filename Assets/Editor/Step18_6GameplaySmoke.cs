using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Step18_6GameplaySmoke
{
    const string Active="BlackCube.Step18_6.GameplaySmoke";
    const string Report="Logs/Step18_6GameplaySmoke.txt";
    static double readyAt;

    static Step18_6GameplaySmoke(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}

    public static void Run()
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report,"Step 18.6 real-scene gameplay smoke\nBalanceLab: NOT RUN\n");
        SessionState.SetBool(Active,true);readyAt=0;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        if(!SessionState.GetBool(Active,false))return;
        if(!EditorApplication.isPlaying){SessionState.EraseBool(Active);EditorApplication.Exit(0);return;}
        if(EditorApplication.timeSinceStartup<readyAt)return;
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var enemy=UnityEngine.Object.FindAnyObjectByType<EnemyAI>();
        var loot=UnityEngine.Object.FindAnyObjectByType<LootManager>();
        if(player==null||enemy==null||loot==null||ModManager.Instance==null)return;
        try{RunChecks(player,enemy,loot);Write("RESULT: PASS");EditorApplication.isPlaying=false;}
        catch(Exception ex){Write("RESULT: FAIL "+ex);SessionState.EraseBool(Active);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(1);}
        readyAt=double.MaxValue;
    }

    static void RunChecks(PlayerController player,EnemyAI enemy,LootManager loot)
    {
        var eventIds=new HashSet<long>();var summaries=new List<string>();
        for(int i=0;i<5;i++)
        {
            var source=new ProductionLootRandomSource();eventIds.Add(source.EventId);
            Gear gear=loot.GenerateLoot(enemy.CurrentRarity,source);Require(gear!=null,"Fresh loot generation returned null.");
            string summary=Summary(gear);summaries.Add(summary);Write($"loot event {source.EventId}: {summary}");
            UnityEngine.Object.Destroy(gear.gameObject);
        }
        Require(eventIds.Count==5,"Equivalent death events did not receive distinct loot event IDs.");
        Write($"PASS repeated-stage loot: 5 fresh entropy-backed events, {summaries.Distinct().Count()} observed item summary pattern(s); natural duplicates are permitted.");

        EnemyDropResult normal=PowerRoll(enemy,360,EnemyAI.EnemyRarity.Normal,1f);
        EnemyDropResult rare=PowerRoll(enemy,360,EnemyAI.EnemyRarity.Rare,3f);
        Require(rare.power.LootPower>normal.power.LootPower&&rare.gearCount>normal.gearCount,"LootPower rarity/quality ordering regressed.");
        Write($"PASS LootPower: Normal={normal.gearCount} gear/{normal.currencyStacks.Count} stacks; high-power Rare={rare.gearCount}/{rare.currencyStacks.Count}.");

        var stats=player.GetComponent<StatsComponent>();
        foreach(var weapon in WeaponTypeCatalog.All)
        {
            var definition=WeaponAttributeScalingProfile.Get(weapon.Id);
            float before=WeaponAttributeScalingProfile.IncreasedDamage(weapon.Id,stats);
            float relevant=definition.PrimaryPerPoint*100f+definition.SecondaryPerPoint*100f;
            Require(relevant>0,$"{weapon.DisplayName} has no attribute scaling profile.");
            Write($"PASS {weapon.DisplayName}: {definition.DisplayName}; current bonus={before*100f:0.##}%; 100-point profile contribution={relevant*100f:0.##}%.");
        }

        var progression=UnityEngine.Object.FindAnyObjectByType<PlayerProgression>();
        int oldLevel=progression!=null?progression.Level:1;
        Gear testWeapon=CreateWeapon(player,WeaponTypeIds.Sword,Element.Phys,100);
        SetLevel(progression,10);float at10=player.BasicAttackDamage;
        SetLevel(progression,20);float at20=player.BasicAttackDamage;
        Require(at20>at10,"Level 20 did not increase representative root hit over level 10.");
        Write($"PASS level scaling: L10 bonus={PlayerLevelDamageProfile.IncreasedDamage(10)*100f:0.##}% hit={at10:0.##}; L20 bonus={PlayerLevelDamageProfile.IncreasedDamage(20)*100f:0.##}% hit={at20:0.##}.");
        SetLevel(progression,oldLevel);

        StatusEffects poison=ScriptableObject.CreateInstance<StatusEffects>();
        poison.ConfigureRuntime("Poison",StatusEffects.StatusType.DamageOverTime,StatusEffects.AilmentKind.Poison,ElementMask.All,1,2,1,StatusEffects.StackPolicy.StackIndependently,1);
        var lightning=new DamageContext(1);lightning.AddDamage(Element.Light,100);
        Require(AilmentEligibilityResolver.EligibleRawDamage(poison,lightning,stats)==0,"Pure Lightning became Poison-eligible.");
        UnityEngine.Object.Destroy(poison);
        Write("PASS ailment regression: pure Lightning with ordinary Poison Chance has no Poison basis.");

        Require(Skill(PlayerSkillId.BowVenomShot).projectile,"Bow projectile contract regressed.");
        Require(Skill(PlayerSkillId.StaffFireball).castMode==PlayerSkillCastMode.AutoCooldown,"Staff AutoCooldown regressed.");
        Require(Skill(PlayerSkillId.AxeRageStrike).effect==WeaponSkillEffect.RageStrike,"Axe Rage skill regressed.");
        Require(Skill(PlayerSkillId.DaggerQuickStrike).castMode==PlayerSkillCastMode.ImmediateCooldown,"Dagger Quick Strike regressed.");
        Write("PASS weapon mechanics: Bow projectile, Staff AutoCooldown, Axe Rage, and Dagger Quick Strike definitions remain active.");

        SetLevel(progression,1);float level1=player.BasicAttackDamage;
        SetLevel(progression,10);float level10=player.BasicAttackDamage;
        var skill=Skill(PlayerSkillId.SwordRapidFlurry);
        Write($"EARLY SANITY: L1 signature-style hit={level1:0.##}; L10 same-gear hit={level10:0.##}; current attribute bonus={player.WeaponAttributeDamageBonus*100f:0.##}%; Rapid Flurry representative aggregate basis={level10*skill.hitDamageMultiplier*skill.secondaryMultiplier:0.##}.");
        Write("Manual progression/game-feel review remains required; this smoke does not claim balance is solved.");
        SetLevel(progression,oldLevel);UnityEngine.Object.Destroy(testWeapon.gameObject);
    }

    static EnemyDropResult PowerRoll(EnemyAI enemy,int level,EnemyAI.EnemyRarity rarity,float quality)
    {
        typeof(EnemyAI).GetField("enemyLevel",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,level);
        typeof(EnemyAI).GetField("<CurrentRarity>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,rarity);
        float expected=EnemyLootProfile.ExpectedGearScore(level,rarity,Mathf.Max(1,enemy.EquippedItems.Count));
        typeof(EnemyAI).GetField("<LastBuildEvaluation>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,new EnemyBuildOptimizer.Evaluation(expected*quality,expected*quality,0,0,0,0,0,0,0,0,0,0,0));
        return EnemyLootProfile.Roll(enemy,false,new SequenceLootRandomSource(18600,.999f));
    }

    static Gear CreateWeapon(PlayerController player,string type,Element element,float damage)
    {
        var profile=WeaponTypeCatalog.Get(type);var go=new GameObject("Step18.6 early weapon");go.transform.SetParent(player.transform);var gear=go.AddComponent<Gear>();
        gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,element,type);gear.BaseDamageMin=gear.BaseDamageMax=gear.BaseDamage=damage;gear.BaseAttackSpeed=profile.AttacksPerSecond;gear.BaseCritChance=profile.BaseCritChance;player.EquipWeapon(gear);return gear;
    }
    static PlayerSkillDefinition Skill(PlayerSkillId id)
    {
        var catalog=Resources.Load<PlayerSkillCatalog>("PlayerSkills");
        var skill=catalog?.skills?.FirstOrDefault(x=>x!=null&&x.id==id);
        return skill??throw new InvalidOperationException("Missing production skill: "+id);
    }
    static void SetLevel(PlayerProgression progression,int level){if(progression!=null)typeof(PlayerProgression).GetField("level",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(progression,level);}
    static string Summary(Gear gear)=>$"{gear.ItemType}/{gear.ItemRarity}/{gear.WeaponTypeId}/{gear.BaseElement} ["+string.Join(",",gear.rolledMods.Where(x=>x!=null).Select(x=>$"{x.statType}:T{x.tierIndex}:{x.value:0.###}-{x.HighValue:0.###}"))+"]";
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static void Write(string value){File.AppendAllText(Report,value+Environment.NewLine);Debug.Log(value);}
}
