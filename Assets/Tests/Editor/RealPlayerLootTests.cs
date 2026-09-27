using NUnit.Framework;
using UnityEngine;
using BlackCube.CombatSimulation;

public sealed class RealPlayerLootTests
{
    [Test]
    public void RareAdditiveThenPowerProducesThirtyPercent()
    {
        var profile=ScriptableObject.CreateInstance<LootDropBalanceProfileSO>();
        try
        {
            profile.ResetToInitialDefaults();
            var context=new DropRateContext(50,EnemyAI.EnemyRarity.Rare,false,3f);
            var result=profile.EvaluateCurrency(CraftingCurrencyType.NormalToMagic,context);
            Assert.That(result.baseChance,Is.EqualTo(.05f));
            Assert.That(result.rarityAdditive,Is.EqualTo(.05f));
            Assert.That(result.finalBudget,Is.EqualTo(.30f).Within(.00001f));
            Assert.That(profile.EvaluateCurrency(CraftingCurrencyType.AffixFocus,
                new DropRateContext(49,EnemyAI.EnemyRarity.Rare,false,3f)).finalBudget,Is.Zero);
        }
        finally { Object.DestroyImmediate(profile); }
    }

    [Test]
    public void BudgetsOverOneRollGuaranteedCopiesPlusStochasticRemainder()
    {
        var low=new SeededSimulationRandomSource(10);
        Assert.That(LootDropBalanceProfileSO.RollCopies(1.8f,new DelegateLootRandomSource(()=>0f)),Is.EqualTo(2));
        Assert.That(LootDropBalanceProfileSO.RollCopies(1.8f,new DelegateLootRandomSource(()=>.9f)),Is.EqualTo(1));
        Assert.That(LootDropBalanceProfileSO.RollCopies(2.4f,new DelegateLootRandomSource(()=>.1f)),Is.EqualTo(3));
        Assert.That(LootDropBalanceProfileSO.RollCopies(3f,low),Is.EqualTo(3));
    }

    [Test]
    public void StageArchetypeEnemyAndBossOverridesApplyInPublishedOrder()
    {
        var profile=ScriptableObject.CreateInstance<LootDropBalanceProfileSO>();
        try
        {
            profile.ResetToInitialDefaults();
            profile.stages.Add(new DropRateOverride{stableId="s",appliesToAllCurrencies=true,additive=.01f,multiplier=2f});
            profile.archetypes.Add(new DropRateOverride{stableId="a",currency=CraftingCurrencyType.NormalToMagic,additive=.02f,multiplier=1.5f});
            profile.enemies.Add(new DropRateOverride{stableId="e",appliesToAllCurrencies=true,additive=.03f,multiplier=2f});
            var context=new DropRateContext(50,EnemyAI.EnemyRarity.Normal,false,2f,"s",null,"a","e");
            var result=profile.EvaluateCurrency(CraftingCurrencyType.NormalToMagic,context);
            Assert.That(result.finalBudget,Is.EqualTo((.05f+.01f+.02f+.03f)*2f*1.5f*2f*2f).Within(.00001f));
        }
        finally { Object.DestroyImmediate(profile); }
    }

    [Test]
    public void EnemyPowerSeparatesOffenseAndDefensePressure()
    {
        var player=new CombatantSnapshot{maximumLife=1000,attackSpeed=1,
            basicDamage=new CombatDamageSnapshot{physical=100}};
        var average=new CombatantSnapshot{maximumLife=500,attackSpeed=1,
            basicDamage=new CombatDamageSnapshot{physical=50}};
        var reference=new EnemyLootPowerReferenceRow{combatLevel=50,rarity=EnemyAI.EnemyRarity.Rare,
            referencePlayer=player,averageEnemy=average};
        var neutral=EnemyLootPowerScorer.Evaluate(average,reference);
        Assert.That(neutral.available,Is.True);
        Assert.That(neutral.power,Is.EqualTo(1f).Within(.0001f));
        var strongerOffense=new CombatantSnapshot{maximumLife=500,attackSpeed=1,
            basicDamage=new CombatDamageSnapshot{physical=100}};
        var strongerDefense=new CombatantSnapshot{maximumLife=1000,attackSpeed=1,
            basicDamage=new CombatDamageSnapshot{physical=50}};
        Assert.That(EnemyLootPowerScorer.Evaluate(strongerOffense,reference).power,Is.GreaterThan(1f));
        Assert.That(EnemyLootPowerScorer.Evaluate(strongerDefense,reference).power,Is.GreaterThan(1f));
        var weaker=new CombatantSnapshot{maximumLife=250,attackSpeed=1,
            basicDamage=new CombatDamageSnapshot{physical=25}};
        Assert.That(EnemyLootPowerScorer.Evaluate(weaker,reference).power,Is.LessThan(1f));
    }
}
