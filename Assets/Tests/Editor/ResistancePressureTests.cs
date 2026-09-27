using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class ResistancePressureTests
{
    [Test]
    public void T1Pressure_UsesLegalProductionSuffixesAndTierGates()
    {
        using var session=new WorkbenchSession();
        var result=ResistancePressureAnalyzer.Analyze(session.Roller.Database);
        Assert.That(result.t1Options.Select(x=>x.stat).Distinct(),
            Does.Contain(StatTypes.FireRes));
        Assert.That(result.t1Options.Select(x=>x.stat).Distinct(),
            Does.Contain(StatTypes.ColdRes));
        Assert.That(result.t1Options.Select(x=>x.stat).Distinct(),
            Does.Contain(StatTypes.LightRes));
        Assert.That(result.t1Options.Select(x=>x.stat).Distinct(),
            Does.Contain(StatTypes.VoidRes));
        Assert.That(result.allT1AvailableAt,Is.InRange(1,100));
        Assert.That(result.referenceInvestmentAvailableAt,Is.InRange(result.allT1AvailableAt,100));
        Assert.That(result.t1Options.All(x=>x.midpoint>0f&&x.midpoint<1f),Is.True);
        Assert.That(result.referenceInvestment.GroupBy(x=>x.slot).All(x=>x.Count()<=2),Is.True);
        Assert.That(result.referenceInvestment.GroupBy(x=>(x.slot,x.stat)).All(x=>x.Count()==1),Is.True);
        Assert.That(result.earlierTiers,Is.Not.Empty);
        Assert.That(result.earlierTiers.All(x=>x.fire<=result.target&&x.cold<=result.target&&
            x.lightning<=result.target&&x.voidResistance<=result.target),Is.True);
    }
}
