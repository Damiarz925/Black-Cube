using System.Collections.Generic;
using UnityEngine;

// Shared, unit-explicit rules for the two reauthored class branches.
public static class RangerSubclassRules
{
    public const float DodgeCap=.75f; // Global cap; the intended first-pass build target is below this.
    public static float DodgeChance(float chance)=>Mathf.Clamp(chance,0,DodgeCap);
    public static int ProjectileCount(float rawAmount,int keystoneBonus,float roll)
    {
        float amount=Mathf.Max(0,rawAmount);
        return Mathf.Max(1,1+Mathf.FloorToInt(amount)+Mathf.Max(0,keystoneBonus)
            +(roll<amount-Mathf.Floor(amount)?1:0));
    }
}

public static class BarbarianRangerCombatRules
{
    public static float RageReductionMultiplier(float rage,float reductionPerPoint)
        =>1-Mathf.Clamp01(Mathf.Max(0,rage)*Mathf.Max(0,reductionPerPoint));
    public static float TitanFortification(int priorHits,float lessPerHit)
        =>Mathf.Max(.2f,1-Mathf.Max(0,priorHits)*Mathf.Max(0,lessPerHit));
    public static float ToxicSuppression(int poisonStacks,float lessPerTen)
        =>Mathf.Max(0,1-(Mathf.Max(0,poisonStacks)/10)*Mathf.Max(0,lessPerTen));
    public static float ProjectileGuard(int liveProjectiles,float lessPerProjectile)
        =>Mathf.Max(0,1-Mathf.Max(0,liveProjectiles)*Mathf.Max(0,lessPerProjectile));
    public static float ProjectileSpeedMore(float increasedProjectileSpeed)
        =>1+Mathf.Max(0,increasedProjectileSpeed)*.5f;
}

// Each Fire leech grant is paid out evenly over four real-time seconds.
// Independent instances overlap; none restores Life immediately on hit.
[RequireComponent(typeof(HealthComponent))]
public sealed class FireLeechState:MonoBehaviour
{
    struct Instance { public float remaining,time; }
    readonly List<Instance> instances=new();
    HealthComponent health;
    void Awake()=>health=GetComponent<HealthComponent>();
    public static FireLeechState For(GameObject player)
        =>player==null?null:player.GetComponent<FireLeechState>()??player.AddComponent<FireLeechState>();
    public void Add(float amount)
    {
        if(amount>0)instances.Add(new Instance{remaining=amount,time=4});
    }
    void Update()
    {
        if(GetComponent<SubclassCombatState>()?.Has(SubclassIds.BarbarianFire)!=true){instances.Clear();return;}
        float delta=Time.deltaTime;if(delta<=0||SkillTreeUI.PausesGameplay)return;
        for(int i=instances.Count-1;i>=0;i--)
        {
            var instance=instances[i];float elapsed=Mathf.Min(delta,instance.time);
            float payout=instance.remaining*elapsed/instance.time;
            health.RestoreLife(payout,HealingSource.SubclassDamage);
            instance.remaining-=payout;instance.time-=elapsed;
            if(instance.time<=.00001f)instances.RemoveAt(i);else instances[i]=instance;
        }
    }
    public void Clear()=>instances.Clear();
}
