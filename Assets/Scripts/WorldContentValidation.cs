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
        foreach(var skill in database.enemySkills??new()){if(skill==null){errors.Add("Enemy skill is null.");continue;}Id(skill.stableId,"Enemy skill");if(skill.hitCount<1||skill.cadence<1||skill.damageMultiplier<=0f)errors.Add($"{skill.stableId} has invalid combat values.");}
        foreach(var loadout in database.enemySkillLoadouts??new()){if(loadout==null){errors.Add("Enemy skill loadout is null.");continue;}Id(loadout.stableId,"Enemy skill loadout");if(loadout.skillIds==null||loadout.skillIds.Count==0||loadout.skillIds.Any(x=>database.EnemySkill(x)==null))errors.Add($"{loadout.stableId} has invalid skill references.");}
        foreach(var profile in database.enemyBuildPreferences??new()){if(profile==null){errors.Add("Enemy build preference is null.");continue;}Id(profile.stableId,"Enemy build preference");if(profile.preferredStats==null||profile.preferredStats.Count==0)errors.Add($"{profile.stableId} has no preferred stats.");}
        if((database.enemyRarityProfiles?.Count??0)!=4)errors.Add("Exactly four enemy rarity profiles are required.");
        foreach(var profile in database.enemyRarityProfiles??new()){if(profile==null){errors.Add("Enemy rarity profile is null.");continue;}Id(profile.stableId,"Enemy rarity profile");if(profile.spawnWeight<0||profile.lifeMultiplier<=0||profile.damageMultiplier<=0||profile.speedMultiplier<=0)errors.Add($"{profile.stableId} has invalid rarity values.");}
        foreach(var profile in database.locationMechanicProfiles??new()){if(profile==null){errors.Add("Location mechanic is null.");continue;}Id(profile.stableId,"Location mechanic");}
        foreach(var profile in database.corruptionMechanicProfiles??new()){if(profile==null){errors.Add("Corruption mechanic is null.");continue;}Id(profile.stableId,"Corruption mechanic");}
        foreach(var profile in database.enemyBehaviorProfiles??new()){if(profile==null){errors.Add("Enemy behavior profile is null.");continue;}Id(profile.stableId,"Enemy behavior profile");errors.AddRange(EnemyBehaviorValidation.Validate(database,profile));}
        foreach(var enemy in database.enemyArchetypes??new())if(enemy!=null&&(database.BehaviorProfile(enemy.behaviorProfileId)==null||database.SkillLoadout(enemy.skillLoadoutId)==null))errors.Add($"{enemy.stableId} has invalid behavior or skill loadout.");
        foreach(var profile in database.bossPhaseProfiles??new()){if(profile==null){errors.Add("Boss phase profile is null.");continue;}Id(profile.stableId,"Boss phase profile");if(profile.phases==null||profile.phases.Count<2||profile.phases.Any(x=>x==null||database.SkillLoadout(x.skillLoadoutId)==null))errors.Add($"{profile.stableId} has invalid phases.");}
        if((database.enemyArchetypes?.Count??0)!=48)errors.Add("Exactly 48 production non-boss enemy archetypes are required.");
        if((database.enemyArchetypes?.Count(x=>x!=null&&x.rank==EnemyContentRank.Elite)??0)!=12)errors.Add("Exactly 12 elite enemy archetypes are required.");
        if((database.bosses?.Count(x=>x!=null&&!x.challengeBoss)??0)!=60)errors.Add("Exactly 60 main boss definitions are required.");
        if((database.bosses?.Count(x=>x!=null&&x.challengeBoss)??0)!=6)errors.Add("Exactly six challenge boss definitions are required.");
        foreach(var enemy in database.enemyArchetypes??new())if(enemy!=null&&(database.SkillLoadout(enemy.skillLoadoutId)==null||database.BuildPreference(enemy.buildPreferenceId)==null))errors.Add($"{enemy.stableId} has invalid production mechanics.");
        foreach(var boss in database.bosses??new())if(boss!=null&&(database.SkillLoadout(boss.skillLoadoutId)==null||database.BossPhase(boss.phaseProfileId)==null))errors.Add($"{boss.stableId} has invalid skills or phase profile.");
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
                if(database.LocationMechanic(location.mechanicProfileId)==null)errors.Add($"{location.stableId} has an invalid mechanic profile.");
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
            if(!challenge.repeatable||string.IsNullOrWhiteSpace(challenge.lootSourceId)||challenge.rewardResourceAmount<1
                ||database.ChallengeSpecialPool(challenge.specialAffixPoolId)==null
                ||database.challengeRewardProfiles.Count(x=>x!=null&&x.associatedContentId==challenge.stableContentId)!=1)
                errors.Add($"{challenge.stableContentId} has invalid repeatable challenge content.");
        }

        if((database.challengeEncounters?.Count??0)!=6)errors.Add("Exactly six challenge encounters are required.");
        if((database.challengeSpecialAffixPools?.Count??0)!=6)errors.Add("Exactly six challenge special-affix pools are required.");

        for (int level = 1; level <= WorldProgression.MaximumAuthoredCombatLevel; level++)
        {
            for(int stage=1;stage<=WorldProgression.BossStage;stage++)
            {
                WorldPosition position=WorldProgression.Resolve(level,stage,database);
                if(position.Biome==null||position.Location==null||position.Corruption==null||position.Encounter==null)
                    errors.Add($"Combat level {level} stage {stage} does not resolve structurally.");
                else if(stage<WorldProgression.BossStage&&(position.Encounter.kind!=EncounterKind.Normal||database.Enemy(position.Encounter.enemyArchetypeId)==null))
                    errors.Add($"Combat level {level} stage {stage} does not resolve to a valid normal enemy.");
                else if(stage==WorldProgression.BossStage&&(position.Encounter.kind!=EncounterKind.Boss||database.Boss(position.Encounter.bossId)==null))
                    errors.Add($"Combat level {level} stage 10 does not resolve to a valid boss.");
            }
        }
        var story=WorldProgression.Resolve(100,WorldProgression.BossStage,database);
        if(database.Boss(story.Encounter?.bossId)?.futureStoryFlags?.Contains(PlayerIdentityState.StoryCompletionMilestoneId)!=true)
            errors.Add("Combat level 100 boss must own story.main.complete.");
        return errors;
    }
}
