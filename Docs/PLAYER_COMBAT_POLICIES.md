# Player Combat Policies

Combat policies are simulation assumptions. They do not change input, automation, skills, or controls in the game.

## Action policies

- **Basic Only** never queues replacement skills and does not activate Dagger ImmediateCooldown skills. Staff AutoCooldown remains automatic because that is production weapon behavior.
- **Skills When Available** uses the first affordable queued skill and activates automatic/immediate cooldown skills.
- **Skill 1 Priority** prefers the first affordable queued skill.
- **Skill 2 Priority** prefers the second/higher-index affordable queued skill.
- **Alternate Skills** rotates between affordable queued skills.
- **Mana Conservative** spends only when the configured Mana reserve remains after payment.

If no eligible manual skill exists, the attack gauge continues normal basic attacks. A policy cannot accidentally stop combat. Staff skills that are ready but unaffordable retain ready state, retry when Mana changes, and report Mana failures plus ready-starved time.

## Rage Finisher policies

- **Never** retains normal Rage benefits and never arms the Finisher.
- **Immediately** uses it on the next attack event at full Rage.
- **Next Skill** waits for any skill attack.
- **Skill 1 Only / Skill 2 Only** waits for the selected skill slot.
- **Target Below Threshold** waits until enemy Life is at/below the configured fraction.

Successful Finisher resolution consumes Rage. A failed Mana decision does not consume it. Use **Compare Built-In Policies**, or store one result as comparison, to compare Win Rate, DPS, duration, starvation, and Rage behavior with the same matchup and seed family.

## Presets and validation

`PlayerCombatPolicySO` stores action policy, Rage policy, Mana reserve, and target-Life threshold. Values are validated before execution; duration/event caps must be positive and percentages must be within 0–100%. Missing build/enemy/boss selections are explicit validation errors. The current fallback is always normal basic/automatic production behavior.

