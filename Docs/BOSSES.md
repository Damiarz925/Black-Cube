# V1 Main Bosses

There are 60 main boss definitions: one stable boss for each of ten locations in each biome. Names and IDs live in `ProductionWorldContent`; the editor inspector resolves any level/stage to the exact definition.

Every boss owns Codex/presentation hooks, biome/location identity, a reusable skill loadout, reward hooks, and a two-phase profile. Phase one begins at full life; phase two becomes eligible at 50% life and carries a desperation mechanic hook plus first-pass damage escalation. Corruption evolution is applied by the current level's central corruption profile, including the 100% apex modifier.

The Cinder Wastes location-10 boss, The Burning Crown, is the authoritative level-100 `story.main.complete` boss. Defeat grants the existing persistent subclass-choice entitlement. The final level-360 boss is The Black Cube.

Numbers are first-pass authoring values, not final balance. Final unique animations, telegraphs, audio, bespoke phase scripts, and game-feel review remain future/manual content work.
