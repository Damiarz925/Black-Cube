// Step 16 architecture: stable class, weapon, subclass, and passive-affinity identities.
// Production subclass identities and weapon-skill assignments intentionally remain unassigned.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerClassIds
{
    public const string Warrior="class.warrior", Mage="class.mage", Ranger="class.ranger";
    public const string Barbarian="class.barbarian", Priest="class.priest", Thief="class.thief";
}

public static class WeaponTypeIds
{
    public const string Sword="weapon.sword", TwoHandedAxe="weapon.two_handed_axe", Staff="weapon.staff";
    public const string Bow="weapon.bow", Dagger="weapon.dagger", Sceptre="weapon.sceptre";
}

public sealed class PlayerClassDefinition
{
    public readonly string Id,DisplayName,SignatureWeaponTypeId,PassiveStartId,PresentationHook,Description;
    public readonly string[] SubclassSlotIds;
    public PlayerClassDefinition(string id,string name,string weapon,string passiveStart)
    {
        Id=id;DisplayName=name;SignatureWeaponTypeId=weapon;PassiveStartId=passiveStart;
        PresentationHook="presentation."+id;Description=$"{name} class foundation; innate stat bonuses are intentionally absent.";
        SubclassSlotIds=new[]{"subclass-slot."+id+".1","subclass-slot."+id+".2"};
    }
}

public static class PlayerClassCatalog
{
    static readonly PlayerClassDefinition[] all={
        new(PlayerClassIds.Warrior,"Warrior",WeaponTypeIds.Sword,"tree.v2.warrior.start"),
        new(PlayerClassIds.Mage,"Mage",WeaponTypeIds.Staff,"tree.v2.mage.start"),
        new(PlayerClassIds.Ranger,"Ranger",WeaponTypeIds.Bow,"tree.v2.ranger.start"),
        new(PlayerClassIds.Barbarian,"Barbarian",WeaponTypeIds.TwoHandedAxe,"tree.v2.barbarian.start"),
        new(PlayerClassIds.Priest,"Priest",WeaponTypeIds.Sceptre,"tree.v2.priest.start"),
        new(PlayerClassIds.Thief,"Thief",WeaponTypeIds.Dagger,"tree.v2.thief.start")};
    public static IReadOnlyList<PlayerClassDefinition> All=>all;
    public static bool TryGet(string id,out PlayerClassDefinition value){foreach(var x in all)if(x.Id==id){value=x;return true;}value=null;return false;}
    public static bool IsValid(string id)=>TryGet(id,out _);
}

public sealed class WeaponTypeDefinition
{
    public readonly string Id,DisplayName,ProjectileMetadataHook,ArtHook,AudioHook;
    public readonly string[] AffixTags;
    public readonly float BaseDamageMin,BaseDamageMax,AttacksPerSecond,BaseCritChance;
    public readonly bool IsRanged,RequiresAccuracyResolution,SupportsPrecision,SupportsRage;
    public int SkillSlotCount=>2;
    public IReadOnlyList<PlayerSkillId> SkillIds=>WeaponSkillBindings.For(Id);
    public WeaponTypeDefinition(string id,string name,float min,float max,float speed,float crit,bool ranged)
    {Id=id;DisplayName=name;BaseDamageMin=min;BaseDamageMax=max;AttacksPerSecond=speed;BaseCritChance=crit;IsRanged=ranged;RequiresAccuracyResolution=false;SupportsPrecision=id==WeaponTypeIds.Bow;SupportsRage=id==WeaponTypeIds.TwoHandedAxe;ProjectileMetadataHook=ranged?"projectile."+id:string.Empty;AffixTags=new[]{id,ranged?"weapon.ranged":"weapon.melee"};ArtHook="art."+id;AudioHook="audio."+id;}
}

public static class WeaponTypeCatalog
{
    public const string HistoricalDefaultId=WeaponTypeIds.Sword;
    static readonly WeaponTypeDefinition[] all={
        new(WeaponTypeIds.Sword,"Sword",18,27,.45f,.05f,false),
        new(WeaponTypeIds.TwoHandedAxe,"Two-Handed Axe",26,38,.30f,.04f,false),
        new(WeaponTypeIds.Staff,"Staff",18,28,.40f,.06f,false),
        new(WeaponTypeIds.Bow,"Bow",17,25,.50f,.05f,true),
        new(WeaponTypeIds.Dagger,"Dagger",14,20,.60f,.08f,false),
        new(WeaponTypeIds.Sceptre,"Sceptre",19,28,.42f,.05f,false)};
    public static IReadOnlyList<WeaponTypeDefinition> All=>all;
    public static bool TryGet(string id,out WeaponTypeDefinition value){foreach(var x in all)if(x.Id==id){value=x;return true;}value=null;return false;}
    public static bool IsValid(string id)=>TryGet(id,out _);
    public static WeaponTypeDefinition Get(string id)=>TryGet(id,out var x)?x:throw new ArgumentException("Unknown weapon type ID: "+id,nameof(id));
}

public sealed class SubclassDefinition
{
    public readonly string Id,ParentClassId,DisplayName,Description,PassiveSectionId,PresentationHook,UnlockMilestoneId;
    public readonly string[] GrantedSystemHooks;
    public SubclassDefinition(string id,string parent,string name,string passiveSection,params string[] hooks)
    {Id=id;ParentClassId=parent;DisplayName=name;Description=name+" developer fixture";PassiveSectionId=passiveSection;PresentationHook="presentation."+id;UnlockMilestoneId=PlayerIdentityState.StoryCompletionMilestoneId;GrantedSystemHooks=hooks??Array.Empty<string>();}
}

public static class SubclassIds
{
    public const string WarriorBleed="subclass.warrior.bleed",WarriorMultihit="subclass.warrior.multihit";
    public const string BarbarianBigHit="subclass.barbarian.big_hit",BarbarianFire="subclass.barbarian.fire";
    public const string RangerPoison="subclass.ranger.poison",RangerProjectile="subclass.ranger.projectile";
    public const string MageCooldown="subclass.mage.cooldown",MageStorm="subclass.mage.storm";
    public const string PriestDark="subclass.priest.dark",PriestLight="subclass.priest.light";
    public const string ThiefAssassin="subclass.thief.assassin",ThiefAilmentCrit="subclass.thief.ailment_crit";
}

public enum SubclassProjectileMode{Volley,Focused}

public static class SubclassCatalog
{
    static readonly SubclassDefinition[] production={
        New(SubclassIds.WarriorBleed,PlayerClassIds.Warrior,"Bleed Warrior","bleed","subclass.warrior.bleed.rupture"),
        New(SubclassIds.WarriorMultihit,PlayerClassIds.Warrior,"Momentum Warrior","multihit","subclass.warrior.multihit.combo"),
        New(SubclassIds.BarbarianBigHit,PlayerClassIds.Barbarian,"Titan Barbarian","big-hit","subclass.barbarian.big_hit.full_life"),
        New(SubclassIds.BarbarianFire,PlayerClassIds.Barbarian,"Fire Barbarian","fire","subclass.barbarian.fire.eruption"),
        New(SubclassIds.RangerPoison,PlayerClassIds.Ranger,"Venom Ranger","poison","subclass.ranger.poison.all_damage"),
        New(SubclassIds.RangerProjectile,PlayerClassIds.Ranger,"Projectile Ranger","projectile","subclass.ranger.projectile.focused"),
        New(SubclassIds.MageCooldown,PlayerClassIds.Mage,"Chrono Mage","cooldown","subclass.mage.cooldown.ignore"),
        New(SubclassIds.MageStorm,PlayerClassIds.Mage,"Storm Mage","storm","subclass.mage.storm.multi_shock"),
        New(SubclassIds.PriestDark,PlayerClassIds.Priest,"Dark Priest","dark","subclass.priest.dark.heal_to_harm"),
        New(SubclassIds.PriestLight,PlayerClassIds.Priest,"Light Priest","light","subclass.priest.light.auras"),
        New(SubclassIds.ThiefAssassin,PlayerClassIds.Thief,"Assassin","assassin","subclass.thief.assassin.first_strike"),
        New(SubclassIds.ThiefAilmentCrit,PlayerClassIds.Thief,"Ailment Assassin","ailment-crit","subclass.thief.ailment_crit.critical_ailment")};
    static readonly Dictionary<string,SubclassDefinition> developerFixtures=new();
    public static IReadOnlyList<SubclassDefinition> All=>production;
    public static bool TryGet(string id,out SubclassDefinition value)
    {if(developerFixtures.TryGetValue(id,out value))return true;foreach(var x in production)if(x.Id==id){value=x;return true;}value=null;return false;}
    public static IReadOnlyList<SubclassDefinition> ForClass(string classId){var result=new List<SubclassDefinition>(2);foreach(var x in production)if(x.ParentClassId==classId)result.Add(x);return result;}
    static SubclassDefinition New(string id,string parent,string name,string section,params string[] hooks)=>new(id,parent,name,"tree.transform."+section,hooks);
#if UNITY_EDITOR
    public static bool RegisterDeveloperFixture(SubclassDefinition value)
    {if(value==null||string.IsNullOrWhiteSpace(value.Id)||!PlayerClassCatalog.IsValid(value.ParentClassId))return false;developerFixtures[value.Id]=value;return true;}
    public static void ClearDeveloperFixtures()=>developerFixtures.Clear();
#endif
}

public static class GameLaunchSelection
{
    static string pendingClassId;
    public static string PendingClassId=>pendingClassId;
    public static bool SelectNewGameClass(string id){if(!PlayerClassCatalog.IsValid(id))return false;pendingClassId=id;return true;}
    public static string ConsumeOrDefault(){string id=pendingClassId;pendingClassId=null;return PlayerClassCatalog.IsValid(id)?id:PlayerClassIds.Warrior;}
    public static void Clear()=>pendingClassId=null;
}

public sealed class PlayerIdentityState:MonoBehaviour
{
    public const string StoryCompletionMilestoneId="story.main.complete";
    public string BaseClassId{get;private set;}=PlayerClassIds.Warrior;
    public string SelectedSubclassId{get;private set;}=string.Empty;
    public bool SubclassChoiceUnlocked{get;private set;}
    public bool HasSubclassSigil=>SubclassChoiceUnlocked;
    public SubclassProjectileMode ProjectileMode{get;private set;}
    public event Action Changed;
    public PlayerClassDefinition ClassDefinition=>PlayerClassCatalog.TryGet(BaseClassId,out var x)?x:null;

    public bool BeginNewGame(string classId)
    {if(!PlayerClassCatalog.IsValid(classId))return false;BaseClassId=classId;SelectedSubclassId=string.Empty;SubclassChoiceUnlocked=false;ProjectileMode=default;Changed?.Invoke();GamePersistence.MarkDirty();return true;}
    public bool CompleteMilestone(string milestoneId)
    {if(milestoneId!=StoryCompletionMilestoneId)return false;SubclassChoiceUnlocked=true;Changed?.Invoke();GamePersistence.MarkDirty();return true;}
    public bool SelectSubclass(string id)
    {if(!SubclassChoiceUnlocked||!SubclassCatalog.TryGet(id,out var value)||value.ParentClassId!=BaseClassId)return false;if(SelectedSubclassId!=id)GetComponent<PlayerProgression>()?.ClearTransformations();SelectedSubclassId=id;Changed?.Invoke();GamePersistence.MarkDirty();return true;}
    public bool SetProjectileMode(SubclassProjectileMode mode){if(SelectedSubclassId!=SubclassIds.RangerProjectile)return false;ProjectileMode=mode;Changed?.Invoke();GamePersistence.MarkDirty();return true;}
    public bool Restore(string classId,bool unlocked,string subclassId,SubclassProjectileMode mode=default)
    {
        if(!PlayerClassCatalog.IsValid(classId))return false;
        if(!string.IsNullOrEmpty(subclassId)&&(!unlocked||!SubclassCatalog.TryGet(subclassId,out var sub)||sub.ParentClassId!=classId))return false;
        BaseClassId=classId;SubclassChoiceUnlocked=unlocked;SelectedSubclassId=subclassId??string.Empty;ProjectileMode=mode;Changed?.Invoke();return true;
    }
}

public enum PassiveAffinity { None, Strength, Intelligence, Dexterity, Life, Mana, Defense, Physical, Elemental, Projectile, DamageOverTime }
public sealed class PassiveExtensionMetadata
{
    public string StableSectionId;
    public string RequiredSubclassId;
    public string[] ClassStartIds=Array.Empty<string>();
    public PassiveAffinity[] Affinities=Array.Empty<PassiveAffinity>();
    public string SpecializationGroupId;
    public bool MutuallyExclusive;
    public bool IsTravelNode;
    // Empty in production for Step 17; future subclass items populate these.
    public string TransformationIdentity;
    public PassiveEffect[] TransformedEffects=Array.Empty<PassiveEffect>();
    public bool SupportsTransformation=true;
}

public static class WeaponSkillBindings
{
    static readonly Dictionary<string,PlayerSkillId[]> production=new(){
        [WeaponTypeIds.Sword]=new[]{PlayerSkillId.SwordRapidFlurry,PlayerSkillId.SwordArmourStrike},
        [WeaponTypeIds.TwoHandedAxe]=new[]{PlayerSkillId.AxeRageStrike,PlayerSkillId.AxeHemorrhage},
        [WeaponTypeIds.Bow]=new[]{PlayerSkillId.BowVenomShot,PlayerSkillId.BowDoubleVolley},
        [WeaponTypeIds.Staff]=new[]{PlayerSkillId.StaffFireball,PlayerSkillId.StaffShockBarrage},
        [WeaponTypeIds.Sceptre]=new[]{PlayerSkillId.SceptreRestorativeStrike,PlayerSkillId.SceptreFrostJudgment},
        [WeaponTypeIds.Dagger]=new[]{PlayerSkillId.DaggerBackstab,PlayerSkillId.DaggerQuickStrike}};
    static readonly Dictionary<string,PlayerSkillId[]> developerFixtures=new();
    public static IReadOnlyList<PlayerSkillId> For(string weaponTypeId)=>!string.IsNullOrEmpty(weaponTypeId)&&developerFixtures.TryGetValue(weaponTypeId,out var fixture)?fixture:!string.IsNullOrEmpty(weaponTypeId)&&production.TryGetValue(weaponTypeId,out var ids)?ids:Array.Empty<PlayerSkillId>();
#if UNITY_EDITOR
    public static bool RegisterDeveloperFixture(string weaponTypeId,PlayerSkillId first,PlayerSkillId second)
    {if(!WeaponTypeCatalog.IsValid(weaponTypeId)||first==second)return false;developerFixtures[weaponTypeId]=new[]{first,second};return true;}
    public static void ClearDeveloperFixtures()=>developerFixtures.Clear();
#endif
}
