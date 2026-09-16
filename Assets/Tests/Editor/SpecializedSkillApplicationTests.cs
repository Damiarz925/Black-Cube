using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SpecializedSkillApplicationTests
{
    readonly List<UnityEngine.Object> created=new();
    [TearDown]public void TearDown()
    {
        for(int i=created.Count-1;i>=0;i--)if(created[i]!=null)UnityEngine.Object.DestroyImmediate(created[i]);
        created.Clear();
    }

    [Test]public void ProductionCatalogKeepsPhysicalSkillIdentityAndSpecializedGuarantees()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerSkillCatalog>("Assets/Resources/PlayerSkills.asset");
        Assert.That(catalog,Is.Not.Null);
        var defaults=PlayerSkillDefinition.CreateDefaults();
        foreach(var source in new[]{catalog.skills,defaults})
        {
            Assert.That(Find(source,PlayerSkillId.HeavyStrike).nonMatchingConversion,Is.EqualTo(1f));
            Assert.That(Find(source,PlayerSkillId.Shiv).nonMatchingConversion,Is.EqualTo(1f));
            Assert.That(Find(source,PlayerSkillId.LightningStrike).manaCost,Is.EqualTo(20f));
            foreach(var id in new[]{PlayerSkillId.Envenom,PlayerSkillId.Shiv,PlayerSkillId.Immolate})
                Assert.That(Find(source,id).guaranteedAilmentApplications,Is.EqualTo(1),id.ToString());
        }
    }

    [Test]public void SpecializedCastsApplyPoisonBleedAndIgniteWithZeroChanceStats()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerSkillCatalog>("Assets/Resources/PlayerSkills.asset");
        var attacker=Track(new GameObject("skill attacker",typeof(StatsComponent)));
        var defender=Track(new GameObject("skill defender",typeof(StatsComponent),typeof(StatusController)));
        var battle=Track(new GameObject("skill battle",typeof(BattleManager))).GetComponent<BattleManager>();
        var status=defender.GetComponent<StatusController>();
        InvokeAwake(status);
        var stats=attacker.GetComponent<StatsComponent>();
        StatusEffects poison=Effect(StatusEffects.AilmentKind.Poison,ElementMask.All,.1f,4,0);
        StatusEffects bleed=Effect(StatusEffects.AilmentKind.Bleed,ElementMask.Phys,.5f,5,5);
        StatusEffects ignite=Effect(StatusEffects.AilmentKind.Ignite,ElementMask.Fire,.8f,2,1);
        SetField(battle,"poisonEffect",poison);SetField(battle,"bleedEffect",bleed);SetField(battle,"igniteEffect",ignite);
        var method=typeof(BattleManager).GetMethod("ApplyOnHitEffects",BindingFlags.Instance|BindingFlags.NonPublic);

        var empty=new DamageContext(1);
        var poisonBasis=Hit(Element.Phys,100f);
        method.Invoke(battle,new object[]{empty,stats,status,Find(catalog.skills,PlayerSkillId.Envenom),poisonBasis,false});
        Assert.That(IndependentStatuses(status).Contains(poison),Is.True);
        status.ClearStatuses();

        var shivHit=Hit(Element.Phys,70f);
        var bleedBasis=Hit(Element.Phys,140f);
        method.Invoke(battle,new object[]{shivHit,stats,status,Find(catalog.skills,PlayerSkillId.Shiv),bleedBasis,false});
        Assert.That(IndependentStatuses(status).Contains(bleed),Is.True);
        status.ClearStatuses();

        var immolateHit=Hit(Element.Fire,10f);
        var igniteBasis=Hit(Element.Fire,100f);
        method.Invoke(battle,new object[]{immolateHit,stats,status,Find(catalog.skills,PlayerSkillId.Immolate),igniteBasis,false});
        Assert.That(IndependentStatuses(status).Contains(ignite),Is.True);
    }

    static PlayerSkillDefinition Find(IReadOnlyList<PlayerSkillDefinition> skills,PlayerSkillId id)
    {
        foreach(var skill in skills)if(skill!=null&&skill.id==id)return skill;
        throw new InvalidOperationException("Missing skill "+id);
    }
    StatusEffects Effect(StatusEffects.AilmentKind kind,ElementMask elements,float magnitude,int duration,int cap)
    {
        var effect=ScriptableObject.CreateInstance<StatusEffects>();created.Add(effect);
        effect.ConfigureRuntime("Skill "+kind,StatusEffects.StatusType.DamageOverTime,kind,elements,
            magnitude,duration,cap,StatusEffects.StackPolicy.StackIndependently,2);
        return effect;
    }
    GameObject Track(GameObject value){created.Add(value);return value;}
    static DamageContext Hit(Element element,float amount){var ctx=new DamageContext(1);ctx.AddDamage(element,amount);return ctx;}
    static IDictionary IndependentStatuses(StatusController status)=>(IDictionary)typeof(StatusController)
        .GetField("IndependentDictionary",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(status);
    static void SetField(object value,string name,object assigned)=>value.GetType()
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(value,assigned);
    static void InvokeAwake(object value)=>value.GetType()
        .GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(value,null);
}
