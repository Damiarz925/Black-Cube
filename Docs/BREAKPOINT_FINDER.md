# Breakpoint Finder

The single-scenario Breakpoint Finder remains available in **Balance Workbench → Breakpoint Finder**. Choose Player Analytical, Enemy Gear, Combat, Drops, or Loot Progression; range, step, metric ID, threshold, and operator. The source tool is invoked at every tested integer level, including levels inside coarse intervals, so non-monotonic crossings are retained. Results include before/at/after, curve, CSV, and PNG.

**Batch Breakpoints** currently runs a class dimension, enemy-rarity dimension, or their cross-product. Results are sorted by earliest crossing and show **NONE** for no crossing. A two-dimensional Class × Enemy Rarity table acts as a compact heatmap. Combat win-rate batches use Wilson 95% confidence intervals. Configure initial fights per point, refinement increment, and maximum fights per point. When a crossing interval overlaps the threshold, the tool reruns the crossing level with a larger explicit sample budget until it is clear or reaches the cap. A refined estimate that no longer crosses is labeled rather than silently accepted. Other metrics report their point estimate without a binomial interval.

The batch UI does not yet expose gear-profile, subclass, weapon, biome, and corruption dimensions. For these, run separate source scenarios. Batch scans can be expensive; cancellation is checked between cases/refinements.
