# Challenge Bosses

Six progression-independent, repeatable challenge definitions exist outside the 9+1 world loop: The Iron Colossus, Phoenix Unbound, Absolute Zero, The Living Tempest, Maw Beyond Stars, and Avatar of the Black Cube.

Each definition has a stable content ID, minimum level, unlock requirements, dedicated key resource, challenge boss, dedicated Essence reward, entropy-backed loot-source ID, and special-affix pool ID. `EndgameResourceLedger` persists character ownership in schema 11. `ChallengeRuntimeService` validates unlock/key state, creates the encounter before spending exactly one key, grants explicit victory rewards, and restores the prior world position. The functional launcher becomes available after `story.main.complete`.

Each special-affix pool contains three Prefixes and three Suffixes (36 total) with stable modular effect IDs, explicit compatibility, and first-pass mechanical behavior. Full values and safety rules are in `BOSS_SPECIAL_AFFIXES.md`. Challenge victory guarantees one matching Essence plus one Catalyst; bonus chances and Reforger acquisition are in `ENDGAME_ITEMIZATION.md`. All numerical tuning is FIRST-PASS PLACEHOLDER VALUES.
