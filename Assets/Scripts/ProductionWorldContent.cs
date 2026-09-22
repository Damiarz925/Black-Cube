// Step 19 production world catalog. Presentation hooks intentionally remain replaceable;
// stable mechanical identities and progression references are authoritative.
using System;
using System.Collections.Generic;
using System.Linq;
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
[Serializable] public sealed class EnemyRarityProfile
{
    public string stableId; public EnemyAI.EnemyRarity rarity; [Min(0)] public int spawnWeight;
    public LootManager.GearRarity gearRarity; [Min(.01f)] public float lifeMultiplier=1f,damageMultiplier=1f,speedMultiplier=1f;
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
public static class BossPhaseResolver
{
    public static BossPhaseDefinition Resolve(BossPhaseProfile profile,float lifeFraction)
        =>profile?.phases?.Where(x=>x!=null&&lifeFraction<=x.beginsAtLifeFraction).OrderBy(x=>x.beginsAtLifeFraction).FirstOrDefault();
}
[Serializable] public sealed class ChallengeRewardProfile
{
    public string stableId, associatedContentId, entryResourceId, rewardResourceId, specialAffixPoolId;
    public int entryAmount=1,rewardAmount=1; public bool repeatable=true;
}

public static class EnemyActionPlanner
{
    public static EnemySkillDefinition Select(WorldContentDatabase db,string loadoutId,int completedTurns)
    {
        var profile=db?.BehaviorForLoadout(loadoutId);
        if(profile!=null)
        {
            var decision=EnemyBehaviorResolver.Resolve(db,profile,new EnemyBehaviorRuntimeState{completedAttacks=Mathf.Max(0,completedTurns)});
            return db.EnemySkill(decision.selectedSkillId);
        }
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
    public static readonly string[] BiomeIds={"ashen-march","cinder-wastes","frostbound-reaches","tempest-heights","voidfen","black-citadel"};
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
        BuildCorruption(db); BuildRarities(db); BuildSkills(db); BuildBehaviorProfiles(db); BuildLocationMechanics(db);
        for(int b=0;b<6;b++)BuildBiome(db,b);
        BuildChallenges(db); return db;
    }
    static void BuildCorruption(WorldContentDatabase db)
    {
        int[] values={0,20,40,60,80,100};
        for(int i=0;i<values.Length;i++){int p=values[i];string id=$"corruption-{p:000}";db.corruptionTiers.Add(new CorruptionTierDefinition{stableId=id,percentage=p,displayName=$"{p}% Corruption",presentationHookId=$"corruption.presentation.{p:000}",mechanicProfileId=$"mechanic.{id}"});db.corruptionMechanicProfiles.Add(new CorruptionMechanicProfile{stableId=$"mechanic.{id}",percentage=p,damageMultiplier=1f+p*.0025f,speedMultiplier=1f+p*.001f,recoveryMultiplier=1f+p*.002f,mechanicIds=p==0?new():new List<string>{p>=40?"corruption.escalating-pressure":"corruption.stirring",p>=80?"corruption.empowered-cadence":"corruption.exposure",p==100?"corruption.apex-modifier":"corruption.stable"}});}
    }
    static void BuildRarities(WorldContentDatabase db)
    {
        int[] weights={40,20,10,1};foreach(EnemyAI.EnemyRarity rarity in Enum.GetValues(typeof(EnemyAI.EnemyRarity)))db.enemyRarityProfiles.Add(new EnemyRarityProfile{stableId="enemy-rarity."+rarity.ToString().ToLowerInvariant(),rarity=rarity,spawnWeight=weights[(int)rarity],gearRarity=(LootManager.GearRarity)(int)rarity,lifeMultiplier=1f,damageMultiplier=1f,speedMultiplier=1f});
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
    static void BuildBehaviorProfiles(WorldContentDatabase db)
    {
        foreach(var loadout in db.enemySkillLoadouts)
        {
            var profile=new EnemyBehaviorProfileDefinition{stableId="behavior."+loadout.stableId,displayName=loadout.stableId.Replace("loadout.",string.Empty).Replace('.',' ') + " Behavior",sourceLoadoutId=loadout.stableId,preserveLegacyRotatingCadence=true};
            for(int i=0;i<loadout.skillIds.Count;i++){var skill=db.EnemySkill(loadout.skillIds[i]);profile.rules.Add(new EnemyActionRule{stableId=$"{profile.stableId}.rule-{i+1:00}",skillId=loadout.skillIds[i],condition=EnemyBehaviorCondition.EveryNAttacks,everyNAttacks=Mathf.Max(1,skill?.cadence??1),priority=0,weight=1,fallbackEligible=true});}
            db.enemyBehaviorProfiles.Add(profile);
        }
    }
    static void BuildLocationMechanics(WorldContentDatabase db)
    {for(int b=0;b<6;b++)for(int l=0;l<10;l++)db.locationMechanicProfiles.Add(new LocationMechanicProfile{stableId=$"location-mechanic.{BiomeIds[b]}.{l+1:00}",displayName=Locations[b][l]+" Rules",encounterModifierId=$"encounter-rule.{BiomeIds[b]}.{l+1:00}",enemyDamageMultiplier=1f+(l%5)*.025f,enemySpeedMultiplier=1f+(l/5)*.04f});}
    static void BuildBiome(WorldContentDatabase db,int b)
    {
        string biome=BiomeIds[b];var definition=new BiomeDefinition{stableId=$"biome.{biome}",displayName=BiomeNames[b],firstCombatLevel=b*60+1,lastCombatLevel=(b+1)*60,placeholder=false};
        for(int e=0;e<8;e++){string eid=$"enemy.{biome}.{Slug(Enemies[b][e])}",loadout=$"loadout.{biome}.{(e<6?"ordinary":"elite")}";db.enemyArchetypes.Add(new EnemyArchetypeDefinition{stableId=eid,displayName=Enemies[b][e],codexEntryId=$"codex.{eid}",rank=e<6?EnemyContentRank.Ordinary:EnemyContentRank.Elite,primaryElement=Elements[b],skillLoadoutId=loadout,behaviorProfileId="behavior."+loadout,buildPreferenceId=$"build.{biome}",damageMultiplier=e<6?1f:1.18f,attackSpeedMultiplier=1f+(e%3)*.04f,lifeMultiplier=e<6?1f:1.3f,futureContentHooks=new(){"presentation.paper-enemy.fallback"}});}
        for(int l=0;l<10;l++)
        {
            string bossId=$"boss.{biome}.{l+1:00}.{Slug(Bosses[b][l])}";string phaseId=$"phase.{bossId}";
            var boss=new BossDefinition{stableId=bossId,displayName=Bosses[b][l],codexEntryId=$"codex.{bossId}",presentationId="presentation.paper-boss.fallback",biomeIndex=b,locationIndex=l,phaseProfileId=phaseId,skillLoadoutId=$"loadout.{biome}.elite",behaviorProfileId=$"behavior.loadout.{biome}.elite",challengeBoss=false,futureMechanicIds=new(){$"mechanic.{biome}.{l+1:00}"},futureSkillIds=new(){$"enemy-skill.{biome}.elite"},futureRewardHooks=new(){$"reward.main-boss.{biome}.{l+1:00}"}};
            if(b==1&&l==9)boss.futureStoryFlags.Add(PlayerIdentityState.StoryCompletionMilestoneId);db.bosses.Add(boss);
            db.bossPhaseProfiles.Add(new BossPhaseProfile{stableId=phaseId,phases=new(){new BossPhaseDefinition{beginsAtLifeFraction=1f,skillLoadoutId=$"loadout.{biome}.elite",mechanicId=$"boss.{biome}.opening",damageMultiplier=1f},new BossPhaseDefinition{beginsAtLifeFraction=.5f,skillLoadoutId=$"loadout.{biome}.elite",mechanicId=$"boss.{biome}.desperation",damageMultiplier=1.2f}}});
            string tableId=$"encounter.{biome}.{l+1:00}";var pool=new List<WeightedEnemyArchetype>();for(int e=0;e<8;e++)pool.Add(new WeightedEnemyArchetype{enemyArchetypeId=$"enemy.{biome}.{Slug(Enemies[b][e])}",weight=e<6?8:2});
            db.encounterTables.Add(new EncounterTableDefinition{stableId=tableId,normalEnemyPool=pool,bossId=bossId,futureEncounterModifierIds=new(){$"encounter-rule.{biome}.{l+1:00}"},futureRewardHooks=new(){$"reward.{biome}"}});
            var location=new LocationDefinition{stableId=$"location.{biome}.{l+1:00}",displayName=Locations[b][l],baseBackgroundAddress="presentation.paper-forest.fallback",optionalEnvironmentSetId=$"environment.{biome}",enemySpawnPresentationId="spawn.paper-enemy",encounterTableId=tableId,mechanicProfileId=$"location-mechanic.{biome}.{l+1:00}"};
            foreach(var tier in db.corruptionTiers)location.corruptionPresentations.Add(new CorruptionPresentationDefinition{corruptionTierId=tier.stableId,environmentSetId=$"environment.{biome}.{tier.percentage:000}"});definition.locations.Add(location);
        }db.biomes.Add(definition);
    }
    static void BuildChallenges(WorldContentDatabase db)
    {
        for(int b=0;b<6;b++)
        {
            string content=$"challenge.{BiomeIds[b]}.apex",bossId=$"boss.challenge.{BiomeIds[b]}.apex",pool=$"special-affix-pool.{BiomeIds[b]}.apex";
            db.bosses.Add(new BossDefinition{stableId=bossId,displayName=ChallengeNames[b],codexEntryId=$"codex.{bossId}",presentationId="presentation.paper-boss.fallback",biomeIndex=b,locationIndex=-1,phaseProfileId=$"phase.boss.{BiomeIds[b]}.10.{Slug(Bosses[b][9])}",skillLoadoutId=$"loadout.{BiomeIds[b]}.elite",behaviorProfileId=$"behavior.loadout.{BiomeIds[b]}.elite",challengeBoss=true,futureRewardHooks=new(){$"reward.{content}"}});
            var c=new ChallengeEncounterDefinition{stableContentId=content,displayName=ChallengeNames[b],minimumCombatLevel=100+b*40,entryResourceId=EndgameResourceIds.ChallengeKey(BiomeIds[b]),entryResourceAmount=1,bossId=bossId,rewardResourceId=EndgameResourceIds.ChallengeEssence(BiomeIds[b]),rewardResourceAmount=1,specialAffixPoolId=pool,lootSourceId=$"loot-source.{content}",repeatable=true,unlockRequirementIds=new(){b==0?PlayerIdentityState.StoryCompletionMilestoneId:$"progress.biome.{b+1}.reached"}};
            db.challengeEncounters.Add(c);db.challengeRewardProfiles.Add(new ChallengeRewardProfile{stableId=$"reward-profile.{content}",associatedContentId=content,entryResourceId=c.entryResourceId,rewardResourceId=c.rewardResourceId,specialAffixPoolId=pool,entryAmount=1,rewardAmount=1,repeatable=true});
            db.challengeSpecialAffixPools.Add(BuildSpecialPool(b,pool,content,c.minimumCombatLevel));
        }
    }
    static readonly LootManager.GearType[] Weapons={LootManager.GearType.Weapons};
    static readonly LootManager.GearType[] Offensive={LootManager.GearType.Weapons,LootManager.GearType.Amulets,LootManager.GearType.Rings};
    static readonly LootManager.GearType[] Defensive={LootManager.GearType.Helmets,LootManager.GearType.BodyArmours,LootManager.GearType.Gloves,LootManager.GearType.Boots,LootManager.GearType.Belts,LootManager.GearType.Amulets,LootManager.GearType.Rings};
    static SpecialAffixDefinition S(string pool,string slug,string name,AffixSide side,LootManager.GearType[] types,StatTypes stat,float value,string effect,string description,int level,float second=0,float duration=0)
        =>new(){stableId=$"{pool}.{slug}",displayName=name,effectId=$"effect.special.{effect}",side=side,allowedItemTypes=types,statType=stat,minimum=value,maximum=value,minimumItemLevel=100,minimumCombatLevel=level,weight=1,description=description,effectValue=value,effectValue2=second,duration=duration};
    static SpecialAffixPoolDefinition BuildSpecialPool(int b,string pool,string content,int level)
    {
        var p=new SpecialAffixPoolDefinition{stableId=pool,poolName=ChallengeNames[b]+" Affixes",associatedContentId=content};
        switch(b)
        {
            case 0:
                p.modifiers.Add(S(pool,"ravaging","Ravaging",AffixSide.Prefix,Weapons,StatTypes.PhysDmg,.35f,"ravaging","+35% Physical Damage and +25% Bleed Damage.",level,.25f));
                p.modifiers.Add(S(pool,"overwhelming-blow","Overwhelming Blow",AffixSide.Prefix,Weapons,StatTypes.PhysDmg,0,"overwhelming-blow","After 1.5s without an attack, the next attack deals 40% more hit damage.",level,.40f,1.5f));
                p.modifiers.Add(S(pool,"deep-wounds","Deep Wounds",AffixSide.Prefix,Offensive,StatTypes.BleedChance,.20f,"deep-wounds","+20 percentage points Bleed Chance; qualifying large hits create 25% stronger Bleeds.",level,.25f,.05f));
                p.modifiers.Add(S(pool,"reprisal","Reprisal",AffixSide.Suffix,Defensive,StatTypes.ArmourPercent,0,"reprisal","After taking a direct hit, the next attack within 4s deals 30% more Physical hit damage.",level,.30f,4));
                p.modifiers.Add(S(pool,"blood-return","Blood Return",AffixSide.Suffix,Defensive,StatTypes.LifeOnHit,0,"blood-return","Hits against Bleeding enemies restore 2% missing Life, once per attack event with a 1s cooldown.",level,.02f,1));
                p.modifiers.Add(S(pool,"partial-rupture","Partial Rupture",AffixSide.Suffix,Defensive,StatTypes.BleedMult,0,"partial-rupture","Hits against Bleeding enemies have 8% chance to deal 25% remaining Bleed damage without consuming it.",level,.08f,.25f));break;
            case 1:
                p.modifiers.Add(S(pool,"cinder-power","Cinder Power",AffixSide.Prefix,Offensive,StatTypes.FireDmg,.35f,"cinder-power","+35% Fire Damage and +25% Ignite Damage.",level,.25f));
                p.modifiers.Add(S(pool,"eruption","Eruption",AffixSide.Prefix,Offensive,StatTypes.FireDmg,0,"eruption","12% chance on hit for a non-recursive Fire hit at 35% pre-defense magnitude.",level,.12f,.35f));
                p.modifiers.Add(S(pool,"rapid-burn","Rapid Burn",AffixSide.Prefix,Offensive,StatTypes.IgniteTickRate,.25f,"rapid-burn","Ignites deal damage 25% faster with 10% less duration.",level,-.10f));
                p.modifiers.Add(S(pool,"scorching-penetration","Scorching Penetration",AffixSide.Suffix,Defensive,StatTypes.FirePenetration,.15f,"scorching-penetration","+15% Fire Penetration and +20% Ignite Duration.",level,.20f));
                p.modifiers.Add(S(pool,"kindled-momentum","Kindled Momentum",AffixSide.Suffix,Defensive,StatTypes.FireDmg,0,"kindled-momentum","Applying Ignite grants 12% Fire Damage for 4s, up to three independently expiring stacks.",level,.12f,4));
                p.modifiers.Add(S(pool,"first-spark","First Spark",AffixSide.Suffix,Defensive,StatTypes.IgniteChance,0,"first-spark","First Fire hit against a non-Ignited enemy gains 50 points Ignite Chance and 30% Ignite magnitude.",level,.50f,.30f));break;
            case 2:
                p.modifiers.Add(S(pool,"deep-winter","Deep Winter",AffixSide.Prefix,Offensive,StatTypes.ColdDmg,.35f,"deep-winter","+35% Cold Damage and +25% Chill Effectiveness.",level,.25f));
                p.modifiers.Add(S(pool,"freezing-edge","Freezing Edge",AffixSide.Prefix,Offensive,StatTypes.ChillChance,0,"freezing-edge","Cold hits against sufficiently Chilled enemies have 10% chance to Freeze.",level,.10f,.20f));
                p.modifiers.Add(S(pool,"fracture","Fracture",AffixSide.Prefix,Offensive,StatTypes.ColdMult,0,"fracture","Shatter damage deals 50% more.",level,.50f));
                p.modifiers.Add(S(pool,"frozen-recovery","Frozen Recovery",AffixSide.Suffix,Defensive,StatTypes.ManaOnHit,0,"frozen-recovery","When Freeze skips an enemy attack, restore 3% maximum Mana and 2% maximum Life.",level,.03f,.02f));
                p.modifiers.Add(S(pool,"chilled-defense","Chilled Defense",AffixSide.Suffix,Defensive,StatTypes.ArmourPercent,0,"chilled-defense","While the enemy is Chilled, gain 12% Armour and 8% elemental resistances.",level,.12f,.08f));
                p.modifiers.Add(S(pool,"cryostasis","Cryostasis",AffixSide.Suffix,Defensive,StatTypes.ChillDuration,.30f,"cryostasis","+30% Chill Duration and +15% Chill Effectiveness.",level,.15f));break;
            case 3:
                p.modifiers.Add(S(pool,"overcharge","Overcharge",AffixSide.Prefix,Offensive,StatTypes.LightDmg,.35f,"overcharge","+35% Lightning Damage and +25% Shock Effectiveness.",level,.25f));
                p.modifiers.Add(S(pool,"static-echo","Static Echo",AffixSide.Prefix,Offensive,StatTypes.LightDmg,0,"static-echo","Every fifth eligible hit against a Shocked enemy triggers a non-recursive Lightning hit at 50% magnitude.",level,5,.50f));
                p.modifiers.Add(S(pool,"shocked-repetition","Shocked Repetition",AffixSide.Prefix,Offensive,StatTypes.ChanceToHitTwice,.12f,"shocked-repetition","+12 percentage points Hit Twice Chance against Shocked enemies.",level));
                p.modifiers.Add(S(pool,"conductive-criticals","Conductive Criticals",AffixSide.Suffix,Defensive,StatTypes.CritChance,.20f,"conductive-criticals","Against Shocked enemies gain 20% Crit Chance and 25% Crit Multiplier.",level,.25f));
                p.modifiers.Add(S(pool,"lingering-charge","Lingering Charge",AffixSide.Suffix,Defensive,StatTypes.ShockDuration,.30f,"lingering-charge","+30% Shock Duration and +15% Shock Effectiveness.",level,.15f));
                p.modifiers.Add(S(pool,"arc-momentum","Arc Momentum",AffixSide.Suffix,Defensive,StatTypes.AttackSpeed,0,"arc-momentum","Hits against Shocked enemies grant 2% Attack Speed for 3s, up to five stacks.",level,.02f,3));break;
            case 4:
                p.modifiers.Add(S(pool,"abyssal-venom","Abyssal Venom",AffixSide.Prefix,Offensive,StatTypes.VoidDmg,.35f,"abyssal-venom","+35% Void Damage and +30% Poison Damage.",level,.30f));
                p.modifiers.Add(S(pool,"corrosive-void","Corrosive Void",AffixSide.Prefix,Offensive,StatTypes.PoisonMult,0,"corrosive-void","Poisons with a legal Void basis deal 30% more damage.",level,.30f));
                p.modifiers.Add(S(pool,"toxic-echo","Toxic Echo",AffixSide.Prefix,Offensive,StatTypes.PoisonChance,0,"toxic-echo","Applied Poisons have 15% chance to echo at 50% magnitude without recursion.",level,.15f,.50f));
                p.modifiers.Add(S(pool,"void-exposure","Void Exposure",AffixSide.Suffix,Defensive,StatTypes.VoidPenetration,.15f,"void-exposure","+15% Void Penetration against Poisoned enemies.",level));
                p.modifiers.Add(S(pool,"accelerated-decay","Accelerated Decay",AffixSide.Suffix,Defensive,StatTypes.PoisonSpeed,.25f,"accelerated-decay","+25% Poison Speed and +15% Poison Duration.",level,.15f));
                p.modifiers.Add(S(pool,"corrupted-sustenance","Corrupted Sustenance",AffixSide.Suffix,Defensive,StatTypes.ManaOnHit,0,"corrupted-sustenance","Once per attack event, hitting a Poisoned enemy restores 2% maximum Mana and 1% maximum Life.",level,.02f,.01f));break;
            default:
                p.modifiers.Add(S(pool,"confluence","Confluence",AffixSide.Prefix,Offensive,StatTypes.GenericMult,0,"confluence","Gain 8% more damage per distinct direct-damage type dealt in the last 4s, up to five.",level,.08f,4));
                p.modifiers.Add(S(pool,"afflicted-dominion","Afflicted Dominion",AffixSide.Prefix,Offensive,StatTypes.GenericMult,0,"afflicted-dominion","Gain 8% more damage per distinct ailment on the enemy, up to five.",level,.08f,5));
                p.modifiers.Add(S(pool,"prismatic-core","Prismatic Core",AffixSide.Prefix,Weapons,StatTypes.GenericDmg,0,"prismatic-core","Gain 8% of weapon Physical damage as each elemental and Void type without recursion.",level,.08f,4));
                p.modifiers.Add(S(pool,"corruption-mastery","Corruption Mastery",AffixSide.Suffix,Defensive,StatTypes.GenericMult,0,"corruption-mastery","Gain 0.10% more damage per percentage point of current corruption.",level,.001f));
                p.modifiers.Add(S(pool,"balanced-assault","Balanced Assault",AffixSide.Suffix,Defensive,StatTypes.CritChance,0,"balanced-assault","Attacks containing at least three damage types gain 20% Crit Chance and Multiplier.",level,.20f,3));
                p.modifiers.Add(S(pool,"omniailment-resonance","Omniailment Resonance",AffixSide.Suffix,Defensive,StatTypes.AttackSpeed,0,"omniailment-resonance","Per distinct enemy ailment gain 5% Attack Speed and Cooldown Reduction, up to five.",level,.05f,5));break;
        }
        return p;
    }
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
