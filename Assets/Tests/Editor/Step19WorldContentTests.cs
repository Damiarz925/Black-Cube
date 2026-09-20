using System.Linq;
using NUnit.Framework;

public sealed class Step19WorldContentTests
{
    WorldContentDatabase db;
    [SetUp]public void SetUp()=>db=WorldContentCatalog.Reference;

    [Test]public void ProductionCatalogHasLockedCountsAndNoValidationErrors()
    {
        Assert.That(db.enemyArchetypes,Has.Count.EqualTo(48));
        Assert.That(db.enemyArchetypes.Count(x=>x.rank==EnemyContentRank.Elite),Is.EqualTo(12));
        Assert.That(db.bosses.Count(x=>!x.challengeBoss),Is.EqualTo(60));
        Assert.That(db.bosses.Count(x=>x.challengeBoss),Is.EqualTo(6));
        Assert.That(db.challengeEncounters,Has.Count.EqualTo(6));
        Assert.That(db.enemySkills.Any(x=>x.kind==EnemySkillKind.Recover),Is.True);
        Assert.That(WorldContentValidation.Validate(db),Is.Empty);
    }

    [Test]public void AllThreeThousandSixHundredStagesResolveDeterministically()
    {
        int count=0;for(int level=1;level<=360;level++)for(int stage=1;stage<=10;stage++)
        {var a=WorldProgression.Resolve(level,stage,db);var b=WorldProgression.Resolve(level,stage,db);Assert.That(a.Encounter.stableId,Is.EqualTo(b.Encounter.stableId));Assert.That(a.Encounter.enemyArchetypeId,Is.EqualTo(b.Encounter.enemyArchetypeId));count++;}
        Assert.That(count,Is.EqualTo(3600));
    }

    [Test]public void BiomeIdentityAndCorruptionEvolutionAreMechanical()
    {
        for(int b=0;b<6;b++){var biome=db.biomes[b];Assert.That(biome.placeholder,Is.False);Assert.That(biome.locations.Select(x=>x.mechanicProfileId).Distinct().Count(),Is.EqualTo(10));Assert.That(db.enemyArchetypes.Count(x=>x.stableId.StartsWith("enemy."+biome.stableId.Substring(6)+".")),Is.EqualTo(8));}
        Assert.That(db.corruptionMechanicProfiles.Select(x=>x.damageMultiplier),Is.Ordered.Ascending);
        Assert.That(db.corruptionMechanicProfiles.Last().mechanicIds,Does.Contain("corruption.apex-modifier"));
    }

    [Test]public void EnemyActionsAreDeterministicAndCadenced()
    {
        string loadout=db.enemyArchetypes[0].skillLoadoutId;
        for(int turn=0;turn<24;turn++)Assert.That(EnemyActionPlanner.Select(db,loadout,turn).stableId,Is.EqualTo(EnemyActionPlanner.Select(db,loadout,turn).stableId));
        Assert.That(EnemyActionPlanner.Select(db,loadout,1).hitCount,Is.GreaterThanOrEqualTo(1));
    }

    [Test]public void EncounterCompositionUsesStableWorldSeedWithoutChangingStructure()
    {
        var a=WorldProgression.Resolve(1,1,db,1919);var repeat=WorldProgression.Resolve(1,1,db,1919);
        Assert.That(a.Encounter.enemyArchetypeId,Is.EqualTo(repeat.Encounter.enemyArchetypeId));
        Assert.That(WorldProgression.Resolve(1,10,db,1919).Encounter.bossId,Is.EqualTo(WorldProgression.Resolve(1,10,db,2020).Encounter.bossId));
    }

    [Test]public void StoryBossAtLevelOneHundredOwnsSubclassMilestone()
    {
        var boss=db.Boss(WorldProgression.Resolve(100,10,db).Encounter.bossId);
        Assert.That(boss.futureStoryFlags,Does.Contain(PlayerIdentityState.StoryCompletionMilestoneId));
    }

    [Test]public void ChallengeResourcesUseInjectedLootEntropyAndRepeatableSpendRewardFlow()
    {
        var service=new ChallengeContentService();var challenge=db.challengeEncounters[0];
        string drop=service.RollEntryResource(db,challenge.minimumCombatLevel,new SequenceLootRandomSource(19,0f,0f),1f);
        Assert.That(drop,Is.EqualTo(challenge.entryResourceId));Assert.That(service.TryEnter(challenge),Is.True);Assert.That(service.Complete(challenge),Is.True);Assert.That(service.Count(challenge.rewardResourceId),Is.EqualTo(1));
        service.Add(challenge.entryResourceId,1);Assert.That(service.TryEnter(challenge),Is.True);
        Assert.That(db.ChallengeSpecialPool(challenge.specialAffixPoolId).modifiers,Has.Count.GreaterThanOrEqualTo(2));
    }
}
