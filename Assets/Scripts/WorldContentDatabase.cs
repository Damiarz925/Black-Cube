using System.Collections.Generic;
using UnityEngine;

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
    public List<EnemyBehaviorProfileDefinition> enemyBehaviorProfiles = new();
    public List<EnemyBuildPreferenceDefinition> enemyBuildPreferences = new();
    public List<EnemyRarityProfile> enemyRarityProfiles = new();
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
    public EnemyBehaviorProfileDefinition BehaviorProfile(string id) => enemyBehaviorProfiles?.Find(x => x != null && x.stableId == id);
    public EnemyBehaviorProfileDefinition BehaviorForLoadout(string id) => enemyBehaviorProfiles?.Find(x => x != null && x.sourceLoadoutId == id);
    public EnemyBuildPreferenceDefinition BuildPreference(string id) => enemyBuildPreferences?.Find(x => x != null && x.stableId == id);
    public EnemyRarityProfile EnemyRarity(EnemyAI.EnemyRarity rarity) => enemyRarityProfiles?.Find(x => x != null && x.rarity == rarity);
    public LocationMechanicProfile LocationMechanic(string id) => locationMechanicProfiles?.Find(x => x != null && x.stableId == id);
    public CorruptionMechanicProfile CorruptionMechanic(string id) => corruptionMechanicProfiles?.Find(x => x != null && x.stableId == id);
    public BossPhaseProfile BossPhase(string id) => bossPhaseProfiles?.Find(x => x != null && x.stableId == id);
    public ChallengeRewardProfile ChallengeReward(string id) => challengeRewardProfiles?.Find(x => x != null && x.stableId == id);
    public SpecialAffixPoolDefinition ChallengeSpecialPool(string id) => challengeSpecialAffixPools?.Find(x => x != null && x.stableId == id);
}
