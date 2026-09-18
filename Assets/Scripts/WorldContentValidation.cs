// Developer map: Deterministic structural validation shared by EditMode tests and authoring reports.
using System.Collections.Generic;
using System.Linq;

public static class WorldContentValidation
{
    static readonly int[] RequiredCorruption = { 0, 20, 40, 60, 80, 100 };

    public static List<string> Validate(WorldContentDatabase database)
    {
        var errors = new List<string>();
        if (database == null) { errors.Add("World content database is missing."); return errors; }
        var ids = new HashSet<string>();
        void Id(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) errors.Add($"{type} has no stable ID.");
            else if (!ids.Add(id)) errors.Add($"Duplicate stable ID: {id}.");
        }

        if (database.corruptionTiers == null || database.corruptionTiers.Count != RequiredCorruption.Length)
            errors.Add("Exactly six corruption tiers are required.");
        else for (int i = 0; i < RequiredCorruption.Length; i++)
        {
            var tier = database.corruptionTiers[i];
            if (tier == null || tier.percentage != RequiredCorruption[i])
                errors.Add($"Corruption index {i} must be {RequiredCorruption[i]}%.");
            if (tier != null) Id(tier.stableId, "Corruption tier");
        }

        foreach (var enemy in database.enemyArchetypes ?? new()) if (enemy != null) Id(enemy.stableId, "Enemy archetype");
        foreach (var boss in database.bosses ?? new()) if (boss != null) Id(boss.stableId, "Boss");
        foreach (var table in database.encounterTables ?? new())
        {
            if (table == null) { errors.Add("Encounter table is null."); continue; }
            Id(table.stableId, "Encounter table");
            if (database.Boss(table.bossId) == null) errors.Add($"{table.stableId} has an invalid boss reference.");
            if (table.normalEnemyPool == null || table.normalEnemyPool.Count == 0)
                errors.Add($"{table.stableId} has no normal enemy pool.");
            else foreach (var entry in table.normalEnemyPool)
                if (entry == null || entry.weight <= 0 || database.Enemy(entry.enemyArchetypeId) == null)
                    errors.Add($"{table.stableId} has an invalid weighted enemy reference.");
        }

        if (database.biomes == null || database.biomes.Count != WorldProgression.BiomeCount)
            errors.Add("Exactly six biome definitions are required.");
        else for (int biomeIndex = 0; biomeIndex < database.biomes.Count; biomeIndex++)
        {
            BiomeDefinition biome = database.biomes[biomeIndex];
            if (biome == null) { errors.Add($"Biome index {biomeIndex} is null."); continue; }
            Id(biome.stableId, "Biome");
            int expectedFirst = biomeIndex * WorldProgression.LevelsPerBiome + 1;
            int expectedLast = expectedFirst + WorldProgression.LevelsPerBiome - 1;
            if (biome.firstCombatLevel != expectedFirst || biome.lastCombatLevel != expectedLast)
                errors.Add($"{biome.stableId} must cover levels {expectedFirst}-{expectedLast}.");
            if (biome.locations == null || biome.locations.Count != WorldProgression.LocationsPerBiome)
            { errors.Add($"{biome.stableId} must define ten base locations."); continue; }
            foreach (var location in biome.locations)
            {
                if (location == null) { errors.Add($"{biome.stableId} contains a null location."); continue; }
                Id(location.stableId, "Location");
                if (database.EncounterTable(location.encounterTableId) == null)
                    errors.Add($"{location.stableId} has an invalid encounter table reference.");
                if (location.corruptionPresentations == null || location.corruptionPresentations.Count != RequiredCorruption.Length)
                    errors.Add($"{location.stableId} must map all six corruption presentations.");
                else foreach (var tier in database.corruptionTiers)
                    if (location.corruptionPresentations.Count(x => x != null && x.corruptionTierId == tier.stableId) != 1)
                        errors.Add($"{location.stableId} does not map {tier.stableId} exactly once.");
            }
        }

        foreach (var challenge in database.challengeEncounters ?? new())
        {
            if (challenge == null) { errors.Add("Challenge encounter is null."); continue; }
            Id(challenge.stableContentId, "Challenge encounter");
            if (database.Boss(challenge.bossId) == null) errors.Add($"{challenge.stableContentId} has an invalid boss reference.");
            if (string.IsNullOrWhiteSpace(challenge.entryResourceId) || challenge.entryResourceAmount < 1
                || string.IsNullOrWhiteSpace(challenge.rewardResourceId)
                || string.IsNullOrWhiteSpace(challenge.specialAffixPoolId))
                errors.Add($"{challenge.stableContentId} has incomplete entry/reward data.");
        }

        for (int level = 1; level <= WorldProgression.MaximumAuthoredCombatLevel; level++)
        {
            WorldPosition normal = WorldProgression.Resolve(level, 1, database);
            WorldPosition boss = WorldProgression.Resolve(level, WorldProgression.BossStage, database);
            if (normal.Biome == null || normal.Location == null || normal.Corruption == null
                || normal.Encounter?.kind != EncounterKind.Normal
                || database.Enemy(normal.Encounter.enemyArchetypeId) == null)
                errors.Add($"Combat level {level} does not resolve to a valid normal encounter.");
            if (boss.Encounter?.kind != EncounterKind.Boss || database.Boss(boss.Encounter.bossId) == null)
                errors.Add($"Combat level {level} stage 10 does not resolve to a valid boss.");
        }
        return errors;
    }
}
