// Transactional player-facing endgame crafting and acquisition policies.
using System.Collections.Generic;
using UnityEngine;

public static class EndgameDropProfile
{
    public static float CatalystChance(int combatLevel)=>combatLevel<120?0f:Mathf.Lerp(.05f,.30f,Mathf.Clamp01((combatLevel-120)/240f));
    public static float ChallengeKeyChance(EnemyAI.EnemyRarity rarity,bool stageBoss,float lootPower=1f)
    {
        float baseChance=stageBoss ? .22f : rarity switch{EnemyAI.EnemyRarity.Magic=>.025f,EnemyAI.EnemyRarity.Rare=>.055f,EnemyAI.EnemyRarity.Legendary=>.11f,_=>.0125f};
        return Mathf.Clamp01(baseChance*Mathf.Clamp(lootPower,.75f,2.5f));
    }
    public static string TryAwardChallengeKey(int combatLevel,EnemyAI.EnemyRarity rarity,bool stageBoss,float lootPower,WorldContentDatabase database,ILootRandomSource random)
    {
        if(database?.challengeEncounters==null||EndgameResourceLedger.Instance==null||random==null||random.Value()>=ChallengeKeyChance(rarity,stageBoss,lootPower))return null;
        var eligible=database.challengeEncounters.FindAll(x=>x!=null&&combatLevel>=x.minimumCombatLevel);
        if(eligible.Count==0)return null;var challenge=eligible[random.Range(0,eligible.Count)];
        return EndgameResourceLedger.Instance.Add(challenge.entryResourceId)?challenge.entryResourceId:null;
    }
}

public static class EndgameCraftingService
{
    public static bool TryEmpower(Gear gear,RolledMod target,int combatLevel,ILootRandomSource random=null)
    {
        if(CurrencyInventory.Instance==null||CurrencyInventory.Instance.Count(CraftingCurrencyType.EmpowermentCatalyst)<1)return false;
        random??=LootRandomSourceFactory.CreateProduction();
        if(!EmpowermentCrafting.TryApply(gear,target,combatLevel,random.Value(),random.Value()))return false;
        if(!CurrencyInventory.Instance.TrySpend(CraftingCurrencyType.EmpowermentCatalyst))return false;
        Publish(gear);return true;
    }
    public static bool TryBossInfuse(Gear gear,RolledMod target,ChallengeEncounterDefinition challenge,int combatLevel,ILootRandomSource random=null)
    {
        if(challenge==null||EndgameResourceLedger.Instance==null||EndgameResourceLedger.Instance.Count(challenge.rewardResourceId)<1)return false;
        var pool=WorldContentCatalog.Reference?.ChallengeSpecialPool(challenge.specialAffixPoolId);random??=LootRandomSourceFactory.CreateProduction();
        if(!BossSpecialCrafting.TryReplace(gear,target,pool,combatLevel,random))return false;
        if(!EndgameResourceLedger.Instance.TrySpend(challenge.rewardResourceId))return false;
        Publish(gear);return true;
    }
    public static bool TryReforgeImplicit(Gear gear,ILootRandomSource random=null)
    {
        if(gear==null||gear.ItemLevel!=100||gear.ItemRarity is not (LootManager.GearRarity.Rare or LootManager.GearRarity.Legendary)
            ||gear.ImplicitMod==null||EndgameResourceLedger.Instance==null||EndgameResourceLedger.Instance.Count(EndgameResourceIds.ImplicitReforger)<1||ModManager.Instance==null)return false;
        if(!TryReforgeImplicitCore(gear,ModManager.Instance,random))return false;
        if(!EndgameResourceLedger.Instance.TrySpend(EndgameResourceIds.ImplicitReforger))return false;
        Publish(gear);return true;
    }
    // Shared production mutation used by the player-facing transaction and isolated Workbench trials.
    public static bool TryReforgeImplicitCore(Gear gear,ModManager mods,ILootRandomSource random=null)
    {
        if(gear==null||gear.ItemLevel!=100||gear.ItemRarity is not (LootManager.GearRarity.Rare or LootManager.GearRarity.Legendary)
            ||gear.ImplicitMod==null||mods==null)return false;
        random??=LootRandomSourceFactory.CreateProduction();StatTypes old=gear.ImplicitMod.statType;RolledMod replacement=null;
        for(int attempt=0;attempt<96;attempt++)
        {
            List<RolledMod> rolled=mods.RollEquipmentModsForItem(gear.ItemType,gear.ItemRarity,100,gear.BaseElement,gear.WeaponTypeId,random);
            if(rolled==null)continue;RolledMod candidate=rolled.Find(x=>x!=null&&x.lockedOriginal&&!Gear.IsWeaponBaseStat(x.statType));
            if(candidate!=null&&candidate.statType!=old){replacement=Clone(candidate);break;}
        }
        if(replacement==null)return false;int index=gear.rolledMods.IndexOf(gear.ImplicitMod);if(index<0)return false;
        replacement.lockedOriginal=true;gear.rolledMods[index]=replacement;gear.RebuildMods();
        return true;
    }
    static RolledMod Clone(RolledMod m)=>new(m.statType,m.tierIndex,m.value,m.HighValue,true){hasSecondaryValue=m.hasSecondaryValue,secondaryValue=m.secondaryValue};
    static void Publish(Gear gear){Inventory.Instance?.NotifyItemChanged(gear);EquipmentManager.Instance?.NotifyItemChanged(gear);GamePersistence.Save();}
}
