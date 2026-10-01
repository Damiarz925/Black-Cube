using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Five logical incoming events, recorded BEFORE lethal damage dispatches death UI.
public sealed class IncomingDamageHistory : MonoBehaviour
{
    readonly Queue<string> events = new();
    public void Record(float damage,DamageContext context,StatsComponent attacker,StatsComponent defender,StatusEffects ailment=null)
    {
        if(damage<=0)return;
        string source=attacker!=null?attacker.gameObject.name.Replace("(Clone)","").Trim():"Effect / self-hit";
        if(ailment!=null)source=ailment.name;
        string tags=(context.IsCrit?" — CRIT":"")+(ailment!=null||(context.EventTags&CombatEventTags.Ailment)!=0?" — AILMENT":"")
            +(context.Hits.Exists(h=>h.Element==Element.True)?" — TRUE DAMAGE":"");
        var components=new StringBuilder();float total=0;
        foreach(var hit in context.Hits)total+=Mathf.Max(0,hit.Amount);
        foreach(var hit in context.Hits)
        {
            if(hit.Amount<=0)continue;
            if(components.Length>0)components.Append(" + ");
            components.Append($"{ItemTooltipUI.ElementName(hit.Element)} {damage*hit.Amount/Mathf.Max(.001f,total):0.#}");
            if(defender!=null)
            {
                if(hit.Element==Element.True) { /* True Damage has no resistance or armour note. */ }
                else if(hit.Element==Element.Phys)
                {
                    float armour=defender.GetStat(StatTypes.FlatArmour)*(1+defender.GetStat(StatTypes.ArmourPercent));
                    components.Append($" (Armour {armour:0}, explicit PDR {defender.GetStat(StatTypes.PhysicalDamageReduction):P0}; hit-size dependent)");
                }
                else
                {
                    StatTypes resistance=hit.Element switch{Element.Fire=>StatTypes.FireRes,Element.Cold=>StatTypes.ColdRes,Element.Light=>StatTypes.LightRes,_=>StatTypes.VoidRes};
                    float value=Mathf.Min(CombatCalculator.GetMaximumResistance(hit.Element,defender),defender.GetStat(resistance)+(hit.Element==Element.Void?0:defender.GetStat(StatTypes.AllRes)));
                    components.Append($" ({value:P0} resistance)");
                }
            }
        }
        events.Enqueue($"{source}: {damage:0.#}{tags} — {components}");while(events.Count>5)events.Dequeue();
    }
    public string Describe()=>string.Join("\n",events);
    public void Clear()=>events.Clear();
}
