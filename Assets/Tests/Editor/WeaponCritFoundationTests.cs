using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class WeaponCritFoundationTests
{
    readonly List<Object> created = new();

    [TearDown] public void TearDown()
    {
        for (int i=created.Count-1;i>=0;i--) if(created[i]!=null)Object.DestroyImmediate(created[i]);
        created.Clear();
    }

    GameObject New(string name)
    {
        var go=new GameObject(name);created.Add(go);return go;
    }

    [Test] public void WeaponRangeAndPairedFlatAffixRetainBothEndsAndExpectedAverage()
    {
        Gear gear=New("range weapon").AddComponent<Gear>();
        gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys);
        gear.BaseDamage=80;gear.BaseDamageMin=64;gear.BaseDamageMax=96;
        gear.ApplyMods(new List<RolledMod>{new(StatTypes.FlatPhys,1,10f,20f,true)});
        gear.GetEffectiveBaseDamageRange(out float low,out float high);
        Assert.That(low,Is.EqualTo(74f));Assert.That(high,Is.EqualTo(116f));
        Assert.That(gear.GetEffectiveBaseDamage(),Is.EqualTo(95f));
        Random.State old=Random.state;
        try
        {
            Random.InitState(5127);float first=gear.RollEffectiveBaseDamage();
            float second=gear.RollEffectiveBaseDamage();
            Assert.That(first,Is.InRange(low,high));Assert.That(second,Is.InRange(low,high));
            Assert.That(second,Is.Not.EqualTo(first),"Independent hits must not reuse the same range roll.");
            Random.InitState(5127);Assert.That(gear.RollEffectiveBaseDamage(),Is.EqualTo(first));
        }
        finally {Random.state=old;}
        GearSnapshotData snapshot=GearSnapshotData.Capture(gear);
        Assert.That(snapshot.baseDamageMin,Is.EqualTo(64f));
        Assert.That(snapshot.baseDamageMax,Is.EqualTo(96f));
        Assert.That(snapshot.mods[0].HighValue,Is.EqualTo(20f));
    }

    [Test] public void PlayerCriticalStrikeChangesActualLifeAndAilmentBasis()
    {
        var actor=New("crit player");var attacker=actor.AddComponent<StatsComponent>();
        var player=actor.AddComponent<PlayerController>();
        typeof(PlayerController).GetField("stats",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(player,attacker);
        var gear=New("fixed weapon").AddComponent<Gear>();
        gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys);
        gear.BaseDamage=gear.BaseDamageMin=gear.BaseDamageMax=100f;player.EquipWeapon(gear);
        var target=New("crit target");var defender=target.AddComponent<StatsComponent>();
        var health=target.AddComponent<HealthComponent>();var receiver=target.AddComponent<DamageReceiver>();
        typeof(DamageReceiver).GetField("health",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(receiver,health);
        typeof(HealthComponent).GetField("maxLife",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(health,1000f);health.ReviveToFullLife();
        gear.BaseCritChance=0f;DamageContext normal=player.BuildAttackContext();
        Assert.That(normal.IsCrit,Is.False);
        float normalDamage=CombatCalculator.CalculateFinalDamage(normal,attacker,defender);
        float before=health.CurrentLife;receiver.TakeDamage(normalDamage,normal);
        Assert.That(before-health.CurrentLife,Is.EqualTo(normalDamage).Within(.001f));
        health.RestoreFullLife();gear.BaseCritChance=1f;
        DamageContext critical=player.BuildAttackContext();
        Assert.That(critical.IsCrit,Is.True);
        Assert.That(critical.CritMultiplier,Is.EqualTo(CombatCalculator.BaseCriticalMultiplier));
        float critDamage=CombatCalculator.CalculateFinalDamage(critical,attacker,defender);
        before=health.CurrentLife;receiver.TakeDamage(critDamage,critical);
        Assert.That(before-health.CurrentLife,Is.EqualTo(critDamage).Within(.001f));
        Assert.That(critDamage/normalDamage,Is.EqualTo(CombatCalculator.BaseCriticalMultiplier).Within(.001f));
        var bleed=ScriptableObject.CreateInstance<StatusEffects>();created.Add(bleed);
        bleed.ConfigureRuntime("Bleed",StatusEffects.StatusType.DamageOverTime,
            StatusEffects.AilmentKind.Bleed,ElementMask.Phys,.1f,5,5,
            StatusEffects.StackPolicy.StackIndependently,2);
        AilmentCalculator.ComputeAilmentFromHit(bleed,normal,attacker,out float normalTick,out _,out _);
        AilmentCalculator.ComputeAilmentFromHit(bleed,critical,attacker,out float criticalTick,out _,out _);
        Assert.That(criticalTick/normalTick,Is.EqualTo(CombatCalculator.BaseCriticalMultiplier).Within(.001f));
        DamageContext skillCrit=player.BuildAttackContext(Element.Fire,.5f,DamageScope.Magic);
        Assert.That(skillCrit.IsCrit,Is.True);
        Assert.That(skillCrit.CritMultiplier,Is.EqualTo(CombatCalculator.BaseCriticalMultiplier));
    }

    [Test] public void EnemyCriticalStrikeUsesSameRealBaseMultiplier()
    {
        var actor=New("crit enemy");var stats=actor.AddComponent<StatsComponent>();
        var enemy=actor.AddComponent<EnemyAI>();
        typeof(EnemyAI).GetField("stats",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(enemy,stats);
        var weapon=New("enemy fixed weapon").AddComponent<Gear>();
        weapon.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys);
        weapon.BaseDamage=weapon.BaseDamageMin=weapon.BaseDamageMax=100f;
        typeof(EnemyAI).GetField("equippedWeapon",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(enemy,weapon);
        weapon.BaseCritChance=0f;DamageContext normal=enemy.BuildAttackContext();
        weapon.BaseCritChance=1f;DamageContext critical=enemy.BuildAttackContext();
        Assert.That(normal.IsCrit,Is.False);Assert.That(critical.IsCrit,Is.True);
        Assert.That(critical.CritMultiplier,Is.EqualTo(CombatCalculator.BaseCriticalMultiplier));
        Assert.That(critical.Hits[0].Amount/normal.Hits[0].Amount,
            Is.EqualTo(CombatCalculator.BaseCriticalMultiplier).Within(.001f));
        weapon.BaseDamageMin=80f;weapon.BaseDamageMax=120f;
        var low=enemy.BuildNonCriticalAttackContextAtRangeEnd(false);
        var high=enemy.BuildNonCriticalAttackContextAtRangeEnd(true);
        Assert.That(low.Hits[0].Amount,Is.EqualTo(normal.Hits[0].Amount*.8f).Within(.001f));
        Assert.That(high.Hits[0].Amount,Is.EqualTo(normal.Hits[0].Amount*1.2f).Within(.001f));
    }

    [Test] public void CriticalPopupUsesActualHealthLossAndReadableCritAccent()
    {
        var camera=New("popup camera").AddComponent<Camera>();camera.tag="MainCamera";
        var canvasObject=new GameObject("popup canvas",typeof(RectTransform),typeof(Canvas));created.Add(canvasObject);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var root=new GameObject("popup root",typeof(RectTransform));created.Add(root);
        root.transform.SetParent(canvasObject.transform,false);
        var prefab=new GameObject("popup prefab",typeof(RectTransform),typeof(TextMeshProUGUI));created.Add(prefab);
        var popup=New("popup owner").AddComponent<DamagePopup>();
        foreach(var (field,value) in new (string,object)[]{("popupPrefab",prefab),
            ("popupRoot",root.GetComponent<RectTransform>()),("canvas",canvas)})
            typeof(DamagePopup).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(popup,value);
        var target=New("popup target");var health=target.AddComponent<HealthComponent>();
        var receiver=target.AddComponent<DamageReceiver>();
        typeof(HealthComponent).GetField("maxLife",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(health,1000f);health.ReviveToFullLife();
        typeof(DamageReceiver).GetField("health",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(receiver,health);
        typeof(DamageReceiver).GetField("damagePopup",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(receiver,popup);
        float before=health.CurrentLife;
        var normal=new DamageContext(1);normal.AddDamage(Element.Phys,23.4f);
        receiver.TakeDamage(23.4f,normal);
        float delta=before-health.CurrentLife;
        Assert.That(root.transform.childCount,Is.EqualTo(1));
        Assert.That(root.GetComponentInChildren<TextMeshProUGUI>().text,
            Is.EqualTo(Mathf.RoundToInt(delta).ToString()));
        before=health.CurrentLife;
        var critical=new DamageContext(1){IsCrit=true,CritMultiplier=CombatCalculator.BaseCriticalMultiplier};
        critical.AddDamage(Element.Phys,35.1f);
        receiver.TakeDamage(35.1f,critical);
        delta=before-health.CurrentLife;
        Assert.That(root.transform.childCount,Is.EqualTo(2));
        var critText=root.transform.GetChild(1).GetComponentInChildren<TextMeshProUGUI>();
        Assert.That(critText.text,Is.EqualTo("CRIT "+Mathf.RoundToInt(delta)));
        Assert.That(root.transform.GetChild(1).localScale.x,Is.GreaterThan(root.transform.GetChild(0).localScale.x));
    }
}
