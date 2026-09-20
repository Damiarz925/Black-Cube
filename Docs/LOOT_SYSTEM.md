# Enemy Loot System (Step 18.5)

Every defeated ordinary enemy drops at least one gear item. `EnemyLootProfile` derives a debuggable snapshot from enemy level, enemy rarity, and the power of the equipment actually selected by `EnemyBuildOptimizer`:

`LootPower = LevelLootFactor × EnemyRarityMultiplier × clamp(ActualGearScore / ExpectedGearScore, 0.60, 3.00)`

The level factor is `lerp(0.75, 2.00, pow(clamp01((level-1)/359), 0.75))`. Rarity multipliers are Normal 1.0, Magic 1.6, Rare 2.6, Legendary 4.0, and boss fallback 5.0. Canonical actual score is `exp(optimizer final score)`. The deterministic expected curve uses capped enemy gear level, equipped-slot count, and rarity; it is a tuning baseline, not a Monte Carlo artifact.

Extra gear budget is `max(0, LootPower-0.75) × 0.60`, stochastically rounded and capped at 12 extras (13 total). Currency roll budget is `0.18 × LootPower`, stochastically rounded and capped at eight. Death rewards run under `GamePersistence.GenerateDeterministicLoot`, derived from the run/encounter seed.

`CurrencyLootTable` owns stable IDs, base weights, level/rarity/progression gates, quality tier, stack range, and ordinary-loot enablement. Enemy rarity biases higher quality tiers by 1.0/1.1/1.3/1.6 (boss 2.0) through `baseWeight × pow(bias, qualityTier)`. All six current ordinary crafting currencies are natural drops. All six Ancient currencies are rare, gated to combat level 60 and appropriate minimum enemy rarity. Empowerment Catalyst remains excluded because challenge content is its intended source. Duplicate currency rolls aggregate into one world stack.

Natural weapon drops choose all six stable weapon types at equal 1/6 weight. Their level-one profiles live in `WeaponTypeCatalog`; generic intrinsic item-level growth is retained above level one. `ItemIconCatalog` maps each stable weapon ID to its own temporary 128×128 transparent icon.
