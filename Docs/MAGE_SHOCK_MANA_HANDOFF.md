# Mage / Shock / Mana defense / Staff handoff

Branch: `codex/repository-cleanup-baseline`; starting HEAD: `dcb95f60`.
No commits created in this pass. Changes remain local. Nothing pushed; main untouched.
Pre-existing inventory/filter/prefab and user-owned ModDatabase changes are preserved.

## Shock

- Base effect 20%; ordinary maximum 100% per instance; normal maximum 1 instance, Storm Mage 3.
- Per-instance formula: `min(maximumEffect, 0.20 * (1 + increasedEffect)) * (1 - reducedEffect)`.
  Formula inputs are fractions; canonical raw percentage stats are percentage points.
- Relic increases to maximum effect remain supported, as do existing unique self-Shock interactions.
- Instances multiply damage taken: three 20% instances yield 1.728x; three 100% instances yield 8x.
- Base duration 3 game seconds, scaled by `(1 + increasedDuration)`, with a positive lower bound.
- Global turns no longer expire Shock. The obsolete five-stack burst API is removed.
- Direct damage and damaging ailments receive the multiplier. Separate badges show each
  instance's effect and remaining seconds.

## Shock Barrage

At cast resolution: sum existing instance strengths, consume them, then resolve
`1 + floor(sum / 0.20)` hits. Use the sum, not the multiplied damage-taken result.
No additional cap: consumed 20/40/60/100/150/300% yields 2/3/4/6/8/16 hits.

Consumed Shocks cannot amplify the first hit. Hits may rebuild Shock, and those new instances
can amplify later hits, but cannot alter the snapshotted hit count. Each Echo is a new cast
with a fresh snapshot and the existing escalating Mana cost.

Staff retains enabled/ready/affordable autocast attack replacement, existing skill priority,
5s base cooldown and 50 base Mana cost. Skill-level scaling and cooldown recovery remain.
Tooltip now shows the formula, consumption/reapplication rules, base damage per hit, cost,
cooldown and current target-dependent hits. The existing shared popup fan separates 16 hits.

## Tree changes

Mage retains five +21% Shock Chance choices: **105% total**. Its five recently added Life-on-Kill
choices become **7% Mana Before Life + 0.75% maximum-Life regeneration/sec each**:
**35% Mana defense and 3.75% Max Life/sec total**. No generic Mage Life-on-Hit/Life-on-Kill choices
remain. Other choices, INT/Mana scaling, stable IDs, topology and geometry are unchanged.

| Staff tier | Mana Before Life choice | Echo alternative |
| --- | ---: | ---: |
| 1 | 2% | 1% |
| 2 | 3% | 1% |
| 3 | 5% | 2% |
| 4 | 8% | 3% |
| 5 | 12% | 3% |
| 6 | 15% | 4% |
| 7 | 20% | 6% |
| Total | **65%** | **20%** |

These are mutually exclusive alternatives in each left-side choice slot, not simultaneous
bonuses. The third utility choice, offensive right side and spines remain. Existing Staff
route restrictions apply. Mage + dedicated defensive Staff investment reaches 100%.

## Mana defense

New canonical stable stat ID **146**, `DamageTakenFromManaBeforeLife`; no reused IDs.
Effective value clamps to 0–100%. Legacy Mana Shield joins this calculation, not a second split.

Ordering: normal mitigation and damage-taken modifiers (including Shock), then spend up to
`finalDamage * fraction` from available Mana, then apply all remaining damage to Life.
Physical/Fire/Cold/Lightning/Void and damaging ailments share this path. Insufficient Mana
spills into Life; at 100% with sufficient Mana, Life loses zero.

It is resource spending, not healing: no recovery triggers, Dark Priest conversion or hidden
Mana compensation. ManaChanged updates the bar immediately without extra floating numbers.
Death recap remains actual Life loss; simulator telemetry separately records Mana absorbed
and correctly apportions Life damage by type.

Wanderer Stats shows the clamped value under Defense; hovering its value expands the
explanation in the existing row. Shock stats distinguish base/increased/current/maximum effect,
maximum instances and lifetime. Save schema stays unchanged; passive IDs remain stable and
no new transient combat status persistence was added.

## Validation

Unity 6000.6.0f1 compiled the updated runtime and Editor scripts.

- Final targeted EditMode selection: **82 passed / 0 failed**, `Logs/MageShockTargeted.xml`.
- Wider related selection: **133 passed / 5 failed**, `Logs/MageShockFocusedFinal.xml`.
- Live BattleManager tests verify 16-hit consumption, removal of the old multiplier before
  damage, fixed hit count despite reapplication, and fresh subsequent-cast snapshots.
- Tests cover effect ladder, raised cap/reduction ordering, multiplication, seconds expiry,
  hit ladder, post-resistance and Poison Mana splits, shortage/full absorption, tree totals,
  Staff mode/cost/cooldown, simulator Life/type accounting and 16 distinct popup offsets.
- No broad balance/progression runs, optimizer execution or Windows build performed.
- Manual visual/play smoke **not performed**; checklist below remains for the user.

Five broader failures are outside this pass's new mechanics:

1. Barbarian authored regeneration differs from T1-derived generator expectation (0.285 vs 0.28).
2. Ranger authored choice differs from generator expectation (0 vs 8).
3. Mage existing automatic-icon node fails an unconditional custom-icon assertion.
4. Older Step18 Poison tick-count expectation differs from current real-time rules (5 vs 2).
5. Older subclass survey Poison extension expectation produces equal damage under current rules.

These failures are reported, not hidden or rebalanced here. The filter test setup was corrected
to account for Gear automatically marking its first modifier implicit. The old Staff Fireball
mode assertion was updated to the current attack-replacement contract.

## Manual smoke

1. Play a Lightning Staff Mage. Inspect the Defense/Shock stats and hover Mana defense.
2. With no increased effect, land a Shock: 20% badge, still active at 2.5s, expired at 3s.
3. Select Storm Mage: inspect three independent effect/duration badges; pause should freeze game time.
4. Enable Barrage: compare tooltip hits, watch old badges consumed and new ones rebuilt;
   check the repeated damage numbers fan apart.
5. Test 35% Mage defense, then 100% with deep Staff defense. Mana should fall immediately;
   depleted Mana should expose Life. Repeat with Poison/Bleed/Ignite ticks.
6. Test Echo alternatives: fresh Shock consumption and increased offensive/defensive Mana pressure.
   Starvation must not grant free casts or bypass cooldown/queue requirements.

## Files changed by this pass

Runtime (`Assets/Scripts`): MageCombatRules.cs (+ meta), BattleManager.cs, CombatCalculator.cs,
DamageReceiver.cs, PassiveKeystoneState.cs, StatusController.cs, StatusController.Display.cs,
UniqueCombatRuntime.cs, StatTypes.cs, StatsComponent.cs, PlayerSkillDefinition.cs,
PlayerStatsPanelUI.cs, StatRowUI.cs, StatCategoryMapping.cs, StatDisplayFormatting.cs,
StatusHUD.cs, WeaponSkillTooltip.cs, ProvisionalWeaponTreeContent.cs, CombatSimulationCore.cs,
EnemyBuildOptimizer.cs.

Editor (`Assets/Editor`): GenericClassPassiveReauthoring.cs, BalanceWorkbenchCombatTools.cs,
BalanceWorkbenchPlayerBuild.cs. Laboratory formulas were aligned, not run as a balance pass.

Data (`Assets/GameData/PassiveTree/Branches`): Class/SO_Mage_Branch.asset,
Weapon/SO_Staff_Branch.asset.

Tests (`Assets/Tests/Editor`): MageShockManaTests.cs (+ meta), Step10MechanicsTests.cs,
Step18ProductionTests.cs, BalanceWorkbenchTooling4Tests.cs, SubclassSurveyPreflightTests.cs,
GenericClassPassiveReworkTests.cs, earlier filter setup in PlaytestFollowUpTests.cs.

All changes are uncommitted. Earlier dirty files remain preserved; no unrelated prefab/UI/
ModDatabase edits were reverted. Unity's automatic SENTIS compiler-symbol removal was restored.
