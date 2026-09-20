using System.Collections.Generic;

public static class EndgameItemizationValidation
{
    public static List<string> Validate(WorldContentDatabase database)
    {
        var errors=new List<string>();if(database==null){errors.Add("World content database is missing.");return errors;}
        if(database.challengeEncounters?.Count!=6)errors.Add("Exactly six challenge encounters are required.");
        var resources=new HashSet<string>();var affixes=new HashSet<string>();
        for(int i=0;i<ProductionWorldContent.BiomeIds.Length;i++)
        {
            string biome=ProductionWorldContent.BiomeIds[i],key=EndgameResourceIds.ChallengeKey(biome),essence=EndgameResourceIds.ChallengeEssence(biome);
            if(!EndgameResourceIds.IsPersistentResource(key)||!resources.Add(key))errors.Add($"Invalid challenge key {key}.");
            if(!EndgameResourceIds.IsPersistentResource(essence)||!resources.Add(essence))errors.Add($"Invalid challenge essence {essence}.");
        }
        foreach(var challenge in database.challengeEncounters??new())
        {
            if(challenge==null)continue;if(!EndgameResourceIds.IsPersistentResource(challenge.entryResourceId))errors.Add($"{challenge.stableContentId} has invalid key.");
            if(!EndgameResourceIds.IsPersistentResource(challenge.rewardResourceId))errors.Add($"{challenge.stableContentId} has invalid essence.");
            if(database.Boss(challenge.bossId)?.challengeBoss!=true)errors.Add($"{challenge.stableContentId} has invalid challenge boss.");
            var pool=database.ChallengeSpecialPool(challenge.specialAffixPoolId);if(pool==null){errors.Add($"{challenge.stableContentId} has invalid special pool.");continue;}
            if(pool.modifiers?.Count!=6)errors.Add($"{pool.stableId} must contain six production affixes.");
            int prefixes=0,suffixes=0;foreach(var mod in pool.modifiers??new())
            {if(mod==null||string.IsNullOrWhiteSpace(mod.stableId)||!affixes.Add(mod.stableId))errors.Add($"{pool.stableId} contains a null/duplicate affix ID.");else if(string.IsNullOrWhiteSpace(mod.effectId)||string.IsNullOrWhiteSpace(mod.displayName)||string.IsNullOrWhiteSpace(mod.description)||mod.allowedItemTypes==null||mod.allowedItemTypes.Length==0)errors.Add($"{mod.stableId} is incomplete.");if(mod?.side==AffixSide.Prefix)prefixes++;else if(mod!=null)suffixes++;}
            if(prefixes!=3||suffixes!=3)errors.Add($"{pool.stableId} must contain three Prefixes and three Suffixes.");
            foreach(LootManager.GearType type in System.Enum.GetValues(typeof(LootManager.GearType)))
            {bool supported=false;foreach(var mod in pool.modifiers)if(mod.Allows(type)){supported=true;break;}if(!supported&&type!=LootManager.GearType.Weapons)errors.Add($"{pool.stableId} has no legal affix for {type}.");}
        }
        if(!EndgameResourceIds.IsPersistentResource(EndgameResourceIds.ImplicitReforger))errors.Add("Implicit Reforger ID is invalid.");
        int[] levels={119,120,159,160,209,210,259,260,309,310,359,360},expected={0,1,1,2,2,3,3,4,4,5,5,6};for(int i=0;i<levels.Length;i++)if(EmpowermentProgressionProfile.MaximumEmpoweredModifiers(levels[i])!=expected[i])errors.Add($"Empowerment threshold {levels[i]} is invalid.");
        return errors;
    }
}
