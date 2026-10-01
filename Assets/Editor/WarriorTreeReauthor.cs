using System;
using UnityEditor;
using UnityEngine;

// One-time, stable-ID-preserving Warrior branch migration.
public static class WarriorTreeReauthor
{
    [MenuItem("Black-Cube/Passive Tree/Reauthor Warrior Choices")]
    public static void Run()
    {
        const string tuningPath="Assets/Resources/GameData/SO_WarriorSubclassTuning.asset";
        if(AssetDatabase.LoadAssetAtPath<WarriorSubclassTuningSO>(tuningPath)==null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WarriorSubclassTuningSO>(),tuningPath);
        const string path="Assets/GameData/PassiveTree/Branches/Class/SO_Warrior_Branch.asset";
        var asset=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(path);
        if(asset==null||asset.Tiers.Count!=10)throw new InvalidOperationException("Expected the ten-tier Warrior branch asset.");
        var serialized=new SerializedObject(asset);
        var tiers=serialized.FindProperty("tiers");
        for(int i=0;i<10;i++)
        {
            var tier=tiers.GetArrayElementAtIndex(i);
            var left=tier.FindPropertyRelative("left");
            var right=tier.FindPropertyRelative("right");
            Set(left,"a","Increased Armour","+30% increased local/global Armour.",StatTypes.ArmourPercent,30);
            Set(left,"b","Life on Hit","Recover 10 Life on a successful hit.",StatTypes.LifeOnHit,10);
            Set(left,"c","All Elemental Resistance","+4% Fire, Cold and Lightning Resistance.",StatTypes.AllRes,4);
            Set(right,"a","Bleed Chance","+15 percentage points Bleed Chance.",StatTypes.BleedChance,15);
            Set(right,"b","Multistrike Chance","+5 percentage points Multistrike Chance.",StatTypes.ChanceToHitTwice,5);
            Set(right,"c","Attack Speed","+8% increased Attack Speed.",StatTypes.AttackSpeed,8);
            bool odd=(i&1)==0;
            if(odd)
            {
                Set(left,"subclassA","Life on Hit vs Bleeding","Recover 25 additional Life when the enemy is already Bleeding as this hit resolves.",StatTypes.LifeOnHitVsBleeding,25);
                Set(right,"subclassA","Damage over Time Multiplier","+5% Damage over Time Multiplier.",StatTypes.WarriorDotMultiplier,5);
                Set(left,"subclassB","Elemental Plating","5% of Armour mitigates Fire, Cold and Lightning hits before Resistance; not Void or DoT.",StatTypes.ElementalPlating,5);
                Set(right,"subclassB","Escalating Multistrikes","Each successive Multistrike deals 5% more damage per prior Multistrike in this attack.",StatTypes.EscalatingMultistrike,5);
            }
            else
            {
                Set(left,"subclassA","Deferred Wounds","8% of post-mitigation damage to Life becomes self-Bleeding over 4 seconds; total damage is unchanged.",StatTypes.DeferredWounds,8);
                Set(right,"subclassA","Rupture Damage","Rupture releases 5% more of the remaining damage in the Bleeds it consumes.",StatTypes.RuptureDamage,5);
                Set(left,"subclassB","Block Chance","+8 percentage points Block Chance; a Block halves a hit before Armour and Resistance.",StatTypes.ChanceToBlock,8);
                Set(right,"subclassB","Unbroken Assault","10% more damage per prior player attack since the last successful enemy hit; Multistrikes do not count separately.",StatTypes.UnbrokenAssault,10);
            }
        }
        var keystones=serialized.FindProperty("keystones");
        for(int i=0;i<keystones.arraySize;i++)
        {
            var node=keystones.GetArrayElementAtIndex(i);
            if(node.FindPropertyRelative("displayName").stringValue=="One Decisive Strike")
                node.FindPropertyRelative("description").stringValue="Consolidate rolled melee Multistrikes into the original hit at 110% of each would-be extra strike. No repeated triggers.";
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("Warrior class branch reauthored at all ten tiers; stable node IDs preserved.");
    }

    private static void Set(SerializedProperty side,string slot,string name,string description,StatTypes stat,float amount)
    {
        var node=side.FindPropertyRelative(slot);
        node.FindPropertyRelative("displayName").stringValue=name;
        node.FindPropertyRelative("description").stringValue=description;
        node.FindPropertyRelative("branch").enumValueIndex=Branch(stat);
        node.FindPropertyRelative("iconMode").enumValueIndex=(int)PassiveIconMode.Auto;
        node.FindPropertyRelative("iconOverride").objectReferenceValue=null;
        var effects=node.FindPropertyRelative("effects");effects.arraySize=1;
        var effect=effects.GetArrayElementAtIndex(0);
        effect.FindPropertyRelative("kind").enumValueIndex=(int)PassiveEffectKind.Stat;
        effect.FindPropertyRelative("stat").enumValueIndex=(int)stat;
        effect.FindPropertyRelative("mechanicId").stringValue=string.Empty;
        effect.FindPropertyRelative("value").floatValue=amount;
    }

    private static int Branch(StatTypes stat)=>stat switch
    {
        StatTypes.ArmourPercent or StatTypes.ElementalPlating or StatTypes.DeferredWounds or StatTypes.ChanceToBlock=>(int)PassiveBranch.Defense,
        StatTypes.LifeOnHit or StatTypes.LifeOnHitVsBleeding=>(int)PassiveBranch.LifeOnHit,
        StatTypes.AllRes=>(int)PassiveBranch.Resistances,
        StatTypes.BleedChance=>(int)PassiveBranch.BleedChance,
        StatTypes.ChanceToHitTwice or StatTypes.EscalatingMultistrike=>(int)PassiveBranch.ChanceToHitTwice,
        StatTypes.AttackSpeed or StatTypes.UnbrokenAssault=>(int)PassiveBranch.AttackSpeed,
        StatTypes.WarriorDotMultiplier or StatTypes.RuptureDamage=>(int)PassiveBranch.BleedDamage,
        _=>(int)PassiveBranch.Physical
    };
}
