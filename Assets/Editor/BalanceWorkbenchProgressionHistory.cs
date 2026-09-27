using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public enum ProgressionHistoryMode { Straight, TargetFarming, Manual }

    [Serializable] public sealed class ProgressionHistoryRequest
    {
        public int targetPlayerLevel=50,extraClears=0,manualCombatLevel=50,manualNormalKills=0,
            manualMagicKills=0,manualRareKills=0,manualLegendaryKills=0,manualBossKills=0;
        public ProgressionHistoryMode mode;
        public bool stochastic=true;
        public long seed=41001;
        public float assumedEnemyPower=1f;
    }

    [Serializable] public sealed class ProgressionHistoryRow
    {
        public int combatLevel,stage,itemLevel;
        public bool boss,farming;
        public EnemyAI.EnemyRarity rarity;
        public double gearDrops,experienceAwarded;
        public List<ProgressionCurrencyCount> currencies=new();
    }
    [Serializable] public sealed class ProgressionCurrencyCount
    { public CraftingCurrencyType currency; public double count; }

    [Serializable] public sealed class ProgressionHistoryResult
    {
        public int targetPlayerLevel,finalPlayerLevel,finalCombatLevel,straightEncounters,farmingEncounters;
        public double normalKills,magicKills,rareKills,legendaryKills,bossKills,gearDrops;
        public bool targetReached;
        public string warning;
        public List<ProgressionCurrencyCount> currencies=new();
        public List<ProgressionHistoryRow> encounters=new();
    }

    // Explicitly models the production ten-stage combat level and XP formulas.
    // This history is not a survival claim: it assumes the player wins each encounter.
    public static class ProgressionHistorySimulator
    {
        public static ProgressionHistoryResult Run(ProgressionHistoryRequest request)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            var result=new ProgressionHistoryResult{targetPlayerLevel=Mathf.Clamp(request.targetPlayerLevel,1,100),finalPlayerLevel=1};
            var rng=new SeededSimulationRandomSource(request.seed);
            var database=WorldContentCatalog.Reference;
            var rarityProfiles=database?.enemyRarityProfiles?.Where(x=>x!=null&&x.spawnWeight>0).ToArray();
            if(rarityProfiles==null||rarityProfiles.Length==0)
                throw new InvalidOperationException("Production enemy rarity weights are unavailable.");
            double totalWeight=rarityProfiles.Sum(x=>x.spawnWeight),experience=0;
            var progressionObject=new GameObject("Progression history XP source"){hideFlags=HideFlags.HideAndDontSave};
            try
            {
                var progression=progressionObject.AddComponent<PlayerProgression>();
                if(request.mode==ProgressionHistoryMode.Manual)
                {
                    result.finalCombatLevel=Mathf.Clamp(request.manualCombatLevel,1,360);
                    result.finalPlayerLevel=result.targetPlayerLevel;
                    result.normalKills=Mathf.Max(0,request.manualNormalKills);
                    result.magicKills=Mathf.Max(0,request.manualMagicKills);
                    result.rareKills=Mathf.Max(0,request.manualRareKills);
                    result.legendaryKills=Mathf.Max(0,request.manualLegendaryKills);
                    result.bossKills=Mathf.Max(0,request.manualBossKills);
                    result.targetReached=true;
                    result.warning="Manual enemy counts are designer overrides; no XP or drops were inferred.";
                    return result;
                }
                int lastLevel=1;
                for(int combatLevel=1;combatLevel<=WorldProgression.MaximumAuthoredCombatLevel &&
                    result.finalPlayerLevel<result.targetPlayerLevel;combatLevel++)
                {
                    lastLevel=combatLevel;
                    for(int stage=1;stage<=WorldProgression.BossStage &&
                        result.finalPlayerLevel<result.targetPlayerLevel;stage++)
                    {
                        Process(combatLevel,stage,false);
                        while(result.finalPlayerLevel<result.targetPlayerLevel &&
                            experience>=progression.RequirementAt(result.finalPlayerLevel))
                        {
                            experience-=progression.RequirementAt(result.finalPlayerLevel);
                            result.finalPlayerLevel++;
                        }
                    }
                }
                result.finalCombatLevel=lastLevel;
                result.targetReached=result.finalPlayerLevel>=result.targetPlayerLevel;
                if(!result.targetReached)
                    result.warning="Target not reached with production XP. EnemyReward uses combat-level RequirementAt, which becomes zero from combat level 100; review this production rule before interpreting a late-level history.";
                if(result.targetReached&&request.mode==ProgressionHistoryMode.TargetFarming)
                    for(int clear=0;clear<Mathf.Clamp(request.extraClears,0,1000);clear++)
                        for(int stage=1;stage<=WorldProgression.BossStage;stage++)
                            Process(lastLevel,stage,true);
                if(!request.stochastic)
                    result.warning=(result.warning??string.Empty)+" Expected currency totals exclude currencies still using the legacy weighted path; use seeded stochastic mode to include those.";
                return result;

                void Process(int combatLevel,int stage,bool farming)
                {
                    bool boss=stage==WorldProgression.BossStage;
                    var rarity=boss?EnemyAI.EnemyRarity.Legendary:RollRarity();
                    var position=WorldProgression.Resolve(combatLevel,stage,database);
                    int itemLevel=Mathf.Clamp(combatLevel+(int)rarity,1,100);
                    var row=new ProgressionHistoryRow{combatLevel=combatLevel,stage=stage,
                        itemLevel=itemLevel,boss=boss,farming=farming,rarity=rarity};
                    double xp=boss?progression.EnemyReward(combatLevel,rarity,true):
                        request.stochastic?progression.EnemyReward(combatLevel,rarity,false):
                        rarityProfiles.Sum(x=>x.spawnWeight/totalWeight*
                            progression.EnemyReward(combatLevel,x.rarity,false));
                    row.experienceAwarded=xp;
                    if(!farming)experience+=xp;
                    if(boss)result.bossKills++;
                    else if(request.stochastic)IncrementRarity(rarity,1);
                    else foreach(var entry in rarityProfiles)IncrementRarity(entry.rarity,entry.spawnWeight/totalWeight);
                    DropRateContext Context(EnemyAI.EnemyRarity rarityValue)=>new(combatLevel,rarityValue,boss,
                        Mathf.Max(0f,request.assumedEnemyPower),position.Encounter?.stableId,
                        position.Location?.stableId,position.Encounter?.enemyArchetypeId,
                        position.Encounter?.bossId);
                    if(request.stochastic)
                    {
                        float expected=EnemyLootProfile.ExpectedGearScore(combatLevel,rarity,1);
                        var legacy=new EnemyLootPowerSnapshot(EnemyLootProfile.LevelFactor(combatLevel),
                            EnemyLootProfile.RarityMultiplier(rarity,boss),expected,expected,1f);
                        var drop=EnemyLootProfile.RollConfigured(Context(rarity),rng,legacy);
                        row.gearDrops=drop.gearCount;
                        foreach(var stack in drop.currencyStacks)
                            row.currencies.Add(new ProgressionCurrencyCount{currency=stack.currency,count=stack.amount});
                    }
                    else
                    {
                        // Expected mode integrates the production rarity distribution.
                        // Bosses use the production boss-rarity assumption directly.
                        var profile=LootDropBalanceProfileSO.Current;
                        var rarities=boss?new[]{new EnemyRarityProfile{rarity=rarity,spawnWeight=1}}:rarityProfiles;
                        double weightSum=boss?1:totalWeight;
                        foreach(var entry in rarities)
                        {
                            double weight=entry.spawnWeight/weightSum;
                            var context=Context(entry.rarity);
                            row.gearDrops+=weight*profile.EvaluateGear(context).finalBudget;
                            foreach(var currency in profile.currencies)
                                if(currency!=null&&!currency.useLegacyWeightedRoll)
                                {
                                    double count=weight*profile.EvaluateCurrency(currency.currency,context).finalBudget;
                                    if(count<=0)continue;
                                    var existing=row.currencies.Find(x=>x.currency==currency.currency);
                                    if(existing==null){existing=new ProgressionCurrencyCount{currency=currency.currency};row.currencies.Add(existing);}
                                    existing.count+=count;
                                }
                        }
                    }
                    result.gearDrops+=row.gearDrops;
                    foreach(var count in row.currencies)
                    {
                        var total=result.currencies.Find(x=>x.currency==count.currency);
                        if(total==null){total=new ProgressionCurrencyCount{currency=count.currency};result.currencies.Add(total);}
                        total.count+=count.count;
                    }
                    result.encounters.Add(row);
                    if(farming)result.farmingEncounters++;else result.straightEncounters++;
                }
                EnemyAI.EnemyRarity RollRarity()
                {
                    double choice=rng.Value()*totalWeight;
                    foreach(var entry in rarityProfiles){if(choice<entry.spawnWeight)return entry.rarity;choice-=entry.spawnWeight;}
                    return rarityProfiles[rarityProfiles.Length-1].rarity;
                }
                void IncrementRarity(EnemyAI.EnemyRarity rarity,double amount)
                {
                    switch(rarity)
                    {
                        case EnemyAI.EnemyRarity.Magic:result.magicKills+=amount;break;
                        case EnemyAI.EnemyRarity.Rare:result.rareKills+=amount;break;
                        case EnemyAI.EnemyRarity.Legendary:result.legendaryKills+=amount;break;
                        default:result.normalKills+=amount;break;
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(progressionObject);}
        }
    }
}
