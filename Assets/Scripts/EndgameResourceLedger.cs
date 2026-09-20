// Character-scoped ownership for challenge keys, essences and deep-endgame resources.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class EndgameResourceIds
{
    public const string EmpowermentCatalyst="currency.empowerment-catalyst";
    public const string ImplicitReforger="currency.implicit-reforger";
    public static string ChallengeKey(string biomeId)=>$"resource.challenge-key.{biomeId}";
    public static string ChallengeEssence(string biomeId)=>$"resource.challenge-essence.{biomeId}";
    public static string ChallengeFirstClear(string biomeId)=>$"first-clear.challenge.{biomeId}.apex";
    public static bool IsPersistentResource(string id)
    {
        if(string.IsNullOrWhiteSpace(id))return false;
        if(id==ImplicitReforger)return true;
        foreach(string biome in ProductionWorldContent.BiomeIds)
            if(id==ChallengeKey(biome)||id==ChallengeEssence(biome))return true;
        return false;
    }
    public static bool IsFirstClear(string id)
    {
        if(string.IsNullOrWhiteSpace(id))return false;
        foreach(string biome in ProductionWorldContent.BiomeIds)if(id==ChallengeFirstClear(biome))return true;
        return false;
    }
}

[Serializable] public sealed class EndgameResourceStack { public string resourceId; public int amount; }

public sealed class EndgameResourceLedger:MonoBehaviour
{
    static EndgameResourceLedger instance;
    public static EndgameResourceLedger Instance=>instance!=null?instance:instance=FindAnyObjectByType<EndgameResourceLedger>();
    readonly Dictionary<string,int> quantities=new(StringComparer.Ordinal);
    readonly HashSet<string> firstClears=new(StringComparer.Ordinal);
    public event Action Changed;

    void Awake(){if(instance!=null&&instance!=this){Destroy(this);return;}instance=this;}
    void OnDestroy(){if(instance==this)instance=null;}
    public int Count(string id)=>!string.IsNullOrWhiteSpace(id)&&quantities.TryGetValue(id,out int n)?n:0;
    public bool HasFirstClear(string id)=>!string.IsNullOrWhiteSpace(id)&&firstClears.Contains(id);
    public bool Add(string id,int amount=1)
    {
        if(!EndgameResourceIds.IsPersistentResource(id)||amount<=0||Count(id)>int.MaxValue-amount)return false;
        quantities[id]=Count(id)+amount;Publish();return true;
    }
    public bool TrySpend(string id,int amount=1)
    {
        if(amount<=0||Count(id)<amount)return false;int left=Count(id)-amount;
        if(left==0)quantities.Remove(id);else quantities[id]=left;Publish();return true;
    }
    public bool MarkFirstClear(string id)
    {if(!EndgameResourceIds.IsFirstClear(id)||!firstClears.Add(id))return false;Publish();return true;}
    public List<EndgameResourceStack> CaptureResources()
    {
        var result=new List<EndgameResourceStack>();foreach(var pair in quantities)if(pair.Value>0)result.Add(new EndgameResourceStack{resourceId=pair.Key,amount=pair.Value});
        result.Sort((a,b)=>string.CompareOrdinal(a.resourceId,b.resourceId));return result;
    }
    public List<string> CaptureFirstClears(){var result=new List<string>(firstClears);result.Sort(StringComparer.Ordinal);return result;}
    public bool Restore(IEnumerable<EndgameResourceStack> resources,IEnumerable<string> clears)
    {
        quantities.Clear();firstClears.Clear();
        if(resources!=null)foreach(var x in resources)
            if(x==null||!EndgameResourceIds.IsPersistentResource(x.resourceId)||x.amount<=0||Count(x.resourceId)>int.MaxValue-x.amount)return false;
            else quantities[x.resourceId]=Count(x.resourceId)+x.amount;
        if(clears!=null)foreach(string id in clears)if(!EndgameResourceIds.IsFirstClear(id)||!firstClears.Add(id))return false;
        Changed?.Invoke();return true;
    }
    public void ResetForNewGame(){quantities.Clear();firstClears.Clear();Changed?.Invoke();}
    void Publish(){Changed?.Invoke();GamePersistence.MarkDirty();}
}
