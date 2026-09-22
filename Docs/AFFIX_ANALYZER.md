# Affix Analyzer

Open **Balance Workbench → Affix Analyzer** after preparing a Player Build Lab character. Choose a slot, item level, rarity, and (for weapons) weapon type and element. The analyzer reads the production ModDatabase, canonical slot pool, item-level tier gates, weapon restrictions, and affix-side capacity. It evaluates the chosen roll position against the selected build objective. **Analyze all legal tiers** exposes tier jumps; the default shows the strongest available tier. **Pair top N** tests combinations of top single-affix families and subtracts their independent gains.

If the current item is full, enable **Replace current mod** and choose a replaceable family. The comparison baseline removes that mod and ranks legal replacements against the open slot. The production item is never changed. **Select Mod Database** pings the owning asset. Export ranking and pair tables to CSV.

**Combat Test Top N** sends only the selected shortlist through the Tooling 4 combat batch using the same seed set; it shows win-rate, P50 fight-duration, and DPS deltas versus the unmodified current build. The first pass does not yet include boss-special and implicit affix analysis. Rankings are controlled comparisons, not a recommendation to edit production values.
