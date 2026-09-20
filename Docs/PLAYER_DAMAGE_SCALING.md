# Player Damage Scaling (Step 18.6)

Player root weapon/skill hits receive two ordinary additive increased-damage contributions in `PlayerController.AddScaledRawDamage`. They are applied before More multipliers, critical/precision resolution, ailment snapshots, and target defenses.

## Weapon attributes

| Weapon | Attributes | Increased damage |
|---|---|---|
| Sword | Strength + Dexterity | 0.25% per point of each |
| Two-Handed Axe | Strength | 0.50% per point |
| Bow | Dexterity | 0.50% per point |
| Staff | Intelligence | 0.50% per point |
| Dagger | Dexterity + Intelligence | 0.25% per point of each |
| Sceptre | Strength + Intelligence | 0.25% per point of each |

`WeaponAttributeScalingProfile` is keyed by equipped weapon type, never class. There is no first-pass cap. Final attributes include their existing percentage increases. The bonus affects basic attacks, weapon-skill roots, converted hits, and virtual bases derived from those hits. Ailments snapshot the already-scaled source. Eruption, Rupture, Shatter, Heal-to-Harm, and other downstream derived events do not run the root builder again and therefore do not double-apply it.

## Player level

`PlayerLevelDamageProfile` contributes `max(0, clamp(level,1,100)-1) × 0.25%` increased damage. Values are 0% at level 1, 2.25% at 10, 4.75% at 20, 12.25% at 50, and 24.75% at 100. It shares the additive increased bucket and is intentionally weaker than multiplicative More scaling.

The Stats panel exposes level, level bonus, final attributes, weapon type, scaling attributes, current attribute bonus, and local weapon DPS. Item tooltip Average Weapon DPS remains strictly local item average damage × local APS and excludes both player-level and attribute contributions.
