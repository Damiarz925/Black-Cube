# Sensitivity Analyzer

Open **Black-Cube → Balance Workbench → Sensitivity**. Prepare the character in Player Build Lab first. Select analytical or combat mode, an output metric, input change, and the stats to test. **Run Sensitivity** ranks the observed differences. A relative input scales the current measured value; an absolute input is a flat stat value, with fraction-of-one values for percentage-point stats. For example, `0.10` Crit Chance means ten percentage points. The stat label shows its unit.

Analytical mode calls the production player-build evaluator. Combat mode calls the headless combat batch with the same seed and fight count for baseline and every variant; use larger batches for noisy win rates. **Pair Analysis** evaluates the first twelve selected stats pairwise. Interaction is `combined gain − gain A − gain B`, in the selected output metric; positive means more than additive, not necessarily a balance defect. **Curve** evaluates −20%, −10%, baseline, +10%, +20%, and +30% for the chosen stat. CSV and curve PNG export to `ReviewCaptures/BalanceWorkbench`.

Results show a production fingerprint and become stale after source-data changes. Combat batch statistics are estimates; rerun with more fights before drawing a win-rate conclusion. The workbench does not modify production stat values.
