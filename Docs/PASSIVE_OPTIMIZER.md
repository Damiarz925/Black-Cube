# Passive Optimizer

Open **Black-Cube → Balance Workbench → Open → Passive Opt**. The default point budget can be set to the production player-level budget (one point at each level, including level 1) or deliberately overridden for an experiment.

The initial and final allocations are checked by `PlayerProgression.ValidateAllocationState`. Intermediate monotone candidates use equivalent local prerequisite, choice-group, subclass and off-class rules; the optimized result is rejected if final production validation fails.

## Algorithms

- **Greedy** evaluates every currently legal node, takes the best immediate objective score, records the point, and repeats. It is fast and is not claimed to be globally optimal.
- **Beam** expands all legal next nodes for every retained state, deduplicates exact allocation signatures, and keeps a bounded width. Before filling by score, it preserves the best state for each topology signature (deepest tier in all six class routes and all six weapon routes). This protects travel investments that unlock later value.

The point table reports immediate Primary, Secondary, objective, and relative score deltas at the moment each node was selected. Search debug shows candidate, unique, retained, pruned, and diversity counts by depth.

## Marginal and path value

**Analyze Legal Next Nodes** answers what one additional point does now. Minimum-path analysis constructs the missing native/class/weapon spine package for a locked target and accepts it only when the production validator approves the final state. Path-adjusted value is total package objective gain divided by package point cost; travel nodes may have zero immediate value but positive package value.

The **Heatmap** uses the authored `PassiveTreeDefinition.LayoutPosition` and edges. Modes display immediate objective, Primary, Secondary, path-adjusted, or the optimizer-selected path. It is an editor-only overlay and never writes sprite/color data. Select a node and use **Select Owning Branch SO** to ping the exact editable class or weapon Branch asset. **Why This Node?** shows deterministic deltas and path cost.

Progression History also offers a bounded **Optimize Gear + Passives** pass. It alternates production-legal greedy passive allocation with the historical whole-inventory gear search, valuing current Life/Armour/resistance deficits rather than reserving defensive nodes. Each alternation restarts from the same source/locked passive allocation so an earlier attempt cannot permanently lock an optional path. This is a local search and does not replace the existing deeper passive-only beam search.

Search has Greedy, Beam 100 and Beam 500 presets; the candidate estimate is a warning, not a hard budget. Long searches show progress and accept cancellation. A cancelled search does not publish a partial selected build. The benchmark runner measures legal-next generation, reused player-build evaluation, beam retention and remaining search work separately. Compare exact node sequence and result hash before interpreting a speed difference. The production evaluator is deliberately retained rather than substituting an approximate score.
