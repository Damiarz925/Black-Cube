# Player Build Lab

Open **Black-Cube → Balance Workbench → Player Build**. Choose player level and combat level separately, then select a class, an unlocked subclass assumption (or None), and weapon type. The weapon determines the two production weapon skills shown by the evaluator.

Choose readable Primary and Secondary objectives. Weighted mode combines normalized log-relative improvements; Lexicographic mode protects Primary priority and uses Secondary as a tie/second priority. Expand optional constraints when the build needs a minimum Life, Armour, or resistance rather than pure offense.

Select a `PlayerGearProfileSO` and generate a complete set. Every item is a production-legal rolled item, not an average template. Inspect shows exact rarity, level, implicit/explicits, tiers, values, and weapon identity. Lock preserves a slot through the next search; Unlock permits replacement; Clear makes the slot empty. Export JSON preserves the exact build snapshot; CSV exports the compact metric row.

The result shows hit range, expected basic DPS, element split, crit/Hit Twice expectation, ailments, defenses, resources, and both production weapon skills separately. Cooldown DPS is labeled as an ideal sufficient-Mana assumption. The damage trace shows production weapon base, attribute and level increased contributions, increased/more buckets, crit, and Hit Twice. The source table is controlled ablation: it evaluates the full build and removes Level, Gear, Weapon, Passives, or Subclass. Interactions mean those marginal values need not add to the total.

Use **Capture as A** and **Capture as B** around any two configurations for an absolute and percentage metric comparison. Player scenarios saved from the Experiments tab retain class, subclass, weapon, objectives, constraints, optimizer settings, sweep settings, and gear-profile path.

