// Developer map: UI grouping labels used by StatCategoryMapping; these categories have no effect on combat formulas.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

public enum StatCategory
{
    WeaponBase,
    FlatDamage,
    IncreasedDamage,
    MoreDamage,
    Penetration,
    DamageOverTime,
    Ailments,
    Defenses,
    Resources,
    Utility,
    Attributes,
    Other
}
