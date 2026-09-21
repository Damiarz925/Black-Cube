# Passive Optimizer

Open **Black-Cube → Balance Workbench → Passive Opt**. The default point budget can be set to the production player-level budget (one point at each level, including level 1) or deliberately overridden for an experiment.

Every proposed state is passed to `PlayerProgression.ValidateAllocationState`. Class spines, native completion before off-class starts, choice-group exclusivity, subclass fourth choices, weapon-route prerequisites, and stable node IDs therefore use the runtime contract.

## Algorithms

- **Greedy** evaluates every currently legal node, takes the best immediate objective score, records the point, and repeats. It is fast and is not claimed to be globally optimal.
- **Beam** expands all legal next nodes for every retained state, deduplicates exact allocation signatures, and keeps a bounded width. Before filling by score, it preserves the best state for each topology signature (deepest tier in all six class routes and all six weapon routes). This protects travel investments that unlock later value.

The point table reports immediate Primary, Secondary, objective, and relative score deltas at the moment each node was selected. Search debug shows candidate, unique, retained, pruned, and diversity counts by depth.

## Marginal and path value

**Analyze Legal Next Nodes** answers what one additional point does now. Minimum-path analysis constructs the missing native/class/weapon spine package for a locked target and accepts it only when the production validator approves the final state. Path-adjusted value is total package objective gain divided by package point cost; travel nodes may have zero immediate value but positive package value.

The **Heatmap** uses the authored `PassiveTreeDefinition.LayoutPosition` and edges. Modes display immediate objective, Primary, Secondary, path-adjusted, or the optimizer-selected path. It is an editor-only overlay and never writes sprite/color data. Select a node and use **Select Owning Branch SO** to ping the exact editable class or weapon Branch asset. **Why This Node?** shows deterministic deltas and path cost.

