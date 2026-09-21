using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class Step18ProductionTests
{
    [Test] public void StableStatsAppendWithoutReinterpretingCastSpeed()
    {Assert.That((int)StatTypes.CastSpeed,Is.EqualTo(120));Assert.That((int)StatTypes.CooldownReduction,Is.EqualTo(126));Assert.That((int)StatTypes.AuraEffect,Is.EqualTo(127));Assert.That((int)StatTypes.PoisonSpeed,Is.EqualTo(128));}

    [Test] public void EveryWeaponBindsExactlyTwoUniqueProductionSkills()
    {
        var definitions=PlayerSkillDefinition.CreateProductionDefaults().Where(x=>!string.IsNullOrEmpty(x.stableId)).ToArray();Assert.That(definitions,Has.Length.EqualTo(12));Assert.That(definitions.Select(x=>x.stableId).Distinct().Count(),Is.EqualTo(12));
        foreach(var weapon in WeaponTypeCatalog.All){var ids=WeaponSkillBindings.For(weapon.Id);Assert.That(ids.Count,Is.EqualTo(2),weapon.Id);foreach(var id in ids)Assert.That(definitions.Any(x=>x.id==id&&x.weaponTypeId==weapon.Id),Is.True,id.ToString());}
    }

    [Test] public void ProductionSkillPlaceholderProfilesMatchContract()
    {
        var skills=PlayerSkillDefinition.CreateProductionDefaults().ToDictionary(x=>x.id);
        Assert.That(WeaponMechanicProfile.RapidFlurryHits(0),Is.EqualTo(3));Assert.That(WeaponMechanicProfile.RapidFlurryHits(1.5f),Is.EqualTo(6));Assert.That(WeaponMechanicProfile.RapidFlurryHits(99),Is.EqualTo(8));
        Assert.That(WeaponMechanicProfile.ArmourStrikeMultiplier(0),Is.EqualTo(1.5f));Assert.That(WeaponMechanicProfile.ArmourStrikeMultiplier(10000),Is.EqualTo(4.5f));
        Assert.That(skills[PlayerSkillId.BowVenomShot].suppressDirectDamage,Is.True);Assert.That(skills[PlayerSkillId.BowVenomShot].ailmentBasisMultiplier,Is.EqualTo(3));Assert.That(skills[PlayerSkillId.BowDoubleVolley].secondaryMultiplier,Is.EqualTo(2));
        Assert.That(skills[PlayerSkillId.StaffFireball].castMode,Is.EqualTo(PlayerSkillCastMode.AutoCooldown));Assert.That(skills[PlayerSkillId.StaffFireball].baseCooldown,Is.EqualTo(5));Assert.That(skills[PlayerSkillId.DaggerQuickStrike].castMode,Is.EqualTo(PlayerSkillCastMode.ImmediateCooldown));Assert.That(skills[PlayerSkillId.DaggerQuickStrike].baseCooldown,Is.EqualTo(4));
    }

    [Test] public void CooldownFormulaUsesNewStatAndClamp()
    {
        var go=new GameObject("cdr");try{var stats=go.AddComponent<StatsComponent>();go.AddComponent<ManaComponent>();var controller=go.AddComponent<PlayerSkillController>();var skill=new PlayerSkillDefinition{baseCooldown=4,scalesWithCooldownReduction=true};stats.SetBaseStat(StatTypes.CooldownReduction,100);stats.SetBaseStat(StatTypes.CastSpeed,900);Assert.That(controller.EffectiveCooldown(skill),Is.EqualTo(2).Within(.001));skill.baseCooldown=.01f;Assert.That(controller.EffectiveCooldown(skill),Is.EqualTo(.2f));}finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void FreezeConsumesOneEnemyOpportunityAndShatterUsesStoredStrength()
    {
        var go=new GameObject("freeze");try{var status=go.AddComponent<StatusController>();Assert.That(status.ApplyFreeze(.5f),Is.True);Assert.That(status.IsFrozen,Is.True);Assert.That(status.ConsumeFrozenAttackSkip(),Is.True);Assert.That(status.ConsumeFrozenAttackSkip(),Is.False);status.ApplyFreeze(.25f);Assert.That(status.TryConsumeFreeze(out var strength),Is.True);Assert.That(strength,Is.EqualTo(.25f));Assert.That(WeaponMechanicProfile.ShatterMultiplier(strength),Is.EqualTo(4f));}finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void TwelveSubclassesAndModularEffectsAreStable()
    {Assert.That(SubclassCatalog.All.Count,Is.EqualTo(12));foreach(var cls in PlayerClassCatalog.All)Assert.That(SubclassCatalog.ForClass(cls.Id).Count,Is.EqualTo(2));Assert.That(SubclassEffectCatalog.All.Select(x=>x.Id).Distinct().Count(),Is.EqualTo(SubclassEffectCatalog.All.Count));Assert.That(SubclassEffectCatalog.TryGet("subclass.priest.dark.heal_to_harm",out _),Is.True);}

    [Test] public void ModularSubclassEffectCanBeGrantedWithoutSelectingItsSubclass()
    {var go=new GameObject("external-effect");try{go.AddComponent<StatsComponent>();go.AddComponent<PlayerController>();var state=go.AddComponent<SubclassCombatState>();Assert.That(state.Has(SubclassIds.PriestDark),Is.False);Assert.That(state.GrantExternalEffect("subclass.priest.dark.heal_to_harm"),Is.True);Assert.That(state.HasEffect("subclass.priest.dark.heal_to_harm"),Is.True);Assert.That(state.RevokeExternalEffect("subclass.priest.dark.heal_to_harm"),Is.True);}finally{UnityEngine.Object.DestroyImmediate(go);}}

    [Test] public void SubclassFormulaProfilesCoverComboFocusedShockAndAuras()
    {Assert.That(SubclassBalanceProfile.ComboMultiplier(0),Is.EqualTo(1));Assert.That(SubclassBalanceProfile.ComboMultiplier(99),Is.EqualTo(1.5f));Assert.That(SubclassBalanceProfile.FocusedMultiplier(5),Is.EqualTo(4));Assert.That(SubclassBalanceProfile.AuraIntensity(50,1000),Is.EqualTo(.5f));Assert.That(SubclassBalanceProfile.FinalAuraBonus(.2f,.5f,.5f),Is.EqualTo(.15f).Within(.001));Assert.That(SubclassBalanceProfile.AilmentExtraMore(2),Is.EqualTo(1.25f));Assert.That(SubclassBalanceProfile.CriticalAilmentMultiplier(2),Is.EqualTo(1.5f));}

    [Test] public void PoisonSpeedCreatesMoreTicksWithoutChangingBaseDurationWindow()
    {
        var source=new GameObject("poison-speed");var effect=ScriptableObject.CreateInstance<StatusEffects>();
        try{var stats=source.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.PoisonSpeed,25);effect.ConfigureRuntime("Poison",StatusEffects.StatusType.DamageOverTime,StatusEffects.AilmentKind.Poison,ElementMask.All,.1f,4,10,StatusEffects.StackPolicy.StackIndependently,2);var instance=new StatusInstance(effect,10,1,4,stats,2);Assert.That(instance.TickRateMultiplier,Is.EqualTo(1.25f));Assert.That(instance.remainingTicks,Is.EqualTo(5));Assert.That(instance.remainingDurationTurns,Is.EqualTo(8));Assert.That(AilmentCalculator.PoisonTickInterval(2,.25f),Is.EqualTo(1.6f).Within(.001));}finally{UnityEngine.Object.DestroyImmediate(effect);UnityEngine.Object.DestroyImmediate(source);}
    }

    [Test] public void IndependentShockInstancesCombineAndExpire()
    {
        var go=new GameObject("shock-instances");try{var status=go.AddComponent<StatusController>();status.AddShockInstance(.2f,2,3);status.AddShockInstance(.2f,3,3);status.AddShockInstance(.2f,4,3);Assert.That(status.CombinedShockEffect,Is.EqualTo(.728f).Within(.001));status.TickStatuses();Assert.That(status.CombinedShockEffect,Is.EqualTo(.728f).Within(.001));status.TickStatuses();Assert.That(status.CombinedShockEffect,Is.EqualTo(.44f).Within(.001));}finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void PassiveTreePatchPreservesTopologyAndProvidesNewStats()
    {Assert.That(PassiveTreeDefinition.NodeCount,Is.EqualTo(750));Assert.That(PassiveTreeDefinition.Nodes.SelectMany(x=>x.Effects).Any(x=>x.Stat==StatTypes.CooldownReduction),Is.True);Assert.That(PassiveTreeDefinition.Nodes.SelectMany(x=>x.Effects).Any(x=>x.Stat==StatTypes.ShockEffect),Is.True);Assert.That(PassiveTreeDefinition.Nodes.SelectMany(x=>x.Effects).Any(x=>x.Stat==StatTypes.ChillEffect),Is.True);Assert.That(PassiveTreeDefinition.Nodes.SelectMany(x=>x.Effects).Any(x=>x.Stat==StatTypes.AuraEffect),Is.True);}

    [Test] public void SchemaNineMigratesToTenWithEmptyPermanentConfiguration()
    {
        string path=Path.Combine(Path.GetTempPath(),"BlackCube-Step18-"+Guid.NewGuid().ToString("N")+".json");try{var payload=new GameStatePayload{playerLevel=1,availablePassivePoints=1,encounterStartLife=100,encounterStartMana=100};for(int i=0;i<RelicInventory.ActiveSlotCount;i++)payload.activeRelicIds.Add(string.Empty);var envelope=new SaveEnvelope{schemaVersion=9,runId="step18",runSeed=18,savedAtUtc=DateTime.UtcNow.ToString("O"),payload=payload};File.WriteAllText(path,JsonUtility.ToJson(envelope));Assert.That(GamePersistence.TryReadFile(path,out var migrated,out var error),Is.True,error);Assert.That(migrated.schemaVersion,Is.EqualTo(GamePersistence.SchemaVersion));Assert.That(migrated.payload.passiveRanks,Is.Empty);Assert.That(migrated.payload.subclassProjectileMode,Is.EqualTo(SubclassProjectileMode.Volley));}finally{if(File.Exists(path))File.Delete(path);}
    }

    [Test] public void SubclassDefinitionsPointAtV3Slots()
    {Assert.That(SubclassCatalog.All.All(x=>x.PassiveSectionId.StartsWith("tree.v3.subclass.",StringComparison.Ordinal)),Is.True);}
}
