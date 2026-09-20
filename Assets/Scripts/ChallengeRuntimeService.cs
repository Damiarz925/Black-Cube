// Functional challenge launch/reward authority. Challenge state is transient; resources are character-persistent.
using System;
using UnityEngine;

public sealed class ChallengeRuntimeService:MonoBehaviour
{
    public const float BonusEssenceChance=.25f,BonusCatalystChance=.20f;
    public static ChallengeRuntimeService Instance{get;private set;}
    public ChallengeEncounterDefinition ActiveChallenge{get;private set;}
    public bool IsActive=>ActiveChallenge!=null;
    public event Action Changed;
    void Awake(){if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;}
    void OnDestroy(){if(Instance==this)Instance=null;}

    public bool IsUnlocked(ChallengeEncounterDefinition challenge)
    {
        if(challenge==null||GameManager.Instance==null||GameManager.Instance.CurrentCombatLevel<challenge.minimumCombatLevel)return false;
        var identity=GameManager.Instance.GetComponent<PlayerIdentityState>();
        return identity!=null&&identity.SubclassChoiceUnlocked;
    }
    public bool TryLaunch(ChallengeEncounterDefinition challenge)
    {
        var ledger=EndgameResourceLedger.Instance;
        if(IsActive||!IsUnlocked(challenge)||ledger==null||ledger.Count(challenge.entryResourceId)<challenge.entryResourceAmount||BattleManager.Instance==null)return false;
        if(!BattleManager.Instance.TrySpawnChallenge(challenge))return false;
        if(!ledger.TrySpend(challenge.entryResourceId,challenge.entryResourceAmount)){BattleManager.Instance.RestoreWorldEncounter();return false;}
        ActiveChallenge=challenge;Changed?.Invoke();GamePersistence.Save();return true;
    }
    public bool Complete(BossDefinition defeated,ILootRandomSource random=null)
    {
        if(ActiveChallenge==null||defeated==null||defeated.stableId!=ActiveChallenge.bossId)return false;
        random??=LootRandomSourceFactory.CreateProduction();var ledger=EndgameResourceLedger.Instance;if(ledger==null)return false;
        ledger.Add(ActiveChallenge.rewardResourceId,ActiveChallenge.rewardResourceAmount);
        if(random.Value()<BonusEssenceChance)ledger.Add(ActiveChallenge.rewardResourceId,1);
        CurrencyInventory.Instance?.Add(CraftingCurrencyType.EmpowermentCatalyst,1);
        if(random.Value()<BonusCatalystChance)CurrencyInventory.Instance?.Add(CraftingCurrencyType.EmpowermentCatalyst,1);
        AwardReforger(ActiveChallenge,random,ledger);
        string completed=ActiveChallenge.stableContentId;ActiveChallenge=null;Changed?.Invoke();BattleManager.Instance?.RestoreWorldEncounter();GamePersistence.Save();
        Debug.Log($"Challenge complete: {completed}.");return true;
    }
    public bool Abandon(bool restoreWorld=true)
    {if(ActiveChallenge==null)return false;ActiveChallenge=null;Changed?.Invoke();if(restoreWorld)BattleManager.Instance?.RestoreWorldEncounter();return true;}
    static void AwardReforger(ChallengeEncounterDefinition challenge,ILootRandomSource random,EndgameResourceLedger ledger)
    {
        int biome=Array.IndexOf(ProductionWorldContent.BiomeIds,challenge.entryResourceId.Replace("resource.challenge-key.",string.Empty));
        if(biome==5)
        {
            string clear=EndgameResourceIds.ChallengeFirstClear(ProductionWorldContent.BiomeIds[5]);
            if(!ledger.HasFirstClear(clear)){ledger.Add(EndgameResourceIds.ImplicitReforger);ledger.MarkFirstClear(clear);return;}
            if(random.Value()<.25f)ledger.Add(EndgameResourceIds.ImplicitReforger);
        }
        else if(GameManager.Instance!=null&&GameManager.Instance.CurrentCombatLevel>=210&&random.Value()<.10f)
            ledger.Add(EndgameResourceIds.ImplicitReforger);
    }
}
