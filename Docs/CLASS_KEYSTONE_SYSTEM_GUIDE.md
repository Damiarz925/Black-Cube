# Class keystones, progression and provisional weapon trees

PROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE

This guide describes the implemented architecture. Automated validation, the survey, fresh build and startup evidence are recorded in `CLASS_KEYSTONE_VALIDATION_AND_SURVEY.md`; native manual smoke is explicitly unverified. The earlier `CLASS_KEYSTONE_SYSTEM_CHECKPOINT.md` records a superseded foundation checkpoint.

## Allocation and equipment

Level progression owns one shared passive budget: one point at level 1 and exactly 100 earned points at level 100. No weapon experience or second passive currency exists.

Your native class has ten spine nodes and twenty side slots. Each side slot offers three generic choices and the existing native-subclass variant. Allocate all thirty physical nodes before choosing one of that class's three keystones in its single keystone slot. The keystone costs the thirty-first point; completing the spine alone no longer unlocks weapon specialization or another class.

After native completion, explicitly select one weapon tree and/or an additional class route. These are independent choices. Any class may select any weapon tree, and equipped weapon type remains unrestricted. Weapon-tree bonuses apply only while the matching weapon is equipped.

Complete each selected off-class route's ten-node spine before selecting the next route. Its twenty sides are optional for travel, but all twenty plus the spine are required for its keystone. Off-class subclass variants remain unavailable.

Only one weapon tree may be selected. Ordinary refunds cannot invalidate prerequisites or downstream route-selection gates. Full passive respec clears allocations, selected class routes and weapon specialization. Changing a subclass refunds incompatible subclass choices; if that invalidates downstream gates, the full passive allocation is refunded safely.

## Tree UI

Class trees use a uniform vertical ten-tier spine, symmetric side slots and a centered keystone slot above the last tier. Click a side slot for its choice popup. The keystone slot becomes eligible only after the thirty-node requirement; click it for three mutually exclusive choices.

After choosing a weapon specialization, its miniature tree appears inside the Player Hub. Click the hub to animate into the readable full-size weapon view. Back, Esc or right-click returns to the prior class-overview pan and zoom. Weapon node effects still require matching equipped gear.

Each weapon tree has seven spines, fourteen single ABC slots and a maximum cost of twenty-one points. Values rise sharply with depth; final-tier left choices emphasize defense/sustain/utility and right choices emphasize offense. These packages are provisional architecture/economy content, not final weapon balance.

## Editor layout authoring

Open **Black-Cube → Passive Tree Authoring → Layout**, then select the desired branch. The editor loads an isolated editable prefab copy. Move nodes and edit connection sockets there; dragging does not save/reimport the production prefab.

Select a connection and adjust its From/To anchor and local offsets to match the intended visual connection points. Center anchors with zero offsets give true center-to-center lines. Moving an endpoint updates the line preview. Unity Undo is supported.

Use the explicit Save control after completing a set of adjustments. Reload/discard abandons staged changes; closing with unsaved edits prompts to save or discard. Runtime framing moves branch roots and camera bounds, not authored internal node positions. Do not rerun the installation/reauthoring commands after manual geometry work unless you intentionally want their generated layouts to replace your edits.

## Combat and resource changes

- Fireball: projectile-tagged automatic spell, 250% Fire basis, 60 base Mana and five-second cooldown. Quick Strike: thrown projectile, 100% basis, 25 base Mana and four-second immediate cooldown; does not consume the normal attack gauge. Both use authored base projectile speed and centralized Projectile Speed travel, captured targets and fizzle-on-target-loss behavior.
- Multistrike requires a melee attack. Sword, Two-Handed Axe, Dagger and Sceptre basics support it; Bow, Staff, Fireball and thrown Quick Strike do not.
- Default maximum Ignite stacks is one; Molten Edge increases it to two. Main hits and Eruption remain independent applications.
- Life Regeneration now uses percentage points of maximum Life per second: a stored value of 3 means 3%/sec, with gameplay fraction `.03`. Mana Regeneration remains flat Mana/sec. Physical Damage Reduction remains fractional: `.02` means 2%, not `.02%`.
- Blood Engine suppresses ordinary Life regeneration and converts half the would-be regeneration fraction into recovery from actual damage dealt. Healing obeys existing recovery modifiers and conversion rules.
- Crown of Fury uses continuous Rage decay and only grants its offensive payoff at full Rage. Its payoff is 40% more than the ordinary capped-Rage multiplier, including Rage Effect scaling.
- Storm's Price produces one enemy bolt and one explicitly incoming self-bolt per spell cast. The self-bolt uses player Lightning resistance without player penetration, cannot Crit or recurse, and does not grant offensive Life/Mana on Hit. Reactive Revenge/Rage/Shock interactions remain possible.

Exact eighteen keystone definitions, seven-tier weapon side packages and six weapon-exclusive ladders are exported from their authoritative data rather than duplicated as hand-maintained values.

### Life Regeneration source audit

| Source/consumer | Change |
|---|---|
| Six class branch assets, including their subclass-option effects | Existing flat values converted conservatively by multiplying authored values by `.01`; installation is guarded against repeated conversion. |
| New provisional weapon packages | Authored directly in percentage points; ordinary Life Regen profile base is `.5` before tier/package scaling. |
| Production helmet/body affix ladders | Existing regeneration ladder values multiplied by `.01`; rarity and tier progression retained. |
| ModDatabase fallback ladder | Converted once by the authoring pass, with the same `.01` source-retuning scale. |
| Historical saved gear | Schema migration explicitly converts old rolls by `.01`; it does not merely reinterpret old large flat numbers as percentages. |
| Stats/Health/Blood Engine | Percentage points become gameplay fractions; Health restores Max Life × fraction per second. Blood Engine removes ordinary regeneration and recovers damage × would-be fraction × `.5`. |
| Tooltips/stat display | Life Regen is labeled percentage of maximum Life per second. Mana Regen remains flat Mana per second. |
| Workbench/Lab/sensitivity | Actor regeneration is derived from maximum Life and the fraction. Metrics may report resolved Life/sec; sensitivity converts that resolved value back to percentage points before perturbing the production stat. |

This is conservative source retuning, not an assertion that every old build keeps its previous flat recovery or that 30%/sec is a normal target. Resource observations are recorded in the controlled survey without automatic adjustment.

## Save compatibility

Current save schema is 13. Schema-12 saves receive a full passive refund at their existing player level because the tree topology and progression contract changed. Selected route/weapon fields are initialized empty. Existing class, subclass, equipment, rebirth and run progression remain preserved.

Old saved Life Regeneration gear rolls are explicitly converted to the new percentage representation. Historical equipment is marked for legacy affix compatibility; newly generated/current-schema items obey current weapon eligibility and paired-roll ranges. No old stat ID is repurposed.

## Survey interpretation

The requested controlled survey contains six level-30 pre-keystone baselines, eighteen level-31 variants and eighteen level-70 variants. It uses signature weapons, no subclasses, straight progression without deliberate farming, shared checkpoint history/inventory seeds, independently cloned inventories, Serious crafting bounded to twelve actions, passive beam width twelve, soft current resistance/PDR targets and thirty final Combat Lab fights per build.

Level-70 weapon ablation keeps the same final gear and class allocation, removes weapon allocations and does not reallocate the refunded points. Raw per-build JSON records allocation, gear/crafting, analytical metrics, Combat Lab results, damage types, ailments, healing, Mana/Rage/crit/projectile counters and keystone telemetry.

Final fights enforce automatic-only skill bindings: both Staff cooldown skills are enabled even if the analytical search preferred "No Skills" or a single skill. The final report separates optimization provenance from corrected combat-replay provenance; final fights do not silently overwrite the earlier bounded-search artifacts.

These are independent fresh encounters, normally starting with full Life/Mana and zero Rage/Stealth. They do not model resource carry across an enemy chain. In particular, Crown of Fury may have no full-Rage uptime against a short-lived fresh target even though Rage can accumulate across live encounters. Next-enemy Stealth consumption and long-run Mana pressure require separate encounter-chain play checks.

Analytical search approximates sustained damage. It cannot prove optimal play, exact target-condition uptime, permanent-Poison fight-length behavior, random damage-type filtering or reactive self-hit survival; Combat Lab resolves those sequences. Results are architecture/economy diagnostics only. They must not be presented as final class or weapon balance, and this task does not automatically tune values from them.

To rerun this bounded survey, close Unity first and run from the repository directory:

```powershell
& 'D:/Unity/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/Unity/Projects/Black-Cube' -executeMethod BlackCube.BalanceWorkbench.ClassKeystoneSurveyRunner.RunAll -logFile 'Logs/ClassKeystoneSurvey.log'
```

The GUI executable starts a background Editor process; the shell returning does not mean the survey finished. Wait for `CLASS KEYSTONE SURVEY: 42/42 COMPLETE` in the log and for that Editor process to exit before opening the project or starting a build. Completed rows resume only under the identical source/data fingerprint. The raw results and `SURVEY.md` live under `Logs/ClassKeystones/Survey/<fingerprint>/`; they are intentionally ignored by Git.

For manual smoke, create a test character with at least 31 points, allocate ten native spines and twenty sides, open the three-keystone popup and choose one. Check route/weapon selection, hub preview, full-size weapon allocation and Back/Esc/right-click restoration. Then test non-signature equipment, full respec, save/load and encounter restart. Scripted prefab renders supplement but do not replace native input testing.

For Vanishing Blade, kill an enemy and observe the **next** enemy's first attack: Stealth must be consumed whether that attack hits or misses and must not stack. Independent one-enemy Combat Lab fights can report activation on kill but cannot establish cross-enemy consumption; do not mistake an absent next-enemy miss counter in those survey rows for a broken live mechanic.
