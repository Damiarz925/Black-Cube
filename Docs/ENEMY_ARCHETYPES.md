# Enemy Archetypes

The production catalog contains six ordinary and two elite archetypes per biome (48 total). Ordinary encounter tables weight ordinary entries at 8 and elite entries at 2.

- Ashen March: March Raider, Ironhide Boar, Bloodwood Archer, Shieldbound, Grave Hound, Riven Veteran; elites Bleak Champion and March Warden.
- Cinder Wastes: Cinder Imp, Ash Stalker, Ember Hound, Charred Zealot, Magma Brute, Flamecaller; elites Pyre Champion and Eruption Herald.
- Frostbound Reaches: Rime Wolf, Icebound Archer, Snow Wraith, Glacial Guard, Frostcaller, Shiverfiend; elites Hoarfrost Knight and Winter Oracle.
- Tempest Heights: Storm Hawk, Sparkblade, Thunder Hound, Galeborn, Arc Sentinel, Tempest Caller; elites Bolt Champion and Storm Conductor.
- Voidfen: Bog Lurker, Venom Fang, Hollow Cultist, Spore Carrier, Null Stalker, Rot Weaver; elites Abyss Knight and Corruption Seer.
- Black Citadel: Citadel Legionary, Prismatic Hound, Rift Archer, Elemental Adept, Blackguard, Chaos Weaver; elites Eclipse Champion and Cube Herald.

Every definition has a stable ID, Codex hook, rank, primary element, skill loadout, build preference, and mechanical multipliers. `EnemyAI.ConfigureWorldContent` applies those profiles after intrinsic scaling and equipment generation without replacing `EnemyBuildOptimizer`, Step 18.5 ailment eligibility, or `LootPower`.
