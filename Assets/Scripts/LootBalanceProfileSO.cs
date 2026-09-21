using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class CurrencyLootConfig
{
    public string stableCurrencyId; public CraftingCurrencyType currency; public float baseWeight;
    public CurrencyQualityTier qualityTier; public int minimumCombatLevel=1,maximumCombatLevel=360,minimumEnemyRarity,stackMinimum=1,stackMaximum=1;
    public bool requiresRebirthAccess,enabledInOrdinaryLoot=true;
    public CurrencyLootEntry ToEntry()=>new(stableCurrencyId,currency,baseWeight,qualityTier,minimumCombatLevel,maximumCombatLevel,minimumEnemyRarity,stackMinimum,stackMaximum,requiresRebirthAccess,enabledInOrdinaryLoot);
}

[CreateAssetMenu(menuName="Black Cube/Balance/Loot Balance Profile")]
public sealed class LootBalanceProfileSO:ScriptableObject
{
    public float levelFactorMinimum=.75f,levelFactorMaximum=2f,levelFactorExponent=.75f;
    public float normalRarityMultiplier=1f,magicRarityMultiplier=1.6f,rareRarityMultiplier=2.6f,legendaryRarityMultiplier=4f,bossRarityMultiplier=5f;
    public float extraGearThreshold=.75f,extraGearCoefficient=.60f,currencyRollCoefficient=.18f;
    public int maximumExtraGear=12,maximumCurrencyRolls=8;
    public float magicQualityBias=1.1f,rareQualityBias=1.3f,legendaryQualityBias=1.6f,bossQualityBias=2f;
    public List<CurrencyLootConfig> currencies=new();
    static LootBalanceProfileSO current;
    public static LootBalanceProfileSO Current
    {
        get
        {
            if(current==null)current=Resources.Load<LootBalanceProfileSO>("LootBalanceProfile");
            if(current==null){current=CreateInstance<LootBalanceProfileSO>();current.hideFlags=HideFlags.HideAndDontSave;current.ResetToProductionDefaults();}
            if(current.currencies==null||current.currencies.Count==0)current.ResetToProductionDefaults();
            return current;
        }
    }
    public void ResetToProductionDefaults()
    {
        currencies=new(){
            C("currency.normal_to_magic",CraftingCurrencyType.NormalToMagic,28,CurrencyQualityTier.Common),C("currency.reroll_magic",CraftingCurrencyType.RerollMagic,24,CurrencyQualityTier.Common),
            C("currency.magic_to_rare",CraftingCurrencyType.MagicToRare,17,CurrencyQualityTier.Uncommon,10),C("currency.reroll_rare",CraftingCurrencyType.RerollRareModifier,13,CurrencyQualityTier.Uncommon,20),
            C("currency.add_rare",CraftingCurrencyType.AddRareModifier,8,CurrencyQualityTier.Rare,30),C("currency.remove_rare",CraftingCurrencyType.RemoveRareModifier,6,CurrencyQualityTier.Rare,35),
            C("currency.ancient.normal_to_magic",CraftingCurrencyType.AncientNormalToMagic,2.5f,CurrencyQualityTier.Ancient,60,360,1,true),C("currency.ancient.magic_to_rare",CraftingCurrencyType.AncientMagicToRare,2,CurrencyQualityTier.Ancient,60,360,1,true),
            C("currency.ancient.rare_to_legendary",CraftingCurrencyType.AncientRareToLegendary,1.2f,CurrencyQualityTier.Ancient,60,360,2,true),C("currency.ancient.reroll",CraftingCurrencyType.AncientReroll,1.5f,CurrencyQualityTier.Ancient,60,360,1,true),
            C("currency.ancient.add",CraftingCurrencyType.AncientAddModifier,.8f,CurrencyQualityTier.Ancient,60,360,2,true),C("currency.ancient.remove",CraftingCurrencyType.AncientRemoveModifier,.7f,CurrencyQualityTier.Ancient,60,360,2,true)};
    }
    static CurrencyLootConfig C(string id,CraftingCurrencyType type,float weight,CurrencyQualityTier tier,int min=1,int max=360,int rarity=0,bool rebirth=false)=>new(){stableCurrencyId=id,currency=type,baseWeight=weight,qualityTier=tier,minimumCombatLevel=min,maximumCombatLevel=max,minimumEnemyRarity=rarity,requiresRebirthAccess=rebirth};
    public float RarityMultiplier(EnemyAI.EnemyRarity rarity,bool boss)=>boss?bossRarityMultiplier:rarity switch{EnemyAI.EnemyRarity.Magic=>magicRarityMultiplier,EnemyAI.EnemyRarity.Rare=>rareRarityMultiplier,EnemyAI.EnemyRarity.Legendary=>legendaryRarityMultiplier,_=>normalRarityMultiplier};
    public float QualityBias(EnemyAI.EnemyRarity rarity,bool boss)=>boss?bossQualityBias:rarity switch{EnemyAI.EnemyRarity.Magic=>magicQualityBias,EnemyAI.EnemyRarity.Rare=>rareQualityBias,EnemyAI.EnemyRarity.Legendary=>legendaryQualityBias,_=>1f};
}
