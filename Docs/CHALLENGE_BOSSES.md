# Challenge Bosses

Six progression-independent, repeatable challenge definitions exist outside the 9+1 world loop: The Iron Colossus, Phoenix Unbound, Absolute Zero, The Living Tempest, Maw Beyond Stars, and Avatar of the Black Cube.

Each definition has a stable content ID, minimum level, unlock requirements, dedicated key resource, challenge boss, dedicated essence reward, entropy-backed loot-source ID, and special-affix pool ID. `ChallengeContentService` provides injected-random key drops, entry spending, reward granting, capture/restore data, and repeatable entry semantics. Production integration must persist the captured stacks when a final challenge launcher/UI is added.

Each special-affix pool currently contains two clearly marked first-pass entries to prove legal weapon/offensive and armour/defensive replacement paths. These are structural placeholders, not final special-affix designs.
