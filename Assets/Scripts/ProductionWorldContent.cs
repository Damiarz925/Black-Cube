// Step 19 production world catalog. Presentation hooks intentionally remain replaceable;
// stable mechanical identities and progression references are authoritative.
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyContentRank { Ordinary, Elite, MainBoss, ChallengeBoss }
public enum EnemySkillKind { Strike, MultiHit, Guard, Recover, ApplyBleed, ApplyIgnite, ApplyChill, ApplyShock, ApplyPoison }

[Serializable] public sealed class EnemySkillDefinition
{
    public string stableId, displayName; public EnemySkillKind kind; public Element element;
    public float damageMultiplier=1f; public int hitCount=1; public int cadence=1; public string telegraphId;
}
[Serializable] public sealed class EnemySkillLoadoutDefinition
{ public string stableId; public List<string> skillIds=new(); }
[Serializable] public sealed class EnemyBuildPreferenceDefinition
{
    public string stableId; public Element preferredElement; public List<StatTypes> preferredStats=new();
}
[Serializable] public sealed class LocationMechanicProfile
{
    public string stableId, displayName, encounterModifierId; public float enemyDamageMultiplier=1f, enemySpeedMultiplier=1f;
}
[Serializable] public sealed class CorruptionMechanicProfile
{
    public string stableId; public int percentage; public float damageMultiplier=1f, speedMultiplier=1f, recoveryMultiplier=1f;
    public List<string> mechanicIds=new();
}
[Serializable] public sealed class BossPhaseDefinition
{
    [Range(0f,1f)] public float beginsAtLifeFraction=1f; public string skillLoadoutId, mechanicId; public float damageMultiplier=1f;
}
[Serializable] public sealed class BossPhaseProfile
{ public string stableId; public List<BossPhaseDefinition> phases=new(); }
[Serializable] public sealed class ChallengeRewardProfile
{
    public string stableId, associatedContentId, entryResourceId, rewardResourceId, specialAffixPoolId;
    public int entryAmount=1,rewardAmount=1; public bool repeatable=true;
}

public static class EnemyActionPlanner
{
    public static EnemySkillDefinition Select(WorldContentDatabase db,string loadoutId,int completedTurns)
    {
        var loadout=db?.SkillLoadout(loadoutId); if(loadout?.skillIds==null||loadout.skillIds.Count==0)return null;
        int turn=Mathf.Max(0,completedTurns);
        for(int offset=0;offset<loadout.skillIds.Count;offset++)
        {
            var skill=db.EnemySkill(loadout.skillIds[(turn+offset)%loadout.skillIds.Count]);
            if(skill!=null&&skill.cadence>0&&(turn+1)%skill.cadence==0)return skill;
        }
        return db.EnemySkill(loadout.skillIds[turn%loadout.skillIds.Count]);
    }
}

public static class ProductionWorldContent
{
    static readonly string[] BiomeIds={"ashen-march","cinder-wastes","frostbound-reaches","tempest-heights","voidfen","black-citadel"};
    static readonly string[] BiomeNames={"Ashen March","Cinder Wastes","Frostbound Reaches","Tempest Heights","Voidfen","Black Citadel"};
    static readonly Element[] Elements={Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Void,Element.Void};
    static readonly string[][] Enemies={
        new[]{"March Raider","Ironhide Boar","Bloodwood Archer","Shieldbound","Grave Hound","Riven Veteran","Bleak Champion","March Warden"},
        new[]{"Cinder Imp","Ash Stalker","Ember Hound","Charred Zealot","Magma Brute","Flamecaller","Pyre Champion","Eruption Herald"},
        new[]{"Rime Wolf","Icebound Archer","Snow Wraith","Glacial Guard","Frostcaller","Shiverfiend","Hoarfrost Knight","Winter Oracle"},
        new[]{"Storm Hawk","Sparkblade","Thunder Hound","Galeborn","Arc Sentinel","Tempest Caller","Bolt Champion","Storm Conductor"},
        new[]{"Bog Lurker","Venom Fang","Hollow Cultist","Spore Carrier","Null Stalker","Rot Weaver","Abyss Knight","Corruption Seer"},
        new[]{"Citadel Legionary","Prismatic Hound","Rift Archer","Elemental Adept","Blackguard","Chaos Weaver","Eclipse Champion","Cube Herald"}};
    static readonly string[][] Locations={
        new[]{"Broken Causeway","Bloodwood Verge","Old Watch","Iron Fields","Grave Ford","Riven Camp","War Road","Black Rampart","Last Muster","March Crown"},
        new[]{"Scorched Gate","Glass Dunes","Ember Quarry","Charred Shrine","Magma Trench","Ashen Bazaar","Pyre Steps","Burning Vault","Eruption Caldera","Cinder Throne"},
        new[]{"Frozen Pass","Whitewood","Ice Caves","Silent Lake","Rime Bastion","Shiver Moor","Hoarfrost Bridge","Winter Archive","Glacial Heart","Frost Crown"},
        new[]{"Windscar Path","Charged Grove","Thunder Mesa","Galebreak Ridge","Arc Foundry","Tempest Spire","Boltway","Storm Vault","Eye of Heaven","Sky Crown"},
        new[]{"Drowned Track","Venom Pools","Hollow Village","Spore Warrens","Null Mire","Rot Cathedral","Abyss Causeway","Corruption Well","Black Fen","Void Crown"},
        new[]{"Outer Bastion","Prismatic Hall","Rift Gallery","Elemental Crucible","Black Barracks","Chaos Archive","Eclipse Stair","Herald Court","Cube Threshold","Heart of the Cube"}};
    static readonly string[][] Bosses={
        new[]{"The Tollkeeper","Bloodwood Matron","Captain Varr","Ironhoof","The Grave Marshal","Riven Twins","Banner Eater","General Karth","The Last Legion","King of the March"},
        new[]{"Coalheart","The Glass Wyrm","Furnace Keeper","Saint of Ash","Molten Maw","The Cinder Witch","Pyre Lord","Vault Inferno","Caldera Titan","The Burning Crown"},
        new[]{"Whitefang","The Icebound Widow","Cavern Colossus","The Drowned Bell","Rime Castellan","Shiver Queen","Bridgebreaker","Archivist Zero","The Living Glacier","Winter Sovereign"},
        new[]{"Wind Talon","The Charged Stag","Thunderclap","Gale Tyrant","Arc Architect","The Tempest","Bolt Regent","Storm Engine","Eye of Ruin","Sovereign of Skies"},
        new[]{"The Drowned One","Venom Mother","Hollow Mayor","Spore King","Null Beast","The Rot Bishop","Abyss Walker","Well of Teeth","Fen Leviathan","The Void Crown"},
        new[]{"Gate of Six","Prismatic Chimera","Rift General","Crucible Avatar","The Black Guard","Chaos Curator","Eclipse Knight","First Herald","The Cube's Shadow","The Black Cube"}};
    static readonly string[] ChallengeNames={"The Iron Colossus","Phoenix Unbound","Absolute Zero","The Living Tempest","Maw Beyond Stars","Avatar of the Black Cube"};

    public static WorldContentDatabase Build()
    {
        var db=ScriptableObject.CreateInstance<WorldContentDatabase>();db.name="V1 Production World Content";db.hideFlags=HideFlags.HideAndDontSave;
        BuildCorruption(db); BuildSkills(db); BuildLocationMechanics(db);
        for(int b=0;b<6;b++)BuildBiome(db,b);
        BuildChallenges(db); return db;
    }
    static void BuildCorruption(WorldContentDatabase db)
    {
        int[] values={0,20,40,60,80,100};
        for(int i=0;i<values.Length;i++){int p=values[i];string id=$"corruption-{p:000}";db.corruptionTiers.Add(new CorruptionTierDefinition{stableId=id,percentage=p,displayName=$"{p}% Corruption",presentationHookId=$"corruption.presentation.{p:000}",mechanicProfileId=$"mechanic.{id}"});db.corruptionMechanicProfiles.Add(new CorruptionMechanicProfile{stableId=$"mechanic.{id}",percentage=p,damageMultiplier=1f+p*.0025f,speedMultiplier=1f+p*.001f,recoveryMultiplier=1f+p*.002f,mechanicIds=p==0?new():new List<string>{p>=40?"corruption.escalating-pressure":"corruption.stirring",p>=80?"corruption.empowered-cadence":"corruption.exposure",p==100?"corruption.apex-modifier":"corruption.stable"}});}
    }
    static void BuildSkills(WorldContentDatabase db)
    {
        EnemySkillKind[] ail={EnemySkillKind.ApplyBleed,EnemySkillKind.ApplyIgnite,EnemySkillKind.ApplyChill,EnemySkillKind.ApplyShock,EnemySkillKind.ApplyPoison,EnemySkillKind.MultiHit};
        for(int b=0;b<6;b++)
        {
            string root=$"enemy-skill.{BiomeIds[b]}";
            db.enemySkills.Add(new EnemySkillDefinition{stableId=$"{root}.strike",displayName="Measured Strike",kind=EnemySkillKind.Strike,element=Elements[b],damageMultiplier=1f,hitCount=1,cadence=1,telegraphId="telegraph.short"});
            db.enemySkills.Add(new EnemySkillDefinition{stableId=$"{root}.pressure",displayName="Escalating Assault",kind=EnemySkillKind.MultiHit,element=Elements[b],damageMultiplier=.68f,hitCount=2,cadence=2,telegraphId="telegraph.double"});
            db.enemySkills.Add(new EnemySkillDefinition{stableId=$"{root}.ailment",displayName="Biotic Infliction",kind=ail[b],element=Elements[b],damageMultiplier=.9f,hitCount=1,cadence=3,telegraphId="telegraph.ailment"});
            db.enemySkills.Add(new EnemySkillDefinition{stableId=$"{root}.elite",displayName="Elite Breaker",kind=EnemySkillKind.Strike,element=Elements[b],damageMultiplier=1.45f,hitCount=1,cadence=4,telegraphId="telegraph.long"});
            if(b==2)db.enemySkills.Add(new EnemySkillDefinition{stableId=$"{root}.recover",displayName="Rime Renewal",kind=EnemySkillKind.Recover,element=Element.Cold,damageMultiplier=.75f,hitCount=1,cadence=4,telegraphId="telegraph.recovery"});
            db.enemySkillLoadouts.Add(new EnemySkillLoadoutDefinition{stableId=$"loadout.{BiomeIds[b]}.ordinary",skillIds=new(){ $"{root}.strike",$"{root}.pressure",$"{root}.ailment"}});
            var eliteSkills=new List<string>{ $"{root}.strike",$"{root}.ailment",$"{root}.elite",$"{root}.pressure"};if(b==2)eliteSkills.Insert(2,$"{root}.recover");
            db.enemySkillLoadouts.Add(new EnemySkillLoadoutDefinition{stableId=$"loadout.{BiomeIds[b]}.elite",skillIds=eliteSkills});
            db.enemyBuildPreferences.Add(new EnemyBuildPreferenceDefinition{stableId=$"build.{BiomeIds[b]}",preferredElement=Elements[b],preferredStats=PreferredStats(b)});
        }
    }
    static List<StatTypes> PreferredStats(int b)=>b switch{0=>new(){StatTypes.PhysDmg,StatTypes.FlatArmour,StatTypes.BleedChance},1=>new(){StatTypes.FireDmg,StatTypes.IgniteChance},2=>new(){StatTypes.ColdDmg,StatTypes.ChillChance,StatTypes.ColdRes},3=>new(){StatTypes.LightDmg,StatTypes.ShockChance,StatTypes.AttackSpeed},4=>new(){StatTypes.VoidDmg,StatTypes.PoisonChance,StatTypes.VoidRes},_=>new(){StatTypes.GenericDmg,StatTypes.AllRes,StatTypes.AttackSpeed}};
    static void BuildLocationMechanics(WorldContentDatabase db)
    {for(int b=0;b<6;b++)for(int l=0;l<10;l++)db.locationMechanicProfiles.Add(new LocationMechanicProfile{stableId=$"location-mechanic.{BiomeIds[b]}.{l+1:00}",displayName=Locations[b][l]+" Rules",encounterModifierId=$"encounter-rule.{BiomeIds[b]}.{l+1:00}",enemyDamageMultiplier=1f+(l%5)*.025f,enemySpeedMultiplier=1f+(l/5)*.04f});}
    static void BuildBiome(WorldContentDatabase db,int b)
    {
        string biome=BiomeIds[b];var definition=new BiomeDefinition{stableId=$"biome.{biome}",displayName=BiomeNames[b],firstCombatLevel=b*60+1,lastCombatLevel=(b+1)*60,placeholder=false};
        for(int e=0;e<8;e++){string eid=$"enemy.{biome}.{Slug(Enemies[b][e])}";db.enemyArchetypes.Add(new EnemyArchetypeDefinition{stableId=eid,displayName=Enemies[b][e],codexEntryId=$"codex.{eid}",rank=e<6?EnemyContentRank.Ordinary:EnemyContentRank.Elite,primaryElement=Elements[b],skillLoadoutId=$"loadout.{biome}.{(e<6?"ordinary":"elite")}",buildPreferenceId=$"build.{biome}",damageMultiplier=e<6?1f:1.18f,attackSpeedMultiplier=1f+(e%3)*.04f,lifeMultiplier=e<6?1f:1.3f,futureContentHooks=new(){"presentation.paper-enemy.fallback"}});}
        for(int l=0;l<10;l++)
        {
            string bossId=$"boss.{biome}.{l+1:00}.{Slug(Bosses[b][l])}";string phaseId=$"phase.{bossId}";
            var boss=new BossDefinition{stableId=bossId,displayName=Bosses[b][l],codexEntryId=$"codex.{bossId}",presentationId="presentation.paper-boss.fallback",biomeIndex=b,locationIndex=l,phaseProfileId=phaseId,skillLoadoutId=$"loadout.{biome}.elite",challengeBoss=false,futureMechanicIds=new(){$"mechanic.{biome}.{l+1:00}"},futureSkillIds=new(){$"enemy-skill.{biome}.elite"},futureRewardHooks=new(){$"reward.main-boss.{biome}.{l+1:00}"}};
            if(b==1&&l==9)boss.futureStoryFlags.Add(PlayerIdentityState.StoryCompletionMilestoneId);db.bosses.Add(boss);
            db.bossPhaseProfiles.Add(new BossPhaseProfile{stableId=phaseId,phases=new(){new BossPhaseDefinition{beginsAtLifeFraction=1f,skillLoadoutId=$"loadout.{biome}.elite",mechanicId=$"boss.{biome}.opening",damageMultiplier=1f},new BossPhaseDefinition{beginsAtLifeFraction=.5f,skillLoadoutId=$"loadout.{biome}.elite",mechanicId=$"boss.{biome}.desperation",damageMultiplier=1.2f}}});
            string tableId=$"encounter.{biome}.{l+1:00}";var pool=new List<WeightedEnemyArchetype>();for(int e=0;e<8;e++)pool.Add(new WeightedEnemyArchetype{enemyArchetypeId=$"enemy.{biome}.{Slug(Enemies[b][e])}",weight=e<6?8:2});
            db.encounterTables.Add(new EncounterTableDefinition{stableId=tableId,normalEnemyPool=pool,bossId=bossId,futureEncounterModifierIds=new(){$"encounter-rule.{biome}.{l+1:00}"},futureRewardHooks=new(){$"reward.{biome}"}});
            var location=new LocationDefinition{stableId=$"location.{biome}.{l+1:00}",displayName=Locations[b][l],baseBackgroundAddress="presentation.paper-forest.fallback",optionalEnvironmentSetId=$"environment.{biome}",enemySpawnPresentationId="spawn.paper-enemy",encounterTableId=tableId,mechanicProfileId=$"location-mechanic.{biome}.{l+1:00}"};
            foreach(var tier in db.corruptionTiers)location.corruptionPresentations.Add(new CorruptionPresentationDefinition{corruptionTierId=tier.stableId,environmentSetId=$"environment.{biome}.{tier.percentage:000}"});definition.locations.Add(location);
        }db.biomes.Add(definition);
    }
    static void BuildChallenges(WorldContentDatabase db)
    {for(int b=0;b<6;b++){string content=$"challenge.{BiomeIds[b]}.apex";string bossId=$"boss.challenge.{BiomeIds[b]}.apex";string pool=$"special-affix-pool.{BiomeIds[b]}.apex";db.bosses.Add(new BossDefinition{stableId=bossId,displayName=ChallengeNames[b],codexEntryId=$"codex.{bossId}",presentationId="presentation.paper-boss.fallback",biomeIndex=b,locationIndex=-1,phaseProfileId=$"phase.boss.{BiomeIds[b]}.10.{Slug(Bosses[b][9])}",skillLoadoutId=$"loadout.{BiomeIds[b]}.elite",challengeBoss=true,futureRewardHooks=new(){$"reward.{content}"}});var c=new ChallengeEncounterDefinition{stableContentId=content,displayName=ChallengeNames[b],minimumCombatLevel=100+b*40,entryResourceId=$"resource.challenge-key.{BiomeIds[b]}",entryResourceAmount=1,bossId=bossId,rewardResourceId=$"resource.challenge-essence.{BiomeIds[b]}",rewardResourceAmount=1,specialAffixPoolId=pool,lootSourceId=$"loot-source.{content}",repeatable=true,unlockRequirementIds=new(){b==0?PlayerIdentityState.StoryCompletionMilestoneId:$"progress.biome.{b+1}.reached"}};db.challengeEncounters.Add(c);db.challengeRewardProfiles.Add(new ChallengeRewardProfile{stableId=$"reward-profile.{content}",associatedContentId=content,entryResourceId=c.entryResourceId,rewardResourceId=c.rewardResourceId,specialAffixPoolId=pool,entryAmount=1,rewardAmount=1,repeatable=true});db.challengeSpecialAffixPools.Add(new SpecialAffixPoolDefinition{stableId=pool,poolName=ChallengeNames[b]+" Affixes",associatedContentId=content,modifiers=new(){new SpecialAffixDefinition{stableId=$"{pool}.power",side=AffixSide.Prefix,allowedItemTypes=new[]{LootManager.GearType.Weapons},statType=b==0?StatTypes.PhysDmg:StatMappings.GetIncDamageStat(Elements[b]),minimum=.18f,maximum=.28f,minimumItemLevel=100,minimumCombatLevel=c.minimumCombatLevel,weight=2,description="Challenge-aligned offensive power."},new SpecialAffixDefinition{stableId=$"{pool}.ward",side=AffixSide.Suffix,allowedItemTypes=new[]{LootManager.GearType.BodyArmours,LootManager.GearType.Helmets},statType=b==5?StatTypes.AllRes:StatMappings.GetResistanceStat(Elements[b]),minimum=.08f,maximum=.14f,minimumItemLevel=100,minimumCombatLevel=c.minimumCombatLevel,weight=1,description="Challenge-aligned defense."}}});}}
    static StatTypes Resistance(Element element)=>element switch{Element.Fire=>StatTypes.FireRes,Element.Cold=>StatTypes.ColdRes,Element.Light=>StatTypes.LightRes,Element.Void=>StatTypes.VoidRes,_=>StatTypes.FlatArmour};
    static string Slug(string value)=>value.ToLowerInvariant().Replace("'","").Replace(" ","-");
}

[Serializable] public sealed class ChallengeResourceStack { public string resourceId; public int amount; }
public sealed class ChallengeContentService
{
    readonly Dictionary<string,int> resources=new();
    public int Count(string id)=>!string.IsNullOrWhiteSpace(id)&&resources.TryGetValue(id,out int value)?value:0;
    public void Add(string id,int amount){if(string.IsNullOrWhiteSpace(id)||amount<=0)return;resources[id]=Count(id)+amount;}
    public string RollEntryResource(WorldContentDatabase db,int combatLevel,ILootRandomSource random,float chance=.04f)
    {
        if(db?.challengeEncounters==null||random==null||random.Value()>=Mathf.Clamp01(chance))return null;
        var eligible=db.challengeEncounters.FindAll(x=>x!=null&&combatLevel>=x.minimumCombatLevel);
        if(eligible.Count==0)return null;var chosen=eligible[random.Range(0,eligible.Count)];Add(chosen.entryResourceId,1);return chosen.entryResourceId;
    }
    public bool TryEnter(ChallengeEncounterDefinition challenge)
    {if(challenge==null||Count(challenge.entryResourceId)<challenge.entryResourceAmount)return false;resources[challenge.entryResourceId]-=challenge.entryResourceAmount;return true;}
    public bool Complete(ChallengeEncounterDefinition challenge)
    {if(challenge==null)return false;Add(challenge.rewardResourceId,challenge.rewardResourceAmount);return true;}
    public List<ChallengeResourceStack> Capture(){var result=new List<ChallengeResourceStack>();foreach(var pair in resources)if(pair.Value>0)result.Add(new ChallengeResourceStack{resourceId=pair.Key,amount=pair.Value});result.Sort((a,b)=>string.CompareOrdinal(a.resourceId,b.resourceId));return result;}
    public void Restore(IEnumerable<ChallengeResourceStack> data){resources.Clear();if(data==null)return;foreach(var stack in data)if(stack!=null&&!string.IsNullOrWhiteSpace(stack.resourceId)&&stack.amount>0)resources[stack.resourceId]=Count(stack.resourceId)+stack.amount;}
}
