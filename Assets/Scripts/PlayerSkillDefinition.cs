// Developer map: Data-only active skill tuning. PlayerSkillController owns selection/mana;
// BattleManager interprets these values through the normal attack and ailment pipelines.
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerSkillId
{
    HeavyStrike,
    IceStrike,
    LightningStrike,
    Fireball,
    Envenom,
    Shiv,
    Immolate,
    SwordRapidFlurry,
    SwordArmourStrike,
    AxeRageStrike,
    AxeHemorrhage,
    BowVenomShot,
    BowDoubleVolley,
    StaffFireball,
    StaffShockBarrage,
    SceptreRestorativeStrike,
    SceptreFrostJudgment,
    DaggerBackstab,
    DaggerQuickStrike,
    SwordRendingStrike // Appended: the retired Rapid Flurry ID remains reserved for old saves.
}
public enum PlayerSkillCastMode{QueuedAttackReplacement,AutoCooldown,ImmediateCooldown,AutoQueuedReplacement}
public enum WeaponSkillEffect
{
    None,RapidFlurry,ArmourStrike,RageStrike,EmpoweredBleed,VirtualPoison,
    DoubleProjectiles,ShockBarrage,HealFromDamage,FrostJudgment,Backstab,RendingStrike
}

[Serializable]
public sealed class PlayerSkillDefinition
{
    public PlayerSkillId id;
    public string stableId;
    public string weaponTypeId;
    public PlayerSkillCastMode castMode=PlayerSkillCastMode.QueuedAttackReplacement;
    [Min(.01f)] public float baseCooldown=3f;
    [System.Obsolete("Use scalesWithCooldownReduction")]
    public bool scalesWithCastSpeed;
    public bool scalesWithCooldownReduction;
    public bool queuedCooldownEnabled;
    public bool supportsPrecision;
    public string displayName;
    [TextArea] public string description;
    [Min(0f)] public float manaCost;
    [Min(0f)] public float hitDamageMultiplier = 1f;
    [Min(1)] public int baseHitCount = 1;
    public bool additionalHitsFromShockChance;
    public bool projectile;
    [Min(.01f), Tooltip("Authored projectile speed relative to the canonical one-second travel profile.")]
    public float baseProjectileSpeed = 1f;
    [Tooltip("Explicit damage tag. Elemental damage is not automatically Magic.")]
    public bool magic;
    public Element conversionElement = Element.Phys;
    [Range(0f, 1f)] public float nonMatchingConversion;
    public StatusEffects.AilmentKind specializedAilment = StatusEffects.AilmentKind.None;
    [Min(0f)] public float ailmentBasisMultiplier = 1f;
    [Tooltip("When non-negative, replaces the status asset's source-damage coefficient for this skill.")]
    public float absoluteAilmentCoefficient = -1f;
    [Min(0)] public int guaranteedAdditionalChill;
    [Min(0)] public int guaranteedAilmentApplications;
    public bool suppressDirectDamage;
    public WeaponSkillEffect effect;
    [Min(0f)] public float secondaryMultiplier;
    [Min(0)] public int maximumCount;
    public string[] tags=System.Array.Empty<string>();
    public DamageScope DamageScopes => (projectile ? DamageScope.Projectile : DamageScope.None)
                                       | (magic ? DamageScope.Magic : DamageScope.None);

    public static List<PlayerSkillDefinition> CreateDefaults()
    {
        return new List<PlayerSkillDefinition>
        {
            New(PlayerSkillId.HeavyStrike, "Heavy Strike", "A crushing physical hit for 130% damage.", 30, 1.3f,
                Element.Phys,1f),
            New(PlayerSkillId.IceStrike, "Ice Strike", "120% damage, 50% converted to Cold, plus one additional Chill.", 20, 1.2f,
                Element.Cold, .5f, guaranteedChill: 1),
            New(PlayerSkillId.LightningStrike, "Lightning Strike", "Each hit deals 55% damage. Shock Chance grants additional independent hits.", 20, .55f,
                Element.Light, .5f, shockHits: true),
            New(PlayerSkillId.Fireball, "Fireball", "Launch a projectile for 200% damage, with 50% converted to Fire.", 100, 2f,
                Element.Fire, .5f, projectile: true),
            New(PlayerSkillId.Envenom, "Envenom", "Deal no hit damage; use the normal attack as the basis for Poison.", 20, 0f,
                specialized: StatusEffects.AilmentKind.Poison, suppressDirect: true,guaranteedAilment:1),
            New(PlayerSkillId.Shiv, "Shiv", "Deal 20% hit damage and apply a strongly Bleed-weighted ailment basis.", 30, .2f,
                Element.Phys,1f,StatusEffects.AilmentKind.Bleed,6f,guaranteedAilment:1),
            New(PlayerSkillId.Immolate, "Immolate", "Deal 10% hit damage; a 50% Fire conversion fuels an Ignite with a 250% basis.", 60, .1f,
                Element.Fire, .5f, StatusEffects.AilmentKind.Ignite, 1f, absoluteAilment: 2.5f,
                guaranteedAilment:1)
        };
    }

    public static List<PlayerSkillDefinition> CreateProductionDefaults()
    {
        var result=CreateDefaults();
        var rending=Production(PlayerSkillId.SwordRendingStrike,"skill.sword.rending_strike",WeaponTypeIds.Sword,"Rending Strike","120% attack hit. Roll Bleed applications normally, then double them before the stack cap. Four-second cooldown.",25,1.2f,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.RendingStrike,cooldown:4);
        rending.queuedCooldownEnabled=true;rending.scalesWithCooldownReduction=true;result.Add(rending);
        var armourStrike=Production(PlayerSkillId.SwordArmourStrike,"skill.sword.armour_strike",WeaponTypeIds.Sword,"Armour Strike","150% hit; 2% more per 100 Armour, capped at 200% more. Four-second cooldown.",25,1.5f,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.ArmourStrike,cooldown:4);
        armourStrike.queuedCooldownEnabled=true;armourStrike.scalesWithCooldownReduction=true;result.Add(armourStrike);
        result.Add(Production(PlayerSkillId.AxeRageStrike,"skill.axe.rage_strike",WeaponTypeIds.TwoHandedAxe,"Rage Strike","150% hit and 50% more Rage generated by this attack.",30,1.5f,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.RageStrike,.5f));
        result.Add(Production(PlayerSkillId.AxeHemorrhage,"skill.axe.hemorrhage",WeaponTypeIds.TwoHandedAxe,"Hemorrhage","60% hit with a guaranteed Bleed at 300% normal basis.",35,.6f,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.EmpoweredBleed,3));
        result.Add(Production(PlayerSkillId.BowVenomShot,"skill.bow.venom_shot",WeaponTypeIds.Bow,"Venom Shot","Deals no direct damage; guaranteed Poison uses 300% of its would-be hit.",35,0,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.VirtualPoison,3,projectile:true));
        result.Add(Production(PlayerSkillId.BowDoubleVolley,"skill.bow.double_volley",WeaponTypeIds.Bow,"Double Volley","Fires twice the final projectile count.",40,1,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.DoubleProjectiles,2,projectile:true));
        result.Add(Production(PlayerSkillId.StaffFireball,"skill.staff.fireball",WeaponTypeIds.Staff,"Fireball","Ready, enabled Fireball replaces the next basic attack with a 250% Fire projectile.",60,2.5f,PlayerSkillCastMode.AutoQueuedReplacement,WeaponSkillEffect.None,0,cooldown:5,projectile:true,element:Element.Fire,conversion:1,magic:true));
        result.Add(Production(PlayerSkillId.StaffShockBarrage,"skill.staff.shock_barrage",WeaponTypeIds.Staff,"Shock Barrage","Consumes all existing Shocks before dealing damage. One base hit plus one hit per 20% summed Shock effect consumed. Consumed Shocks do not amplify this Barrage; its hits can apply new Shocks.",50,1,PlayerSkillCastMode.AutoQueuedReplacement,WeaponSkillEffect.ShockBarrage,20,0,cooldown:5,element:Element.Light,conversion:1,magic:true));
        result.Add(Production(PlayerSkillId.SceptreRestorativeStrike,"skill.sceptre.restorative_strike",WeaponTypeIds.Sceptre,"Restorative Strike","Heals for 35% of actual post-mitigation damage.",30,1,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.HealFromDamage,.35f));
        result.Add(Production(PlayerSkillId.SceptreFrostJudgment,"skill.sceptre.frost_judgment",WeaponTypeIds.Sceptre,"Frost Judgment","Chilled targets may Freeze; Frozen targets Shatter.",40,1,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.FrostJudgment,0,0,element:Element.Cold,conversion:1,magic:true));
        result.Add(Production(PlayerSkillId.DaggerBackstab,"skill.dagger.backstab",WeaponTypeIds.Dagger,"Backstab","150% hit with independent Bleed and Poison rolls.",25,1.5f,PlayerSkillCastMode.QueuedAttackReplacement,WeaponSkillEffect.Backstab));
        result.Add(Production(PlayerSkillId.DaggerQuickStrike,"skill.dagger.quick_strike",WeaponTypeIds.Dagger,"Quick Strike","Off-gauge thrown Dagger projectile for 100% damage; travel scales with Projectile Speed.",25,1,PlayerSkillCastMode.ImmediateCooldown,WeaponSkillEffect.None,0,0,cooldown:4,projectile:true));
        return result;
    }

    // Upgrade older serialized catalogs without editing a shared ScriptableObject at runtime.
    public static List<PlayerSkillDefinition> UpgradeProjectileDefinitions(IEnumerable<PlayerSkillDefinition> source)
    {
        var result = new List<PlayerSkillDefinition>();
        foreach (var original in source)
        {
            if (original == null) { result.Add(null); continue; }
            var definition = JsonUtility.FromJson<PlayerSkillDefinition>(JsonUtility.ToJson(original));
            if(definition.id==PlayerSkillId.SwordRapidFlurry)
            {
                definition.id=PlayerSkillId.SwordRendingStrike;
                definition.stableId="skill.sword.rending_strike";
                definition.displayName="Rending Strike";
                definition.description="120% attack hit. Bleed applications are doubled before the stack cap. Four-second cooldown.";
                definition.hitDamageMultiplier=1.2f;
                definition.baseHitCount=1;
                definition.effect=WeaponSkillEffect.RendingStrike;
                definition.baseCooldown=4f;
                definition.queuedCooldownEnabled=true;
                definition.scalesWithCooldownReduction=true;
            }
            if(definition.id==PlayerSkillId.SwordArmourStrike)
            {definition.baseCooldown=4f;definition.queuedCooldownEnabled=true;definition.scalesWithCooldownReduction=true;definition.description="150% hit; 2% more per 100 Armour, capped at 200% more. Four-second cooldown.";}
            if (definition.id is PlayerSkillId.StaffFireball or PlayerSkillId.StaffShockBarrage)
            {
                definition.castMode = PlayerSkillCastMode.AutoQueuedReplacement;
                definition.scalesWithCooldownReduction = true;
            }
            if(definition.id==PlayerSkillId.StaffShockBarrage)
            {
                definition.description="Consumes all existing Shocks before damage. One base hit plus one hit per 20% summed Shock effect consumed. Consumed Shocks do not amplify this Barrage; its hits can apply new Shocks.";
                definition.maximumCount=0;definition.additionalHitsFromShockChance=false;definition.projectile=false;
            }
            if (definition.id is PlayerSkillId.StaffFireball or PlayerSkillId.DaggerQuickStrike)
            {
                definition.projectile = true;
                definition.supportsPrecision = true;
            }
            if (definition.baseProjectileSpeed <= 0) definition.baseProjectileSpeed = 1;
            result.Add(definition);
        }
        return result;
    }

    static PlayerSkillDefinition Production(PlayerSkillId id,string stable,string weapon,string name,string description,
        float mana,float damage,PlayerSkillCastMode mode,WeaponSkillEffect effect,float secondary=0,int maximum=0,
        float cooldown=0,bool projectile=false,Element element=Element.Phys,float conversion=0,bool magic=false)=>new()
    {
        id=id,stableId=stable,weaponTypeId=weapon,displayName=name,description=description,manaCost=mana,
        hitDamageMultiplier=damage,castMode=mode,effect=effect,secondaryMultiplier=secondary,maximumCount=maximum,
        baseCooldown=cooldown,scalesWithCooldownReduction=mode!=PlayerSkillCastMode.QueuedAttackReplacement,
        projectile=projectile,supportsPrecision=projectile,conversionElement=element,nonMatchingConversion=conversion,magic=magic,
        specializedAilment=effect==WeaponSkillEffect.EmpoweredBleed?StatusEffects.AilmentKind.Bleed:effect==WeaponSkillEffect.VirtualPoison?StatusEffects.AilmentKind.Poison:StatusEffects.AilmentKind.None,
        ailmentBasisMultiplier=effect is WeaponSkillEffect.EmpoweredBleed or WeaponSkillEffect.VirtualPoison?secondary:1,
        guaranteedAilmentApplications=effect is WeaponSkillEffect.EmpoweredBleed or WeaponSkillEffect.VirtualPoison?1:0,
        suppressDirectDamage=effect==WeaponSkillEffect.VirtualPoison,tags=new[]{"WeaponSkill",weapon}
    };

    private static PlayerSkillDefinition New(PlayerSkillId id, string name, string description, float mana,
        float hitMultiplier, Element conversion = Element.Phys, float conversionAmount = 0f,
        StatusEffects.AilmentKind specialized = StatusEffects.AilmentKind.None, float ailmentBasis = 1f,
        bool projectile = false, bool shockHits = false, int guaranteedChill = 0, bool suppressDirect = false,
        float absoluteAilment = -1f,int guaranteedAilment=0)
    {
        return new PlayerSkillDefinition
        {
            id = id, displayName = name, description = description, manaCost = mana,
            hitDamageMultiplier = hitMultiplier, baseHitCount = 1,
            conversionElement = conversion, nonMatchingConversion = conversionAmount,
            specializedAilment = specialized, ailmentBasisMultiplier = ailmentBasis,
            absoluteAilmentCoefficient = absoluteAilment,
            projectile = projectile, additionalHitsFromShockChance = shockHits,
            guaranteedAdditionalChill = guaranteedChill,
            guaranteedAilmentApplications = guaranteedAilment,
            suppressDirectDamage = suppressDirect
        };
    }
}
