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
    public string mechanicProfileId;
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
    public string mechanicProfileId;
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
    public EnemyContentRank rank;
    public Element primaryElement;
    public string skillLoadoutId;
    public string buildPreferenceId;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float lifeMultiplier = 1f;
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
    public int biomeIndex;
    public int locationIndex;
    public string phaseProfileId;
    public string skillLoadoutId;
    public bool challengeBoss;
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
    public int rewardResourceAmount = 1;
    public string lootSourceId;
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
    public List<EnemySkillDefinition> enemySkills = new();
    public List<EnemySkillLoadoutDefinition> enemySkillLoadouts = new();
    public List<EnemyBuildPreferenceDefinition> enemyBuildPreferences = new();
    public List<LocationMechanicProfile> locationMechanicProfiles = new();
    public List<CorruptionMechanicProfile> corruptionMechanicProfiles = new();
    public List<BossPhaseProfile> bossPhaseProfiles = new();
    public List<ChallengeRewardProfile> challengeRewardProfiles = new();
    public List<SpecialAffixPoolDefinition> challengeSpecialAffixPools = new();

    public BiomeDefinition Biome(string id) => biomes?.Find(x => x != null && x.stableId == id);
    public CorruptionTierDefinition Corruption(string id) => corruptionTiers?.Find(x => x != null && x.stableId == id);
    public EnemyArchetypeDefinition Enemy(string id) => enemyArchetypes?.Find(x => x != null && x.stableId == id);
    public BossDefinition Boss(string id) => bosses?.Find(x => x != null && x.stableId == id);
    public EncounterTableDefinition EncounterTable(string id) => encounterTables?.Find(x => x != null && x.stableId == id);
    public ChallengeEncounterDefinition Challenge(string id) => challengeEncounters?.Find(x => x != null && x.stableContentId == id);
    public EnemySkillDefinition EnemySkill(string id) => enemySkills?.Find(x => x != null && x.stableId == id);
    public EnemySkillLoadoutDefinition SkillLoadout(string id) => enemySkillLoadouts?.Find(x => x != null && x.stableId == id);
    public EnemyBuildPreferenceDefinition BuildPreference(string id) => enemyBuildPreferences?.Find(x => x != null && x.stableId == id);
    public LocationMechanicProfile LocationMechanic(string id) => locationMechanicProfiles?.Find(x => x != null && x.stableId == id);
    public CorruptionMechanicProfile CorruptionMechanic(string id) => corruptionMechanicProfiles?.Find(x => x != null && x.stableId == id);
    public BossPhaseProfile BossPhase(string id) => bossPhaseProfiles?.Find(x => x != null && x.stableId == id);
    public ChallengeRewardProfile ChallengeReward(string id) => challengeRewardProfiles?.Find(x => x != null && x.stableId == id);
    public SpecialAffixPoolDefinition ChallengeSpecialPool(string id) => challengeSpecialAffixPools?.Find(x => x != null && x.stableId == id);
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
        return ProductionWorldContent.Build();
    }
}
