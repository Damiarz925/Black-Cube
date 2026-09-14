# Passive skill tree

`SkillTreeUI` generates the active tree at runtime from `PassiveTreeDefinition`: one player root, ten clockwise spokes, ten bridge branches, a forty-node statless outer travel ring, ten inner keystones, and ten outer keystones. The spoke order is Defense, Life, Mana, Magic, Lightning, Fire, Poison, Projectile, Physical, Cold. Each main or specialized branch ends in one new keystone connected directly to its terminal Large node.

The bridges fill each clockwise gap in the same order: Life Regeneration (Defense–Life), Chance to Hit Twice (Life–Mana), Mana Regeneration (Mana–Magic), Shock Chance (Magic–Lightning), Ignite Chance (Lightning–Fire), Poison Chance (Fire–Poison), Increased Projectile Amount (Poison–Projectile), Attack Speed (Projectile–Physical), Bleed Chance (Physical–Cold), and Chill Chance (Cold–Defense). Each bridge has two two-node approaches, a Large merge, five Medium nodes, and a terminal Large node. Allocation follows the undirected graph, so either approach reaches the merge.

Four Empty Travel nodes connect each neighboring pair of bridge terminals, forming a complete outer cycle. The ring leaves the left and right sides of each specialized terminal Large at radius 2700; the outer keystone projects radially beyond that terminal and remains a one-edge leaf. Ring travel never requires a keystone. These nodes cost one passive point and provide no stat bonus. A node may be refunded with right-click only when every allocation left behind still has a path to one of the ten root-connected spoke starts; a successful refund restores its point.

## Stable IDs and migration

All existing IDs remain stable. The reordered spokes keep their historic serialized slots: Poison 0–11, Life 12–23, Defense 24–35, Mana 36–47 (formerly Speed), Magic 48–59, Projectile 60–71, Cold 72–83, Fire 84–95, Lightning 96–107, and Physical 108–119. Bridge IDs are Increased Projectile Amount 120–130, Attack Speed 131–141 (formerly Projectile Speed), Bleed 142–152, Poison Chance 153–163, Chill 164–174, Ignite 175–185, Shock 186–196, Chance to Hit Twice 197–207, Life Regeneration 208–218, and Mana Regeneration 219–229. Empty Travel uses IDs 230–269. Inner keystones append IDs 270–279 in canonical spoke order.

Outer keystones append IDs 280–289 in this order: Open Wounds, Wildfire, Deep Freeze, Overcharged, Toxic Saturation, Undying Flesh, Endless Current, Frenzy, Bullet Hell, and Echoing Strikes. Older serialized arrays copy every available ID into the new 290-entry array and normalize values to binary allocations. Existing modifiers and keystone state are recomputed when allocations change or refund, preventing stale effects.

## Outer keystones

The specialized keystones implement doubled application chances, multiplicative damage tradeoffs, Bleed/Ignite stack-cap changes, doubled regeneration, maximum-resource penalties, Frenzy and Echoing Strikes hit penalties, and Overcharged shock-hit behavior. Bullet Hell exposes +2 projectiles as an explicit state field and applies its projectile damage penalty, while projectile spawning remains a placeholder. Deep Freeze applies its chance and effectiveness modifiers; its maximum-effect increase is serialized centrally as `deepFreezeMaximumEffectIncrease` and intentionally defaults to zero until balance supplies a value.

## Inner keystones

IDs 270–279 are Iron Bastion, Living Fortress, Mana Shield, Arcane Overload, Living Current, Infernal Conversion, Venomous Transmutation, Ballistic Barrage, Brute Force, and Absolute Zero. Their full effect text lives in `PassiveTreeDefinition`; `PassiveKeystoneState` projects allocations onto combat without mutating base stats. MORE/LESS factors multiply. Element keystones convert half of non-target hit damage, suppress the remainder, and apply their 25% more factor. Brute Force suppresses all non-Physical damage, doubles retained Physical damage, and applies 25% less attack speed. Mana Shield redirects half of post-mitigation damage to available mana with shortfall spilling to life. Arcane Overload doubles real skill mana costs. Venomous Transmutation suppresses the direct hit and applies its full transformed hit basis as Poison without double-counting. Ballistic Barrage's scoped damage behavior is active; projectile spawning remains the documented placeholder.

## Stat bindings

The spokes bind respectively to `ArmourPercent`, `LifePercent`, `ManaPercent`, `MagicDmg`, `LightDmg`, `FireDmg`, `PoisonDmg`, `ProjectileDmg`, `PhysDmg`, and `ColdDmg`. Attack Speed binds to `AttackSpeed`; Projectile Amount is flat; Life and Mana Regeneration are flat units per second; ailment and hit-twice branches use their named percentage-point stats. Empty Travel has no modifier.

## Art and interaction

Every family loads its supplied transparent image from `Assets/Resources/UI/PassiveTree`. Inner keystones load their ten canonical 1254×1254 images from the `Keystones` subfolder and preserve the complete source canvas/alpha bounds, rendered as larger 156px nodes. Per-tier ordinary crops remain centered and render at 58/78/108 UI sizes. Neutral artwork is unchanged; hover, allocated, and unavailable nodes use state tints. The central player emblem is always shown in its original colors.

Dragging pans the 7000-pixel canvas. Wheel zoom retains the old range and adds exactly two discrete steps between the former 0.19 minimum and the new 0.11 minimum. Opening the passive tree does not pause gameplay by default; the persisted Gameplay Options toggle can opt into pausing.
