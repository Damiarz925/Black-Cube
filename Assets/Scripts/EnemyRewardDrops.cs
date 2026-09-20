// Developer map: Per-enemy reward table, independent roll result, and exactly-once world currency pickup presentation.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CurrencyDropRule
{
    public CraftingCurrencyType currency;
    [Range(0f,1f)] public float chance;
    public CurrencyDropRule(CraftingCurrencyType type,float probability){currency=type;chance=probability;}
}

public sealed class EnemyDropResult
{
    public bool equipment;
    public int gearCount;
    public readonly List<CraftingCurrencyType> currencies=new();
    public readonly List<CurrencyDropStack> currencyStacks=new();
    public EnemyLootPowerSnapshot power;
}

[Serializable] public struct CurrencyDropStack{public CraftingCurrencyType currency;public int amount;public CurrencyDropStack(CraftingCurrencyType value,int count){currency=value;amount=count;}}

public readonly struct EnemyLootPowerSnapshot
{
    public readonly float LevelLootFactor,RarityMultiplier,ActualGearScore,ExpectedGearScore,GearQualityFactor,LootPower,ExtraGearBudget,CurrencyRollBudget;
    public EnemyLootPowerSnapshot(float level,float rarity,float actual,float expected,float quality)
    {LevelLootFactor=level;RarityMultiplier=rarity;ActualGearScore=actual;ExpectedGearScore=expected;GearQualityFactor=quality;LootPower=level*rarity*quality;ExtraGearBudget=Mathf.Max(0,LootPower-.75f)*.60f;CurrencyRollBudget=.18f*LootPower;}
    public override string ToString()=>$"LevelLootFactor: {LevelLootFactor:0.00} / RarityMultiplier: {RarityMultiplier:0.00} / GearQualityFactor: {GearQualityFactor:0.00} / LootPower: {LootPower:0.00}";
}

public enum CurrencyQualityTier{Common=0,Uncommon=1,Rare=2,Ancient=3}
[Serializable] public sealed class CurrencyLootEntry
{
    public readonly string StableCurrencyId;public readonly CraftingCurrencyType Currency;public readonly float BaseWeight;public readonly int MinimumCombatLevel,MaximumCombatLevel,MinimumEnemyRarity,StackMinimum,StackMaximum;public readonly CurrencyQualityTier QualityTier;public readonly bool RequiresRebirthAccess,EnabledInOrdinaryLoot;
    public CurrencyLootEntry(string id,CraftingCurrencyType currency,float weight,CurrencyQualityTier quality,int minLevel=1,int maxLevel=360,int minRarity=0,int minStack=1,int maxStack=1,bool rebirth=false,bool enabled=true)
    {StableCurrencyId=id;Currency=currency;BaseWeight=weight;QualityTier=quality;MinimumCombatLevel=minLevel;MaximumCombatLevel=maxLevel;MinimumEnemyRarity=minRarity;StackMinimum=minStack;StackMaximum=maxStack;RequiresRebirthAccess=rebirth;EnabledInOrdinaryLoot=enabled;}
}

public static class CurrencyLootTable
{
    static readonly CurrencyLootEntry[] entries={
        new("currency.normal_to_magic",CraftingCurrencyType.NormalToMagic,28,CurrencyQualityTier.Common),
        new("currency.reroll_magic",CraftingCurrencyType.RerollMagic,24,CurrencyQualityTier.Common),
        new("currency.magic_to_rare",CraftingCurrencyType.MagicToRare,17,CurrencyQualityTier.Uncommon,10),
        new("currency.reroll_rare",CraftingCurrencyType.RerollRareModifier,13,CurrencyQualityTier.Uncommon,20),
        new("currency.add_rare",CraftingCurrencyType.AddRareModifier,8,CurrencyQualityTier.Rare,30),
        new("currency.remove_rare",CraftingCurrencyType.RemoveRareModifier,6,CurrencyQualityTier.Rare,35),
        new("currency.ancient.normal_to_magic",CraftingCurrencyType.AncientNormalToMagic,2.5f,CurrencyQualityTier.Ancient,60,360,1,1,1,true),
        new("currency.ancient.magic_to_rare",CraftingCurrencyType.AncientMagicToRare,2,CurrencyQualityTier.Ancient,60,360,1,1,1,true),
        new("currency.ancient.rare_to_legendary",CraftingCurrencyType.AncientRareToLegendary,1.2f,CurrencyQualityTier.Ancient,60,360,2,1,1,true),
        new("currency.ancient.reroll",CraftingCurrencyType.AncientReroll,1.5f,CurrencyQualityTier.Ancient,60,360,1,1,1,true),
        new("currency.ancient.add",CraftingCurrencyType.AncientAddModifier,.8f,CurrencyQualityTier.Ancient,60,360,2,1,1,true),
        new("currency.ancient.remove",CraftingCurrencyType.AncientRemoveModifier,.7f,CurrencyQualityTier.Ancient,60,360,2,1,1,true)};
    public static IReadOnlyList<CurrencyLootEntry> Entries=>entries;
    public static float QualityBias(EnemyAI.EnemyRarity rarity,bool boss)=>boss?2f:rarity switch{EnemyAI.EnemyRarity.Magic=>1.1f,EnemyAI.EnemyRarity.Rare=>1.3f,EnemyAI.EnemyRarity.Legendary=>1.6f,_=>1f};
    public static CurrencyLootEntry Choose(int level,EnemyAI.EnemyRarity rarity,bool boss,bool rebirthAccess,ILootRandomSource random)
    {
        float bias=QualityBias(rarity,boss),total=0;var eligible=new List<(CurrencyLootEntry,float)>();
        foreach(var entry in entries){if(!entry.EnabledInOrdinaryLoot||level<entry.MinimumCombatLevel||level>entry.MaximumCombatLevel||(int)rarity<entry.MinimumEnemyRarity||(entry.RequiresRebirthAccess&&!rebirthAccess))continue;float weight=entry.BaseWeight*Mathf.Pow(bias,(int)entry.QualityTier);eligible.Add((entry,weight));total+=weight;}
        if(total<=0||eligible.Count==0)return null;float roll=random.Value()*total;
        foreach(var pair in eligible){if(roll<pair.Item2)return pair.Item1;roll-=pair.Item2;}return eligible[eligible.Count-1].Item1;
    }
    public static CurrencyLootEntry Choose(int level,EnemyAI.EnemyRarity rarity,bool boss,bool rebirthAccess,Func<float> next)
        =>Choose(level,rarity,boss,rebirthAccess,new DelegateLootRandomSource(next));
}

public static class EnemyLootProfile
{
    public const int MaximumExtraGear=12,MaximumCurrencyRolls=8;
    public static float LevelFactor(int level)=>Mathf.Lerp(.75f,2f,Mathf.Pow(Mathf.Clamp01((Mathf.Max(1,level)-1)/359f),.75f));
    public static float RarityMultiplier(EnemyAI.EnemyRarity rarity,bool boss)=>boss?5f:rarity switch{EnemyAI.EnemyRarity.Magic=>1.6f,EnemyAI.EnemyRarity.Rare=>2.6f,EnemyAI.EnemyRarity.Legendary=>4f,_=>1f};
    public static float ExpectedGearScore(int level,EnemyAI.EnemyRarity rarity,int equippedSlots)
    {
        // Formula baseline mirrors the enemy model's capped gear-level and growing slot count without sampling.
        float gearLevel=EnemyAI.EffectiveEnemyGearItemLevel(level);return 30f*Mathf.Pow(1f+gearLevel*.075f,.75f)*Mathf.Pow(Mathf.Max(1,equippedSlots),.25f)*Mathf.Pow(RarityMultiplier(rarity,false),.15f);
    }
    public static EnemyLootPowerSnapshot Evaluate(EnemyAI enemy,bool boss)
    {
        int level=enemy!=null?enemy.EnemyLevel:1;var rarity=enemy!=null?enemy.CurrentRarity:EnemyAI.EnemyRarity.Normal;int slots=enemy?.EquippedItems?.Count??1;
        float actual=enemy!=null?EnemyBuildOptimizer.CanonicalGearScore(enemy.LastBuildEvaluation):ExpectedGearScore(level,rarity,slots);float expected=ExpectedGearScore(level,rarity,slots);float quality=Mathf.Clamp(actual/Mathf.Max(.001f,expected),.60f,3f);
        return new EnemyLootPowerSnapshot(LevelFactor(level),RarityMultiplier(rarity,boss),actual,expected,quality);
    }
    public static int StochasticRound(float budget,int cap,ILootRandomSource random)=>Mathf.Clamp(Mathf.FloorToInt(budget)+(random.Value()<budget-Mathf.Floor(budget)?1:0),0,cap);
    public static int StochasticRound(float budget,int cap,Func<float> next)=>StochasticRound(budget,cap,new DelegateLootRandomSource(next));
    public static EnemyDropResult Roll(EnemyAI enemy,bool boss,ILootRandomSource random)
    {
        random??=LootRandomSourceFactory.CreateProduction();var snapshot=Evaluate(enemy,boss);var result=new EnemyDropResult{equipment=true,power=snapshot};
        result.gearCount=1+StochasticRound(snapshot.ExtraGearBudget,MaximumExtraGear,random);
        int currencyRolls=StochasticRound(snapshot.CurrencyRollBudget,MaximumCurrencyRolls,random);int level=enemy!=null?enemy.EnemyLevel:1;var rarity=enemy!=null?enemy.CurrentRarity:EnemyAI.EnemyRarity.Normal;bool access=level>=RebirthManager.RequiredZone;
        var aggregate=new Dictionary<CraftingCurrencyType,int>();for(int i=0;i<currencyRolls;i++){var entry=CurrencyLootTable.Choose(level,rarity,boss,access,random);if(entry==null)continue;int amount=random.Range(entry.StackMinimum,entry.StackMaximum+1);aggregate[entry.Currency]=(aggregate.TryGetValue(entry.Currency,out int old)?old:0)+amount;}
        foreach(var pair in aggregate){result.currencyStacks.Add(new CurrencyDropStack(pair.Key,pair.Value));for(int i=0;i<pair.Value;i++)result.currencies.Add(pair.Key);}return result;
    }
    public static EnemyDropResult Roll(EnemyAI enemy,bool boss,Func<float> next)=>Roll(enemy,boss,new DelegateLootRandomSource(next));
}

[Serializable]
public sealed class EnemyDropTable
{
    [Range(0f,1f)] public float equipmentChance=.5f;
    [SerializeField] List<CurrencyDropRule> currencyRules=Defaults();
    public IReadOnlyList<CurrencyDropRule> CurrencyRules=>currencyRules;

    public EnemyDropResult Roll(Func<float> next=null,bool isBoss=false)
    {
        EnsureDefaults();next??=()=>UnityEngine.Random.value;
        // Preserve the independent equipment and ordinary-currency rolls for
        // both roles. The boss then receives one extra guaranteed ordinary orb.
        bool equipmentRoll=next()<Mathf.Clamp01(equipmentChance);
        var result=new EnemyDropResult{equipment=isBoss||equipmentRoll};
        foreach(var rule in currencyRules)if(rule!=null&&!CurrencyInventory.IsAncient(rule.currency)&&next()<Mathf.Clamp01(rule.chance))result.currencies.Add(rule.currency);
        if(isBoss)
        {
            var types=OrdinaryTypes();
            int index=Mathf.Clamp(Mathf.FloorToInt(next()*types.Length),0,types.Length-1);
            result.currencies.Add(types[index]);
        }
        return result;
    }

    public void EnsureDefaults()
    {
        currencyRules??=new List<CurrencyDropRule>();
        foreach(var type in OrdinaryTypes())if(!currencyRules.Exists(rule=>rule!=null&&rule.currency==type))currencyRules.Add(new CurrencyDropRule(type,DefaultChance(type)));
    }

    public static float DefaultChance(CraftingCurrencyType type)=>type switch
    {
        CraftingCurrencyType.NormalToMagic or CraftingCurrencyType.RerollMagic=>.07f,
        CraftingCurrencyType.MagicToRare=>.05f,
        CraftingCurrencyType.RerollRareModifier=>.04f,
        CraftingCurrencyType.AddRareModifier=>.03f,
        CraftingCurrencyType.RemoveRareModifier=>.02f,
        _=>0f
    };
    static List<CurrencyDropRule> Defaults(){var list=new List<CurrencyDropRule>();foreach(var type in OrdinaryTypes())list.Add(new CurrencyDropRule(type,DefaultChance(type)));return list;}
    public static CraftingCurrencyType[] OrdinaryTypes()=>new[]{CraftingCurrencyType.NormalToMagic,CraftingCurrencyType.RerollMagic,CraftingCurrencyType.MagicToRare,CraftingCurrencyType.RerollRareModifier,CraftingCurrencyType.AddRareModifier,CraftingCurrencyType.RemoveRareModifier};
}

public sealed class CurrencyRewardClaim
{
    readonly CraftingCurrencyType type;readonly int amount;bool claimed;
    public CurrencyRewardClaim(CraftingCurrencyType value,int count=1){type=value;amount=Mathf.Max(1,count);}
    public bool TryClaim()
    {
        if(claimed)return false;claimed=true;return true;
    }
    public bool TryAward()
    {
        if(CurrencyInventory.Instance==null||!TryClaim())return false;
        CurrencyInventory.Instance.Add(type,amount);
        GamePersistence.Save();
        return true;
    }
}

public sealed class CurrencyWorldPickup:MonoBehaviour
{
    CurrencyRewardClaim claim;Transform target;Vector3 start;float age;float displayScale=.58f;
    const float ReadDelay=.35f,FlyDuration=.55f;
    public static CurrencyWorldPickup Spawn(CraftingCurrencyType type,Vector3 position,Transform player)=>Spawn(type,1,position,player);
    public static CurrencyWorldPickup Spawn(CraftingCurrencyType type,int amount,Vector3 position,Transform player)
    {
        var go=new GameObject("Dropped "+CurrencyPresentation.Name(type),typeof(SpriteRenderer),typeof(CurrencyWorldPickup));
        go.transform.position=position+new Vector3(UnityEngine.Random.Range(-.28f,.28f),.25f+UnityEngine.Random.Range(0f,.18f),0);
        var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=InventoryArtCatalog.Currency(type);renderer.sortingOrder=80;
        var pickup=go.GetComponent<CurrencyWorldPickup>();pickup.claim=new CurrencyRewardClaim(type,amount);pickup.target=player;pickup.start=go.transform.position;
        float largestDimension = renderer.sprite != null
            ? Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y)
            : 0f;
        pickup.displayScale = largestDimension > 0f ? .62f / largestDimension : .58f;
        go.transform.localScale=Vector3.one*pickup.displayScale;
        return pickup;
    }
    void Update()
    {
        if(claim==null)return;age+=Time.unscaledDeltaTime;
        if(age<ReadDelay){transform.position=start+Vector3.up*(Mathf.Sin(age*8f)*.04f);return;}
        if(target==null){AwardAndFinish();return;}
        float t=Mathf.Clamp01((age-ReadDelay)/FlyDuration);transform.position=Vector3.Lerp(start,target.position,1f-(1f-t)*(1f-t));transform.localScale=Vector3.one*(displayScale*(1f-.55f*t));
        if(t>=1f)AwardAndFinish();
    }
    void AwardAndFinish(){claim?.TryAward();claim=null;Destroy(gameObject);}
    void OnDestroy(){claim?.TryAward();claim=null;}
}
