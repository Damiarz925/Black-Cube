using System.Linq;
using UnityEngine;

public static class UniqueTierRules
{
    public static int Band(int level)
    {var levels=UniqueTierProfileSO.Current?.minimumLevels??new[]{1,20,40,60,80};int i=0;while(i+1<levels.Length&&level>=levels[i+1])i++;return i;}
    public static int Tier(int level)=>5-Band(level);
    public static ModDatabase Database=>ModManager.Instance?.Database??UniqueTierProfileSO.Current?.database;
    static float Factor(int level)=>(UniqueTierProfileSO.Current?.magnitude??new[]{.22f,.38f,.58f,.78f,1})[Band(level)];
    public static AffixTier Best(StatTypes stat,LootManager.GearType slot,string weapon,int level)
        =>ModManager.ApplicableTiers(Database?.GetDefinition(stat),slot,weapon).Where(t=>t.minItemLevel<=level).OrderBy(t=>t.tierIndex).FirstOrDefault();
    public static Vector2 StatRange(UniqueStatRange range,UniqueDefinition definition,int level)
    {
        if(range.stat==StatTypes.ProjectileAmount)return new(range.minimum,range.maximum);
        float min=range.minimum*Factor(level),max=range.maximum*Factor(level);
        if(range.stat is StatTypes.ChanceToHitTwice or StatTypes.SpellEchoChance or StatTypes.BleedChance or StatTypes.RageGeneration or StatTypes.RageDecayReduction)
        {
            var ordinary=Best(range.stat,definition.slot,definition.weapon,level);
            if(ordinary!=null)
            {min=Mathf.Max(min,ordinary.maxValue*(UniqueTierProfileSO.Current?.signatureMinimumPremium??1.1f));max=Mathf.Max(max,ordinary.maxValue*(UniqueTierProfileSO.Current?.signatureMaximumPremium??2.2f));}
        }
        // Echo currently has no ordinary gear affix. Its narrower Staff passive
        // is 5 points, so even the entry-band signature remains build-defining.
        if(range.stat==StatTypes.SpellEchoChance){min=Mathf.Max(min,10);max=Mathf.Max(max,20);}
        return new(min,max);
    }
    public static Vector2 PowerRange(UniquePowerRange range,int level)
    {
        // Mechanic switches, penalties and defining multipliers survive unchanged.
        if(range.power is UniquePower.AnyWeaponRage or UniquePower.Rupture or UniquePower.GrantedAuras or UniquePower.LastBreath
            or UniquePower.LessHit or UniquePower.AilmentBasis or UniquePower.HydraAilments or UniquePower.SelfShockEffect
            or UniquePower.SelfShockDuration or UniquePower.EchoSelfDamage)return new(range.minimum,range.maximum);
        float min=range.minimum*Factor(level),max=range.maximum*Factor(level);
        if(range.power==UniquePower.ShockedShockEffect)
        {var ordinary=Best(StatTypes.ShockEffect,LootManager.GearType.Helmets,null,level);if(ordinary!=null){min=Mathf.Max(min,ordinary.maxValue/100*1.1f);max=Mathf.Max(max,ordinary.maxValue/100*2.2f);}}
        if(range.power==UniquePower.PreservePoison){min=Mathf.Max(1,Mathf.Round(min));max=Mathf.Max(min,Mathf.Round(max));}
        return new(min,max);
    }
    public static void WeaponBase(Gear gear,UniqueDefinition definition,int level)
    {
        var profile=WeaponTypeCatalog.Get(definition.weapon);
        var damage=Best(StatTypes.WeaponBaseDmg,definition.slot,definition.weapon,level);
        var speed=Best(StatTypes.WeaponBaseAttackSpeed,definition.slot,definition.weapon,level);
        var crit=Best(StatTypes.WeaponBaseCrit,definition.slot,definition.weapon,level);
        float average=damage!=null?(damage.minValue+damage.maxValue)*.5f:new[]{40f,78.5f,155,306,603}[Band(level)];
        // Same highest-unlocked natural base, 90% of its midpoint. Local authored
        // offense supplies the premium; narrow build mechanics are not free perfect gear.
        average*=((profile.BaseDamageMin+profile.BaseDamageMax)*.5f/40)*.9f;
        var flatStat=definition.element switch{Element.Fire=>StatTypes.FlatFire,Element.Cold=>StatTypes.FlatCold,Element.Light=>StatTypes.FlatLight,Element.Void=>StatTypes.FlatVoid,_=>StatTypes.FlatPhys};
        var damageStat=definition.element switch{Element.Fire=>StatTypes.FireDmg,Element.Cold=>StatTypes.ColdDmg,Element.Light=>StatTypes.LightDmg,Element.Void=>StatTypes.VoidDmg,_=>StatTypes.PhysDmg};
        var flat=Best(flatStat,definition.slot,definition.weapon,level);var increased=Best(damageStat,definition.slot,definition.weapon,level);
        float flatAverage=flat==null?0:(flat.minValue+flat.maxValue+(flat.pairedDamage?flat.minHighValue+flat.maxHighValue:flat.minValue+flat.maxValue))*.25f;
        average=(average+flatAverage*.85f)*(1+(increased==null?0:(increased.minValue+increased.maxValue)*.005f)*.85f);
        var local=definition.stats.FirstOrDefault(x=>x.stat==damageStat);
        if(local!=null){var range=StatRange(local,definition,level);average/=1+(range.x+range.y)*.005f;}
        if(definition.weapon==WeaponTypeIds.TwoHandedAxe)average*=1.35f;
        gear.BaseDamage=average;gear.BaseDamageMin=average*.85f;gear.BaseDamageMax=average*1.15f;
        float speedMid=speed!=null?(speed.minValue+speed.maxValue)*.5f:new[]{.6f,.75f,.9f,1.05f,1.2f}[Band(level)];
        gear.BaseAttackSpeed=profile.AttacksPerSecond*speedMid/.6f*(definition.weapon==WeaponTypeIds.TwoHandedAxe?.8f:1.05f);
        gear.BaseCritChance=Mathf.Max(definition.crit,profile.BaseCritChance*(crit!=null?(crit.minValue+crit.maxValue)*.5f/5:1.5f));
    }
}
