using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// IDs and individual rolls are save identities, not ordinary craftable affixes.
public enum UniquePower
{
    AnyWeaponRage, SelfShockDuration, SelfShockEffect, ShockedLightningMore,
    ShockedShockEffect, ShockedCooldownReduction, LessHit, AilmentBasis,
    LavaFireTaken, Rupture, GrantedAuras, PreservePoison, MultistrikeDamage,
    RevengeScaling, HydraAilments, EchoSelfDamage, VoidToRegeneration, RegenerationToVoid,
    LastBreath
}
[Serializable] public sealed class UniqueRoll
{
    public UniquePower power; public float value; public int auraMask;
    public UniqueRoll Copy()=>new(){power=power,value=value,auraMask=auraMask};
}
[Serializable] public sealed class UniqueItemData
{
    public string definitionId; public int rollVersion; public List<UniqueRoll> powers=new();
    public UniqueItemData Copy()=>new(){definitionId=definitionId,rollVersion=rollVersion,powers=powers.Select(x=>x.Copy()).ToList()};
}
public sealed class UniqueStatRange
{
    public StatTypes stat;public float minimum,maximum;
    public UniqueStatRange(StatTypes s,float a,float b){stat=s;minimum=a;maximum=b;}
}
public sealed class UniquePowerRange
{
    public UniquePower power;public float minimum,maximum;
    public UniquePowerRange(UniquePower p,float a,float b){power=p;minimum=a;maximum=b;}
}
public sealed class UniqueDefinition
{
    public string id,name,weapon; public LootManager.GearType slot; public Element element;
    public int minimumLevel=1;public float damageScale=1,speed=1,crit=.05f;
    public UniqueStatRange[] stats=Array.Empty<UniqueStatRange>();
    public UniquePowerRange[] powers=Array.Empty<UniquePowerRange>();
}
public static class UniqueCatalog
{
    public static readonly UniqueDefinition[] All=
    {
        new(){id="unique.heart-berserker",name="Heart of the Berserker",slot=LootManager.GearType.BodyArmours,stats=new[]{S(StatTypes.RageGeneration,40,120),S(StatTypes.RageDecayReduction,15,40),S(StatTypes.Life,40,160),S(StatTypes.ArmourPercent,20,70)},powers=new[]{P(UniquePower.AnyWeaponRage,1,1)}},
        new(){id="unique.static-glass",name="Crown of Static Glass",slot=LootManager.GearType.Helmets,powers=new[]{P(UniquePower.SelfShockDuration,1.5f,2.5f),P(UniquePower.SelfShockEffect,1.5f,1.5f),P(UniquePower.ShockedLightningMore,.25f,.75f),P(UniquePower.ShockedShockEffect,.20f,.80f),P(UniquePower.ShockedCooldownReduction,.10f,.35f)}},
        new(){id="unique.hands-many",name="Hands of the Many",slot=LootManager.GearType.Gloves,stats=new[]{S(StatTypes.ChanceToHitTwice,60,120),S(StatTypes.PoisonChance,30,90),S(StatTypes.BleedChance,30,90),S(StatTypes.IgniteChance,30,90)},powers=new[]{P(UniquePower.LessHit,.30f,.45f),P(UniquePower.AilmentBasis,2,3)}},
        new(){id="unique.ashen-footsteps",name="Ashen Footsteps",slot=LootManager.GearType.Boots,powers=new[]{P(UniquePower.LavaFireTaken,.15f,.40f)}},
        new(){id="unique.bottomless-wound",name="The Bottomless Wound",slot=LootManager.GearType.Belts,stats=new[]{S(StatTypes.BleedChance,60,150)},powers=new[]{P(UniquePower.Rupture,1,1)}},
        new(){id="unique.prism-voices",name="Prism of Five Voices",slot=LootManager.GearType.Amulets,stats=new[]{S(StatTypes.AuraEffect,-50,50)},powers=new[]{P(UniquePower.GrantedAuras,2,3)}},
        new(){id="unique.venomous-memory",name="Venomous Memory",slot=LootManager.GearType.Rings,powers=new[]{P(UniquePower.PreservePoison,5,20)}},
        new(){id="unique.final-argument",name="Final Argument",slot=LootManager.GearType.Weapons,weapon=WeaponTypeIds.Sword,element=Element.Phys,damageScale=1.6f,speed=1.2f,crit=.08f,stats=new[]{S(StatTypes.PhysDmg,70,150),S(StatTypes.AttackSpeed,10,30),S(StatTypes.CritChance,20,60),S(StatTypes.ChanceToHitTwice,50,100)},powers=new[]{P(UniquePower.MultistrikeDamage,-.05f,.10f)}},
        new(){id="unique.heartsplitter",name="Heartsplitter",slot=LootManager.GearType.Weapons,weapon=WeaponTypeIds.TwoHandedAxe,element=Element.Phys,damageScale=4,speed=.55f,stats=new[]{S(StatTypes.PhysDmg,80,180)},powers=new[]{P(UniquePower.RevengeScaling,.4f,1.2f)}},
        new(){id="unique.hydra-string",name="Hydra String",slot=LootManager.GearType.Weapons,weapon=WeaponTypeIds.Bow,element=Element.Phys,damageScale=1.8f,speed=1.15f,stats=new[]{S(StatTypes.ProjectileAmount,3,3)},powers=new[]{P(UniquePower.LessHit,.30f,.40f),P(UniquePower.HydraAilments,1.30f,1.30f)}},
        new(){id="unique.stormcaller",name="Stormcaller",slot=LootManager.GearType.Weapons,weapon=WeaponTypeIds.Staff,element=Element.Light,damageScale=1.8f,speed=1,stats=new[]{S(StatTypes.SpellEchoChance,20,40)},powers=new[]{P(UniquePower.EchoSelfDamage,.15f,.40f)}},
        new(){id="unique.saint-contradiction",name="Saint's Contradiction",slot=LootManager.GearType.Weapons,weapon=WeaponTypeIds.Sceptre,element=Element.Void,damageScale=1.8f,speed=1,stats=new[]{S(StatTypes.LifeRegeneration,1,4)},powers=new[]{P(UniquePower.VoidToRegeneration,.10f,.50f),P(UniquePower.RegenerationToVoid,.10f,.50f)}},
        new(){id="unique.last-breath",name="Last Breath",slot=LootManager.GearType.Weapons,weapon=WeaponTypeIds.Dagger,element=Element.Phys,damageScale=1.8f,speed=1.6f,crit=.08f,powers=new[]{P(UniquePower.LastBreath,1,1)}}
    };
    static UniqueStatRange S(StatTypes s,float a,float b)=>new(s,a,b);
    static UniquePowerRange P(UniquePower p,float a,float b)=>new(p,a,b);
    public static UniqueDefinition Get(string id)=>All.FirstOrDefault(x=>x.id==id);
    public static Gear Create(string id,int level,Func<float> random=null,float? fixedRoll=null)
    {
        var definition=Get(id);if(definition==null||level<definition.minimumLevel)return null;
        random??=()=>UnityEngine.Random.value;
        float Roll()=>Mathf.Clamp01(fixedRoll??random());
        var gear=new GameObject(definition.name).AddComponent<Gear>();
        gear.Initialize(definition.slot,LootManager.GearRarity.Unique,level,definition.element,definition.weapon);
        var data=new UniqueItemData{definitionId=id,rollVersion=1};
        foreach(var range in definition.powers)
        {
            var band=UniqueTierRules.PowerRange(range,level);
            var roll=new UniqueRoll{power=range.power,value=Mathf.Lerp(band.x,band.y,Roll())};
            if(range.power==UniquePower.PreservePoison)roll.value=Mathf.Round(roll.value);
            if(range.power==UniquePower.GrantedAuras)
            {
                roll.value=Roll()<.5f?2:3;
                var candidates=new List<int>{0,1,2,3,4};
                for(int i=0;i<(int)roll.value;i++){int index=Mathf.Min(candidates.Count-1,Mathf.FloorToInt(Roll()*candidates.Count));roll.auraMask|=1<<candidates[index];candidates.RemoveAt(index);}
            }
            data.powers.Add(roll);
        }
        var mods=definition.stats.Select(x=>{var band=UniqueTierRules.StatRange(x,definition,level);return new RolledMod(x.stat,UniqueTierRules.Tier(level),Mathf.Lerp(band.x,band.y,Roll()));}).ToList();
        gear.ApplyMods(mods);gear.RestoreUnique(data);
        if(definition.slot==LootManager.GearType.Weapons)
        {
            UniqueTierRules.WeaponBase(gear,definition,level);
        }
        return gear;
    }
    public static bool Validate(UniqueItemData data,LootManager.GearType slot,string weapon,Element element,int level,IReadOnlyList<RolledMod> mods)
    {
        var d=Get(data?.definitionId);if(d==null||d.slot!=slot||d.minimumLevel>level||d.element!=element||(d.weapon??string.Empty)!=(weapon??string.Empty)||data.powers==null||data.powers.Count!=d.powers.Length||mods==null||mods.Count!=d.stats.Length)return false;
        if(data.rollVersion<0||data.rollVersion>1)return false;
        for(int i=0;i<mods.Count;i++){var band=data.rollVersion==0?new Vector2(d.stats[i].minimum,d.stats[i].maximum):UniqueTierRules.StatRange(d.stats[i],d,level);if(mods[i]==null||mods[i].statType!=d.stats[i].stat||mods[i].value<band.x-.001f||mods[i].value>band.y+.001f||!float.IsFinite(mods[i].value)||mods[i].isEmpowered||mods[i].isBossSpecial||data.rollVersion>0&&mods[i].tierIndex!=UniqueTierRules.Tier(level))return false;}
        for(int i=0;i<data.powers.Count;i++)
        {var p=data.powers[i];var r=d.powers[i];var band=data.rollVersion==0?new Vector2(r.minimum,r.maximum):UniqueTierRules.PowerRange(r,level);if(p==null||p.power!=r.power||!float.IsFinite(p.value)||p.value<band.x-.001f||p.value>band.y+.001f)return false;if(p.power==UniquePower.GrantedAuras){if(p.auraMask<0||p.auraMask>31||Enumerable.Range(0,5).Count(x=>(p.auraMask&(1<<x))!=0)!=(int)p.value)return false;}}
        return true;
    }
    public static float Power(StatsComponent actor,UniquePower power)
    {
        if(actor?.GetComponent<PlayerController>()==null)return 0;
        var isolated=actor.GetComponent<UniqueLoadoutState>();if(isolated!=null)return isolated.Power(power);
        if(EquipmentManager.Instance!=null&&EquipmentManager.Instance.PlayerStats!=actor)return 0;
        float value=RelicInventory.Instance?.ForgedPower(power)??0;var equipment=EquipmentManager.Instance;if(equipment==null)return value;
        foreach(var gear in equipment.EquippedItems.Values)if(gear!=null&&gear.UniqueData!=null)foreach(var roll in gear.UniqueData.powers)if(roll.power==power)value+=roll.value;
        return value;
    }
    public static string Describe(UniqueRoll roll)
    {
        string percent=(roll.value*100).ToString("0.#")+"%";
        return roll.power switch
        {
            UniquePower.AnyWeaponRage=>"Enables Rage with any weapon",
            UniquePower.SelfShockDuration=>$"Shock on you lasts {roll.value:0.#}× as long",
            UniquePower.SelfShockEffect=>$"Shock on you has {roll.value:0.#}× effect",
            UniquePower.ShockedLightningMore=>$"{percent} more Lightning damage while Shocked",
            UniquePower.ShockedShockEffect=>$"{percent} increased Shock effect while Shocked",
            UniquePower.ShockedCooldownReduction=>$"{percent} cooldown reduction while Shocked",
            UniquePower.LessHit=>$"{percent} less hit damage",
            UniquePower.AilmentBasis=>$"Ailments use {roll.value:0.#}× pre-penalty hit damage",
            UniquePower.LavaFireTaken=>$"Expired Ignite creates a 2-second pool: {percent} increased Fire damage taken",
            UniquePower.Rupture=>"Half Bleed duration; direct hits consume remaining Bleed damage",
            UniquePower.GrantedAuras=>"Grants "+string.Join(", ",Enumerable.Range(0,5).Where(i=>(roll.auraMask&(1<<i))!=0).Select(i=>new[]{"Physical","Fire","Cold","Lightning","Void"}[i]))+" auras",
            UniquePower.PreservePoison=>$"Preserves up to {roll.value:0} strongest remaining Poison stacks for the next enemy",
            UniquePower.MultistrikeDamage=>$"{percent} additional-strike damage modifier",
            UniquePower.RevengeScaling=>$"{percent} increased Revenge scaling",
            UniquePower.HydraAilments=>$"{percent} pre-penalty ailment strength",
            UniquePower.EchoSelfDamage=>$"Echoes hit you for {percent} of spell damage as Lightning and apply Shock",
            UniquePower.VoidToRegeneration=>$"{percent} of Void damage becomes temporary Life regeneration",
            UniquePower.RegenerationToVoid=>$"{percent} of Life regeneration is duplicated as Void damage",
            UniquePower.LastBreath=>"Cull threshold: 0.2% per Poison + 1% per Bleed; requires separate Cull chance",
            _=>roll.power+": "+roll.value.ToString("0.###")
        };
    }
    public static int AuraMask(StatsComponent actor)
    {if(actor?.GetComponent<UniqueLoadoutState>() is UniqueLoadoutState isolated)return isolated.AuraMask;int mask=RelicInventory.Instance?.ForgedAuraMask()??0;if(actor?.GetComponent<PlayerController>()==null)return 0;if(EquipmentManager.Instance==null||EquipmentManager.Instance.PlayerStats!=actor)return 0;foreach(var gear in EquipmentManager.Instance.EquippedItems.Values)if(gear?.UniqueData!=null)foreach(var p in gear.UniqueData.powers)if(p.power==UniquePower.GrantedAuras)mask|=p.auraMask;return mask;}
    public static float HitMultiplier(StatsComponent actor)=>Mathf.Max(.01f,1-Power(actor,UniquePower.LessHit));
    public static float AilmentMultiplier(StatsComponent actor)
    {float more=Mathf.Max(1,Power(actor,UniquePower.AilmentBasis));float hydra=Power(actor,UniquePower.HydraAilments);return more*(hydra>0?hydra/HitMultiplier(actor):1);}
}
