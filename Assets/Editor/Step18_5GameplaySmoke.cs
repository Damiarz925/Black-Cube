using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Step18_5GameplaySmoke
{
    const string Active="BlackCube.Step18_5.GameplaySmoke";
    const string Report="Logs/Step18_5GameplaySmoke.txt";
    static double readyAt;

    static Step18_5GameplaySmoke(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}

    public static void Run()
    {
        Directory.CreateDirectory("Logs");File.WriteAllText(Report,"Step 18.5 real-scene gameplay smoke\n");
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
        if(player==null||enemy==null)return;
        try{RunChecks(player,enemy);Write("RESULT: PASS");EditorApplication.isPlaying=false;}
        catch(Exception ex){Write("RESULT: FAIL "+ex);SessionState.EraseBool(Active);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(1);}
        readyAt=double.MaxValue;
    }

    static void RunChecks(PlayerController player,EnemyAI enemy)
    {
        var stats=player.GetComponent<StatsComponent>();var state=player.GetComponent<SubclassCombatState>();
        StatusEffects poison=Effect(StatusEffects.AilmentKind.Poison,StatusEffects.StatusType.DamageOverTime);
        StatusEffects ignite=Effect(StatusEffects.AilmentKind.Ignite,StatusEffects.StatusType.DamageOverTime);
        StatusEffects shock=Effect(StatusEffects.AilmentKind.None,StatusEffects.StatusType.Shock);
        DamageContext lightning=Hit(Element.Light),physical=Hit(Element.Phys),voidHit=Hit(Element.Void);
        Require(AilmentEligibilityResolver.EligibleRawDamage(poison,lightning,stats)==0,"Warrior-style Lightning incorrectly qualified for Poison.");
        Require(AilmentEligibilityResolver.EligibleRawDamage(poison,physical,stats)>0,"Physical failed to qualify for Poison.");
        state.GrantExternalEffect(SubclassEffectIds.RangerAllDamagePoison);Require(AilmentEligibilityResolver.EligibleRawDamage(poison,lightning,stats)>0,"Ranger Poison override failed.");state.RevokeExternalEffect(SubclassEffectIds.RangerAllDamagePoison);
        state.GrantExternalEffect(SubclassEffectIds.DarkPriestVoidAilments);Require(AilmentEligibilityResolver.EligibleRawDamage(ignite,voidHit,stats)>0,"Dark Priest Void override failed.");state.RevokeExternalEffect(SubclassEffectIds.DarkPriestVoidAilments);
        state.GrantExternalEffect(SubclassEffectIds.MageAllDamageShock);Require(AilmentEligibilityResolver.EligibleRawDamage(shock,physical,stats)>0,"Mage Storm Shock override failed.");state.RevokeExternalEffect(SubclassEffectIds.MageAllDamageShock);
        Write("PASS ailments: default matrix plus Ranger Poison, Dark Priest Void, and Mage Storm overrides.");

        foreach(var profile in WeaponTypeCatalog.All)
        {
            var go=new GameObject("Step18.5 "+profile.DisplayName);go.transform.SetParent(player.transform);
            var gear=go.AddComponent<Gear>();gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,profile.Id);
            gear.BaseDamageMin=profile.BaseDamageMin;gear.BaseDamageMax=profile.BaseDamageMax;gear.BaseDamage=(profile.BaseDamageMin+profile.BaseDamageMax)*.5f;gear.BaseAttackSpeed=profile.AttacksPerSecond;gear.BaseCritChance=profile.BaseCritChance;
            player.EquipWeapon(gear);
            Require(player.EquippedWeapon==gear,profile.DisplayName+" did not equip.");
            Require(ItemIconCatalog.Get(gear)!=null,profile.DisplayName+" icon did not resolve.");
            Require(WeaponSkillBindings.For(profile.Id).Count==2,profile.DisplayName+" production skill pair is incomplete.");
        }
        Write("PASS weapons: all six equipped with mapped icon, production skill pair, and type profile.");

        int level=360;
        EnemyDropResult normal=Roll(enemy,level,EnemyAI.EnemyRarity.Normal,1f);
        EnemyDropResult magic=Roll(enemy,level,EnemyAI.EnemyRarity.Magic,1f);
        EnemyDropResult rare=Roll(enemy,360,EnemyAI.EnemyRarity.Rare,3f);
        Require(normal.gearCount>=1&&magic.gearCount>normal.gearCount,"Magic loot did not exceed the same-quality Normal profile.");
        Require(rare.gearCount>magic.gearCount&&rare.currencyStacks.Count>0,"High-power Rare did not produce a larger gear/currency event.");
        Require(rare.gearCount<=1+EnemyLootProfile.MaximumExtraGear&&rare.currencies.Count<=EnemyLootProfile.MaximumCurrencyRolls,"Loot safety cap failed.");
        Write($"PASS loot: Normal={normal.gearCount} gear, Magic={magic.gearCount}, high-power Rare={rare.gearCount} plus {rare.currencyStacks.Count} currency stack(s).");
    }

    static EnemyDropResult Roll(EnemyAI enemy,int level,EnemyAI.EnemyRarity rarity,float quality)
    {
        typeof(EnemyAI).GetField("enemyLevel",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,level);
        typeof(EnemyAI).GetField("<CurrentRarity>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,rarity);
        float expected=EnemyLootProfile.ExpectedGearScore(level,rarity,Mathf.Max(1,enemy.EquippedItems.Count));
        var evaluation=new EnemyBuildOptimizer.Evaluation(expected*quality,expected*quality,0,0,0,0,0,0,0,0,0,0,0);
        typeof(EnemyAI).GetField("<LastBuildEvaluation>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,evaluation);
        return EnemyLootProfile.Roll(enemy,false,()=>.999f);
    }

    static DamageContext Hit(Element element){var hit=new DamageContext(1);hit.AddDamage(element,100);return hit;}
    static StatusEffects Effect(StatusEffects.AilmentKind kind,StatusEffects.StatusType type){var effect=ScriptableObject.CreateInstance<StatusEffects>();effect.ConfigureRuntime(kind.ToString(),type,kind,ElementMask.All,1,2,1,StatusEffects.StackPolicy.StackIndependently,1);return effect;}
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static void Write(string value){File.AppendAllText(Report,value+Environment.NewLine);Debug.Log(value);}
}
