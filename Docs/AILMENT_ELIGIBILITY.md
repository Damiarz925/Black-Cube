# Ailment Eligibility (Step 18.5)

`AilmentEligibilityResolver` is the sole production authority for deciding which typed portions of a hit can apply each ailment and how much raw source damage forms its basis. Chance never creates eligibility.

| Ailment | Default eligible damage |
|---|---|
| Bleed | Physical |
| Ignite | Fire |
| Shock | Lightning |
| Chill | Cold |
| Poison | Physical and Void |

Mixed hits use only eligible components. Poison converts eligible non-Void source components through the established Void scaling exactly once; Void components are not scaled twice.

Explicit exceptions are stable modular effect IDs: Venom Shot marks its virtual 300% context as `FullAilmentBasis`; Venom Ranger makes the full mixed hit Poison-eligible; Storm Mage makes the full mixed hit Shock-eligible; Dark Priest adds only the Void component to every ailment's normal mask. Backstab has no exception. Staff Fireball, Staff Shock Lightning, and Sceptre Frost Judgment naturally qualify through Fire, Lightning, and Cold respectively.

New mechanics must extend the resolver or add an explicit context/effect grant. They must not add weapon, class, or skill checks to `BattleManager`, `AilmentCalculator`, or status code.
