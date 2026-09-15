# Black-Cube stat and affix audit

## Step 12.5 live itemization delta

The 120 serialized stat IDs and Step 10 historical identity count remain stable. `AffixDefinition`/`AffixTier` support variable authored tier counts, paired damage endpoints and explicit Prefix/Suffix side metadata; T1 is the strongest tier. Step 12.5G equipment rolls exactly one permanent implicit from any legal item-type family, independent of explicit side/group occupancy. The implicit may repeat an explicit family; explicits may not repeat each other. Natural equipment has Normal 0, Magic 2 (1/1), Rare 4 (2/2), Legendary 6 (3/3) explicits, plus the implicit. Guaranteed weapon bases remain outside those counts. Crafting permits under-filled equipment and never mutates the implicit. Direct ordinary PoE analogue families use the researched slot/gate/range table in [POEDB_AFFIX_BASELINE.md](POEDB_AFFIX_BASELINE.md), while other Black-Cube families keep authored rows until Step 13. Newly rolled flat weapon damage has independent low/high endpoints; schema-2 scalars migrate to X–X, schema-3 locked-original rolls become schema-4 implicits without value changes, and historical over-cap gear carries a legacy marker. The ordinary Poison-named passive branch grants Void direct damage, while Poison Chance remains ailment-specific. Poison stacks schedule on global combat turns, whereas Bleed/Ignite schedule on afflicted-actor turns. Fresh Step 12.5G verification is 232/232 EditMode, 1,312 zero-error itemization cases, and a successful strict Windows build/standalone startup; see [PROJECT_STATE.md](PROJECT_STATE.md) for the exact reports and isolated-checkout caveat.

> **Step 10 delta with historical Step 9 inventory retained below:** The original matrices and proposed triage describe the `d658dc67` baseline and must not be read as current rollability. The current counters/policies are in this new section. `GAME_DESIGN_CONTRACT.md` governs approved intent; `PROJECT_STATE.md` reports current verification limits.

## Step 10 current reachability and consumer reconciliation

| Measure | Current source-level count | Meaning |
|---|---:|---|
| Stable IDs / serialized definitions | **120 / 120** | Contiguous numeric identities 0–119, six new Void IDs append at 114–119 |
| Pooled definitions with tier(s) | **107** | Three guaranteed intrinsic weapon bases and **104 random affixes**; no pooled zero-tier definition |
| Non-generatable definitions | **13** | Seven deprecated (`3,51,53,80,82,84,103`) and six internal/passive-only (`108–113`) |
| Enemy-only excluded subset | **16** | Player-facing Mana/resource scalers, Life on Kill and seven active-skill levels are filtered from enemy candidates (but remain valid player rolls) |

The exact pool count was obtained from distinct `StatTypes` entries in `GearStatLists.BuildDefaultStatPools`; asset records/tier reachability were checked against all 120 `ModDatabase.asset` definitions. Current equipment generation and all six ordinary crafting mutations use the same pool/definition selection; the Codex Mod List reads these same definitions and slot-relative tiers rather than a second documentation table. Removed IDs remain deserializable and a legacy implicit remains protected. Advanced pickup-filter choices derive from obtainable player pools rather than all enum labels; existing saved deprecated selections still deserialize, and mismatch auto-dismantling is unchanged.

Removed from *new* v1 generation: Accuracy, flat/increased Evasion, Block, Cooldown Recovery, Mana Cost and the accuracy-per-Dex derivative. Minion and Projectile Speed are internal/dormant. Poison is no longer a generated direct weapon base; its ailment-related affixes remain active. The newly added Void family mirrors elemental slot/gate/tier parity. Seven skill-level affixes use intentionally **one** tier (+1, item level 40, weight 5), not five fabricated tiers; Mana scalers have five seeded singleton tiers. Internal passive Projectile Amount now affects Fireball but remains non-rollable on gear.

Every one of the **104 currently random-generatable player affix identities** has a source-level gameplay consumer: damage/ailments/defenses/resources, the dedicated query-time attribute derivation, seven active-skill mappings, mana snapshot scaling, or the credited kill pipeline. Enemy candidate generation excludes its player-only subset before weighting, and optimizer snapshots use the full current enum length rather than a hard-coded 108/113 ceiling. Void direct hits, Poison-as-Void ticks, Shock, Chill, max resistance, enemy life and Hit Twice have matching enemy/player structural paths where relevant. Fresh Unity 6000.6.0f1 EditMode verification ran **172/172 passing**, including **29 Step 10** fixture cases; four synchronous Play checks, rich menu/load, six-entry lifecycle/Pause, references, strict Windows build and headless standalone startup passed. Structural reachability plus these tests is not final combat balance or an exhaustive run through every possible generated item permutation.

The historical Step 9 tables below preserve their precise old evidence and discrepancy decisions for comparison; their `N`, zero-tier and rollability cells are not Step 10 current-state claims.

## Step 11 + 12 scaling/measurement delta

Enemy intrinsic Life now starts from the authored prefab seed and grows through `EnemyScalingProfile`/`EnemyScalingMath` **before** the unchanged gear pools and affix rolls. Existing flat Life, Life%, Strength-derived Life, Armour%, ordinary/max resistance, Void, Hit Twice, ailments and optimizer projection remain equipment/stat consumers; no new stable stat ID, affix identity, tier, item-level gate or gear-definition multiplier was added. `EnemyBuildOptimizer` receives the scaled baseline and the separate intrinsic outgoing-damage factor, so its candidate comparison remains structurally aware of the Step 10 mechanics. `BalanceSimulationRunner` measures actual rolled gear/build distributions and records diversity questions for Step 13; its synthetic reference player is not a player affix or balance rule. The fresh 184/184 EditMode suite includes the scaling/optimizer parity and seeded-lab cases; generated distribution measurements are not final balance. The following Step 9 triage tables remain historical, including stale phrases that anticipated enemy-Life wiring in Step 11.

## Historical Step 9 inventory (sections 1–15; not current rollability)

## 1. Scope, method, and exact counts

This audit traced every stable `StatTypes` value through `StatsComponent`, modifier math, slot pools, `ModDatabase`, `ModManager`, gear projection, player/enemy combat, ailments/statuses, passives/keystones, skills, UI/filtering, crafting, optimizer scoring, persistence DTOs, and current tests. A display label or serialized definition was not treated as a gameplay consumer.

| Measure | Exact Step 9 count | Meaning |
|---|---:|---|
| Stable `StatTypes` IDs | **114** | Explicit, contiguous numeric IDs `0–113` |
| Serialized `AffixDefinition` records | **114** | One record per stable stat ID |
| Stats named in at least one `GearStatLists` pool | **108** | Includes three intrinsic weapon bases and eleven zero-tier records |
| Actually generatable definitions | **97** | **3 intrinsic weapon bases + 94 random affixes** |
| Pooled but structurally unreachable | **11** | Present in a slot pool, but have zero tiers and are skipped by `RollSingleMod` |
| Internal/non-rollable definitions | **6** | IDs `108–113`; intentionally absent from gear pools and have zero tiers |
| Serialized tier rows | **485** | 97 definitions × 5 tiers |

“Rollable affix count” in this audit means a definition that `ModManager` can actually generate now. The broader pool-listed count is retained separately because it exposes eleven dead selections in the advanced pickup filter.

## 2. Triage definitions and totals

| Code | Meaning | Count |
|---|---|---:|
| **A — KEEP, WORKING** | Intended for v1 and has a meaningful current runtime consumer | **56** |
| **B — KEEP, NEEDS IMPLEMENTATION** | Retained/likely retained, but incomplete or asymmetric | **21** |
| **C — REMOVE FROM V1** | Proposed removal from future roll pools; preserve stable IDs/data for save safety | **31** |
| **I — INTERNAL / NON-ROLLABLE** | Not a gear affix; consumer is assessed independently | **6** |
| **Total** | Exactly one primary classification per stable ID | **114** |

The C classification is a Step 9 recommendation, not an applied design decision. For unresolved families it means “recommended removal if the user accepts the decision.”

## 3. Shared conventions used by the matrices

- Slots: `W` Weapon, `H` Helmet, `C` Body Armour, `G` Gloves, `Bt` Boots, `A` Amulet, `R` Ring, `B` Belt, `—` none.
- Sources: `B` base setup, `G` generated gear, `P` passive tree, `K` keystone, `S` active skill/runtime context. Relics use a separate modifier model and do not directly feed `StatTypes`.
- Consumer cells: `Y` meaningful, `N` none, `Partial` state or only part of the formula exists, `Mis` optimizer models a value the enemy runtime does not receive.
- Units: `%pt` means stored raw percentage points and returned by `GetStat` as a fraction; `flat` is unscaled; `count` is an exact count/fractional count; `sec/turn` is a raw duration or interval modifier.
- Every gear-generated modifier is currently a `StatOp.Additive` modifier. Percentage-classified buckets divide the final raw total by 100. `*Mult` buckets then compound independently in `StatValue`; their serialized values are still percentage points.
- All stats default to zero unless initialized by player/enemy setup or supplied by an item. Player base Life/Mana are 1000/100; enemy `StatsComponent.Life` mirrors prefab max life but actual enemy maximum health still comes from `HealthComponent`. Weapon base values come from intrinsic item rolls.
- Every generated affix is unique by `StatTypes` on one item. Group exclusion code exists, but **all 114 serialized `groups` arrays are empty**, so there are no configured cross-stat exclusions.
- UI column `Y` means friendly-name fallback and numeric formatting exist. It does not mean the mechanic works. Item tooltips add `%` using `StatsComponent.IsPercentStat`; local weapon effects are included in the weapon summary but individual mod lines do not explicitly say “local.”
- All gear/mod identities and enum numeric IDs are schema-2 persistence relevant.

## 4. Master stat matrix

The user-facing name is shown after the stable enum name. `G0` means pool-listed but zero-tier/unreachable. The player/enemy columns describe gameplay consumption, not mere storage.

| ID | Stat / display | Unit | Sources; gear slots | Player | Enemy | Optimizer | UI | Triage; current reality / proposed v1 |
|---:|---|---|---|---|---|---|---|---|
| 0 | `WeaponBaseDmg` / Weapon Damage | flat | G; W intrinsic | Y | Y | Y | Y | **A**; local weapon base |
| 1 | `WeaponBaseAttackSpeed` / Weapon Attack Speed | flat attacks/s | G; W intrinsic | Y | Y | Y | Y | **A**; local weapon base |
| 2 | `WeaponBaseCrit` / Weapon Base Critical Chance | %pt chance | G; W intrinsic | Y | Y | Y | Y | **A**; local weapon base |
| 3 | `ChanceToBlock` / Chance to Block | %pt chance | G; C,B | N | N | N | Y | **C**; no mitigation/prevention rule |
| 4 | `FlatPhys` / Added Physical Damage | flat | G; W | Y | Y | Y | Y | **A**; local only on matching Physical weapon |
| 5 | `FlatCold` / Added Cold Damage | flat | G; W | Y | Y | Y | Y | **A**; local only on matching Cold weapon |
| 6 | `FlatLight` / Added Lightning Damage | flat | G; W | Y | Y | Y | Y | **A**; local only on matching Lightning weapon |
| 7 | `FlatFire` / Added Fire Damage | flat | G; W | Y | Y | Y | Y | **A**; local only on matching Fire weapon |
| 8 | `GenericDmg` / Increased Damage | %pt increased | G; W,H,C,G,Bt,A,R | Y | Y | Y | Y | **A** |
| 9 | `GenericMult` / More Damage | %pt more | G; W,H,C,G,Bt,A,R | Y | Y | Y | Y | **A** |
| 10 | `GenericDotMult` / Damage Over Time Multiplier | %pt more | G; W,H,C,G,Bt,A,R | Y | Y | Y | Y | **A** |
| 11 | `CritChance` / Increased Critical Strike Chance | %pt increased | G; W,H,G,Bt,A,R | Y | Y | Y | Y | **A**; local on weapon, global elsewhere |
| 12 | `CritMult` / Critical Strike Multiplier | %pt | G; W,G,A | Y | Y | Y | Y | **A**; percent-classified, despite stale older comments |
| 13 | `BaseCritChance` / Added Base Critical Chance | %pt chance | G; W,G,A | Y | Y | Y | Y | **A**; local on weapon, global elsewhere |
| 14 | `PhysDmg` / Increased Physical Damage | %pt increased | G,P; W,H,G,A | Y | Y | Y | Y | **A**; local on Physical weapon |
| 15 | `ColdDmg` / Increased Cold Damage | %pt increased | G,P; W,G,Bt,A,R | Y | Y | Y | Y | **A**; local on Cold weapon |
| 16 | `LightDmg` / Increased Lightning Damage | %pt increased | G,P; W,Bt,A,R | Y | Y | Y | Y | **A**; local on Lightning weapon |
| 17 | `FireDmg` / Increased Fire Damage | %pt increased | G,P; W,H,A,R | Y | Y | Y | Y | **A**; local on Fire weapon |
| 18 | `PhysMult` / More Physical Damage | %pt more | G; W,H,G,A | Y | Y | Y | Y | **A** |
| 19 | `ColdMult` / More Cold Damage | %pt more | G; W,G,Bt,A,R | Y | Y | Y | Y | **A** |
| 20 | `LightMult` / More Lightning Damage | %pt more | G; W,Bt,A,R | Y | Y | Y | Y | **A** |
| 21 | `FireMult` / More Fire Damage | %pt more | G; W,H,A,R | Y | Y | Y | Y | **A** |
| 22 | `PhysPenetration` / Physical Penetration | %pt | G; W,H,G,A | Y | Y | Y | Y | **A** |
| 23 | `ColdPenetration` / Cold Penetration | %pt | G; W,G,Bt,A,R | Y | Y | Y | Y | **A** |
| 24 | `LightPenetration` / Lightning Penetration | %pt | G; W,Bt,A,R | Y | Y | Y | Y | **A** |
| 25 | `FirePenetration` / Fire Penetration | %pt | G; W,H,A,R | Y | Y | Y | Y | **A** |
| 26 | `PoisonDmg` / Increased Poison Damage | %pt increased | G,P; W,G,Bt,A | Y | Y | Y | Y | **A** |
| 27 | `IgniteDmg` / Increased Ignite Damage | %pt increased | G; W,H,A | Y | Y | Y | Y | **A** |
| 28 | `BleedDmg` / Increased Bleed Damage | %pt increased | G; W,H,G,A | Y | Y | Y | Y | **A** |
| 29 | `PoisonMult` / More Poison Damage | %pt more | G; W,G,Bt,A | Y | Y | Y | Y | **A** |
| 30 | `IgniteMult` / More Ignite Damage | %pt more | G; W,H,A | Y | Y | Y | Y | **A** |
| 31 | `BleedMult` / More Bleed Damage | %pt more | G; W,H,G,A | Y | Y | Y | Y | **A** |
| 32 | `PoisonChance` / Chance to Poison | %pt chance | G,P,K; W,G,Bt,A | Y | Y | Y | Y | **A**; overflow applications supported |
| 33 | `IgniteChance` / Chance to Ignite | %pt chance | G,P,K; W,H,A | Y | Y | Y | Y | **A**; overflow applications supported |
| 34 | `BleedChance` / Chance to Bleed | %pt chance | G,P,K; W,H,G,A | Y | Y | Y | Y | **A**; overflow applications supported |
| 35 | `PoisonTickRate` / Poison Speed | flat interval delta | G; W,G,Bt,A | Y | Y | Y | Y | **A** |
| 36 | `IgniteTickRate` / Ignite Speed | flat interval delta | G; W,H,A | Y | Y | Y | Y | **A** |
| 37 | `BleedTickRate` / Bleed Speed | flat interval delta | G; W,H,G,A | Y | Y | Y | Y | **A** |
| 38 | `PoisonDuration` / Poison Duration | flat turns | G; W,G,Bt,A | Y | Y | Y | Y | **A** |
| 39 | `IgniteDuration` / Ignite Duration | flat turns | G; W,H,A | Y | Y | Y | Y | **A** |
| 40 | `BleedDuration` / Bleed Duration | flat turns | G; W,H,G,A | Y | Y | Y | Y | **A** |
| 41 | `PoisonPenetration` / Poison Penetration | %pt | G; W,G,Bt,A | Y | Y | Y | Y | **A** |
| 42 | `IgnitePenetration` / Ignite Penetration | %pt | G; W,H,A | Y | Y | Y | Y | **A** |
| 43 | `BleedPenetration` / Bleed Penetration | %pt | G; W,H,G,A | Y | Y | Y | Y | **A** |
| 44 | `ShockChance` / Chance to Shock | %pt chance | G,P,K,S; W,Bt,R | Partial | Partial | N | Y | **B**; status exists, intended threshold effect absent |
| 45 | `ChillChance` / Chance to Chill | %pt chance | G,P,K,S; W,Bt,R | Partial | Partial | N | Y | **B**; status exists, speed effect absent |
| 46 | `ShockEffect` / Shock Effect | %pt | G; W,Bt,R | N | N | N | Y | **B**; stored affix has no effect formula |
| 47 | `ChillEffect` / Chill Effect | %pt | G; W,Bt,R | N | N | N | Y | **B**; stored affix has no speed formula |
| 48 | `ShockDuration` / Shock Duration | flat turns | G; W,Bt,R | Partial | Partial | N | Y | **B**; lifetime only |
| 49 | `ChillDuration` / Chill Duration | flat turns | G; W,Bt,R | Partial | Partial | N | Y | **B**; lifetime only |
| 50 | `FlatArmour` / Added Armour | flat | G,B; H,C,G,Bt | Y | Y | Y | Y | **A** |
| 51 | `FlatEvasion` / Added Evasion | flat | G; H,C,G,Bt | N | N | N | Y | **C**; no hit-resolution consumer |
| 52 | `ArmourPercent` / Increased Armour | %pt increased | G,P; H,C,G,Bt | Y | Y | Y | Y | **A** |
| 53 | `EvasionPercent` / Increased Evasion | %pt increased | G; H,C,G,Bt | N | N | N | Y | **C** |
| 54 | `ColdRes` / Cold Resistance | %pt | G; H,C,G,Bt,A,R,B | Y | Y | Y | Y | **A** |
| 55 | `LightRes` / Lightning Resistance | %pt | G; H,C,G,Bt,A,R,B | Y | Y | Y | Y | **A** |
| 56 | `FireRes` / Fire Resistance | %pt | G,B; H,C,G,Bt,A,R,B | Y | Y | Y | Y | **A** |
| 57 | `AllRes` / All Elemental Resistance | %pt | G; H,C,R,B | Y | Y | Y | Y | **A** |
| 58 | `MaxColdRes` / Maximum Cold Resistance | %pt cap | G; H,C | N | N | N | Y | **B**; fixed ±90% calculator clamp ignores it |
| 59 | `MaxLightRes` / Maximum Lightning Resistance | %pt cap | G; H,C | N | N | N | Y | **B** |
| 60 | `MaxFireRes` / Maximum Fire Resistance | %pt cap | G; H,C | N | N | N | Y | **B** |
| 61 | `MaxAllRes` / Maximum All Resistance | %pt cap | G; C,B | N | N | N | Y | **B** |
| 62 | `PoisonRes` / Poison Resistance | %pt | G; H,C,Bt | Y | Y | Y | Y | **A** |
| 63 | `IgniteRes` / Ignite Resistance | %pt | G; H,C,Bt | Y | Y | Y | Y | **A** |
| 64 | `BleedRes` / Bleed Resistance | %pt | G; H,C,Bt | Y | Y | Y | Y | **A** |
| 65 | `ShockRes` / Shock Resistance | %pt | G; H,C,Bt | N | N | N | Y | **B**; not consulted during application |
| 66 | `ChillRes` / Chill Resistance | %pt | G; H,C,Bt | N | N | N | Y | **B**; not consulted during application |
| 67 | `AllAilmentRes` / All Ailment Resistance | %pt | G; C | Y | Y | Y | Y | **A** for damaging ailments; no Shock/Chill resistance effect |
| 68 | `Life` / Added Life | flat | B,G; H,C,G,Bt,A,R,B | Y | N | Mis | Y | **B**; optimizer values enemy life that runtime ignores |
| 69 | `Mana` / Added Mana | flat | B,G; H,C,G,Bt,A,R,B | Y | N | N | Y | **B**; dead on enemy gear |
| 70 | `LifePercent` / Increased Life | %pt increased | G,P; C | Y | N | Mis | Y | **B**; enemy max-health mismatch |
| 71 | `ManaPercent` / Increased Mana | %pt increased | G,P; C,Bt | Y | N | N | Y | **B**; dead on enemy gear |
| 72 | `LifeRegeneration` / Life Regeneration | flat/turn | G,P; H,C | Y | Y | Y | Y | **A** |
| 73 | `ManaRegeneration` / Mana Regeneration | flat/turn | G,P; H,C,Bt | Y | N | N | Y | **B**; dead on enemy gear |
| 74 | `LifeOnHit` / Life on Hit | flat/hit | G; C | Y | Y | Y | Y | **A** |
| 75 | `ManaOnHit` / Mana on Hit | flat/hit | G; C,Bt | Y | N | N | Y | **B**; dead on enemy gear |
| 76 | `LifeOnKill` / Life on Kill | flat/kill | G; C | N | N | N | Y | **B**; death/reward pipeline never reads it |
| 77 | `ManaOnKill` / Mana on Kill | flat/kill | G; C,Bt | N | N | N | Y | **B** |
| 78 | `DmgPerMaxMana` / Damage per Maximum Mana | other | G0; Bt | N | N | N | Y | **C**; no tiers and no consumer |
| 79 | `DmgPerCurrentMana` / Damage per Current Mana | other | G0; Bt | N | N | N | Y | **C**; no tiers and no consumer |
| 80 | `ManaCost` / Mana Cost | flat | G0; Bt,R | Partial | N | N | Y | **C**; positive raw value would increase cost; unreachable |
| 81 | `AttackSpeed` / Increased Attack Speed | %pt increased | B,G,P; W,G,A,R | Y | Y | Y | Y | **A**; local on weapon, global elsewhere |
| 82 | `Accuracy` / Accuracy | flat | G; G,A,R | N | N | N | Y | **C**; no hit-resolution consumer |
| 83 | `ChanceToHitTwice` / Chance to Hit Twice | %pt chance | G,P,K; W,A,B | Y | N | N | Y | **B**; dead enemy roll |
| 84 | `CooldownRecovery` / Cooldown Recovery | %pt | G0; G,Bt,A,R | N | N | N | Y | **C**; no tiers and no cooldown system |
| 85 | `Plus1Phys` / +1 Physical Skill Level | flat level | G0; A | N | N | N | Y | **C**; no tiers or skill-level model |
| 86 | `Plus1Fire` / +1 Fire Skill Level | flat level | G0; A | N | N | N | Y | **C** |
| 87 | `Plus1Cold` / +1 Cold Skill Level | flat level | G0; A | N | N | N | Y | **C** |
| 88 | `Plus1Light` / +1 Lightning Skill Level | flat level | G0; A | N | N | N | Y | **C** |
| 89 | `Plus1Poison` / +1 Poison Skill Level | flat level | G0; A | N | N | N | Y | **C** |
| 90 | `Plus1Bleed` / +1 Bleed Skill Level | flat level | G0; A | N | N | N | Y | **C** |
| 91 | `Plus1Ignite` / +1 Ignite Skill Level | flat level | G0; A | N | N | N | Y | **C** |
| 92 | `Strength` / Strength | flat | G; H,C,G,Bt,A,R,B | N | N | N | Y | **C**; stored/displayed only |
| 93 | `Intelligence` / Intelligence | flat | G; H,C,G,Bt,A,R,B | N | N | N | Y | **C** |
| 94 | `Dexterity` / Dexterity | flat | G; H,C,G,Bt,A,R,B | N | N | N | Y | **C** |
| 95 | `StrengthPercent` / Increased Strength | %pt increased | G; H,C,B | N | N | N | Y | **C** |
| 96 | `IntelligencePercent` / Increased Intelligence | %pt increased | G; C,Bt,B | N | N | N | Y | **C** |
| 97 | `DexterityPercent` / Increased Dexterity | %pt increased | G; C,G,B | N | N | N | Y | **C** |
| 98 | `LifePerStrength` / Life per Strength | flat/attribute | G; H,C,B | N | N | N | Y | **C** |
| 99 | `DamagePerStrength` / Damage per Strength | %pt/attribute | G; H,C,B | N | N | N | Y | **C** |
| 100 | `ManaPerIntelligence` / Mana per Intelligence | flat/attribute | G; C,Bt,B | N | N | N | Y | **C** |
| 101 | `DoTMultPerIntelligence` / DoT Multiplier per Intelligence | %pt/attribute | G; C,Bt,B | N | N | N | Y | **C** |
| 102 | `AttackSpeedPerDexterity` / Attack Speed per Dexterity | %pt/attribute | G; C,G,B | N | N | N | Y | **C** |
| 103 | `AccuracyPerDexterity` / Accuracy per Dexterity | flat/attribute | G; C,G,B | N | N | N | Y | **C** |
| 104 | `FlatFirePerStrength` / Added Fire Damage per Strength | flat/attribute | G; B | N | N | N | Y | **C** |
| 105 | `FlatLightPerIntelligence` / Added Lightning Damage per Intelligence | flat/attribute | G; B | N | N | N | Y | **C** |
| 106 | `FlatColdPerDexterity` / Added Cold Damage per Dexterity | flat/attribute | G; B | N | N | N | Y | **C** |
| 107 | `DmgPerLowestStat` / Damage per Lowest Attribute | %pt/attribute | G; B | N | N | N | Y | **C** |
| 108 | `UnarmedDamage` / Unarmed Damage | flat | B; — | Y (base 0) | N | N/A | Y | **I**; working fallback infrastructure, inactive by default |
| 109 | `MagicDmg` / Increased Magic Damage | %pt increased | P; — | Y | N | N | Y | **I**; passive-only scoped multiplier works |
| 110 | `ProjectileDmg` / Increased Projectile Damage | %pt increased | P; — | Y | N | N | Y | **I**; passive-only scoped multiplier works |
| 111 | `MinionDmg` / Increased Minion Damage | %pt increased | —; — | framework only | framework only | N | Y | **I**; no minion entity/attack system; defer from v1 |
| 112 | `ProjectileAmount` / Projectile Amount | count | P,K; — | N | N | N | Y | **I**; retained passive projection, launcher still emits one projectile |
| 113 | `ProjectileSpeed` / Projectile Speed | %pt | —; — | N | N | N | Y | **I**; no source or consumer; defer from v1 |

## 5. Master generatable-affix tier matrix

This table contains every currently generatable definition. Join it to the master matrix by stable ID for consumers, UI, optimizer, persistence, and triage. All rows have item-level gates **1/20/40/60/75**, tier weights **50/35/20/10/5**, empty exclusion groups, additive modifier operation, same-stat duplicate prevention, and five distinct tier indices. Values are T1→T5 raw serialized ranges.

Common structural behavior for every row: either player or enemy gear may generate it because both use the same pools; the first non-intrinsic generated affix is locked/original; generic crafting can add/reroll/remove unlocked rows subject to rarity count, item level, slot pool, used-stat and group checks. The three intrinsic rows `0–2` are always reserved on weapons, do not count toward crafting mod limits, and cannot be crafting targets.

| ID | Affix | Slots | T1 / T2 / T3 / T4 / T5 | Scope |
|---:|---|---|---|---|
| 0 | Weapon Damage | W | 26–54 / 51–106 / 101–209 / 198–412 / 390–810 | intrinsic local |
| 1 | Weapon Attack Speed | W | .45–.75 / .6–.9 / .75–1.05 / .9–1.2 / 1.05–1.4 | intrinsic local |
| 2 | Weapon Base Critical Chance | W | 5–10 / 5–10 / 5–10 / 5–10 / 5–10 | intrinsic local; non-progressing ranges |
| 3 | Chance to Block | C,B | 5–10 / 7–15 / 11–23 / 16–34 / 24–51 | global, dead |
| 4–7 | Added Physical/Cold/Lightning/Fire Damage | W | 5–11 / 10–21 / 20–42 / 40–82 / 78–162 | local only when element matches; otherwise global projection |
| 8,11,14–17,26–28 | Increased damage family | see master | 7–14 / 11–24 / 19–40 / 32–67 / 55–113 | increased/global except matching local weapon element |
| 9 | More Damage | see master | 1–3 / 2–4 / 3–6 / 5–10 / 7–15 | global more |
| 10 | Damage Over Time Multiplier | see master | 3–7 / 5–11 / 8–17 / 12–26 / 20–40 | global more |
| 12 | Critical Strike Multiplier | W,G,A | 8–18 / 13–28 / 20–43 / 30–65 / 50–100 | global |
| 13 | Added Base Critical Chance | W,G,A | 1–1 / 1–2 / 1–2 / 2–3 / 2–4 | local on W, global elsewhere |
| 18–21,29–31 | Typed more-damage family | see master | 2–4 / 3–6 / 4–9 / 6–13 / 10–20 | global more |
| 22,24–25,41,43 | Penetration family | see master | 2–4 / 3–6 / 5–9 / 8–13 / 12–19 | global |
| 23 | Cold Penetration | W,G,Bt,A,R | 2–3 / 3–5 / 4–7 / 6–10 / 10–15 | global |
| 32–34 | Damaging-ailment chance | see master | 14–28 / 21–44 / 33–69 / 52–109 / 82–170 | global; overflow stacks |
| 35–37 | Damaging-ailment speed | see master | 1–1 / 1–2 / 1–3 / 2–5 / 3–7 | global raw interval modifier |
| 38–40 | Damaging-ailment duration | see master | 1–1 / 1–1 / 1–1 / 1–2 / 1–2 | global turns |
| 42 | Ignite Penetration | W,H,A | 3–5 / 4–8 / 7–12 / 10–17 / 16–25 | global |
| 44–45 | Shock/Chill chance | W,Bt,R | 7–14 / 10–20 / 14–28 / 19–40 / 27–57 | global, incomplete |
| 46–47 | Shock/Chill effect | W,Bt,R | 3–7 / 5–10 / 6–13 / 9–19 / 13–27 | global, no effect consumer |
| 48–49 | Shock/Chill duration | W,Bt,R | 1–1 / 1–1 / 1–1 / 1–2 / 1–2 | lifetime only |
| 50–51 | Added Armour/Evasion | H,C,G,Bt | 26–54 / 41–85 / 64–132 / 100–207 / 156–324 | global; Evasion dead |
| 52–53 | Increased Armour/Evasion | H,C,G,Bt | 8–17 / 11–24 / 16–34 / 23–48 / 32–67 | global; Evasion dead |
| 54–56,62–66 | Single resistance | see master | 3–7 / 5–11 / 8–17 / 12–26 / 20–40 | global; Shock/Chill res dead |
| 57 | All Elemental Resistance | H,C,R,B | 2–4 / 3–6 / 4–8 / 6–11 / 8–16 | global |
| 58–60 | Single Maximum Resistance | H,C | 1–1 / 1–2 / 2–3 / 2–5 / 4–8 | global, dead |
| 61 | Maximum All Resistance | C,B | 1–1 / 1–2 / 1–3 / 2–4 / 3–5 | global, dead |
| 67 | All Ailment Resistance | C | 2–5 / 3–7 / 5–9 / 6–13 / 9–19 | global; only damaging ailments consume it |
| 68 | Added Life | see master | 120–250 / 237–492 / 466–967 / 917–1904 / 1804–3746 | global; enemy miswired |
| 69 | Added Mana | see master | 58–122 / 115–239 / 227–471 / 446–926 / 878–1822 | global; player only |
| 70 | Increased Life | C | 3–7 / 5–11 / 8–17 / 12–26 / 20–40 | global; enemy miswired |
| 71 | Increased Mana | C,Bt | 4–8 / 6–13 / 9–19 / 14–29 / 21–44 | global; player only |
| 72–73 | Life/Mana Regeneration | see master | 2–4 / 3–7 / 5–10 / 8–16 / 12–25 | global; mana player only |
| 74 | Life on Hit | C | 8–17 / 13–26 / 20–41 / 31–64 / 48–100 | global |
| 75 | Mana on Hit | C,Bt | 4–8 / 6–13 / 10–21 / 16–32 / 24–50 | global; player only |
| 76 | Life on Kill | C | 15–31 / 24–49 / 38–79 / 59–123 / 93–193 | global, dead |
| 77 | Mana on Kill | C,Bt | 7–15 / 12–24 / 19–39 / 29–61 / 46–95 | global, dead |
| 81 | Increased Attack Speed | W,G,A,R | 2–5 / 4–8 / 6–12 / 9–18 / 13–27 | local on W, global elsewhere |
| 82 | Accuracy | G,A,R | 7–14 / 11–24 / 19–40 / 32–67 / 55–113 | global, dead |
| 83 | Chance to Hit Twice | W,A,B | 1–2 / 2–4 / 3–6 / 5–9 / 7–13 | global; player only |
| 92–94 | Strength/Intelligence/Dexterity | see master | 3–6 / 6–12 / 11–22 / 20–40 / 36–72 | global, dead |
| 95–97 | Increased attribute | see master | 2–4 / 3–6 / 5–9 / 7–13 / 10–19 | global, dead |
| 98,100 | Life/Mana per attribute | see master | 1–2 / 2–3 / 3–5 / 4–7 / 6–10 | global, dead |
| 99 | Damage per Strength | H,C,B | .1–.2 / .15–.3 / .25–.45 / .35–.65 / .5–.9 | global, dead |
| 101 | DoT Multiplier per Intelligence | C,Bt,B | .05–.1 / .08–.15 / .12–.22 / .18–.32 / .25–.45 | global, dead |
| 102 | Attack Speed per Dexterity | C,G,B | .03–.06 / .05–.09 / .07–.13 / .1–.18 / .15–.25 | global, dead |
| 103 | Accuracy per Dexterity | C,G,B | .25–.5 / .4–.8 / .6–1.2 / .9–1.8 / 1.3–2.7 | global, dead |
| 104–107 | Per-attribute damage family | B | .05–.1 / .08–.16 / .13–.25 / .2–.4 / .3–.6 | global, dead |

The family rows above enumerate each member by its stable ID range/list and share exactly identical tier data. There are no multiple definitions feeding the same stat: the relationship is one stable ID → one definition. The eleven pooled zero-tier definitions are deliberately excluded from this “generatable” matrix and listed in section 10.

## 6. Equipment-slot pools

| Slot | Current pool | Findings |
|---|---|---|
| Weapon | Generic/typed hit damage and more multipliers; four flats; four penetration; Crit Mult; all Poison/Ignite/Bleed six-stat families; Shock/Chill chance/effect/duration; Hit Twice; three intrinsic bases; Crit Chance, Base Crit, Attack Speed | Intrinsic bases are guaranteed/reserved. Matching flat/damage, Crit and Attack Speed are local; others global. Element eligibility filters matching weapon damage/ailment families. |
| Gloves | Generic, Physical/Cold damage/more/penetration; three crit stats; Poison/Bleed families; three elemental resists; armour/evasion flat/%; Accuracy, Attack Speed, Cooldown; Life/Mana; attributes, Dex%, Attack Speed/Accuracy per Dex | Contains dead evasion, accuracy, cooldown and attribute families. |
| Helmet | Generic, Physical/Fire families; Crit Chance; Bleed/Ignite families; elemental/all res; armour/evasion; three max res; five ailment res; Life/Mana/regen; attributes, Str%; Life/Damage per Str | Contains dead evasion/max-res/attributes and incomplete Shock/Chill resistance. |
| Body Armour | Generic; elemental/all res; armour/evasion; all max-res; all ailment res; Block; Life/Mana, %, regen, on-hit/on-kill; all attributes/% and six derived scalers | Largest defensive/resource pool and largest dead-affix exposure. |
| Boots | Generic, Cold/Lightning hit families; Crit; Poison; Shock/Chill; armour/evasion; elemental and five ailment res; Life/Mana/Mana%/regen/on-hit/on-kill; Mana Cost and mana-damage scalers; Cooldown; attributes/Int%/Int scalers | Contains three zero-tier mana records plus cooldown and multiple dead families. |
| Amulet | All hit families; crit; all damaging ailments; Hit Twice; elemental res; Life/Mana; Cooldown/Attack Speed/Accuracy; seven +1 skill stats; attributes | Seven +1 and Cooldown are zero-tier; advanced filter nevertheless exposes them. |
| Ring | Generic/elemental hit families; Crit Chance; Shock/Chill; elemental/all res; Life/Mana/Mana Cost; Cooldown/Attack Speed/Accuracy; attributes | Mana Cost and Cooldown are zero-tier; other dead families remain generatable. |
| Belt | Elemental/all/max-all res; Life/Mana; all attributes/%; all nine derived scalers; Block and Hit Twice | Concentrated dead attribute pool; no generic damage family. |

No per-slot list contains a duplicate stat. No pool reference lacks an `AffixDefinition`. There are no configured exclusion groups, so unusual combinations are prevented only when they repeat the exact same stat. Definitions `108–113` are intentionally unreachable from all gear pools. Definitions `78–80` and `84–91` are unintentionally unreachable despite pool membership.

## 7. Player/enemy symmetry

| Family | Player roll/use | Enemy roll/use | Optimizer | Required reconciliation |
|---|---|---|---|---|
| Hit/crit/attack speed/penetration | roll + use | roll + use | values | None for mechanics; retain local/global regression coverage |
| Damaging ailments | roll + use | roll + use | values | None for mechanics |
| Armour/elemental/damaging-ailment resistance | roll + use | roll + use | values | None for mechanics |
| Life/Life% | roll + use | rolls; actual prefab `HealthComponent` ignores both | values nonexistent enemy benefit | Step 11 dependency: unify enemy maximum-life source, then retest optimizer |
| Mana/Mana%/regen/on-hit | roll + use | rolls; enemy has no mana/skill-cost loop | ignores | Filter resource affixes from enemy generation unless enemy mana is explicitly designed |
| Hit Twice | roll + use | rolls; no extra enemy attack | ignores | Implement approved symmetric enemy behavior or remove it from enemy candidates |
| Shock/Chill | rolls; stores/displays only | same | ignores | Implement locked identities after formulas are approved |
| Accuracy/evasion/block/max-res | rolls; no effect | same | ignores | Remove proposed families or approve and implement rules |
| Attributes/+skills/cooldown/kill resource | exposed as applicable; no useful consumer | same/no resource | ignores | Remove proposed families; implement kill recovery if retained |

Both sides draw from the same slot pools; there is no “player-only” generation filter. This is the root cause of mana affixes and player-only Hit Twice becoming dead enemy outcomes.

## 8. EnemyBuildOptimizer coverage

The optimizer explicitly or indirectly simulates weapon-local effective damage/crit/speed, Physical/Fire/Cold/Lightning hits, generic/typed increased and more multipliers, penetration, Poison/Ignite/Bleed, armour, elemental/damaging-ailment resistance, Life/Life%, life regeneration, and life on hit.

It ignores mana families, evasion/accuracy/block, max resistance, Shock/Chill, Hit Twice, cooldowns, +skill levels, attributes/derived scaling, and kill recovery. Those omissions mostly mirror dead runtime behavior and should not be patched with invented weights. Two material mismatches remain:

1. It values `Life`/`LifePercent`, but enemy runtime maximum life ignores the stat buckets.
2. It ignores enemy Hit Twice while enemy gear can generate the affix; runtime also ignores it.

Its snapshot array is sized through ID 108 (`UnarmedDamage`), so IDs 109–113 are not captured. They are currently non-rollable/passive-only and therefore do not corrupt current enemy gear selection, but any future enemy use must expand the snapshot safely.

## 9. Units and percentage consistency

- Raw affix values for percentages/chances are percentage points: `15` means 15%, and `GetStat` returns `0.15`. Tooltips correctly display the raw value with `%`.
- `CritMult` and penetration are percent-classified in current code. Older prose/comments describing them as raw fractions are stale. Combat and optimizer currently agree with percentage-point affix data.
- Intrinsic `WeaponBaseCrit`, additive `BaseCritChance`, ailment chances, resistances, Hit Twice and max-res values all use percentage points.
- `WeaponBaseAttackSpeed` is a flat attacks-per-second base. `AttackSpeed` is percentage points and is local increased speed on weapons, global elsewhere.
- Damage/ailment `*Mult` affixes use percentage points but are compounded as independent “more” modifiers, not added as raw multipliers.
- Tick-rate and duration affixes are raw flat values, despite “Speed” wording. Their sign/order semantics deserve a later formula review, but no observed double conversion exists.
- Per-attribute affixes serialize small decimal-looking raw values while lacking consumers. Their intended unit cannot be declared until the attribute model is approved.
- `ManaCost` is a raw flat addition; if positive tiers existed, the current consumer would increase cost rather than reduce it. It currently has zero tiers.
- No schema conversion is applied to persisted modifier values; save/load retains raw `value`, `tierIndex`, stable stat ID, and lock state.

## 10. Structurally invalid, dead, or misleading affixes

| Problem | Exact affected records | Impact |
|---|---|---|
| Pool-listed, zero tiers | `DmgPerMaxMana` (78), `DmgPerCurrentMana` (79), `ManaCost` (80), `CooldownRecovery` (84), `Plus1Phys`–`Plus1Ignite` (85–91) | Eleven advanced-filter choices can never match a generated drop; rolling silently skips them |
| Empty exclusion configuration | All 114 definitions | Cross-family exclusions do nothing; only exact-stat duplicates are prevented |
| Non-progressing intrinsic tier range | `WeaponBaseCrit` (2): 5–10 at every item-level gate | Structurally reachable but higher tiers provide no better range |
| Rollable with no runtime consumer | Block, evasion, accuracy, max-res, attributes/scalers, kill recovery; effect portions of Shock/Chill | Misleading gear power and dead enemy/player outcomes |
| Rollable with one-sided consumer | Life, Life%, mana family, Hit Twice | Dead or misvalued enemy outcomes |
| Filter overexposure | Advanced filter is the 108 pool IDs minus 3 intrinsic = 105 choices | Includes all eleven unreachable zero-tier records |
| Tooltip ambiguity | Random local weapon modifiers appear in the affix list without a “local” label | Effective weapon summary is correct, but scope is not explicit |
| Category anomaly | `ChanceToBlock` maps to the weapon-base category | Display grouping is misleading even though formatting works |

Tier gates are monotonic and tier indices/weights are present for all 97 generatable definitions. No slot pool references a missing definition, and no generatable definition has an unreachable tier at its stated item level.

## 11. Known incomplete mechanic families

- **Shock (B, locked intent):** status application/storage/display/expiry exist. General Shock does not accumulate to the contract’s threshold or trigger an additional Lightning hit. Lightning Strike instead converts attacker Shock Chance overflow directly into immediate extra hits, bypassing persisted target threshold state. Shock Effect and Shock Resistance have no consumer.
- **Chill (B, locked intent):** application/storage/display/expiry exist; `PlayerController.ApplyChill` and its counterpart effect path do not reduce attack speed. Cold-hit strength curve, cap and stacking remain unresolved. Chill Effect and Chill Resistance have no effective consumer.
- **Maximum resistance (B):** four rollable stats exist; `CombatCalculator` uses a fixed final ±90% reduction clamp. Wiring requires an approved default cap and ordering rule.
- **Enemy Hit Twice (B):** player basic/skill paths consume it; enemy turns and optimizer do not.
- **Life/resource symmetry (B):** enemy health ignores Life/Life%; enemy gear can roll player-only mana stats.
- **Kill recovery (B):** on-hit and regeneration work, but the death/reward pipeline never grants Life/Mana on Kill.
- **Projectile Amount (I, needs implementation if retained):** passives and Bullet Hell project a fractional count/damage tradeoff, but `SkillProjectile.Launch` emits one projectile. No shotgun/parallel/spread rule is approved.
- **Accuracy/evasion/block (C recommendation):** data/UI/pools exist; there is no hit or block resolution. No formula, cap, mitigation amount or ordering is authoritative.
- **Cooldown Recovery (C recommendation):** no active skill has cooldown state/timers/UI, and the definition has zero tiers.
- **Attributes and derived scaling (C recommendation):** 16 generatable stats store/display but no runtime system projects them.
- **Skill levels (C recommendation):** seven pool-listed definitions have zero tiers and active skills have no level model.
- **Minions (I/defer):** scoped damage calculation exists; no minion entity, ownership, command, attack or gear affix exists.
- **Void (no dedicated stat/affix):** enum/mask/display scaffolding exists, player weapon generation excludes it, no mitigation/skill/passive/optimizer identity is defined, and fallback mappings are unsafe if exposed. Keep it out of v1 content pending design.
- **Status callbacks (not a `StatTypes` family):** virtual apply/tick/expire/outgoing-damage hooks exist but current ticking does not dispatch them. Leave dormant or remove the unused extension surface; do not expose it as an affix.
- **Deep Freeze (not an affix):** keystone doubles Chill applications and carries a serialized zero-default maximum-effect-increase field. Because Chill has no speed effect, the latter has no realized mechanic and exact value remains unresolved.

## 12. Save compatibility

Schema 2 persists each rolled modifier’s numeric `StatTypes` identity, value, tier and locked/original state. Step 10 must not renumber enum values, reuse IDs, or casually delete definitions. Preferred removal is to exclude C-family IDs from future slot pools/filter choices while retaining enum/data recognition so existing saves deserialize. If old saves can contain removed-but-previously-generatable affixes, load should preserve them or run an explicitly approved migration; silently changing their identity is unsafe. Step 9 does not increment the save schema.

## 13. Proposed v1 decisions

| Family | Proposal | Reason |
|---|---|---|
| Shock/Chill | Keep and implement after threshold/strength equations are approved | Their identities are locked in the design contract |
| Max resistance | Keep, approve one shared cap model, wire both actors | Small coherent defensive family once rules are explicit |
| Enemy Hit Twice | Keep and implement symmetrically | Existing player mechanic and gear exposure make symmetry preferable |
| Kill recovery | Keep and wire once in canonical death ownership | Low implementation surface; clear distinction from on-hit/regen |
| Enemy Life/Life% | Keep; make enemy health consume stats in Step 11 | Optimizer and gear already expect this; base scaling owns the dependency |
| Enemy mana affixes | Exclude from enemy generation until an enemy mana model is approved | Avoid inventing enemy skills/resources during Step 10 |
| Accuracy/evasion | Remove from v1 pools/filter | Requires a coupled hit formula and broad rebalance; no approved behavior |
| Block | Remove from v1 pools/filter | No approved prevention/mitigation/order rule |
| Cooldown Recovery | Remove from v1 pools/filter | No approved cooldown system and zero-tier definition |
| Attributes/scalers | Remove from v1 pools/filter | Sixteen dead affixes require a large progression model before balance |
| +skill levels | Remove from v1 pools/filter | No level model; seven definitions are already unreachable |
| Mana damage scalers/Mana Cost | Remove from v1 pools/filter | No tiers; scalers are dead and positive Mana Cost has contrary semantics |
| Projectile Amount | Keep internal/passive-only; approve spawn semantics before implementation | It is promised by passive/keystone documentation, not currently a gear affix |
| Minion/Projectile Speed/Void | Keep dormant and non-rollable; defer beyond v1 unless separately approved | No complete gameplay ecosystems exist |
| Status callbacks | Leave dormant or remove unused hooks; no v1 affix work | Engineering extensibility is not player-facing design |

## 14. User decisions required before Step 10

Locked Shock threshold/extra-Lightning-hit identity, locked Chill attack-speed-reduction identity, and the approved passive/keystone structure are not being re-decided here. Exact Shock/Chill equations still require approval before implementation.

| Mechanic | Current state | Keep cost | Remove cost | Recommendation | User decision needed |
|---|---|---|---|---|---|
| Accuracy/Evasion | Four live affixes, no hit test | Formula, caps, actor/UI/optimizer/tests | Remove four IDs from future pools/filter | Remove from v1 | Keep system for v1, or remove exposure? |
| Block | One live affix, no effect | Define avoidance/mitigation/order and symmetry | Remove from pools/filter | Remove from v1 | Keep or remove; if keep, what does a block do? |
| Max resistance | Four live affixes, fixed ±90% clamp | Define default cap, max cap and ordering; wire both actors | Remove four from pools/filter | Keep and implement | Approve cap model or remove? |
| Cooldowns | Zero-tier pool entry; no timer | Full skill cooldown/UI/state design | Remove from pools/filter | Remove from v1 | Are cooldowns in v1? |
| Attributes/scaling | 16 live affixes, no consumers | Full attribute formulas, recalculation, UI, enemy/optimizer | Remove 16 from pools/filter | Remove from v1 | Are attributes in v1? |
| Skill levels | Seven zero-tier entries; no level model | Level curves for seven skills and UI/save rules | Remove from pools/filter | Remove from v1 | Are skill levels in v1? |
| Mana cost/scalers | Three zero-tier entries; one contrary raw consumer | Define cost reduction and mana-damage equations | Remove from pools/filter | Remove from v1 | Keep any of this family? |
| Kill recovery | Two live affixes, no death consumer | Wire canonical kill claimant and cap at max | Remove two from pools/filter | Keep and implement | Confirm recovery occurs on credited kill after death resolution |
| Enemy Hit Twice | Live affix; player-only use | Mirror overflow application in enemy turn and score it | Enemy-specific generation exclusion | Keep symmetric | Confirm enemies may hit twice |
| Projectile Amount | Passive/internal value; one projectile emitted | Define eligible skills, spread/parallel/shotgun and damage rules | Remove passive grants/keystone promise | Keep after semantics approval | Choose projectile-spawn semantics |
| Minions | Only scoped multiplier infrastructure | Entire entity/combat/content system | Leave dormant/non-rollable | Defer beyond v1 | Confirm defer |
| Void | Enum/mask scaffolding only | Full identity, offense, defense, content and optimizer | Continue excluding it | Defer beyond v1 | Confirm defer |
| Status callbacks | Uncalled virtual hooks | Define dispatch lifecycle and tests | Delete/leave dormant | Leave dormant | Retain engineering extension surface? |

## 15. Dependency-aware Step 10 backlog

1. **Approve decisions and formulas first:** resolve the table above; separately approve Shock threshold/consumption/extra-hit basis and Chill strength/cap/replace policy.
2. **Sanitize generation without breaking saves:** remove approved C IDs from `GearStatLists`, allowed-slot data and advanced-filter choices; preserve stable IDs/definitions; add structural tests that every pool-listed non-intrinsic stat has tiers and a v1 disposition.
3. **Fix actor eligibility:** add player/enemy generation eligibility so enemy gear cannot roll mana-only outcomes; retain shared pools where both actors consume stats.
4. **Implement locked ailments:** build one symmetric Shock threshold path and one symmetric Chill attack-speed modifier; wire effect, resistance, duration, status UI and tests; reconcile Lightning Strike without changing its approved numeric catalog.
5. **Implement retained small families:** enemy Hit Twice, Life/Mana on Kill, and approved max-res cap wiring; update optimizer only after runtime effects exist.
6. **Handle Projectile Amount:** after semantics approval, make projectile-capable skill spawning consume the internal count and verify Bullet Hell tradeoffs. Keep it non-rollable unless separately approved.
7. **Defer enemy maximum-life repair to Step 11 coordination:** unify `StatsComponent` Life/Life% with `HealthComponent` while adding base scaling, then correct optimizer validation.
8. **Regression and persistence:** load fixtures containing now-excluded IDs, run crafting/filter/tooltip/optimizer/combat tests, then run full EditMode, lifecycle/save smoke checks and Windows build in proportion to code changed.

No Step 10 mechanics or pool removals were performed by this audit.
