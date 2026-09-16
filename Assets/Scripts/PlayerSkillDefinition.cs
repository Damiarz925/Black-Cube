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
    Immolate
}

[Serializable]
public sealed class PlayerSkillDefinition
{
    public PlayerSkillId id;
    public string displayName;
    [TextArea] public string description;
    [Min(0f)] public float manaCost;
    [Min(0f)] public float hitDamageMultiplier = 1f;
    [Min(1)] public int baseHitCount = 1;
    public bool additionalHitsFromShockChance;
    public bool projectile;
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
            New(PlayerSkillId.Shiv, "Shiv", "Deal 50% hit damage and apply a strongly Bleed-weighted ailment basis.", 30, .5f,
                Element.Phys,1f,StatusEffects.AilmentKind.Bleed,20f,guaranteedAilment:1),
            New(PlayerSkillId.Immolate, "Immolate", "Deal 10% hit damage; a 50% Fire conversion fuels an Ignite with a 250% basis.", 60, .1f,
                Element.Fire, .5f, StatusEffects.AilmentKind.Ignite, 1f, absoluteAilment: 2.5f,
                guaranteedAilment:1)
        };
    }

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
