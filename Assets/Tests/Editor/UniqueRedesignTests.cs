using System.Linq;
using NUnit.Framework;
using UnityEngine;
using BlackCube.CombatSimulation;
using BlackCube.BalanceWorkbench;

public sealed class UniqueRedesignTests
{
    [Test] public void AllThirteenIdentitiesPreserveImmutableRollsThroughBothSnapshotFormats()
    {
        Assert.That(UniqueCatalog.All.Length,Is.EqualTo(13));Assert.That(UniqueCatalog.All.Select(x=>x.id).Distinct().Count(),Is.EqualTo(13));
        foreach(var definition in UniqueCatalog.All)foreach(float fraction in new[]{0f,.5f,1f})
        {
            var gear=UniqueCatalog.Create(definition.id,100,fixedRoll:fraction);Gear restored=null;GameObject root=null;
            try
            {
                var snapshot=JsonUtility.FromJson<GearSnapshotData>(JsonUtility.ToJson(GearSnapshotData.Capture(gear)));restored=snapshot.Create();
                Assert.That(UniqueCatalog.Validate(restored.UniqueData,restored.ItemType,restored.WeaponTypeId,restored.BaseElement,restored.ItemLevel,restored.rolledMods),Is.True,definition.name);
                Assert.That(JsonUtility.ToJson(restored.UniqueData),Is.EqualTo(JsonUtility.ToJson(gear.UniqueData)));Assert.That(restored.CurrentCraftingPotential,Is.Zero);
                Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.RerollRareModifier,gear),Is.False);
                Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.RemoveRareModifier,gear),Is.False);
                root=new GameObject("snapshot fixture");var lab=GearSnapshot.Capture(gear).Materialize(root.transform,"lab");Assert.That(lab.UniqueData.definitionId,Is.EqualTo(definition.id));
            }
            finally{Object.DestroyImmediate(gear.gameObject);if(restored!=null)Object.DestroyImmediate(restored.gameObject);if(root!=null)Object.DestroyImmediate(root);}
        }
    }
    [Test] public void PrismHasTwoOrThreeDistinctImmutableAurasAndNegativeEffectIsReal()
    {
        var gear=UniqueCatalog.Create("unique.prism-voices",100,fixedRoll:0);
        try{var power=gear.UniqueData.powers.Single();Assert.That(power.value,Is.EqualTo(2));Assert.That(Enumerable.Range(0,5).Count(x=>(power.auraMask&(1<<x))!=0),Is.EqualTo(2));Assert.That(SubclassBalanceProfile.FinalAuraBonus(1,1,-.5f),Is.EqualTo(.5f));}finally{Object.DestroyImmediate(gear.gameObject);}
    }
    [Test] public void UniqueSaveRejectsOutOfRangeOrWrongSlotWithoutReroll()
    {
        var gear=UniqueCatalog.Create("unique.last-breath",100,fixedRoll:.5f);
        try{var data=gear.UniqueData.Copy();data.powers[0].value=2;Assert.That(UniqueCatalog.Validate(data,gear.ItemType,gear.WeaponTypeId,gear.BaseElement,100,gear.rolledMods),Is.False);Assert.That(UniqueCatalog.Create("unique.last-breath",1),Is.Null);}finally{Object.DestroyImmediate(gear.gameObject);}
    }
    [Test] public void ForgeRejectsMissingAuthorityWithoutConsumingAnything()
    {Assert.That(UniqueRelicForge.TryForge(null,null,null,null,null,0,0,()=>0,out var result,out var error),Is.False);Assert.That(result,Is.Null);Assert.That(error,Is.Not.Empty);}
    [Test] public void PoisonMemoryRanksExpectedRemainingDamageAndCopiesTiming()
    {
        var root=new GameObject("poison fixture",typeof(StatsComponent),typeof(StatusController));var source=new GameObject("source",typeof(StatsComponent));var effect=ScriptableObject.CreateInstance<StatusEffects>();
        try{effect.ConfigureRuntime("Poison",StatusEffects.StatusType.DamageOverTime,StatusEffects.AilmentKind.Poison,ElementMask.Void,.05f,1,20,StatusEffects.StackPolicy.StackIndependently);var status=root.GetComponent<StatusController>();status.ApplyStatus(effect,1,2,2,source.GetComponent<StatsComponent>(),1);status.ApplyStatus(effect,1,9,2,source.GetComponent<StatsComponent>(),1);var best=status.HighestRemainingPoison(1);Assert.That(best.Count,Is.EqualTo(1));Assert.That(best[0].damagePerTick,Is.EqualTo(9));Assert.That(best[0].RemainingSeconds,Is.EqualTo(1));Assert.That(best[0].SecondsUntilNextTick,Is.EqualTo(.5f));}finally{Object.DestroyImmediate(root);Object.DestroyImmediate(source);Object.DestroyImmediate(effect);}
    }
    [Test] public void HeadlessHeartGrantsNonAxeRageWithoutCrownPenalty()
    {
        var player=new CombatantSnapshot{id="player",player=true,weaponTypeId=WeaponTypeIds.Sword,maximumLife=1000,attackSpeed=2,basicDamage=new CombatDamageSnapshot{physical=10}};
        player.uniquePowers.Add(new UniqueRoll{power=UniquePower.AnyWeaponRage,value=1});
        var result=HeadlessCombatSimulator.Run(player,new CombatantSnapshot{id="enemy",maximumLife=10000,attackSpeed=.01f},new CombatSimulationConfig{maximumDuration=3,actionPolicy=PlayerActionPolicy.BasicOnly});
        Assert.That(result.error,Is.Null.Or.Empty);Assert.That(result.playerRage,Is.GreaterThan(0));
    }
    [Test] public void HeadlessLastBreathStillRequiresSeparateCullChance()
    {
        var player=new CombatantSnapshot{id="player",player=true,weaponTypeId=WeaponTypeIds.Dagger,maximumLife=1000,attackSpeed=1,basicDamage=new CombatDamageSnapshot{physical=10},cullingStrike=1,cullingChance=1};
        player.uniquePowers.Add(new UniqueRoll{power=UniquePower.LastBreath,value=1});var result=HeadlessCombatSimulator.Run(player,new CombatantSnapshot{id="enemy",maximumLife=1000,attackSpeed=.01f},new CombatSimulationConfig{maximumDuration=2,actionPolicy=PlayerActionPolicy.BasicOnly});Assert.That(result.executionTriggers,Is.Zero,"Last Breath ignores ordinary threshold and has no free Cull.");
    }
    [Test] public void FractionalDurationRetainsAilmentUntilActualExpiry()
    {
        var root=new GameObject("timing fixture",typeof(StatsComponent),typeof(StatusController));var effect=ScriptableObject.CreateInstance<StatusEffects>();
        try
        {
            effect.ConfigureRuntime("Poison",StatusEffects.StatusType.DamageOverTime,StatusEffects.AilmentKind.Poison,ElementMask.Void,.05f,1,20,StatusEffects.StackPolicy.StackIndependently);
            var source=root.GetComponent<StatsComponent>();source.AddModifier(new StatModifier(StatTypes.PoisonDuration,StatOp.Additive,20,root));var status=root.GetComponent<StatusController>();status.ApplyStatus(effect,1,1,2,source,1);
            int expired=0;status.AilmentExpired+=_=>expired++;status.TickRealtime(1.05f);
            Assert.That(status.HasAilment(StatusEffects.AilmentKind.Poison),Is.True);Assert.That(status.AilmentStackCount(StatusEffects.AilmentKind.Poison),Is.EqualTo(1));Assert.That(expired,Is.Zero);
            status.TickRealtime(.2f);Assert.That(status.HasAilment(StatusEffects.AilmentKind.Poison),Is.False);Assert.That(expired,Is.EqualTo(1));
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(effect);}
    }
    [Test] public void IsolatedUniqueLoadoutNeverReadsLiveEquipment()
    {
        var root=new GameObject("isolated fixture",typeof(StatsComponent),typeof(PlayerController),typeof(UniqueLoadoutState));
        try{var state=root.GetComponent<UniqueLoadoutState>();state.powers.Add(new UniqueRoll{power=UniquePower.LessHit,value=.35f});Assert.That(UniqueCatalog.Power(root.GetComponent<StatsComponent>(),UniquePower.LessHit),Is.EqualTo(.35f));Assert.That(UniqueCatalog.AuraMask(root.GetComponent<StatsComponent>()),Is.Zero);}
        finally{Object.DestroyImmediate(root);}
    }
    [Test] public void WorkbenchRelicsRemainAppliedAfterPassiveReevaluationAndAreNotDoubledByAdapter()
    {
        var build=new PlayerBuildSnapshot{playerLevel=100,combatLevel=100,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,
            equipment=new(){new GearSnapshot{slot=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Normal,itemLevel=100,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,baseMin=100,baseMax=100,baseSpeed=1,baseCrit=.05f}},
            activeRelics=new(){new RelicData{modifiers=new(){new RelicModifier(RelicModifierType.MoreDamage,10,true,1),new RelicModifier(RelicModifierType.MoreAttackSpeed,10,false,1),new RelicModifier(RelicModifierType.AllResistances,10,false,1)}}}};
        using var evaluation=new PlayerBuildEvaluation(build);
        var first=evaluation.Metrics;var reused=evaluation.ReevaluatePassives(build);Assert.That(reused.fireResistance,Is.EqualTo(first.fireResistance));Assert.That(reused.basicDps,Is.EqualTo(first.basicDps).Within(.001));
        var snapshot=CombatLabAdapters.PlayerSnapshot(build);Assert.That(snapshot.attackSpeed,Is.EqualTo(first.attacksPerSecond).Within(.001));Assert.That(snapshot.fireResistance,Is.EqualTo(first.fireResistance).Within(.001));
    }
    [Test] public void ExtractedAxeHybridHasTwoLiveConsumersInsteadOfADeadLocalStat()
    {
        var root=new GameObject("extraction fixture",typeof(StatsComponent));
        try
        {
            var stats=root.GetComponent<StatsComponent>();
            var relic=new RelicData{uniqueRelic=true,forgedStats=new(){new RolledMod(StatTypes.AxePhysicalRage,1,15,30,false)}};
            RelicLoadoutRules.ApplyStats(new[]{relic},stats,this);
            Assert.That(stats.GetStat(StatTypes.PhysMult),Is.EqualTo(.15f).Within(.0001));Assert.That(stats.GetStat(StatTypes.RageGeneration),Is.EqualTo(.30f).Within(.0001));
            Assert.That(CharacterDamageEstimate.ExpectedDirectFactor(0,1,1,false,.95f),Is.EqualTo(1.95f).Within(.0001));
        }
        finally{Object.DestroyImmediate(root);}
    }
    [Test] public void CleanCheckpointResetClearsRageGenerationAndUniquePool()
    {
        var root=new GameObject("transient fixture",typeof(StatsComponent),typeof(PlayerController),typeof(UniqueLoadoutState),typeof(RageState),typeof(UniqueCombatRuntime));
        try
        {
            root.GetComponent<UniqueLoadoutState>().powers.Add(new UniqueRoll{power=UniquePower.AnyWeaponRage,value=1});
            var rage=root.GetComponent<RageState>();
            // EditMode does not invoke ordinary MonoBehaviour Awake automatically.
            typeof(RageState).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rage,null);
            rage.GainFromDamageDealt(100,1000);rage.Tick(1);Assert.That(rage.Rage,Is.GreaterThan(0));Assert.That(rage.GenerationRate,Is.GreaterThan(0));
            rage.ResetTransient();Assert.That(rage.Rage,Is.Zero);Assert.That(rage.GenerationRate,Is.Zero);Assert.That(rage.FinisherArmed,Is.False);
            var unique=root.GetComponent<UniqueCombatRuntime>();typeof(UniqueCombatRuntime).GetField("poolUntil",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(unique,100f);typeof(UniqueCombatRuntime).GetField("poolStrength",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(unique,.4f);
            Assert.That(unique.FireTakenMultiplier,Is.EqualTo(1.4f).Within(.0001));unique.ResetTransient();Assert.That(unique.FireTakenMultiplier,Is.EqualTo(1));
        }
        finally{Object.DestroyImmediate(root);}
    }
    [Test] public void GauntletBoundaryIsExactly341Through350()
    {Assert.That(EndgameGauntletRules.IsGauntlet(340),Is.False);Assert.That(EndgameGauntletRules.IsGauntlet(341),Is.True);Assert.That(EndgameGauntletRules.IsGauntlet(350),Is.True);Assert.That(EndgameGauntletRules.IsGauntlet(351),Is.False);}
}
