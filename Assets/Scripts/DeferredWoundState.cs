using System.Collections.Generic;
using UnityEngine;

// Damage already mitigated and assigned to Life, paid in full over four seconds.
// This deliberately bypasses Mana-before-Life and the deferral hook on every tick.
public sealed class DeferredWoundState : MonoBehaviour
{
    struct Wound { public float remaining, seconds; }
    readonly List<Wound> wounds = new();
    public float RemainingDamage { get { float sum=0;foreach(var wound in wounds)sum+=wound.remaining;return sum; } }
    public void Add(float amount)
    {
        if(amount>0)wounds.Add(new Wound{remaining=amount,seconds=WarriorSubclassRules.DeferredSeconds});
    }
    void Update()
    {
        float delta=Time.deltaTime;
        if(delta<=0||SkillTreeUI.PausesGameplay||PlayerSkillMenuUI.IsOpen)return;
        var receiver=GetComponent<DamageReceiver>();
        for(int i=wounds.Count-1;i>=0;i--)
        {
            var wound=wounds[i];float step=Mathf.Min(delta,wound.seconds);
            float damage=step>=wound.seconds?wound.remaining:wound.remaining*step/wound.seconds;
            wound.remaining-=damage;wound.seconds-=step;
            if(wound.seconds<=0||wound.remaining<=.00001f)wounds.RemoveAt(i);else wounds[i]=wound;
            receiver?.TakeDeferredWoundDamage(damage);
            if(GetComponent<HealthComponent>()?.CurrentLife<=0)break;
        }
    }
    public void Clear()=>wounds.Clear();
}
