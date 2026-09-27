# Enemy power and production loot budgets

The editable production profile is `Assets/Resources/LootDropBalanceProfile.asset`. Open **Black-Cube → Balance Workbench → Drops / Loot → Production Editing** to author rates and overrides, then use the preview before applying. Normal→Magic starts at 5%, Magic Reroll 3%, Magic→Rare 2.5%, Rare Reroll 1%, Remove 0.5%, and Affix Focus 0.2% from combat level 50. These are editable initial assumptions, not final tuned values. Add Rare and older/Ancient/Empowerment currencies remain explicitly on the historical weighted path until their own rates are designed. Base gear budget is one copy per enemy.

For a direct-rate currency, the budget is:

`max(0, (base chance + rarity additive + applicable stage/location/archetype/enemy/boss additives) × applicable multipliers × enemy power)`.

Rarity additives start at zero for Normal, +2 percentage points for Magic, and +5 percentage points for Rare. Legendary and boss values are separately editable. Additives apply **before** the power multiplier. A 5% base currency on a Rare enemy with 3× power therefore has `(5% + 5 points) × 3 = 30%` budget. Budgets over one generate multiple copies: the integer portion is guaranteed and the remainder is rolled. Gear uses the same budget structure and multi-copy rule.

Enemy Power is not raw GearScore. `EnemyLootPowerScorer` compares actual versus average enemy DPS against a selected reference player (offensive pressure), and player time-to-kill against actual versus average enemy Life (defensive pressure). Its configurable weighted geometric mean defaults to 50/50. It is a cheap analytical approximation; it currently represents basic-hit mitigation, crit, attack rate, and Hit Twice, but **does not yet include the full enemy skill/ailment policy**. This limitation must be resolved and compared with Combat Lab before treating power-scaled loot as final.

The reference asset is `Assets/Resources/EnemyLootPowerReference.asset`. It starts empty. In **Enemy → Enemy Power**, select an equipped reference Player Build whose Combat Level matches the target level, choose enemy rarity and sample count, then explicitly generate a row. The asset records the production-data fingerprint and marks it stale when relevant authored data changes. Missing rows use neutral 1× power instead of an invented Balanced-player curve. Generation is deliberate, Undo-safe, and never silently triggered by runtime drops.

The Enemy Power preview now accepts a specific production archetype and seed, generates its actual optimized enemy gear, and shows diagnostic GearScore, equipment, basic DPS, Life/Armour, effective Life against the reference player's hit mix, player/enemy TTK estimates, offense/defense pressure, and resulting gear/currency budgets. The preview explicitly excludes stage/location override context until one is selected in the Drop Simulator. These are analytical hit-based estimates; use Combat Lab for enemy skills, ailments, and behavior. No reference rows are auto-authored from arbitrary or hypothetical player builds.

Live drops use entropy-backed production RNG. Workbench simulations use injected seeded RNG. Production and Workbench call the same configured drop-budget evaluator and rolling rule. The older weighted table remains only for currency types explicitly marked legacy; a direct-rate currency cannot also be awarded by that path.
