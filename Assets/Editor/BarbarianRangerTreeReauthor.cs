using System;
using UnityEditor;
using UnityEngine;

// One-time stable-ID-preserving migration. Only side choices change; spine and keystones stay intact.
public static class BarbarianRangerTreeReauthor
{
    [MenuItem("Black-Cube/Passive Tree/Reauthor Barbarian and Ranger Choices")]
    public static void Run()
    {
        Reauthor("Barbarian",Barbarian);
        Reauthor("Ranger",Ranger);
        AssetDatabase.SaveAssets();
        Debug.Log("Barbarian and Ranger ten-tier side choices reauthored; stable IDs and spine preserved.");
    }

    static void Reauthor(string className,Action<int,SerializedProperty,SerializedProperty> configure)
    {
        string path=$"Assets/GameData/PassiveTree/Branches/Class/SO_{className}_Branch.asset";
        var asset=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(path);
        if(asset==null||asset.Tiers.Count!=10)throw new InvalidOperationException("Expected ten-tier class branch: "+path);
        var serialized=new SerializedObject(asset);
        var tiers=serialized.FindProperty("tiers");
        for(int tier=0;tier<10;tier++)
        {
            var entry=tiers.GetArrayElementAtIndex(tier);
            configure(tier,entry.FindPropertyRelative("left"),entry.FindPropertyRelative("right"));
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    static void Barbarian(int tier,SerializedProperty left,SerializedProperty right)
    {
        Set(left,"a","Life Regeneration","Regenerate 1.5% maximum Life each second.",PassiveBranch.LifeRegeneration,(StatTypes.LifeRegeneration,1.5f));
        Set(left,"b","Damage Reduction per Rage","0.02% damage reduction per current Rage.",PassiveBranch.Defense,(StatTypes.DamageReductionPerRage,.02f));
        Set(left,"c","Reduced Rage Decay","5% reduced Rage decay.",PassiveBranch.RageRetention,(StatTypes.RageDecayReduction,5));
        Set(right,"a","Heavy Physical Damage","30% increased Physical Damage; -15% additive increased Attack Speed.",PassiveBranch.Physical,(StatTypes.PhysDmg,30),(StatTypes.AttackSpeed,-15));
        Set(right,"b","Rage Generation","15% increased Rage generation.",PassiveBranch.RageGeneration,(StatTypes.RageGeneration,15));
        Set(right,"c","Maximum Rage","+5 maximum Rage.",PassiveBranch.RageEffect,(StatTypes.MaximumRage,5));
        bool odd=(tier&1)==0;
        if(odd)
        {
            Set(left,"subclassA","Repeated-Hit Fortification","4% less damage per prior enemy hit since your last attack, to a 20% incoming floor.",PassiveBranch.Defense,(StatTypes.TitanFortification,4));
            Set(right,"subclassA","Revenge Amplification","Add 5 percentage points to Revenge's more-damage bonus.",PassiveBranch.LargeHitPower,(StatTypes.TitanRevengeBonus,5));
            Set(left,"subclassB","Damage Taken as Fire","8% of incoming damage is taken as Fire before mitigation.",PassiveBranch.Defense,(StatTypes.DamageTakenAsFire,8));
            Set(right,"subclassB","Eruption Damage","Add 5 percentage points to Eruption's raw-hit coefficient.",PassiveBranch.Fire,(StatTypes.EruptionCoefficient,5));
        }
        else
        {
            Set(left,"subclassA","Rage-Fueled Regeneration","Regenerate 0.06% maximum Life per second per current Rage.",PassiveBranch.LifeRegeneration,(StatTypes.TitanRageRegeneration,.06f));
            Set(right,"subclassA","Full-Life Power","7.5% more damage against full-Life enemies.",PassiveBranch.LargeHitPower,(StatTypes.TitanFullLifeMore,7.5f));
            Set(left,"subclassB","Fire Life Leech","Leech 2% of Fire damage as Life over four seconds.",PassiveBranch.LifeRegeneration,(StatTypes.FireLifeLeech,2));
            Set(right,"subclassB","Fire Conversion","Convert 12% Physical damage to Fire; 30% increased Fire Damage.",PassiveBranch.Fire,(StatTypes.PhysicalToFireConversion,12),(StatTypes.FireDmg,30));
        }
    }

    static void Ranger(int tier,SerializedProperty left,SerializedProperty right)
    {
        Set(left,"a","Dodge Chance","+3 percentage points Dodge Chance against attack hits.",PassiveBranch.Defense,(StatTypes.DodgeChance,3));
        Set(left,"b","Life on Kill","Recover 50 Life on kill.",PassiveBranch.LifeOnKill,(StatTypes.LifeOnKill,50));
        Set(left,"c","Shock and Chill Resistance","4% reduced Shock effect and 4% reduced Chill effect.",PassiveBranch.Resistances,(StatTypes.ReducedShockEffect,4),(StatTypes.ReducedChillEffect,4));
        Set(right,"a","Projectile Precision Chance","+8 percentage points Projectile Precision Chance.",PassiveBranch.PrecisionChance,(StatTypes.ProjectilePrecisionChance,8));
        Set(right,"b","Projectile Speed","15% increased Projectile Speed.",PassiveBranch.ProjectileSpeed,(StatTypes.ProjectileSpeed,15));
        Set(right,"c","Poison Chance","+30 percentage points Poison Chance.",PassiveBranch.PoisonChance,(StatTypes.PoisonChance,30));
        bool odd=(tier&1)==0;
        if(odd)
        {
            Set(left,"subclassA","Poison Life Leech","Recover 2% of Poison damage as Life.",PassiveBranch.LifeRegeneration,(StatTypes.PoisonLifeLeech,2));
            Set(right,"subclassA","Poison Speed","10% increased Poison Speed.",PassiveBranch.PoisonSpeed,(StatTypes.PoisonSpeed,10));
            Set(left,"subclassB","Projectile Guard","0.1% less damage per live player projectile in flight.",PassiveBranch.Defense,(StatTypes.ProjectileGuard,.1f));
            Set(right,"subclassB","Precision Power","5% more Precision Damage.",PassiveBranch.PrecisionDamage,(StatTypes.PrecisionMore,5));
        }
        else
        {
            Set(left,"subclassA","Toxic Suppression","Poisoned attackers deal 0.5% less damage per ten Poison stacks on them.",PassiveBranch.Defense,(StatTypes.ToxicSuppression,.5f));
            Set(right,"subclassA","Poison Duration","10% increased Poison Duration.",PassiveBranch.PoisonDamage,(StatTypes.PoisonDuration,10));
            Set(left,"subclassB","Evasive Recovery","+2 percentage points Dodge Chance; recover 2% maximum Life on a successful Dodge.",PassiveBranch.Defense,(StatTypes.DodgeChance,2),(StatTypes.DodgeLifeRecovery,2));
            Set(right,"subclassB","Projectile Amount","+0.4 Projectile Amount; fractional remainder rolls for one extra projectile.",PassiveBranch.IncreasedProjectileAmount,(StatTypes.ProjectileAmount,.4f));
        }
    }

    static void Set(SerializedProperty side,string slot,string name,string description,PassiveBranch branch,params (StatTypes stat,float value)[] values)
    {
        var node=side.FindPropertyRelative(slot);
        node.FindPropertyRelative("displayName").stringValue=name;
        node.FindPropertyRelative("description").stringValue=description;
        node.FindPropertyRelative("branch").enumValueIndex=(int)branch;
        node.FindPropertyRelative("iconMode").enumValueIndex=(int)PassiveIconMode.Auto;
        node.FindPropertyRelative("iconOverride").objectReferenceValue=null;
        var effects=node.FindPropertyRelative("effects");effects.arraySize=values.Length;
        for(int i=0;i<values.Length;i++)
        {
            var effect=effects.GetArrayElementAtIndex(i);
            effect.FindPropertyRelative("kind").enumValueIndex=(int)PassiveEffectKind.Stat;
            effect.FindPropertyRelative("stat").enumValueIndex=(int)values[i].stat;
            effect.FindPropertyRelative("mechanicId").stringValue=string.Empty;
            effect.FindPropertyRelative("value").floatValue=values[i].value;
        }
    }
}
