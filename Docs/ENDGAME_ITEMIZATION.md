# Endgame Itemization

All numbers in this document are **FIRST-PASS PLACEHOLDER VALUES**.

## Character-owned resources

Schema 11 persists six `resource.challenge-key.<biome>` stacks, six `resource.challenge-essence.<biome>` stacks, `currency.empowerment-catalyst`, `currency.implicit-reforger`, and valid challenge first-clear flags. Ownership is per character slot. Schema 10 migrates with empty endgame ownership; the migration never fabricates kills or resources. These resources survive Rebirth. New Game clears them.

The six challenge keys and Essences use the biome slugs `ashen-march`, `cinder-wastes`, `frostbound-reaches`, `tempest-heights`, `voidfen`, and `black-citadel`.

## Challenge loop

The Challenge launcher unlocks after `story.main.complete`. It lists all six Step 19 challenges, their actual combat-level requirements, key balance/cost, biome, theme, Essence, pool, and lock reason. Entry consumes exactly one matching key only after the challenge encounter is successfully created. Failed validation or failed creation consumes nothing; defeat does not refund. Victory returns to the saved world encounter without advancing or corrupting the main 9+1 position. Defeat returns to the same world position with clean player resources and encounter state.

Victory guarantees one matching Essence with a 25% entropy-backed chance for a second. It also guarantees one Empowerment Catalyst with a 20% chance for a second. Biomes 1–5 have a 10% Reforger chance at combat level 210+. The first Biome-6 clear guarantees one Reforger per character; later kills have a 25% chance.

Key drops use the Step 18.6 per-death production RNG. First-pass chances, before LootPower adjustment, are 1.25% ordinary, 2.5% Magic, 5.5% Rare, 11% Legendary, and 22% stage boss. These rates are not final balance.

## Endgame crafting panel

The gameplay HUD exposes one coherent panel with Empowerment, Boss Infusion, and Implicit Reforge tabs. It includes item/mod/pool selectors, previews, costs, validation state, and a resource ledger. Invalid actions consume nothing.

Boss Infusion requires a Legendary item, one selected ordinary non-Empowered explicit, the matching Essence, and 3 Crafting Potential. It replaces the selected explicit with one random compatible same-side APEX affix. It never adds a seventh explicit. Results use fresh production crafting entropy with deterministic injection for tests. APEX affixes persist and cannot be ordinarily rerolled, removed, Empowered, or selected by ordinary Add/Remove.

Implicit Reforge requires ilvl 100 Rare or Legendary gear and one `currency.implicit-reforger`. It costs 0 Potential and rerolls only the permanent implicit. Item type, rarity, level, all explicits, Empowered/APEX provenance, and current/maximum Potential are preserved. The current implicit family is excluded whenever an alternative legal family exists.

None of challenge reward, infusion, or implicit crafting randomness is derived from world, stage, encounter, run, or item-creation seeds.
