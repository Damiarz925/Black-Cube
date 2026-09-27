# Ordinary itemization after the real-player pass

`GearStatLists.BuildDefaultStatPools` is the authoritative ordinary generated-affix pool. `AffixPolicy.Side` assigns each stat to Prefix or Suffix; the same policy is used by drops and crafting. `ModDatabase.asset` owns the actual legal tier gates and ranges. Legacy serialized stat identities remain reserved for save compatibility but are removed from generation when they are not in a current pool.

Ordinary generated `More Damage` families, ailment resistance, and ailment penetration have been removed. Physical Damage Reduction is a new armor-family prefix on Helmets and Body Armour. Reduced Shock Effect and Reduced Chill Effect are new prefixes on Helmets, Boots, and Body Armour. `+All Skills` appears on Weapons, Amulets, and Helmets and has five levels with increasing item-level gates. Skill damage uses `1.1^(effective level - 1)`; typed and All Skills levels combine through the production level calculation.

Ordinary Poison requires a Void damage source; Bleed uses Physical, Ignite Fire, Poison Void. Those DoTs inherit the corresponding damage mitigation and penetration rather than separate ailment resistance/penetration. Explicitly specialized skill/subclass exceptions remain explicit in their respective systems.

Weapons carry shared generic affixes plus an element-specific family. Physical weapons can roll Physical damage, Physical penetration, and Bleed; Fire weapons Fire damage, Fire penetration, and Ignite; Cold weapons Cold damage, Cold penetration, and Chill; Lightning weapons Lightning damage, Lightning penetration, and Shock; Void weapons Void damage, Void penetration, and Poison. No cross-element ordinary weapon-affix roll is intended. Weapon base damage, speed, and crit are guaranteed base stats, not ordinary Prefix/Suffix rolls.

For exact per-slot eligible stats and their sides, inspect `GearStatLists.BuildDefaultStatPools` together with `AffixPolicy.Side`, or use **Balance Workbench → Affix Analyzer**. These two production sources supersede old POEDB-style baseline documents. Enemy intrinsic equipment retains its separate affix-count profile; this pass does not rebalance enemies.
