# Enemy Skills

Enemy skills are reusable definitions selected by `EnemyActionPlanner`. Selection is deterministic from the loadout and completed authored turn count; Unity random is not used for skill cadence. Each biome supplies four reusable definitions—measured strike, pressure/multi-hit, thematic ailment, and elite breaker—and ordinary/elite loadouts reference them.

Definitions carry stable identity, damage element, damage multiplier, hit count, cadence, and telegraph hook. `BattleManager` begins one authored action per enemy turn, executes its explicit hit count, and leaves the existing independent Hit Twice mechanic intact. `EnemyAI` exposes the current authored skill to inspection UI.

Thematic ailment identities are Bleed, Ignite, Chill, Shock, Poison, and hybrid multi-hit for biomes 1–6. Frostbound elite loadouts also use Rime Renewal, a conservative maximum-Life recovery action scaled by the central corruption recovery profile. Actual status application continues through the existing centralized on-hit and eligibility pipeline. Frozen enemies still consume their attack skip before selecting or executing an authored action, preserving player-control safety.

At 40% corruption and above, authored cadence advances more aggressively. At 80% and 100%, deterministic periodic pressure adds one explicit authored hit (every fourth or third authored action respectively). This is the non-stat corruption behavior; it remains deterministic and does not recurse into Hit Twice.
