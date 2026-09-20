# Step 17 weapon mechanics

## Staff auto-cast and Cast Speed

Weapon skills declare `QueuedAttackReplacement` or `AutoCooldown`; combat code does not branch globally on Staff identity. Two AutoCooldown slots tick independently and in stable slot order. A ready skill pays Mana and casts automatically; if Mana is insufficient it remains ready without restarting its timer and fires when Mana becomes available. Weapon changes rebuild cooldown state, and partial cooldowns are transient rather than saved.

Cast Speed is stable appended stat ID 120 and is separate from Attack Speed. Eligible cooldowns use `max(0.20 seconds, BaseCooldown / (1 + CastSpeed))`. Attack Speed continues to govern the basic-attack gauge only. Final Staff skill identities remain intentionally unassigned; editor fixtures validate the production mechanism.

## Bow Precision and projectiles

Bow is Precision-capable by default, while a future skill/weapon may opt in explicitly. Baseline Precision is 10% chance and 1.50× damage. Added Precision Chance and Damage are appended stats 121–122. Crit and Precision roll independently and their multipliers compose; Precision is applied once before ailment-basis capture and defenses. Normal Bow projectiles always hit—Accuracy was not reintroduced.

Baseline projectile travel is 1.0 second and uses `max(0.10 seconds, BaseTravel / (1 + ProjectileSpeed))`. Launch captures the intended target and damage context; a dead/replaced target causes a fizzle, never retargeting. Additional projectiles are distinct packets staggered by 0.10 seconds and independently roll weapon damage, Crit, Precision, and ailments.

## Axe Rage and Rage Finisher

Rage exists only while a supported Two-Handed Axe is equipped and is cleared on weapon swap. A nonzero hit dealt or taken grants 5 Rage plus one per approximately 2% of relevant maximum Life, capped at +10 bonus; Rage Generation scales this amount. After three seconds without gain, Rage decays at 10 per second, reduced by Rage Decay Reduction. Centralized first-pass benefits add damage per Rage, up to 10% defensive reduction, and a significant full-Rage damage bonus; Rage Effect scales these benefits.

With the Rage Finisher keystone and 100 Rage, the player may manually arm the next scheduled attack. It does not attack or alter the gauge. The entire basic or queued-skill attack event—including its natural hits, projectiles, and ailment bases—receives one centralized 2× multiplier, then all Rage is consumed after successful resolution. Swapping away cancels Rage and the armed state. The state/API can support a future auto-arm effect, but no such unique exists.

## Weapon DPS

Tooltip Average Weapon DPS is strictly the item's final local average damage multiplied by final local attacks per second. Local base-stat affixes are included. Crit, global player damage, Precision, Hit Twice, ailments, skills, and rotations are excluded and remain separately displayed. A true full-character DPS/rotation metric is explicitly future work after the final skill roster exists.
# Step 18 production skills

All six weapon profiles now bind two production skills. See `PRODUCTION_SKILLS.md` for values and cast modes. Cooldown skills use stat ID 126 and the centralized 0.20-second floor. ImmediateCooldown resolves outside the attack gauge and cannot be activated while unavailable.

## Step 18.5 natural weapon drops

Natural weapon loot selects Sword, Two-Handed Axe, Bow, Staff, Dagger, and Sceptre at equal 1/6 weight. Level-one profiles are 18–27/.45/5%, 26–38/.30/4%, 17–25/.50/5%, 18–28/.40/6%, 14–20/.60/8%, and 19–28/.42/5% respectively. Generic intrinsic tiers continue to scale these identities above level one. Stable type-to-icon mapping is authoritative; a missing icon warns rather than silently displaying a different weapon.
