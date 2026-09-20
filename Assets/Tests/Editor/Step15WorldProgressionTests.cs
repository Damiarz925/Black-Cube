using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class Step15WorldProgressionTests
{
    WorldContentDatabase content;

    [SetUp] public void SetUp() => content = WorldContentCatalog.Reference;

    [Test] public void LevelOneStartsForestLocationOneAtZeroCorruption()
    {
        WorldPosition world = WorldProgression.Resolve(1, 1, content);
        Assert.That(world.BiomeIndex, Is.Zero); Assert.That(world.LocationIndex, Is.Zero);
        Assert.That(world.CorruptionIndex, Is.Zero); Assert.That(world.Corruption.percentage, Is.Zero);
        Assert.That(world.Biome.stableId, Is.EqualTo("biome.ashen-march"));
        Assert.That(world.Location.stableId, Is.EqualTo("location.ashen-march.01"));
    }

    [TestCase(10,0,9)] [TestCase(11,1,0)] [TestCase(20,1,9)] [TestCase(21,2,0)]
    [TestCase(50,4,9)] [TestCase(51,5,0)] [TestCase(60,5,9)]
    public void CorruptionAndLocationBoundariesAreDeterministic(int level, int corruption, int location)
    {
        WorldPosition world = WorldProgression.Resolve(level, 1, content);
        Assert.That(world.CorruptionIndex, Is.EqualTo(corruption));
        Assert.That(world.LocationIndex, Is.EqualTo(location));
    }

    [Test] public void BiomeBoundaryMapsSixtyAndSixtyOneExactly()
    {
        WorldPosition sixty = WorldProgression.Resolve(60, 1, content);
        WorldPosition sixtyOne = WorldProgression.Resolve(61, 1, content);
        Assert.That((sixty.BiomeIndex,sixty.LocationIndex,sixty.CorruptionIndex),Is.EqualTo((0,9,5)));
        Assert.That((sixtyOne.BiomeIndex,sixtyOne.LocationIndex,sixtyOne.CorruptionIndex),Is.EqualTo((1,0,0)));
    }

    [Test] public void FinalBoundariesMapThreeFiftyNineAndThreeSixtyExactly()
    {
        WorldPosition a = WorldProgression.Resolve(359, 9, content);
        WorldPosition b = WorldProgression.Resolve(360, 10, content);
        Assert.That((a.BiomeIndex,a.LocationIndex,a.CorruptionIndex),Is.EqualTo((5,8,5)));
        Assert.That((b.BiomeIndex,b.LocationIndex,b.CorruptionIndex),Is.EqualTo((5,9,5)));
        Assert.That(b.Encounter.kind,Is.EqualTo(EncounterKind.Boss));
    }

    [Test] public void EveryAuthoredLevelAndStageResolvesToTheExpectedEncounterKind()
    {
        for(int level=1;level<=360;level++)
        {
            for(int stage=1;stage<=9;stage++)
            {
                WorldPosition world=WorldProgression.Resolve(level,stage,content);
                Assert.That(world.Encounter,Is.Not.Null,$"level {level}, stage {stage}");
                Assert.That(world.Encounter.kind,Is.EqualTo(EncounterKind.Normal));
                Assert.That(content.Enemy(world.Encounter.enemyArchetypeId),Is.Not.Null);
            }
            WorldPosition boss=WorldProgression.Resolve(level,10,content);
            Assert.That(boss.Encounter.kind,Is.EqualTo(EncounterKind.Boss));
            Assert.That(content.Boss(boss.Encounter.bossId),Is.Not.Null);
        }
    }

    [Test] public void ReferenceContentHasUniqueStableIdsAndCompleteMappings()
        => Assert.That(WorldContentValidation.Validate(content),Is.Empty);

    [Test] public void ChallengeDefinitionsRemainOutsideMainProgressionTables()
    {
        Assert.That(content.challengeEncounters,Has.Count.EqualTo(6));
        foreach(var challenge in content.challengeEncounters)
            Assert.That(WorldProgression.Resolve(challenge.minimumCombatLevel,10,content).Encounter.stableId,Does.Not.Contain(challenge.stableContentId));
    }

    [Test] public void PostThreeSixtyUsesDocumentedFinalContentFallback()
    {
        WorldPosition world=WorldProgression.Resolve(999,1,content);
        Assert.That(world.CombatLevel,Is.EqualTo(999));Assert.That(world.ContentLevel,Is.EqualTo(360));
        Assert.That(world.UsesPost360Fallback,Is.True);Assert.That(world.BiomeIndex,Is.EqualTo(5));
        Assert.That(world.LocationIndex,Is.EqualTo(9));Assert.That(world.CorruptionIndex,Is.EqualTo(5));
    }

    [Test] public void SaveRoundTripDerivesTheSameWorldPositionWithoutNewSchemaState()
    {
        string path=Path.Combine(Path.GetTempPath(),"BlackCube-Step15-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            var envelope=new SaveEnvelope{runId="step15",runSeed=15,savedAtUtc="2026-09-18T00:00:00Z",
                payload=new GameStatePayload{combatLevel=359,completedNormalEncounters=8,playerLevel=1,experience=0,
                    availablePassivePoints=1,encounterStartLife=100,encounterStartMana=100}};
            for(int i=0;i<RelicInventory.ActiveSlotCount;i++)envelope.payload.activeRelicIds.Add(string.Empty);
            File.WriteAllText(path,JsonUtility.ToJson(envelope));
            Assert.That(GamePersistence.TryReadFile(path,out var restored,out var error),Is.True,error);
            WorldPosition world=WorldProgression.Resolve(restored.payload.combatLevel,
                restored.payload.completedNormalEncounters+1,content);
            Assert.That((world.BiomeIndex,world.LocationIndex,world.CorruptionIndex,world.Stage),Is.EqualTo((5,8,5,9)));
            Assert.That(restored.schemaVersion,Is.EqualTo(GamePersistence.SchemaVersion));
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }

    [Test] public void WeightedProductionPoolsAreStableAndUseValidBiomeContent()
    {
        for(int level=1;level<=360;level+=17){var a=WorldProgression.Resolve(level,1,content);var b=WorldProgression.Resolve(level,1,content);Assert.That(a.Encounter.enemyArchetypeId,Is.EqualTo(b.Encounter.enemyArchetypeId));Assert.That(content.Enemy(a.Encounter.enemyArchetypeId),Is.Not.Null);}
        Assert.That(content.Boss(WorldProgression.Resolve(1,10,content).Encounter.bossId),Is.Not.Null);
    }
}
