# Realistic-progression subclass survey

The survey runner is `BlackCube.BalanceWorkbench.SubclassSurveyRunner.RunAll`.
Its local artifacts live in `ReviewCaptures/BalanceWorkbench/SubclassSurvey/`.
This directory is intentionally ignored by Git: raw simulation evidence is large,
while this runner, its tests, and this methodology are versioned.

## Reproduce or resume

Close the Unity Editor before starting a batch process. Use the project's Unity
version with `-batchmode -nographics -projectPath <checkout> -executeMethod
BlackCube.BalanceWorkbench.SubclassSurveyRunner.RunAll -logFile <local-log>`.
The manifest resumes completed, checksummed results only when the production
data fingerprint matches. Preserve the output directory before starting a new
data revision. No old BalanceLab entry point is used.

The three straight-progression seeds are 20001, 50001, and 80001. Each level's
original loot, currency, progression history, and Rare reference enemy are shared
across all twelve subclasses. Each subclass crafts from an independent clone.
Signature weapons, Serious crafting with a uniform twelve-action budget,
joint gear/passive optimization, soft production-derived resistance targets,
and soft combined physical mitigation are used consistently.

Ten distinct gear finalists share the joint search's passive allocation. Each
receives ten Combat Lab fights; the winner receives one hundred fights against
the same representative Rare enemy. This is a bounded heuristic search, not a
proof of a global optimum or a multi-seed loot-distribution study.

Run `BlackCube.BalanceWorkbench.SubclassSurveyAblationRunner.Run` after all
thirty-six results are complete. It verifies every result checksum, evaluates
analytical ablations, runs one hundred combat fights with the passive tree or
subclass removed, and exports detailed per-level/cross-level CSVs. Ablations
hold equipment and policy fixed rather than reoptimizing. A single seeded
ten-times-Life target per build provides an explicitly non-canonical duration
diagnostic. Weapon provenance includes the best ground Physical Axe by local
weapon DPS and the selected weapon's local DPS.

## Interpreting the outputs

- `subclass_survey_summary.md`: designer findings, level tables, limitations,
  and answers to the requested survey questions.
- `subclass_survey_level20.csv`, `subclass_survey_level50.csv`, and
  `subclass_survey_level80.csv`: sorted full build/defense/resource/ablation
  comparison tables. Percentage fields use fractions: 0.02 means 2%.
- `subclass_survey_cross_level.csv`: matched subclass scaling ratios.
- `subclass_survey_raw.json`: all canonical final results and finalist sets.
- `subclass_survey_ablations.json`: one-factor combat/analytical ablations and
  longer-target evidence.
- `mechanic_audit.md`: before/root-cause/fix/after records and unresolved model
  caveats. A completed run is not a blanket PASS for every mechanic.
- `level*_shared.json` and `level*_subclass_*.json`: reproduction metadata,
  full inventories/crafting actions, final equipment, passives, enemy snapshots,
  skill/ailment counters, and combat distributions.

Combat revision 2 includes the production weapon-skill fallback. Earlier
basic-only results were invalid and are archived locally, not used for rankings.
Both search and Combat Lab now use production's floor rule for projectile count.
Do not rerun `RunAll` after the ablation export merely to view the results: its
checkpoint export writes the compact pre-ablation CSVs. Rerun the ablation
export afterwards if needed.

Analytical ailment potential is not encounter DPS. It omits timed stack
replacement/caps and uses neutral mitigation; short fights and projectile travel
can sharply reduce realized output. Rage Finisher and Rupture timing are not
fully valued by the fast search. Assassin's documented Attack-Speed-to-Crit-Mult
conversion was not found in production code. These limitations must remain
visible in balance decisions rather than being interpreted as measured balance.

## Completed checkpoint: 2026-09-27

Data fingerprint `A2089E702CAFEA46`; base commit `0b9f82f5`; correctness fixes
`ed9a6ed2`. All 36 results/checksums and CSV-to-JSON verification passed.
Validation: 558 EditMode tests, zero failures; production/Workbench validators
passed. No balancing or push was performed.

| Subclass | L20 combat DPS | L50 combat DPS | L80 combat DPS |
|---|---:|---:|---:|
| Bleed Warrior | 126.59 | 1,014.15 | 10,740.47 |
| Momentum Warrior | 206.13 | 2,609.58 | 30,350.40 |
| Titan Barbarian | 40.87 | 258.38 | 8,688.25 |
| Fire Barbarian | 47.52 | 314.13 | 6,990.95 |
| Venom Ranger | 97.31 | 485.81 | 3,950.48 |
| Projectile Ranger | 86.90 | 830.45 | 6,458.40 |
| Chrono Mage | 67.68 | 410.09 | 4,065.77 |
| Storm Mage | 92.46 | 537.39 | 5,270.33 |
| Dark Priest | 32.07 | 399.43 | 1,238.82 |
| Light Priest | 24.33 | 394.17 | 1,543.24 |
| Assassin | 252.60 | 1,310.91 | 24,794.49 |
| Ailment Assassin | 86.67 | 549.82 | 10,794.97 |

Medians: 86.79 / 511.60 / 6,724.67. Assassin and Momentum exceed twice the
median at all checkpoints. Thirty builds exceed 20% analytical/combat
divergence. Thirty-one meaningfully miss soft defenses under a five-percentage-
point deficit rule; all L50/L80 builds miss the PDR target. Fire is dominant
total damage in 13/36 builds, but Ignite is never 20% of canonical damage.

Bleed/Rupture is inactive in the later Bleed builds. No Axe final allocates
Finisher. Both Warriors ignore the Sword district, and later builds use many
off-class points. Momentum loses 92.2% of L80 combat DPS without the tree.
L80 Dark Priest wins 41/100; all other final batches win 100/100. Crafted
equipped items average 2.83; net analytical pipeline gain averages 28.50%,
which is not an isolated combat crafting gain. Lucky-weapon dependence
cannot be established from one seed per checkpoint.

Review analytical ailment/projectile bias first, then intended Flurry/combo
scaling, late tree/off-class power, Physical target affordability, dynamic
keystone valuation and late Priest performance. No tuning is implied.
