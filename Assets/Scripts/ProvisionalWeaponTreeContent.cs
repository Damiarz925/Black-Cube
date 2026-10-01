using System;
using System.Collections.Generic;
using UnityEngine;

// PROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE.
// All effects use existing consumers and inherit the route's weapon restriction.
public static class ProvisionalWeaponTreeContent
{
    public const string Warning = "PROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE";
    public static readonly float[] StaffManaDefense={2,3,5,8,12,15,20};
    public static readonly float[] StaffEcho={1,1,2,3,3,4,6};
    public static readonly float[] SpineValues = {4,6,8,10,12,15,20};
    public static readonly float[] DepthWeights = {.6f,.8f,1,1.25f,1.5f,3,6};
    public sealed class Profile
    {
        public readonly StatTypes[] Offense,Defense,Utility;
        public Profile(StatTypes[] offense,StatTypes[] defense,StatTypes[] utility){Offense=offense;Defense=defense;Utility=utility;}
    }
    public static Profile For(string weapon)=>weapon switch
    {
        WeaponTypeIds.Sword=>new(new[]{StatTypes.PhysDmg,StatTypes.AttackSpeed,StatTypes.ChanceToHitTwice,StatTypes.CritChance},new[]{StatTypes.LifePercent,StatTypes.ArmourPercent,StatTypes.LifeOnHit,StatTypes.LifeRecoveryEffect},new[]{StatTypes.ManaOnHit,StatTypes.RevengeEffect,StatTypes.Strength,StatTypes.Dexterity}),
        WeaponTypeIds.TwoHandedAxe=>new(new[]{StatTypes.PhysDmg,StatTypes.BleedDmg,StatTypes.BleedChance,StatTypes.RageEffect},new[]{StatTypes.LifePercent,StatTypes.LifeRegeneration,StatTypes.LifeOnHit,StatTypes.PhysicalDamageReduction},new[]{StatTypes.RageGeneration,StatTypes.RageDecayReduction,StatTypes.Strength,StatTypes.RevengeEffect}),
        WeaponTypeIds.Bow=>new(new[]{StatTypes.ProjectileDmg,StatTypes.AttackSpeed,StatTypes.ProjectilePrecisionMultiplier,StatTypes.CritChance},new[]{StatTypes.LifePercent,StatTypes.ArmourPercent,StatTypes.ColdRes,StatTypes.LightRes},new[]{StatTypes.ProjectileSpeed,StatTypes.ProjectileAmount,StatTypes.ProjectilePrecisionChance,StatTypes.ManaOnHit}),
        WeaponTypeIds.Staff=>new(new[]{StatTypes.FireDmg,StatTypes.ColdDmg,StatTypes.LightDmg,StatTypes.VoidDmg},new[]{StatTypes.LifePercent,StatTypes.AllRes,StatTypes.ArmourPercent,StatTypes.LifeRecoveryEffect},new[]{StatTypes.CooldownReduction,StatTypes.ManaPercent,StatTypes.ManaRegeneration,StatTypes.ManaOnHit}),
        WeaponTypeIds.Sceptre=>new(new[]{StatTypes.MagicDmg,StatTypes.ColdDmg,StatTypes.FireDmg,StatTypes.VoidDmg},new[]{StatTypes.LifeRegeneration,StatTypes.LifePercent,StatTypes.AllRes,StatTypes.LifeRecoveryEffect},new[]{StatTypes.ManaRegeneration,StatTypes.AuraEffect,StatTypes.ManaOnHit,StatTypes.LifeOnHit}),
        WeaponTypeIds.Dagger=>new(new[]{StatTypes.CritChance,StatTypes.CritMult,StatTypes.AttackSpeed,StatTypes.VoidDmg},new[]{StatTypes.LifePercent,StatTypes.ArmourPercent,StatTypes.LifeOnHit,StatTypes.ColdRes},new[]{StatTypes.PoisonChance,StatTypes.PoisonSpeed,StatTypes.ManaOnHit,StatTypes.Intelligence}),
        _=>throw new ArgumentException("Unknown weapon tree",nameof(weapon))
    };
    public static float BaseValue(StatTypes stat)=>stat switch
    {
        StatTypes.PhysDmg or StatTypes.ProjectileDmg or StatTypes.FireDmg or StatTypes.ColdDmg or StatTypes.LightDmg or StatTypes.VoidDmg or StatTypes.MagicDmg or StatTypes.BleedDmg=>20,
        StatTypes.AttackSpeed=>6,StatTypes.CritChance=>10,StatTypes.CritMult=>20,
        StatTypes.ChanceToHitTwice or StatTypes.ProjectilePrecisionChance=>6,
        StatTypes.ProjectilePrecisionMultiplier=>15,StatTypes.LifePercent=>8,
        StatTypes.ArmourPercent=>12,StatTypes.LifeOnHit=>.8f,StatTypes.LifeRecoveryEffect=>8,
        StatTypes.LifeRegeneration=>1.5f,StatTypes.PhysicalDamageReduction=>.015f,
        StatTypes.ManaOnHit=>.45f,StatTypes.ManaPercent=>10,StatTypes.ManaRegeneration=>3,
        StatTypes.Strength or StatTypes.Dexterity or StatTypes.Intelligence=>5,
        StatTypes.RevengeEffect or StatTypes.RageEffect or StatTypes.RageGeneration or StatTypes.RageDecayReduction=>10,
        StatTypes.ColdRes or StatTypes.LightRes or StatTypes.AllRes=>6,
        StatTypes.ProjectileSpeed=>12,StatTypes.ProjectileAmount=>.15f,
        StatTypes.CooldownReduction or StatTypes.AuraEffect or StatTypes.PoisonChance or StatTypes.PoisonSpeed or StatTypes.BleedChance=>10,
        _=>throw new ArgumentException("No provisional design value for "+stat)
    };
    public static List<PassiveWeaponTierData> Create(string weapon)
    {
        var profile=For(weapon);var tiers=new List<PassiveWeaponTierData>();
        for(int t=0;t<7;t++)
        {
            var tier=new PassiveWeaponTierData();tier.SetTier(t+1);string prefix=$"tree.v3.{weapon}.t{t+1:00}.";
            tier.Spine.Configure(prefix+"spine","Increased Weapon Damage",Warning,PassiveBranch.Physical,PassiveNodeSize.Medium,PassiveNodeKind.WeaponSpine,PassiveKeystone.None,new[]{new PassiveEffect(StatTypes.GenericDmg,SpineValues[t])});
            var left=new[]{tier.Left.A,tier.Left.B,tier.Left.C};var right=new[]{tier.Right.A,tier.Right.B,tier.Right.C};
            for(int option=0;option<3;option++)
            {
                int i=(t+option)%4;float weight=DepthWeights[t];
                var defenses=new List<PassiveEffect>{new(profile.Defense[i],BaseValue(profile.Defense[i])*weight)};
                var offense=new List<PassiveEffect>{new(profile.Offense[i],BaseValue(profile.Offense[i])*weight)};
                if(option==2||t==6){var utility=profile.Utility[(t+option)%4];defenses.Add(new(utility,BaseValue(utility)*weight*.75f));}
                if(t==6){offense.Add(new(StatTypes.GenericMult,option==0?30:option==1?20:15));var other=profile.Offense[(i+1)%4];offense.Add(new(other,BaseValue(other)*weight*.5f));}
                else if(option==2){var utility=profile.Utility[(t+2)%4];offense.Add(new(utility,BaseValue(utility)*weight*.5f));}
                if(weapon==WeaponTypeIds.Staff&&option==0)defenses=new(){new(StatTypes.DamageTakenFromManaBeforeLife,StaffManaDefense[t])};
                if(weapon==WeaponTypeIds.Staff&&option==1)defenses=new(){new(StatTypes.SpellEchoChance,StaffEcho[t])};
                string token=((char)('a'+option)).ToString();
                left[option].Configure(prefix+"left."+token,t==6?"Bulwark "+(option+1):"Sustain "+(option+1),Warning+". Weapon-only sustain/utility package.",PassiveBranch.Defense,t==6?PassiveNodeSize.Large:PassiveNodeSize.Small,PassiveNodeKind.Choice,PassiveKeystone.None,defenses);
                bool finisher=weapon==WeaponTypeIds.TwoHandedAxe&&t==6&&option==2;
                right[option].Configure(prefix+"right."+token,finisher?"Rage Finisher":t==6?"Mastery "+(option+1):"Offense "+(option+1),Warning+". Weapon-only offensive package; final-tier MORE damage is multiplicative."+(finisher?" At maximum Rage, arm one 2x empowered attack consuming Rage.":""),PassiveBranch.Physical,t==6?PassiveNodeSize.Large:PassiveNodeSize.Small,PassiveNodeKind.Choice,finisher?PassiveKeystone.RageFinisher:PassiveKeystone.None,offense);
            }
            if(weapon==WeaponTypeIds.Staff)
            {
                left[0].Configure(prefix+"left.a","Mana Before Life",ManaBeforeLifeRules.Description,PassiveBranch.Defense,t==6?PassiveNodeSize.Large:PassiveNodeSize.Small,PassiveNodeKind.Choice,PassiveKeystone.None,new[]{new PassiveEffect(StatTypes.DamageTakenFromManaBeforeLife,StaffManaDefense[t])});
                left[1].Configure(prefix+"left.b","Spell Echo Chance",Warning+". Echo casts cost escalating Mana.",PassiveBranch.Magic,t==6?PassiveNodeSize.Large:PassiveNodeSize.Small,PassiveNodeKind.Choice,PassiveKeystone.None,new[]{new PassiveEffect(StatTypes.SpellEchoChance,StaffEcho[t])});
            }
            tiers.Add(tier);
        }
        return tiers;
    }
    public static Vector2 SpinePosition(string weapon,int tier)
    {
        float y=180*(tier-1);float x=weapon==WeaponTypeIds.Bow?Mathf.Sin((tier-1)/6f*Mathf.PI)*180:0;
        return new Vector2(x,y);
    }
    public static float SideOffset(string weapon,int tier)=>weapon switch
    {WeaponTypeIds.TwoHandedAxe=>tier>=5?300:160,WeaponTypeIds.Sword=>tier==3?300:140,WeaponTypeIds.Dagger=>tier==2?260:110,WeaponTypeIds.Staff=>tier>=6?260:130,WeaponTypeIds.Sceptre=>tier>=6?300:160,_=>200};
}
