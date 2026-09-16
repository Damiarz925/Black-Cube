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
    public readonly List<CraftingCurrencyType> currencies=new();
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
    readonly CraftingCurrencyType type;bool claimed;
    public CurrencyRewardClaim(CraftingCurrencyType value)=>type=value;
    public bool TryClaim()
    {
        if(claimed)return false;claimed=true;return true;
    }
    public bool TryAward()
    {
        if(CurrencyInventory.Instance==null||!TryClaim())return false;
        CurrencyInventory.Instance.Add(type);
        GamePersistence.Save();
        return true;
    }
}

public sealed class CurrencyWorldPickup:MonoBehaviour
{
    CurrencyRewardClaim claim;Transform target;Vector3 start;float age;float displayScale=.58f;
    const float ReadDelay=.35f,FlyDuration=.55f;
    public static CurrencyWorldPickup Spawn(CraftingCurrencyType type,Vector3 position,Transform player)
    {
        var go=new GameObject("Dropped "+CurrencyPresentation.Name(type),typeof(SpriteRenderer),typeof(CurrencyWorldPickup));
        go.transform.position=position+new Vector3(UnityEngine.Random.Range(-.28f,.28f),.25f+UnityEngine.Random.Range(0f,.18f),0);
        var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=InventoryArtCatalog.Currency(type);renderer.sortingOrder=80;
        var pickup=go.GetComponent<CurrencyWorldPickup>();pickup.claim=new CurrencyRewardClaim(type);pickup.target=player;pickup.start=go.transform.position;
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
