# Developer map: Maintains these reviewed responsibility comments and the linked code map, checking token/AST identity for each application. The original documentation baseline is kept as historical evidence.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Apply the reviewed source map as comments and prove executable tokens unchanged.
This maintenance tool writes comments only; it does not run Unity or art installers.
The per-file descriptions also form Docs/CODE_MAP.md for navigation.
"""
from pathlib import Path
import ast, hashlib, io, json, re, tokenize

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = {
'PaperEnemyAnimationSet':'Enemy display name, rest/attack sprites and popup anchor shared by prefab and builder. Eight attack poses use index3 impact; this asset does not set boss role, health, loot or speed.',
'AffixDefinitions':'Serialized affix tiers, item-level gates and exclusion groups used by ModDatabase and ModManager. Tier generation needs a seeded first tier; tier indices are one-based.',
'AilmentCalculator':'Converts eligible pre-defense hit components into per-tick strength, tick count and global-turn interval. Generic hit scaling is already in the source hit and must not be applied twice.',
'BattleManager':'Owns combat clocks, target replacement and damage resolution. Attack speeds are attacks/second at threshold 100; sprite poses only visualize this clock and never cause damage.',
'BleedStatusEffect':'Bleed asset defaults and editor validation for physical-hit damage over time. Active stacks and ticking belong to StatusController, not this shared asset.',
'CombatCalculator':'Pure hit and DOT mitigation shared by both sides. Evasion has no hit roll here; physical resistance currently falls through StatMappings to FireRes, an existing limitation.',
'DamageContext':'Per-attack snapshot of typed damage components and critical metadata. The components are one attack, not separate combat turns; critical scaling is applied by the attacker.',
'DamageNumberAccent':'Draws the decorative UI geometry behind styled damage numbers. Presentation only: no damage calculation or timing authority.',
'DamagePopup':'Creates styled screen-space numbers from world-space targets; popup instances snapshot positions so target destruction does not move them. DamageReceiver has already applied life loss.',
'DamageReceiver':'Single entry point for already-mitigated damage and its popup. The strongest raw component chooses a mixed hit color; it does not split the life deduction into multiple hits.',
'DeathMenuUI':'Displays the killer and pauses via Time.timeScale; restart delegates to GameManager. Hide/menu/quit restore time scale so later scenes do not inherit a paused clock.',
'EnemyAI':'Builds enemy attack snapshots from generated gear and StatsComponent; BattleManager owns attack scheduling. Enemy rarity is randomly rolled separately from the HealthComponent boss role.',
'EnemyStatSetup':'Initial enemy stat buckets; AttackSpeed zero means zero increased speed, not disabled attacks. SetupForZone is a placeholder, and enemy maximum life comes from its HealthComponent prefab.',
'EquipmentGlyph':'Procedural UI silhouettes for gear slots when no icon art is needed. Used by inventory and equipped-item panels; independent of world weapon sprites.',
'EquipmentManager':'Transfers Gear between inventory and one slot per GearType, replacing source-owned global modifiers. Batches StatsChanged, then emits EquipmentChanged; weapon swaps also notify PlayerController.',
'EquipmentStatsUI':'Refreshes equipped-slot glyphs, rarity borders and hover targets from EquipmentChanged. This view does not own equipment or apply modifiers.',
'EquippedItemHoverUI':'Bridges an equipped slot to the shared InventoryUI tooltip. Equipped items use the protected tooltip mode that disables dismantling.',
'GameManager':'Session run coordinator: nine normal kills lead to stage10 boss, whose death advances the combat level. EncounterStage drives HUD numbering; death claims precede XP/loot/spawn callbacks.',
'Gear':'Runtime item state, scrap flags, local weapon values and global rolled modifiers. ApplyMods accumulates once during generation; local percentages are fractions, global rolls remain raw percentage points.',
'GearStatLists':'Authoritative stat pools per gear slot, shared by rolling and ModDatabase editor population. Adding a stat to the enum alone does not make it rollable.',
'HealthComponent':'Owns life, death and single-claim enemy rewards; death can synchronously cause a replacement encounter. Player life reads StatsComponent; enemy life uses serialized maxLife.',
'IgniteStatusEffect':'Ignite/Burn asset defaults and validation for fire-hit damage over time. Shared definition only; each target owns its runtime StatusInstance.',
'Inventory':'Session item ownership, pickup-only auto-scrap and one accumulated Scrap stack. Pickup applies filters once; Add returns equipped items without applying pickup filters.',
'InventoryFilterUI':'Builds the pickup-filter controls and edits Inventory cutoff settings. Enabled level/rarity cutoffs combine with OR; existing inventory is not retroactively scrapped.',
'InventoryUI':'Maintains Gear-to-slot views and a shared root-canvas tooltip, with a responsive scrolling grid. Subscribes to inventory changes while enabled and preserves unchanged slot objects.',
'ItemSlotUI':'Binds a Gear to its grid button, rarity visuals and pointer actions. Left click equips, right click requests dismantling; Inventory remains the authority for allowed actions.',
'ItemTooltipUI':'Formats actual local/global item values and owns hover-card scrolling/placement. Revalidates ownership before scrapping and closes when the item or anchor disappears.',
'LevelGenerator':'Legacy 3D placement planner using theme prefab lists and world-space anchors. It builds data, not GameObjects; ZoneManager applies plans, and the paper scene disables this path.',
'LevelPlan':'Data passed from LevelGenerator to ZoneManager: theme, seed, bounds and placement lists. Bounds/foreground fields are not all enforced or consumed by the current planner.',
'LootManager':'Rolls item type/rarity/element and delegates affixes to ModManager, then returns one Gear to GameManager. Item level uses combat level plus enemy-rarity bonus.',
'MainMenuUI':'Menu button wiring; New Game loads SampleScene. Achievements and Load Game are placeholders, not persistent save/load implementations.',
'ModDatabase':'Serialized affix catalog with lazy enum/id lookups. Editor validation fills missing definitions and allowed slots; configured tier ranges remain the balancing data.',
'ModManager':'Weighted stat and tier selection, item-level gating and duplicate-group exclusion. Weapon base damage/speed/crit are guaranteed rolls outside the random affix count.',
'PaperBattleHUD':'Reads run, health and current-enemy state for the paper HUD and toggles inventory/stats panels. Opening those panels is separate from the skill-tree combat pause.',
'PaperPlayerAnimationSet':'Shared serialized player idle/attack frames and matching hand anchors. Eight attack slots follow PaperSpriteActor impact conventions; idle speed is frames/second.',
'PaperSpriteActor':'Presentation layer for authored idle and attack cels plus legacy four-pose actors. Attachment positions use body-local Unity units, angles use degrees; empty player equipment hides the entire weapon.',
'PaperWeaponVisual':'Whole weapon sprite profile, including handle, with pivot correction and scale. Sprite art points along +X; gripOffset is measured in sprite-local world units before scale/rotation.',
'PlayerController':'Builds deterministic noncritical previews and randomized actual attacks from equipment/stats. EquipWeapon publishes AttackChanged for UI/art; EquipmentManager applies the global item modifiers.',
'PlayerProgression':'Session XP, level cap and six skill ranks on GameManager. Skills replace modifiers by this component as source; death restart keeps XP while StartNewRun resets it.',
'PlayerStatSetup':'Early player baseline setup before health Awake reads Life. Values entering StatsComponent use raw units: Life is HP and percentage buckets use percentage points.',
'PlayerStatsPanelUI':'Rebuilds categorized stat rows on StatsChanged/AttackChanged while retaining collapsed sections. Attack previews use the noncritical context so inspecting stats does not consume random rolls.',
'PoisonStatusEffect':'Poison asset defaults and validation. Eligible source damage is selected centrally in AilmentCalculator; stack instances belong to the target.',
'Pool':'Legacy scenery reuse by string key. ZoneManager sets placement and activation after Get; Release deactivates and returns the object to its key stack.',
'PooledObject':'Stores the pool key assigned by Pool.Get so Release returns scenery to the correct stack.',
'RolledMod':'One resolved affix: StatTypes key, one-based tier and raw numeric roll. Gear routes this value into local weapon fields or global modifiers.',
'SkillTreeUI':'Builds the six-node skill panel over PaperBattleHUD and calls PlayerProgression.TrySpend. IsOpen freezes combat and sprite clocks without setting global time scale.',
'SpawnAnchorGroup':'Serialized world transforms for legacy 3D scenery categories. LevelGenerator samples these anchors; they do not control combat enemy spawns.',
'StatCategory':'UI grouping labels used by StatCategoryMapping; these categories have no effect on combat formulas.',
'StatCategoryMapping':'Maps stat enum members to display sections and labels. Explicit cases depend on names, not enum ordering; newly added stats need an intentional display category.',
'StatDisplayFormatting':'Friendly names and raw-value formatting for stats and tooltips. Percent classification must agree with StatsComponent; display formatting never changes gameplay units.',
'StatHeaderUI':'Small serialized TMP label binding for a stats section heading; PlayerStatsPanelUI creates and manages these views.',
'StatMappings':'Maps damage elements to stat keys for attacker scaling and defender mitigation. Unsupported elements use fallback keys; physical resistance currently maps to FireRes even in the hit pipeline.',
'StatModifiers':'Stat operations and a modifier source token used for removal on gear/skill changes. Most values are raw points; non-bucket multiplicative operations take a fractional multiplier delta.',
'StatRowUI':'Binds a stat name/value pair to serialized TMP labels. Formatting and which rows exist are decided by PlayerStatsPanelUI.',
'StatsComponent':'Actor stat store with cached StatValue entries and batched change events. GetStat converts classified percent points to fractions; GetRawStat preserves stored units.',
'StatTypes':'Serialized stat identifiers consumed by gear catalogs, formulas and UI. Preserve numeric identities when extending; presence in this enum does not imply an implemented mechanic.',
'StatusBadge':'Pointer target for one status summary in StatusHUD. Hover delegates to the HUD tooltip rather than changing the status model.',
'StatusController':'Target-owned stacks with global-turn ticking and pending effect aggregation. Mutation is separated from application because damage/death callbacks can clear statuses or replace targets.',
'StatusController.Display':'Produces per-effect, frequency-weighted summaries for status UI and the shared per-stack tick calculation. This partial class participates in damage math as well as display.',
'StatusEffects':'Shared status definition and element/mask enums. Magnitude is a coefficient and duration/interval are global turns; virtual callbacks are extension hooks not dispatched by current ticking.',
'StatusGlyph':'Procedural UI glyph for a status kind. Rebuilds mesh through Unity UI; status strength, eligibility and duration live in the model.',
'StatusHUD':'Builds player/enemy status strips and hover tooltips from StatusController summaries. Rebinds to the current spawned enemy and discards stale badges on target replacement.',
'StatusInstance':'Mutable lifetime, stack strength and source-stat reference for one applied effect. Interval >0 ticks every N global turns; <=0 allows 1-interval ticks per turn.',
'StatValue':'Caches one raw stat bucket and invalidates on base/modifier changes. Result is (base + flat + additive) times multiplicative; more-percent buckets multiply each roll and expose the effective percentage.',
'ThemeDefinition':'Legacy 3D theme art and per-category placement rules. ZoneManager currently applies the skybox; BackgroundSprite/AmbientTint are not the active paper-forest background source.',
'ThemeSet':'Weighted legacy 3D theme catalog with optional exact level overrides. The paper forest cycle instead uses six sprites on ZoneManager.',
'ThemeSpawnRules':'Legacy scenery placement controls in world units/degrees, with count, density and skip probability. PositionJitter x/y fields perturb world X/Z; vertical offset is separate.',
'ZoneManager':'Selects the six paper forest images in ten-level blocks repeating every sixty levels. Also retains optional legacy 3D scenery generation and placeholder prestige/scaling hooks.',
}
EDITOR = {
'AttackStatsChecks':'Opt-in Play fixture for deterministic damage previews, status gating and legendary tooltips.',
'AuthorizedProgressionPass':'Opt-in multi-phase Play diagnostics for kill/boss progression, restart and UI transition.',
'DamageNumberStyleBuilder':'Asset-writing builder for TMP damage-number materials and prefab wiring; also exposes a Play preview.',
'DeferredProgressionChecks':'Opt-in multi-phase progression and UI fixtures, including F6/F7/F8 menu shortcuts.',
'InventoryChecks':'Opt-in Play checks for scrap yields, ownership, equipment modifiers and tooltip content.',
'InventoryGridChecks':'Opt-in Play fixtures for responsive inventory, pickup filters, gear damage and ailment regression.',
'PaperBattlePlayChecks':'Opt-in Play diagnostics and captures for the paper battle presentation.',
'PaperBattleSceneBuilder':'Asset-writing rebuild from the legacy scene/enemy prefab into the paper prefab and SampleScene. Rebuilds can replace hand edits; inspect output and keep art configuration changes here too.',
'PaperBattleStatsBuilder':'Asset-writing builder for stat-panel and equipped-slot UI wiring.',
'PaperBattleUIBuilder':'Asset-writing paper HUD/inventory UI configuration shared with the scene builder.',
'PlayerProgressionChecks':'Opt-in Play tests for XP, points, skill prerequisites and retention across encounter restart.',
'PlayerStatModelChecks':'Editor checks for player baseline stats, percent buckets and life/stat behavior.',
'ProgressionChecks':'Editor checks for encounter progression and role/death-reward guards.',
'StatusModelChecks':'Editor checks for ailment strength, durations and status model invariants.',
'TooltipCritChecks':'Opt-in Play checks for typed damage rows, final critical chance and inventory/equipment tooltip interactions.',
}
ART = {
'BossCadenceHarness.cs':'Dependency stubs for actual GameManager with production quota/death-claim methods. Verifies nine normals then stage10 boss, duplicate reward guards, direct loads and restart across121 combat levels.',
'verify_boss_cadence.ps1':'Compiles actual GameManager with production ZoneManager quota and HealthComponent death-claim methods into a file-only encounter progression harness. Does not launch Unity.',
'prepare_forest_enemies.py':'Current authored goblin/hobgoblin connected-component extraction, alpha cleanup and uniform species registration. Writes rest/attack PNGs and previews; no motion warping.',
'install_forest_enemies.py':'Current enemy art/config installer cloning legacy normal/boss prefab gameplay blocks and changing only presentation. Updates both main battle slots with stable GUIDs.',
'verify_forest_enemies.py':'Checks enemy sprite references/alpha, cloned gameplay blocks, normal/boss slots, unchanged progression logic and preview durations; writes a common-scale lineup and report.',
'apply_forward_attack_fix.py':'Legacy tall-player forward-strike cleanup; writes PlayerAttack PNGs and reviews. Superseded by ChibiPlayer; do not run to update the current player.',
'assemble_attack_recovery_review.py':'Legacy review-only replacement of recovery panel six; writes comparison images, not the current player configuration.',
'assemble_authored_inbetweens.py':'Legacy tall-player inbetween extraction/order/alignment and previews; inspect output option before running. Not the current eight-cel player pipeline.',
'build_combat_idle.py':'Archived synthetic idle experiment that writes PlayerIdle art using sampled deformations. Not approved for the current authored chibi animation.',
'build_fixed_body_proof.py':'Archived fixed-body/arm compositing proof; writes review images only and does not install ChibiPlayer.',
'chibi_player_attachments.py':'Current player grip authoring and equipped/unarmed previews. Run after preparation and before installation; source coordinates are mapped through extraction scale/offset.',
'CombatTimingHarness.cs':'Unity/dependency stubs and assertions compiled with actual combat/actor sources by verify_combat_animation.ps1. This is not an in-engine renderer or full-project build.',
'ForestCycleHarness.cs':'Minimal Unity stubs and level-boundary assertions compiled with actual ZoneManager by verify_forest_cycle.ps1.',
'install_authored_idle.py':'Legacy tall-player authored idle extraction and repeated-frame installation; also provides the runs helper imported by current preparation.',
'install_chibi_player.py':'Current deterministic sprite metadata, animation/sword assets and main-prefab installer. Run only after preparation and attachment authoring; persistent GUIDs preserve references.',
'install_consistent_idle_subset.py':'Legacy fixed-body idle subset installer that rewires the player prefab. Do not run against the current chibi configuration.',
'install_forest_cycle.py':'Copies six external forest source PNGs and writes metadata/background prefab references. Read the source folder before running; generation itself is unnecessary for already installed images.',
'make_idle_pose_guides.py':'Archived tall-player idle pose guides for authored-image generation; outputs review guides rather than current runtime art.',
'prepare_chibi_player.py':'Current body extraction/alpha cleanup/registration and full-sword preparation. Dense bands locate figures, empty gutters define cells, and complete alpha bounds preserve hair tips.',
'prepare_player_attack.py':'Legacy tall-player attack extraction and cleanup; outputs the superseded PlayerAttack set and forward-strike correction.',
'prepare_player_idle.py':'Legacy tall-player idle extraction and background flood helper. Current chibi preparation imports flood but does not run this script as an installer.',
'refine_player_idle.py':'Archived tall-player idle refinement experiment and review output; not part of the current authored chibi pipeline.',
'review_attack_gray.py':'Legacy diagnostic masks for unwanted gray pixels in PlayerAttack; review output only.',
'review_coherent_keys.py':'Archived tall-player key-pose registration, contact sheets and detail previews.',
'review_coherent_test.py':'Archived tall-player transition cutouts and timing preview; does not install the chibi player.',
'snapshot_attack_fix.py':'Archives legacy attack cels and prepares edit references before forward-strike cleanup.',
'verify_chibi_player_assets.py':'File-only current-player verification: alpha bounds, ordered sprite/config/grip references, previews and protected files. Does not invoke Unity.',
'verify_combat_animation.ps1':'Compiles actual selected C# sources with CombatTimingHarness in a fresh PowerShell process and runs assertions. Dependency stubs limit this to logic verification.',
'verify_forest_assets.py':'Checks source PNG identity and forest metadata/prefab ordering; writes a JSON result. Requires the recorded external forest source directory.',
'verify_forest_cycle.ps1':'Compiles actual ZoneManager with minimal stubs and checks level boundaries without launching Unity.',
'verify_installed_animation_assets.py':'Legacy player-layout verifier with superseded frame expectations. Use verify_chibi_player_assets.py for the installed player.',
'verify_player_idle.ps1':'Compatibility entry point that runs the current combat/idle harness in verify_combat_animation.ps1.',
'wire_player_attack.py':'Legacy PlayerAttack reference installer that rewrites the player prefab; superseded by install_chibi_player.py.',
'wire_player_idle.py':'Legacy PlayerIdle metadata/reference installer; superseded by install_chibi_player.py and may require old external sources.',
}

# Preserve literals verbatim while ignoring comments/whitespace in C#. This proves
# this documentation pass did not change code tokens; it is not a C# compiler.
CS_TOKEN = re.compile(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/|\s+|[^\s]', re.M)
def fingerprint(path, text):
    if path.suffix == '.py':
        content=ast.dump(ast.parse(text), include_attributes=False)
    elif path.suffix == '.cs':
        content=''.join(t for t in CS_TOKEN.findall(text) if not t.isspace() and not t.startswith(('//','/*')))
    else:
        content='\n'.join(l for l in text.splitlines() if not l.lstrip().startswith('#'))
    return hashlib.sha256(content.encode()).hexdigest()

def main():
    entries={**{f'Assets/Scripts/{k}.cs':v for k,v in RUNTIME.items()}, **{f'Assets/Editor/{k}.cs':v for k,v in EDITOR.items()}, **{f'Tools/Art/{k}':v for k,v in ART.items()}}
    entries['Tools/document_first_party.py']='Maintains these reviewed responsibility comments and the linked code map, checking token/AST identity for each application. The original documentation baseline is kept as historical evidence.'
    found={p.relative_to(ROOT).as_posix() for folder in ['Assets/Scripts','Assets/Editor','Tools'] for p in (ROOT/folder).rglob('*') if p.suffix in ['.cs','.py','.ps1']}
    assert found==set(entries), ('Unmapped files',found-set(entries),'Missing files',set(entries)-found)
    docs=ROOT/'Docs';docs.mkdir(exist_ok=True)
    before={};rows=[]
    for relative,description in entries.items():
        path=ROOT/relative;original=path.read_text(encoding='utf-8-sig');before[relative]=fingerprint(path,original)
        prefix='//' if path.suffix=='.cs' else '#'
        header=prefix+' Developer map: '+description+'\n'+prefix+' See Docs/DEVELOPER_HANDOFF.md for system flow and validation.\n'
        if not original.startswith(prefix+' Developer map:'):
            newline='\n'
            with path.open('w',encoding='utf-8',newline=newline) as f:f.write(header+original)
        assert fingerprint(path,path.read_text())==before[relative], relative
        rows.append(f'| [{relative}](../{relative}) | {description} |')
    (docs/'CODE_MAP.md').write_text('# First-party source map\n\nEvery runtime/editor C# file and art-tool C#/Python/PowerShell file is listed below. Unity tutorial, TextMesh Pro, Store Assets, Packages, Library and generated project files are excluded. Archived art tools are explicitly labeled; do not run them to update current art.\n\n| File | Responsibility and connection |\n| --- | --- |\n'+'\n'.join(rows)+'\n')
    if not (docs/'documentation-code-baseline.json').exists():
        (docs/'documentation-code-baseline.json').write_text(json.dumps(before,indent=2))
    print(f'Documented {len(entries)} source files; executable fingerprints unchanged.')

if __name__=='__main__':main()
