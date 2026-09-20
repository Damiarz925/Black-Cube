# Empowerment

All values are **FIRST-PASS PLACEHOLDER VALUES**.

The stable currency identity is `currency.empowerment-catalyst`, represented by the existing `CraftingCurrencyType.EmpowermentCatalyst`. It is character-slot owned, schema-11 persisted, and Rebirth-persistent.

An eligible target is an ordinary, numeric/scalable, authored-empowerable T1 explicit that is neither implicit, already Empowered, nor APEX/boss-special. Empowerment costs one Catalyst and zero Crafting Potential. Invalid actions consume nothing. The item may own at most the progression allowance:

| Combat level | Maximum Empowered explicits |
|---:|---:|
| 0–119 | 0 |
| 120–159 | 1 |
| 160–209 | 2 |
| 210–259 | 3 |
| 260–309 | 4 |
| 310–359 | 5 |
| 360+ | 6 |

Unless an affix author supplies a custom Empowered range, the Empowered range is 125% of the T1 endpoints. An Empowered modifier retains its explicit slot/side, displays `EMPOWERED`, persists through save/load/Rebirth with its item, and cannot be ordinarily rerolled/removed or boss-infused.

Stage-10 progression bosses have no Catalyst chance below combat level 120. Chance linearly interpolates from 5% at 120 to 30% at 360. Every challenge-boss victory guarantees one Catalyst and has a 20% entropy-backed chance for a second. These are first-pass rates. Reward RNG is fresh and is not seeded by world/stage/encounter identity.
