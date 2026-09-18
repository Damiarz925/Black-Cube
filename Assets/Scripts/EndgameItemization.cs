// Central finite-crafting and post-100 item progression policies.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class CraftingPotentialProfile
{
    public static int Maximum(LootManager.GearRarity origin) => origin switch
    {
        LootManager.GearRarity.Normal => 6,
        LootManager.GearRarity.Magic => 8,
        LootManager.GearRarity.Rare => 10,
        _ => 14
    };

    public static int OrdinaryCost(CraftingCurrencyType type) => type switch
    {
        CraftingCurrencyType.NormalToMagic => 1,
        CraftingCurrencyType.RerollMagic => 1,
        CraftingCurrencyType.MagicToRare => 1,
        CraftingCurrencyType.RerollRareModifier => 1,
        CraftingCurrencyType.AddRareModifier => 2,
        CraftingCurrencyType.RemoveRareModifier => 2,
        _ => 0
    };

    public const int BossSpecialReplacementCost = 3;
}

public static class EmpowermentProgressionProfile
{
    static readonly (int level, int count)[] Thresholds =
    {
        (120,1),(160,2),(210,3),(260,4),(310,5),(360,6)
    };

    public static int MaximumEmpoweredModifiers(int combatLevel)
    {
        int result=0;
        foreach(var threshold in Thresholds)if(combatLevel>=threshold.level)result=threshold.count;
        return result;
    }
}

public static class EmpowermentCrafting
{
    public static bool TryApply(Gear gear,int combatLevel,float candidateRoll=-1f,float valueRoll=-1f,float highRoll=-1f)
    {
        if(gear==null||gear.EmpoweredModifierCount>=EmpowermentProgressionProfile.MaximumEmpoweredModifiers(combatLevel))return false;
        var candidates=new List<(RolledMod mod,AffixTier tier,AffixDefinitions definition)>();
        foreach(var mod in gear.rolledMods)
        {
            if(mod==null||mod.lockedOriginal||mod.isEmpowered||mod.isBossSpecial||Gear.IsWeaponBaseStat(mod.statType)||mod.tierIndex!=1)continue;
            var definition=ModManager.Instance?.Database?.GetDefinition(mod.statType);
            if(!IsEmpowerable(definition,mod.statType))continue;
            var tier=ModManager.ApplicableTiers(definition,gear.ItemType,gear.WeaponTypeId).Find(x=>x.tierIndex==1);
            if(tier!=null)candidates.Add((mod,tier,definition));
        }
        if(candidates.Count==0)return false;
        float pick=candidateRoll<0f?UnityEngine.Random.value:Mathf.Clamp01(candidateRoll);
        var selected=candidates[Mathf.Min(candidates.Count-1,Mathf.FloorToInt(pick*candidates.Count))];
        float lowRoll=valueRoll<0f?UnityEngine.Random.value:Mathf.Clamp01(valueRoll);
        float upperRoll=highRoll<0f?UnityEngine.Random.value:Mathf.Clamp01(highRoll);
        EmpoweredRange(selected.definition,selected.tier,out float min,out float max,out float minHigh,out float maxHigh);
        selected.mod.value=Round(Mathf.Lerp(min,max,lowRoll));
        if(selected.tier.pairedDamage)
        {
            selected.mod.hasSecondaryValue=true;
            selected.mod.secondaryValue=Mathf.Max(selected.mod.value,Round(Mathf.Lerp(minHigh,maxHigh,upperRoll)));
        }
        selected.mod.isEmpowered=true;
        gear.RebuildMods();
        return true;
    }

    public static bool IsEmpowerable(AffixDefinitions definition,StatTypes stat)
    {
        if(definition==null||!definition.empowerable||Gear.IsWeaponBaseStat(stat))return false;
        return stat is < StatTypes.Plus1Phys or > StatTypes.Plus1Ignite;
    }

    public static void EmpoweredRange(AffixDefinitions definition,AffixTier tier,
        out float min,out float max,out float minHigh,out float maxHigh)
    {
        if(definition!=null&&definition.hasCustomEmpoweredRange)
        {
            min=definition.empoweredMin;max=definition.empoweredMax;
            minHigh=definition.empoweredMinHigh;maxHigh=definition.empoweredMaxHigh;return;
        }
        min=Round(tier.minValue*1.25f);max=Round(tier.maxValue*1.25f);
        minHigh=Round(tier.minHighValue*1.25f);maxHigh=Round(tier.maxHighValue*1.25f);
    }

    static float Round(float value)=>Mathf.Round(value*100f)/100f;
}

[Serializable]
public sealed class SpecialAffixDefinition
{
    public string stableId;
    public AffixSide side;
    public LootManager.GearType[] allowedItemTypes;
    public StatTypes statType;
    public float minimum,maximum;
    public bool pairedDamage;
    public float minimumHigh,maximumHigh;
    public int minimumItemLevel=100,minimumCombatLevel=100,weight=1;
    public string description;

    public bool Allows(LootManager.GearType type)
    {
        if(allowedItemTypes==null)return false;
        foreach(var allowed in allowedItemTypes)if(allowed==type)return true;
        return false;
    }
}

[Serializable]
public sealed class SpecialAffixPoolDefinition
{
    public string stableId;
    public string poolName;
    public string associatedContentId;
    public List<SpecialAffixDefinition> modifiers=new();
}

public static class BossSpecialCrafting
{
    public static bool TryReplace(Gear gear,SpecialAffixPoolDefinition pool,int combatLevel,
        float definitionRoll=-1f,float replacementRoll=-1f,float valueRoll=-1f,float highRoll=-1f)
    {
        if(gear==null||pool==null||gear.ItemRarity!=LootManager.GearRarity.Legendary
            ||gear.CurrentCraftingPotential<CraftingPotentialProfile.BossSpecialReplacementCost)return false;
        var definitions=new List<SpecialAffixDefinition>();
        foreach(var definition in pool.modifiers)if(definition!=null&&!string.IsNullOrWhiteSpace(definition.stableId)
            &&definition.weight>0&&definition.minimum<=definition.maximum&&definition.Allows(gear.ItemType)
            &&gear.ItemLevel>=definition.minimumItemLevel&&combatLevel>=definition.minimumCombatLevel)
        {
            foreach(var mod in gear.rolledMods)if(IsReplaceable(mod)&&AffixPolicy.Side(mod)==definition.side)
            {definitions.Add(definition);break;}
        }
        if(definitions.Count==0)return false;
        var chosen=ChooseWeighted(definitions,definitionRoll<0f?UnityEngine.Random.value:definitionRoll);
        var replacements=new List<RolledMod>();
        foreach(var mod in gear.rolledMods)if(IsReplaceable(mod)&&AffixPolicy.Side(mod)==chosen.side)replacements.Add(mod);
        if(replacements.Count==0)return false;
        float pick=replacementRoll<0f?UnityEngine.Random.value:Mathf.Clamp01(replacementRoll);
        var replaced=replacements[Mathf.Min(replacements.Count-1,Mathf.FloorToInt(pick*replacements.Count))];
        float roll=valueRoll<0f?UnityEngine.Random.value:Mathf.Clamp01(valueRoll);
        var special=new RolledMod(chosen.statType,1,Mathf.Lerp(chosen.minimum,chosen.maximum,roll),false)
            {isBossSpecial=true,specialPoolId=pool.stableId,specialModifierId=chosen.stableId,specialAffixSide=chosen.side};
        if(chosen.pairedDamage)
        {
            float high=highRoll<0f?UnityEngine.Random.value:Mathf.Clamp01(highRoll);
            special.hasSecondaryValue=true;
            special.secondaryValue=Mathf.Max(special.value,Mathf.Lerp(chosen.minimumHigh,chosen.maximumHigh,high));
        }
        gear.rolledMods[gear.rolledMods.IndexOf(replaced)]=special;
        if(!gear.TrySpendCraftingPotential(CraftingPotentialProfile.BossSpecialReplacementCost))return false;
        gear.RebuildMods();
        return true;
    }

    static bool IsReplaceable(RolledMod mod)=>mod!=null&&!mod.lockedOriginal&&!mod.isEmpowered
        &&!Gear.IsWeaponBaseStat(mod.statType);

    static SpecialAffixDefinition ChooseWeighted(List<SpecialAffixDefinition> definitions,float unitRoll)
    {
        int total=0;foreach(var definition in definitions)total+=definition.weight;
        float choice=Mathf.Clamp01(unitRoll)*total;
        foreach(var definition in definitions){choice-=definition.weight;if(choice<=0f)return definition;}
        return definitions[^1];
    }
}
