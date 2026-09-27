using System;
using System.Collections.Generic;
using UnityEngine;

// Percentages are stored as fractions: .05 is five percentage points.
[Serializable]
public sealed class CurrencyDropRate
{
    public CraftingCurrencyType currency;
    [Min(0)] public float baseChance;
    [Min(1)] public int minimumCombatLevel=1;
    public bool useLegacyWeightedRoll;
}

[Serializable]
public sealed class DropRateOverride
{
    public string stableId;
    public int minimumCombatLevel=1,maximumCombatLevel=360;
    public CraftingCurrencyType currency;
    public bool appliesToAllCurrencies;
    public float additive;
    public float multiplier=1f;
    public float gearAdditive;
    public float gearMultiplier=1f;
}

public readonly struct DropRateContext
{
    public readonly int level;
    public readonly EnemyAI.EnemyRarity rarity;
    public readonly bool boss;
    public readonly string stageId,locationId,archetypeId,enemyId;
    public readonly float enemyPower;
    public DropRateContext(int level,EnemyAI.EnemyRarity rarity,bool boss,float enemyPower,
        string stageId=null,string locationId=null,string archetypeId=null,string enemyId=null)
    {this.level=level;this.rarity=rarity;this.boss=boss;this.enemyPower=enemyPower;
        this.stageId=stageId;this.locationId=locationId;this.archetypeId=archetypeId;this.enemyId=enemyId;}
}

public readonly struct DropBudgetBreakdown
{
    public readonly float baseChance,rarityAdditive,stageAdditive,locationAdditive,
        archetypeAdditive,entityAdditive,bossAdditive,stageMultiplier,locationMultiplier,
        archetypeMultiplier,entityMultiplier,bossMultiplier,enemyPower,finalBudget;
    public DropBudgetBreakdown(float baseChance,float rarityAdditive,float stageAdditive,
        float locationAdditive,float archetypeAdditive,float entityAdditive,float bossAdditive,
        float stageMultiplier,float locationMultiplier,float archetypeMultiplier,
        float entityMultiplier,float bossMultiplier,float enemyPower)
    {
        this.baseChance=baseChance;this.rarityAdditive=rarityAdditive;
        this.stageAdditive=stageAdditive;this.locationAdditive=locationAdditive;
        this.archetypeAdditive=archetypeAdditive;this.entityAdditive=entityAdditive;
        this.bossAdditive=bossAdditive;this.stageMultiplier=stageMultiplier;
        this.locationMultiplier=locationMultiplier;this.archetypeMultiplier=archetypeMultiplier;
        this.entityMultiplier=entityMultiplier;this.bossMultiplier=bossMultiplier;
        this.enemyPower=enemyPower;
        finalBudget=Mathf.Max(0f,(baseChance+rarityAdditive+stageAdditive+locationAdditive+
            archetypeAdditive+entityAdditive+bossAdditive)*stageMultiplier*locationMultiplier*
            archetypeMultiplier*entityMultiplier*bossMultiplier*enemyPower);
    }
}

[CreateAssetMenu(menuName="Black Cube/Balance/Loot Drop Balance Profile")]
public sealed class LootDropBalanceProfileSO:ScriptableObject
{
    [Header("Editable starting assumptions, not final balance")]
    public float gearBaseBudget=1f;
    public float normalRarityAdditive,magicRarityAdditive=.02f,rareRarityAdditive=.05f;
    public float legendaryRarityAdditive,bossAdditive;
    public float normalGearAdditive,magicGearAdditive,rareGearAdditive,legendaryGearAdditive,bossGearAdditive;
    public List<CurrencyDropRate> currencies=new();
    public List<DropRateOverride> stages=new(),locations=new(),archetypes=new(),enemies=new(),bosses=new();
    static LootDropBalanceProfileSO current;
    public static LootDropBalanceProfileSO Current
    {
        get
        {
            if(current==null)current=Resources.Load<LootDropBalanceProfileSO>("LootDropBalanceProfile");
            if(current==null){current=CreateInstance<LootDropBalanceProfileSO>();current.hideFlags=HideFlags.HideAndDontSave;current.ResetToInitialDefaults();}
            return current;
        }
    }
    public void ResetToInitialDefaults()
    {
        gearBaseBudget=1f;normalRarityAdditive=0;magicRarityAdditive=.02f;rareRarityAdditive=.05f;
        legendaryRarityAdditive=0;bossAdditive=0;
        currencies=new()
        {
            Rate(CraftingCurrencyType.NormalToMagic,.05f),Rate(CraftingCurrencyType.RerollMagic,.03f),
            Rate(CraftingCurrencyType.MagicToRare,.025f),Rate(CraftingCurrencyType.RerollRareModifier,.01f),
            Rate(CraftingCurrencyType.RemoveRareModifier,.005f),Rate(CraftingCurrencyType.AffixFocus,.002f,50),
            Legacy(CraftingCurrencyType.AddRareModifier),Legacy(CraftingCurrencyType.AncientNormalToMagic),
            Legacy(CraftingCurrencyType.AncientMagicToRare),Legacy(CraftingCurrencyType.AncientRareToLegendary),
            Legacy(CraftingCurrencyType.AncientReroll),Legacy(CraftingCurrencyType.AncientAddModifier),
            Legacy(CraftingCurrencyType.AncientRemoveModifier),
            Legacy(CraftingCurrencyType.EmpowermentCatalyst)
        };
        stages=new();locations=new();archetypes=new();enemies=new();bosses=new();
    }
    static CurrencyDropRate Rate(CraftingCurrencyType currency,float chance,int minLevel=1)
        =>new(){currency=currency,baseChance=chance,minimumCombatLevel=minLevel};
    static CurrencyDropRate Legacy(CraftingCurrencyType currency)
        =>new(){currency=currency,baseChance=0,useLegacyWeightedRoll=true};
    public CurrencyDropRate Rule(CraftingCurrencyType currency)
        =>currencies?.Find(x=>x!=null&&x.currency==currency);
    public float RarityAdditive(EnemyAI.EnemyRarity rarity)=>rarity switch
    {
        EnemyAI.EnemyRarity.Magic=>magicRarityAdditive,
        EnemyAI.EnemyRarity.Rare=>rareRarityAdditive,
        EnemyAI.EnemyRarity.Legendary=>legendaryRarityAdditive,
        _=>normalRarityAdditive
    };
    public DropBudgetBreakdown EvaluateCurrency(CraftingCurrencyType currency,DropRateContext context)
    {
        var rule=Rule(currency);
        if(rule==null||rule.useLegacyWeightedRoll||context.level<rule.minimumCombatLevel)return default;
        return Evaluate(rule.baseChance,RarityAdditive(context.rarity),context,currency,false);
    }
    public DropBudgetBreakdown EvaluateGear(DropRateContext context)
    {
        float rarity=context.rarity switch
        {
            EnemyAI.EnemyRarity.Magic=>magicGearAdditive,
            EnemyAI.EnemyRarity.Rare=>rareGearAdditive,
            EnemyAI.EnemyRarity.Legendary=>legendaryGearAdditive,
            _=>normalGearAdditive
        };
        return Evaluate(gearBaseBudget,rarity,context,default,true);
    }
    DropBudgetBreakdown Evaluate(float baseChance,float rarity,DropRateContext context,CraftingCurrencyType currency,bool gear)
    {
        var stage=Find(stages,context.stageId,context.level,currency,gear);
        var location=Find(locations,context.locationId,context.level,currency,gear);
        var archetype=Find(archetypes,context.archetypeId,context.level,currency,gear);
        var entity=Find(enemies,context.enemyId,context.level,currency,gear);
        var boss=context.boss?Find(bosses,context.enemyId,context.level,currency,gear):null;
        return new DropBudgetBreakdown(baseChance,rarity,Add(stage,gear),Add(location,gear),
            Add(archetype,gear),Add(entity,gear),context.boss?(gear?bossGearAdditive:bossAdditive)+Add(boss,gear):0f,
            Mult(stage,gear),Mult(location,gear),Mult(archetype,gear),Mult(entity,gear),Mult(boss,gear),
            Mathf.Max(0f,context.enemyPower));
    }
    static DropRateOverride Find(List<DropRateOverride> list,string id,int level,CraftingCurrencyType currency,bool gear)
    {
        if(list==null||string.IsNullOrEmpty(id))return null;
        return list.Find(x=>x!=null&&x.stableId==id&&level>=x.minimumCombatLevel&&level<=x.maximumCombatLevel
            &&(gear||x.appliesToAllCurrencies||x.currency==currency));
    }
    static float Add(DropRateOverride entry,bool gear)=>entry==null?0f:gear?entry.gearAdditive:entry.additive;
    static float Mult(DropRateOverride entry,bool gear)=>entry==null?1f:Mathf.Max(0f,gear?entry.gearMultiplier:entry.multiplier);
    public static int RollCopies(float budget,ILootRandomSource random)
    {
        budget=Mathf.Max(0f,budget);
        int whole=Mathf.FloorToInt(budget);
        return whole+(random.Value()<budget-whole?1:0);
    }
}
