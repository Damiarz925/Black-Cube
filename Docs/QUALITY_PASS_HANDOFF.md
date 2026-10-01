# Class passive / playtest quality pass

This pass is source and prefab work on `codex/repository-cleanup-baseline`. It has not been committed or pushed. The repository already contained unrelated work-in-progress changes; those were preserved.

## Class-node audit

All six authored class branches were enumerated, including both subclass variants and both left/right placements. The full per-node audit is in `Logs/QualityPass/class-node-audit.txt` (local validation output). The following table lists **every changed node pair**; each `left/right` entry means both independent nodes changed. Values are authored raw stat values.

| Class / subclass | Tier and nodes | Stat | Old each | New each | Reason |
|---|---|---|---:|---:|---|
| Mage / Cooldown | 1 left/right | Mana Regeneration | 3.60 | 4.50 | Ordinary 3.0 reference ×1.5 |
| Mage / Cooldown | 2 left/right | Mana on Hit | 0.552 | 0.675 | Ordinary 0.45 reference ×1.5 |
| Mage / Cooldown | 4 left/right | Mana Regeneration | 3.84 | 4.80 | Preserve tier growth |
| Mage / Cooldown | 5 left/right | Mana on Hit | 0.588 | 0.720 | Preserve tier growth |
| Mage / Cooldown | 7 left/right | Mana Regeneration | 4.08 | 5.10 | Preserve tier growth |
| Mage / Cooldown | 8 left/right | Mana on Hit | 0.624 | 0.765 | Preserve tier growth |
| Mage / Cooldown | 10 left/right | Mana Regeneration | 4.32 | 5.40 | Preserve tier growth |
| Priest / Light | 1 left/right | Life on Hit | 0.360 | 1.20 | Ordinary 0.8 reference ×1.5 |
| Priest / Light | 4 left/right | Life on Hit | 0.384 | 1.28 | Preserve tier growth |
| Priest / Light | 7 left/right | Life on Hit | 0.408 | 1.36 | Preserve tier growth |
| Priest / Light | 10 left/right | Life on Hit | 0.432 | 1.44 | Preserve tier growth |
| Warrior / Bleed | 2 left/right | Life on Hit | 0.368 | 1.20 | Martial sustain class premium |
| Warrior / Bleed | 5 left/right | Life on Hit | 0.392 | 1.28 | Preserve tier growth |
| Warrior / Bleed | 8 left/right | Life on Hit | 0.416 | 1.36 | Preserve tier growth |

The other Barbarian, Ranger, Thief, Mage, Priest, and Warrior class/subclass choices were retained; their values were not indiscriminately raised. The existing first-two-tier sustain routes remain in place. Reduced Shock/Chill Effect gear affixes retain their 4–25-per-piece T5–T1 progression from the prior pass.

## Runtime and UX

- Lingering player ailments no longer retain a destroyed enemy `StatsComponent` after enemy replacement. The exact reported stack ran `StatusHUD.Refresh → StatusController.GetStatusSummaries → CombatCalculator.CalculateAilmentTickDamage → UniqueCombatRuntime.For`. Source references are released at enemy teardown, and destroyed Unity objects are no longer dereferenced with C# null propagation.
- Visible class trees use a stable horizontal row ordered Warrior, Ranger, Thief, Mage, Priest, Barbarian, at 900 units spacing and 760 units above the hub. The weapon tree stays in the central hub. `RefundAll` already cleared all allocation ranks and preserved selected route/weapon unlock history; regression tests exercise that behavior.
- Occupied equipment slots now unequip on click (unless a crafting currency is armed). Unarmed basic attacks use 15–23 Physical base damage, 0.42 attacks/sec, 4% base Crit, and the normal conversion/damage pipeline. `Damage per Lowest Attribute` is now Unarmed-only with dedicated 10–20% through 60–120% per-10-lowest-attribute tiers. Weapon-specific mechanics remain gated by an actual weapon.
- Base-class and subclass mouseovers explain playstyle and mechanics. The subclass button stays hidden until unlocked, then pulses briefly. Subclass switching text says old subclass points refund for reallocation at no currency cost and ordinary class nodes remain.
- Auras last 5 real-time seconds per element, survive attack launches and enemy transitions, and only equal/stronger applications refresh or replace a current aura. The effect readout includes intensity and remaining seconds.
- The compact player Stats screen shows weapon type (including Unarmed), nonzero damage types and resources, a separate final-resistances section, and no enemy Shock row or separate All Resistances row.
- Item tooltip headers state item type. The overall upgrade estimate remains in the tooltip. Development-only diagnostics compute per-modifier marginal contribution using the production replacement evaluator and list its known exclusions; T1 accents remain intact. Legendary/Unique manual dismantles require an authored confirmation; locked items remain blocked and Uniques remain excluded from auto-dismantle.
- Ordinary weapon drops weight the native signature weapon 125 versus 100 for each other type (20% versus 16.7% under six equal base types); Unique selection is untouched. Auto-restart on death is an opt-in persisted option with a 1.5-second real-time recap delay.
- Sceptre is melee; only Bow and Staff are ranged. Staff basic attacks launch a Magic Bolt projectile with travel time and projectile-speed scaling. Priest max-Life damage is typed True, bypasses ordinary mitigation, produces white floating numbers, and remains a real damage/kill event.

## Validation

- Focused quality-pass EditMode tests: 8 passed, 0 failed (`Logs/QualityPass/focused-tests.txt`), including a single-instance Main Menu authoring check.
- A broader run of legacy suites: 52 passed, 6 failed due to older expectations against already changed authored data and affix tiers. These were not silently reclassified as passing; see `Logs/quality-pass-focused.log` if investigating the old suites.
- Fresh Windows x64 build: succeeded, 0 errors (`Logs/QualityPass/windows-build.txt`); output is `Builds/QualityPassWindows/BlackCube.exe`.
- Headless player startup: remained running for 12 seconds, loaded Main Menu, and logged no gameplay exception (`Logs/QualityPass/startup-player.log`). Unity cloud telemetry connection warnings occurred in the sandbox. This is not a visual click-through.
- Interactive desktop smoke could not be completed because the computer-use app launch timed out. Manually verify equipped-slot click/unequip, subclass hover and unlock pulse, Stats section layout, dismantle modal, aura readout, and Magic Bolt travel in the fresh build.

No BalanceLab or broad simulation was run. Main was not modified and nothing was pushed.
