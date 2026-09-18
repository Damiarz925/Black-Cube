// Developer map: Authoritative 1-360 world mapping and data contracts for progression,
// encounter pools, bosses and progression-independent challenge encounters.
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EncounterKind { Normal, Boss }

[Serializable]
public sealed class CorruptionTierDefinition
{
    public string stableId;
    [Range(0, 100)] public int percentage;
    public string displayName;
    public string presentationHookId;
}

[Serializable]
public sealed class CorruptionPresentationDefinition
{
    public string corruptionTierId;
    public Sprite backgroundOverride;
    public Sprite overlay;
    public string environmentSetId;
}

[Serializable]
public sealed class LocationDefinition
{
    public string stableId;
    public string displayName;
    public Sprite baseBackground;
    public string baseBackgroundAddress;
    public string optionalEnvironmentSetId;
    public string enemySpawnPresentationId;
    public string encounterTableId;
    public List<CorruptionPresentationDefinition> corruptionPresentations = new();

    public CorruptionPresentationDefinition Presentation(string tierId) =>
        corruptionPresentations?.Find(x => x != null && x.corruptionTierId == tierId);
}

[Serializable]
public sealed class BiomeDefinition
{
    public string stableId;
    public string displayName;
    public int firstCombatLevel;
    public int lastCombatLevel;
    public bool placeholder;
    public List<LocationDefinition> locations = new();
}

[Serializable]
public sealed class EnemyArchetypeDefinition
{
    public string stableId;
    public string displayName;
    public GameObject prefab;
    public string codexEntryId;
    public List<string> futureSkillIds = new();
    public List<string> futureContentHooks = new();
}

[Serializable]
public sealed class BossDefinition
{
    public string stableId;
    public string displayName;
    public GameObject prefab;
    public string codexEntryId;
    public string presentationId;
    public List<string> futureMechanicIds = new();
    public List<string> futureSkillIds = new();
    public List<string> futureRewardHooks = new();
    public List<string> futureStoryFlags = new();
}

[Serializable]
public sealed class WeightedEnemyArchetype
{
    public string enemyArchetypeId;
    [Min(1)] public int weight = 1;
}

[Serializable]
public sealed class EncounterTableDefinition
{
    public string stableId;
    public List<WeightedEnemyArchetype> normalEnemyPool = new();
    public string bossId;
    public List<string> futureEnemySkillIds = new();
    public List<string> futureEncounterModifierIds = new();
    public List<string> futureRewardHooks = new();
}

[Serializable]
public sealed class EncounterDefinition
{
    public string stableId;
    public string encounterTableId;
    public EncounterKind kind;
    public int stage;
    public string enemyArchetypeId;
    public string bossId;
    public List<string> futureModifierIds;
    public List<string> futureRewardHooks;
}

[Serializable]
public sealed class ChallengeEncounterDefinition
{
    public string stableContentId;
    public string displayName;
    public int minimumCombatLevel;
    public string entryResourceId;
    public int entryResourceAmount = 1;
    public string bossId;
    public string rewardResourceId;
    public string specialAffixPoolId;
    public bool repeatable = true;
    public List<string> unlockRequirementIds = new();
}

[CreateAssetMenu(menuName = "Black Cube/World Content Database")]
public sealed class WorldContentDatabase : ScriptableObject
{
    public List<BiomeDefinition> biomes = new();
    public List<CorruptionTierDefinition> corruptionTiers = new();
    public List<EnemyArchetypeDefinition> enemyArchetypes = new();
    public List<BossDefinition> bosses = new();
    public List<EncounterTableDefinition> encounterTables = new();
    public List<ChallengeEncounterDefinition> challengeEncounters = new();

    public BiomeDefinition Biome(string id) => biomes?.Find(x => x != null && x.stableId == id);
    public CorruptionTierDefinition Corruption(string id) => corruptionTiers?.Find(x => x != null && x.stableId == id);
    public EnemyArchetypeDefinition Enemy(string id) => enemyArchetypes?.Find(x => x != null && x.stableId == id);
    public BossDefinition Boss(string id) => bosses?.Find(x => x != null && x.stableId == id);
    public EncounterTableDefinition EncounterTable(string id) => encounterTables?.Find(x => x != null && x.stableId == id);
    public ChallengeEncounterDefinition Challenge(string id) => challengeEncounters?.Find(x => x != null && x.stableContentId == id);
}

public readonly struct WorldPosition
{
    public readonly int CombatLevel, ContentLevel, BiomeIndex, LocationIndex, CorruptionIndex, Stage;
    public readonly bool UsesPost360Fallback;
    public readonly BiomeDefinition Biome;
    public readonly LocationDefinition Location;
    public readonly CorruptionTierDefinition Corruption;
    public readonly EncounterDefinition Encounter;

    public WorldPosition(int combatLevel, int contentLevel, int biomeIndex, int locationIndex,
        int corruptionIndex, int stage, bool fallback, BiomeDefinition biome,
        LocationDefinition location, CorruptionTierDefinition corruption, EncounterDefinition encounter)
    {
        CombatLevel = combatLevel; ContentLevel = contentLevel; BiomeIndex = biomeIndex;
        LocationIndex = locationIndex; CorruptionIndex = corruptionIndex; Stage = stage;
        UsesPost360Fallback = fallback; Biome = biome; Location = location;
        Corruption = corruption; Encounter = encounter;
    }

    public string BiomeLabel => Biome?.displayName ?? "Unknown Biome";
    public string LocationLabel => Location?.displayName ?? "Unknown Location";
    public string CorruptionLabel => Corruption?.displayName ?? "Unknown Corruption";
}

public static class WorldProgression
{
    public const int BiomeCount = 6;
    public const int LocationsPerBiome = 10;
    public const int CorruptionTierCount = 6;
    public const int LevelsPerBiome = LocationsPerBiome * CorruptionTierCount;
    public const int MaximumAuthoredCombatLevel = BiomeCount * LevelsPerBiome;
    public const int NormalStagesPerLevel = 9;
    public const int BossStage = 10;

    public static WorldPosition Resolve(int combatLevel, int stage = 1, WorldContentDatabase database = null)
    {
        database ??= WorldContentCatalog.Reference;
        int requested = Mathf.Max(1, combatLevel);
        int contentLevel = Mathf.Min(requested, MaximumAuthoredCombatLevel);
        int zeroBased = contentLevel - 1;
        int biomeIndex = zeroBased / LevelsPerBiome;
        int withinBiome = zeroBased % LevelsPerBiome;
        int corruptionIndex = withinBiome / LocationsPerBiome;
        int locationIndex = withinBiome % LocationsPerBiome;
        int safeStage = Mathf.Clamp(stage, 1, BossStage);
        BiomeDefinition biome = database != null && biomeIndex < database.biomes.Count ? database.biomes[biomeIndex] : null;
        LocationDefinition location = biome != null && locationIndex < biome.locations.Count ? biome.locations[locationIndex] : null;
        CorruptionTierDefinition corruption = database != null && corruptionIndex < database.corruptionTiers.Count
            ? database.corruptionTiers[corruptionIndex] : null;
        EncounterTableDefinition table = database?.EncounterTable(location?.encounterTableId);
        EncounterDefinition encounter = ResolveEncounter(table, safeStage, requested, database);
        return new WorldPosition(requested, contentLevel, biomeIndex, locationIndex, corruptionIndex,
            safeStage, requested > MaximumAuthoredCombatLevel, biome, location, corruption, encounter);
    }

    static EncounterDefinition ResolveEncounter(EncounterTableDefinition table, int stage, int combatLevel,
        WorldContentDatabase database)
    {
        if (table == null) return null;
        bool boss = stage == BossStage;
        string enemyId = boss ? null : ChooseNormalEnemy(table, combatLevel, stage, database);
        return new EncounterDefinition
        {
            stableId = $"{table.stableId}.stage-{stage:00}", encounterTableId = table.stableId,
            kind = boss ? EncounterKind.Boss : EncounterKind.Normal, stage = stage,
            enemyArchetypeId = enemyId, bossId = boss ? table.bossId : null,
            futureModifierIds = table.futureEncounterModifierIds,
            futureRewardHooks = table.futureRewardHooks
        };
    }

    static string ChooseNormalEnemy(EncounterTableDefinition table, int combatLevel, int stage,
        WorldContentDatabase database)
    {
        int total = 0;
        foreach (var entry in table.normalEnemyPool)
            if (entry != null && entry.weight > 0 && database?.Enemy(entry.enemyArchetypeId) != null) total += entry.weight;
        if (total <= 0) return null;
        uint state = unchecked((uint)(combatLevel * 486187739 + stage * 16777619 + StableHash(table.stableId)));
        state ^= state >> 16; state *= 0x7feb352dU; state ^= state >> 15; state *= 0x846ca68bU; state ^= state >> 16;
        int roll = (int)(state % (uint)total);
        foreach (var entry in table.normalEnemyPool)
        {
            if (entry == null || entry.weight <= 0 || database?.Enemy(entry.enemyArchetypeId) == null) continue;
            if (roll < entry.weight) return entry.enemyArchetypeId;
            roll -= entry.weight;
        }
        return null;
    }

    static int StableHash(string value)
    {
        unchecked { int hash = 17; foreach (char c in value ?? string.Empty) hash = hash * 31 + c; return hash; }
    }
}

public static class WorldContentCatalog
{
    static WorldContentDatabase reference;
    public static WorldContentDatabase Reference => reference != null ? reference : reference = BuildReference();

    static WorldContentDatabase BuildReference()
    {
        var database = ScriptableObject.CreateInstance<WorldContentDatabase>();
        database.name = "Reference World Content (Runtime Placeholder)";
        database.hideFlags = HideFlags.HideAndDontSave;
        int[] corruption = { 0, 20, 40, 60, 80, 100 };
        foreach (int percentage in corruption) database.corruptionTiers.Add(new CorruptionTierDefinition
        {
            stableId = $"corruption-{percentage:000}", percentage = percentage,
            displayName = $"{percentage}% Corruption", presentationHookId = $"corruption.presentation.{percentage:000}"
        });
        database.enemyArchetypes.Add(new EnemyArchetypeDefinition
        {
            stableId = "enemy.goblin", displayName = "Goblin", codexEntryId = "codex.enemy.goblin"
        });
        database.bosses.Add(new BossDefinition
        {
            stableId = "boss.hobgoblin", displayName = "Hobgoblin",
            codexEntryId = "codex.boss.hobgoblin", presentationId = "presentation.boss.hobgoblin"
        });
        for (int biomeIndex = 0; biomeIndex < WorldProgression.BiomeCount; biomeIndex++)
        {
            string biomeId = $"biome-{biomeIndex + 1:00}";
            string encounterId = $"encounter.{biomeId}.placeholder";
            var biome = new BiomeDefinition
            {
                stableId = biomeId,
                displayName = biomeIndex == 0 ? "Forest" : $"Biome {biomeIndex + 1} Placeholder",
                firstCombatLevel = biomeIndex * WorldProgression.LevelsPerBiome + 1,
                lastCombatLevel = (biomeIndex + 1) * WorldProgression.LevelsPerBiome,
                placeholder = true
            };
            for (int locationIndex = 0; locationIndex < WorldProgression.LocationsPerBiome; locationIndex++)
            {
                var location = new LocationDefinition
                {
                    stableId = $"location.{biomeId}.{locationIndex + 1:00}",
                    displayName = $"Location {locationIndex + 1}",
                    baseBackgroundAddress = "reference.paper-forest",
                    optionalEnvironmentSetId = "environment.paper-forest.placeholder",
                    enemySpawnPresentationId = "spawn.paper-enemy",
                    encounterTableId = encounterId
                };
                foreach (var tier in database.corruptionTiers) location.corruptionPresentations.Add(new CorruptionPresentationDefinition
                {
                    corruptionTierId = tier.stableId,
                    environmentSetId = $"environment.paper-forest.{tier.percentage:000}.placeholder"
                });
                biome.locations.Add(location);
            }
            database.biomes.Add(biome);
            database.encounterTables.Add(new EncounterTableDefinition
            {
                stableId = encounterId,
                normalEnemyPool = new List<WeightedEnemyArchetype>
                    { new() { enemyArchetypeId = "enemy.goblin", weight = 1 } },
                bossId = "boss.hobgoblin"
            });
        }
        return database;
    }
}
