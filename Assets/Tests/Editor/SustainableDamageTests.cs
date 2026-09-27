using System.Collections.Generic;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class SustainableDamageTests
{
    [Test]
    public void Optimizer_SkipsWeakSecondQueuedSkillWhenItDisplacesBetterOne()
    {
        var metrics=new PlayerBuildMetrics{basicDps=20,attacksPerSecond=1,mana=100,
            manaRegen=60,skills=new List<SkillAnalyticalMetrics>
            {
                new(){castMode=PlayerSkillCastMode.QueuedAttackReplacement.ToString(),
                    averageDamagePerUse=120,manaCost=60},
                new(){castMode=PlayerSkillCastMode.QueuedAttackReplacement.ToString(),
                    averageDamagePerUse=10,manaCost=30}
            }};
        var result=SustainableDamageEstimator.Best(metrics);
        Assert.That(result.policy,Is.EqualTo("Skill 1 Only"));
        Assert.That(result.totalDps,Is.GreaterThan(metrics.basicDps));
    }

    [Test]
    public void ManaRegen_CanOutvalueSmallBasicDamageIncrease()
    {
        var metrics=new PlayerBuildMetrics{basicDps=10,attacksPerSecond=1,mana=30,
            manaRegen=1,skills=new List<SkillAnalyticalMetrics>
            {
                new(){castMode=PlayerSkillCastMode.AutoCooldown.ToString(),
                    averageDamagePerUse=100,manaCost=30,effectiveCooldown=1}
            }};
        double baseline=SustainableDamageEstimator.Best(metrics).totalDps;
        metrics.basicDps+=5;
        double damageUpgrade=SustainableDamageEstimator.Best(metrics).totalDps;
        metrics.basicDps-=5;
        metrics.manaRegen+=20;
        double regenUpgrade=SustainableDamageEstimator.Best(metrics).totalDps;
        Assert.That(regenUpgrade,Is.GreaterThan(damageUpgrade));
        Assert.That(regenUpgrade,Is.GreaterThan(baseline));
    }
}
