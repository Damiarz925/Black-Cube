using BlackCube.CombatSimulation;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class FullLevelRecoveryOverrideTests
{
    [Test]
    public void NoRecoveryOverride_DisablesRenewableLifeButPreservesOffenseAndMana()
    {
        var player=new CombatantSnapshot{player=true,maximumLife=100,maximumMana=40,
            attackSpeed=1,lifeRegeneration=10,lifeOnHit=10,
            basicDamage=new CombatDamageSnapshot{physical=1}};
        var enemy=new CombatantSnapshot{maximumLife=1000,attackSpeed=1,
            basicDamage=new CombatDamageSnapshot()};
        var normal=HeadlessCombatSimulator.Run(player,enemy,new CombatSimulationConfig
            {maximumDuration=3,startingPlayerLife=50,startingPlayerMana=20,retainTrace=false});
        var noRecovery=HeadlessCombatSimulator.Run(player,enemy,new CombatSimulationConfig
            {maximumDuration=3,startingPlayerLife=50,startingPlayerMana=20,
                disablePlayerLifeRecovery=true,retainTrace=false});
        Assert.That(normal.playerLife,Is.GreaterThan(noRecovery.playerLife));
        Assert.That(noRecovery.playerLife,Is.EqualTo(50).Within(.001));
        Assert.That(noRecovery.playerMana,Is.EqualTo(normal.playerMana));
        Assert.That(noRecovery.playerAttacks,Is.EqualTo(normal.playerAttacks));
        Assert.That(noRecovery.healing,Is.Empty);
    }

    [Test]
    public void FullLevelCalibration_UsesProductionStageOrder()
    {
        var result=FullLevelCalibrationSimulator.Run(new FullLevelCalibrationRequest
        {
            player=new PlayerBuildSnapshot{playerLevel=1,combatLevel=1},
            combatLevel=1,trials=1,seed=231,maximumFightDuration=5,noRecovery=true
        });
        Assert.That(result.trials,Is.EqualTo(1));
        Assert.That(result.failedAtStage.Count+result.clears,Is.EqualTo(1));
        Assert.That(result.bossReached,Is.LessThanOrEqualTo(1));
    }
}
