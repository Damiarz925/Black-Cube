# Class, weapon, subclass, and passive-extension architecture

## Shipped Step 16 contract

`ClassWeaponArchitecture.cs` is the stable-ID authority. The six production base classes are `class.warrior`, `class.mage`, `class.ranger`, `class.barbarian`, `class.priest`, and `class.thief`. Their signature starter mappings are Sword, Staff, Bow, Two-Handed Axe, Sceptre, and Dagger respectively. A base class currently grants no innate stats. New Game requires a menu choice; the chosen ID is fixed for the run, survives Load and Rebirth, and is replaced only by confirmed New Game.

The six weapon IDs are `weapon.sword`, `weapon.two_handed_axe`, `weapon.staff`, `weapon.bow`, `weapon.dagger`, and `weapon.sceptre`. They remain items in the single `Weapons` equipment slot, and no class restriction exists. Central placeholder profiles are:

| Weapon | Damage | Attacks/sec | Crit | Range |
|---|---:|---:|---:|---|
| Sword | 18–27 | 0.45 | 5% | Melee |
| Two-Handed Axe | 26–38 | 0.30 | 5% | Melee |
| Staff | 18–28 | 0.40 | 5% | Melee |
| Bow | 17–25 | 0.50 | 5% | Ranged |
| Dagger | 14–20 | 0.60 | 5% | Melee |
| Sceptre | 19–28 | 0.42 | 5% | Melee |

The shared starter pipeline accepts the signature weapon type, then applies the existing relic element, base-damage, item-level, and Legendary-chance transformations. Random legacy-compatible weapon drops currently identify as Sword until weapon-type drop weighting is authored. This is a content placeholder, not a class equipment lock.

## Weapon skills

Every weapon definition has a two-slot active-skill contract. `PlayerSkillController` resolves the equipped weapon pair and owns one transient next-attack queue: choosing slot 1 or 2 replaces the queued choice; swapping weapons clears it. Mana remains checked at queue and paid at attack resolution under the Step 14.5 rules. The HUD exposes both slots and refreshes on weapon change.

The final mapping of the seven existing skills is explicitly TBD. `WeaponSkillBindings` is empty in production; editor tests may register clearly non-production pairs. No fake mapping is serialized or presented as V1 content.

## Subclasses and story unlock

Each base class exposes exactly two stable subclass-slot references. `SubclassDefinition` supports a stable ID, one parent class, passive-section/presentation references, and broad system hooks. The production catalog intentionally contains no final subclass identities. Editor fixtures can register temporary definitions.

`story.main.complete` is the stable milestone that unlocks subclass selection. The story boss itself is not implemented. Selection rejects a subclass from the wrong parent class; unlock and selected IDs persist and survive Rebirth. Respec behavior is unresolved.

## Passive-tree seam

`PassiveExtensionMetadata` supports stable sections, class starts, affinity tags, subclass requirements, specialization/exclusivity groups, and travel classification. Passive Tree V3 uses deterministic radial class/weapon templates, selected-class reachability, and native-route subclass choice slots. Transformation metadata and effects no longer exist. See [PASSIVE_TREE_V3.md](PASSIVE_TREE_V3.md).

## Persistence and unresolved decisions

Schema 8 adds base class ID, story/subclass state, and stable weapon type per weapon. Schema 9 added the historical V2 refund and six per-character file slots. Schema 12 performs the V3 respec/refund while preserving class, selected subclass, and all non-passive state.

Bow Precision and target-snapshotted projectile latency are real; Accuracy was not reintroduced. Staff supports data-driven AutoCooldown skills and Axe supports Rage. Final weapon skill assignments, weapon drop weighting, all twelve subclass identities/effects, subclass respec, and class art/audio remain future content work.
# Step 18 production binding

`WeaponSkillBindings` now provides two production IDs for every weapon. `SubclassCatalog` provides two production entries per class. `SubclassEffectCatalog` is deliberately source-agnostic: effects can later be granted by subclass, transformed passive, unique, or item affix without branching unrelated combat code on selected subclass.
