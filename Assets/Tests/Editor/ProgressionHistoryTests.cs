using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class ProgressionHistoryTests
{
    [Test]
    public void SeededHistory_ReplaysAndUsesProductionTenStageSequence()
    {
        var request=new ProgressionHistoryRequest{targetPlayerLevel=10,seed=9123,
            stochastic=true,mode=ProgressionHistoryMode.TargetFarming,extraClears=2};
        var first=ProgressionHistorySimulator.Run(request);
        var second=ProgressionHistorySimulator.Run(request);
        Assert.That(first.targetReached,Is.True);
        Assert.That(first.farmingEncounters,Is.EqualTo(2*WorldProgression.BossStage));
        Assert.That(first.bossKills,Is.EqualTo(first.encounters.Count(x=>x.boss)));
        Assert.That(first.encounters.All(x=>x.stage>=1&&x.stage<=WorldProgression.BossStage),Is.True);
        Assert.That(first.encounters.All(x=>x.itemLevel<=100),Is.True);
        Assert.That(first.gearDrops,Is.EqualTo(second.gearDrops));
        Assert.That(first.currencies.Select(x=>x.count),Is.EqualTo(second.currencies.Select(x=>x.count)));
    }

    [Test]
    public void ExpectedHistory_UsesFractionalRarityCountsAndDirectDropBudgets()
    {
        var result=ProgressionHistorySimulator.Run(new ProgressionHistoryRequest
            {targetPlayerLevel=10,stochastic=false,assumedEnemyPower=1f});
        Assert.That(result.targetReached,Is.True);
        Assert.That(result.normalKills+result.magicKills+result.rareKills+result.legendaryKills+
            result.bossKills,Is.EqualTo(result.straightEncounters).Within(.0001));
        Assert.That(result.gearDrops,Is.GreaterThanOrEqualTo(result.straightEncounters));
        Assert.That(result.warning,Does.Contain("legacy weighted"));
    }

    [Test]
    public void HistoricalInventory_KeepsEveryDropAndOriginalItemLevel()
    {
        var history=ProgressionHistorySimulator.Run(new ProgressionHistoryRequest
            {targetPlayerLevel=3,stochastic=true,seed=53});
        var inventory=RealisticInventoryGenerator.Generate(history,54);
        Assert.That(inventory.items.Count,Is.EqualTo(history.gearDrops));
        Assert.That(inventory.items.All(x=>x.itemLevel>=1&&x.itemLevel<=100),Is.True);
        Assert.That(inventory.items.Any(x=>x.itemLevel<history.finalCombatLevel+3),Is.True);
    }
}
