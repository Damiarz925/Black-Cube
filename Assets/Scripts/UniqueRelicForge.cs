using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EndgameGauntletRules
{
    public static bool IsGauntlet(int combatLevel)=>combatLevel>=341&&combatLevel<=350;
}
public static class UniqueRelicForge
{
    // Index into powers followed by fixed ordinary-stat rolls. Choice preserves actual drop values.
    public static IReadOnlyList<string> Choices(Gear item)
    {
        if(item?.UniqueData==null)return Array.Empty<string>();
        return item.UniqueData.powers.Select(UniqueCatalog.Describe)
            .Concat(item.rolledMods.Select(x=>$"{StatDisplayFormatting.ToFriendlyName(x.statType)}: {x.value:0.##}")).ToArray();
    }
    public static bool TryForge(RelicInventory inventory,Gear first,Gear second,Gear legendary1,Gear legendary2,int firstChoice,int secondChoice,Func<float> random,out RelicData result,out string error)
    {
        result=null;error=null;var items=new[]{first,second,legendary1,legendary2};
        bool Owned(Gear g)=>Inventory.Instance!=null&&Inventory.Instance.Items.Contains(g);
        if(inventory==null||inventory.ForgeOpportunities<=0){error="No stage-350 forge opportunity available.";return false;}
        if(items.Any(x=>x==null||x.IsLocked||!Owned(x))||items.Distinct().Count()!=4){error="Choose four distinct, unlocked items in your inventory.";return false;}
        if(first.UniqueData==null||second.UniqueData==null||legendary1.ItemRarity!=LootManager.GearRarity.Legendary||legendary2.ItemRarity!=LootManager.GearRarity.Legendary||legendary1.CraftingModCount!=6||legendary2.CraftingModCount!=6){error="Recipe requires two Uniques and two Legendaries with six explicit modifiers each.";return false;}
        if(firstChoice<0||firstChoice>=Choices(first).Count||secondChoice<0||secondChoice>=Choices(second).Count){error="Choose one modifier from each Unique.";return false;}
        random??=()=>UnityEngine.Random.value;
        var output=new RelicData{id=Guid.NewGuid().ToString("N"),cycle=Mathf.Max(1,inventory.CurrentCycle),relicLevel=100,rarity=LootManager.GearRarity.Unique,crafted=true,uniqueRelic=true,craftableThisCycle=false};
        void Extract(Gear gear,int choice)
        {
            if(choice<gear.UniqueData.powers.Count)output.forgedPowers.Add(gear.UniqueData.powers[choice].Copy());
            else output.forgedStats.Add(CopyStat(gear.rolledMods[choice-gear.UniqueData.powers.Count]));
        }
        Extract(first,firstChoice);Extract(second,secondChoice);
        foreach(var gear in new[]{legendary1,legendary2})
        {
            var mods=gear.rolledMods.Where(x=>x!=null&&!x.lockedOriginal&&!Gear.IsWeaponBaseStat(x.statType)).ToArray();
            int index=Mathf.Min(5,Mathf.FloorToInt(Mathf.Clamp01(random())*6));output.forgedStats.Add(CopyStat(mods[index]));
        }
        output.forgeSourceIds=items.Select(x=>x.PersistentId).ToList();
        // Construct and validate before mutating either inventory or opportunity count.
        if(!Validate(output)){error="Extracted recipe could not produce a valid relic.";return false;}
        foreach(var gear in items){Inventory.Instance.Remove(gear);gear.Dismantled=true;UnityEngine.Object.Destroy(gear.gameObject);}
        inventory.ConsumeForgeOpportunity();inventory.Grant(output);result=output;GamePersistence.Save();return true;
    }
    public static RolledMod CopyStat(RolledMod mod)=>new(mod.statType,mod.tierIndex,mod.value,mod.HighValue,false)
        {hasSecondaryValue=mod.hasSecondaryValue,isEmpowered=mod.isEmpowered,isBossSpecial=mod.isBossSpecial,specialPoolId=mod.specialPoolId,specialModifierId=mod.specialModifierId,specialAffixSide=mod.specialAffixSide};
    public static bool Validate(RelicData relic)=>relic.uniqueRelic&&relic.rarity==LootManager.GearRarity.Unique&&!relic.craftableThisCycle&&relic.crafted
        &&relic.modifiers!=null&&relic.modifiers.Count==0&&relic.forgedPowers!=null&&relic.forgedStats!=null&&relic.forgedPowers.Count+relic.forgedStats.Count==4
        &&relic.forgeSourceIds!=null&&relic.forgeSourceIds.Count==4&&relic.forgeSourceIds.All(x=>!string.IsNullOrWhiteSpace(x))&&relic.forgeSourceIds.Distinct().Count()==4
        &&relic.forgedPowers.All(x=>x!=null&&Enum.IsDefined(typeof(UniquePower),x.power)&&float.IsFinite(x.value))
        &&relic.forgedStats.All(x=>x!=null&&Enum.IsDefined(typeof(StatTypes),x.statType)&&!Gear.IsWeaponBaseStat(x.statType)&&float.IsFinite(x.value)&&float.IsFinite(x.HighValue)&&x.tierIndex>=1&&x.tierIndex<=20);
}
public sealed partial class RelicInventory
{
    [SerializeField] int forgeOpportunities;
    public int ForgeOpportunities=>forgeOpportunities;
    public void RestoreForgeOpportunities(int count)=>forgeOpportunities=Mathf.Max(0,count);
    public void AwardForgeOpportunity(){forgeOpportunities++;PublishChanged();}
    internal void ConsumeForgeOpportunity(){forgeOpportunities--;}
    public float ForgedPower(UniquePower power)=>ActiveRelics().Where(x=>x.uniqueRelic).SelectMany(x=>x.forgedPowers).Where(x=>x.power==power).Sum(x=>x.value);
    public int ForgedAuraMask()=>ActiveRelics().Where(x=>x.uniqueRelic).SelectMany(x=>x.forgedPowers).Where(x=>x.power==UniquePower.GrantedAuras).Aggregate(0,(mask,x)=>mask|x.auraMask);
}
