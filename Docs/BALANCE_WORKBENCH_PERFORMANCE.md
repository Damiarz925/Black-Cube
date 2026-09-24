# Balance Workbench performance pass

This pass changes Editor tooling only. It does not change production stat values, passive node definitions, loot tables, enemy tuning, or combat formulas. Benchmark fixtures use Unity 6000.6.0f1 on the Windows development machine, branch `codex/repository-cleanup-baseline`, source revision `951642c0` before this pass. Timings are not CI thresholds.

## Reproduce the benchmarks

Close the interactive Unity Editor, then run the following commands from the repository root. The runner writes CSV and JSON into ignored `Logs/BalanceWorkbenchPerformance/`. The pre-change baseline files are retained there; `BaselineCore` and `BaselinePassive` should only be run on an unmodified pre-pass checkout. `AfterCore` and `AfterPassive` use the current code.

```powershell
& 'D:\Unity\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\Unity\Projects\Black-Cube' -executeMethod BlackCube.BalanceWorkbench.WorkbenchPerformanceBenchmarks.AfterCore -logFile 'D:\Unity\Projects\Black-Cube\Logs\BalanceWorkbenchPerformance\AfterCore.log'
& 'D:\Unity\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\Unity\Projects\Black-Cube' -executeMethod BlackCube.BalanceWorkbench.WorkbenchPerformanceBenchmarks.AfterPassive -logFile 'D:\Unity\Projects\Black-Cube\Logs\BalanceWorkbenchPerformance\AfterPassive.log'
```

The passive fixture is Warrior/Sword, level 50, combat level 50, Mid gear, seed 41001, no subclass, 50 points, weighted Physical Hit DPS (0.70) and Attacks per Second (0.30). Each passive result hash covers the complete selected build, score, and allocation sequence. Result identity is more important than timing.

## Findings and changes

- Pre-pass greedy search spent 11.4 seconds on just 869 candidate evaluations. Beam 100 spent 922.1 seconds on 263,757 evaluations. The expensive path repeatedly copied a 750-node rank array, ran the full production validator, materialized complete GameObjects for duplicate allocations, and deduplicated only *after* full evaluation.
- Passive search now checks the exact incremental condition for adding one node to a previously valid allocation. It still calls the full production validator on the initial and final allocations. A compact allocation bitset deduplicates states *before* evaluation. A sorted allocation key is maintained incrementally. Greedy/beam scoring, node order, diversity retention, and final metrics remain unchanged.
- A passive-only search reuses one isolated production actor with fixed gear and reapplies the changing passive modifier package; it does not instantiate/destroy a player and every gear GameObject for each candidate. The focused suite compares this path against a fresh production actor across changing allocations and all six class/weapon pairs.
- `PlayerBuildEvaluator` caches complete production metrics by full serialized build snapshot plus production-data fingerprint. Entries are deep-copied without JSON round-trip to preserve every `double` bit and avoid caller mutation of cached results. The cache is bounded to 1,024 entries; fingerprint changes invalidate it.
- Fingerprint SHA-256 calculation is cached until Unity asset postprocessing, project changes, or Undo/Redo invalidates it. The Workbench header also caches the Git commit label for 30 seconds instead of starting a process on each IMGUI event.
- Chart geometry is built only on repaint; scenario curves are reused for the same sweep and metric; heatmap marginal lookup is reused until results or mode change. The search UI adds Fast/Normal/Deep presets without changing a selected beam width automatically. The Performance tab displays timing, collection counts, cache counters, candidate counts, evaluations/second, and CSV export.

## Benchmark results

The `AllocatedMB` counter remained zero even where collections occurred, so this Unity/Mono environment does not provide reliable per-thread allocation-byte measurements. Use Gen0/1/2 counts and the Unity Profiler for allocation diagnosis. The original Beam 500 run exceeded five minutes and was intentionally stopped to release the project lock; its baseline is a lower bound, not a completed measurement.

| Scenario | Before | After | Identity / note |
|---|---:|---:|---|
| Single player evaluation | 1.20 ms | 0.73 ms | exact hash match |
| 1,000 repeated player evaluations | 774.28 ms | 121.90 ms | exact hash match; 6.35× faster |
| Greedy, 50 points | 11,387.97 ms | 226.07 ms | exact hash match; 50.4× faster |
| Beam 100, 50 points | 922,126.31 ms | 40,336.69 ms | exact hash match; 22.9× faster |
| Beam 500, 50 points | >300,000 ms (censored) | 239,626.84 ms | completed; no baseline hash available |
| 1,000 items | 264.71 ms | 250.37 ms | baseline runner serialized `List<T>` as `{}`, so baseline hash is unusable |
| 1,000 enemies | 36,642.17 ms | 37,516.28 ms | no material improvement; baseline list hash unusable |
| 10,000 drop kills | 143.52 ms | 92.91 ms | baseline hash included run metadata and is not comparable |
| 1,000 pure combats | 518.16 ms | 344.62 ms | exact hash match |
| 10,000 pure combats | 2,138.29 ms | 2,025.05 ms | exact hash match; no regression |
| Analytical sensitivity | 130.43 ms | 73.15 ms | baseline hash included run metadata and is not comparable |
| 100 production fingerprints | 5,032.54 ms | 0.16 ms | exact hash match |

The benchmark runner now canonicalizes item/enemy row collections and drop/sensitivity values for future reproducible comparisons. Existing Tooling 1–5 and EditMode parity tests also guard the result semantics that the original benchmark hashes cannot compare. **Beam 500 still takes about four minutes** on this fixture; it remains the main unmet responsiveness target. No width reduction or approximate search was introduced. Its 1,330,644 unique full analytical evaluations dominate the run.

## Validation and remaining work

The full Unity EditMode run passed **489/489** tests, including all five focused performance/parity tests. Tooling 1–5 validators and editor smokes passed. A fresh Windows x64 build completed with zero errors, and the built executable reached the main menu in a headless startup smoke. Logs are retained under ignored `Logs/BalanceWorkbenchPerformance/` and the existing `Logs/Tooling*` files.

An interactive Unity 6000.6.0f1 smoke on 2026-09-24 confirmed that the Workbench opens; the Performance tab displays reports and live repaint metrics (about 5 ms average across ten initial samples); Export Performance Report writes a CSV; Fast/Greedy returns a 50-point allocation; its results scroll; the authored passive heatmap renders and Calculate Current Build Heatmap completes; and Curves generates a chart whose legend toggle updates the plot. Deep Beam 500's responsiveness remains the outstanding performance issue described above. This was a representative UI smoke, not an exhaustive click-through of every Workbench panel or chart gesture.

The focused EditMode suite checks cached-versus-direct production metrics for all six class/weapon combinations and the Mid-gear benchmark fixture, and compares incremental legal-node results against the full validator on randomized valid allocation paths and off-class budgets. Run it with:

```powershell
& 'D:\Unity\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\Unity\Projects\Black-Cube' -executeMethod BlackCube.BalanceWorkbench.WorkbenchPerformanceTestRunner.Focused -logFile 'D:\Unity\Projects\Black-Cube\Logs\BalanceWorkbenchPerformance\Focused.log'
```

For an interactive UI smoke, open **Black-Cube → Balance Workbench** after batchmode finishes. Visit Player Build, Passive Opt, Heatmap, Curves, and Performance; switch tabs and scroll result panels; run a small Greedy search, then inspect candidate/unique counts and elapsed time in Performance. On a chart, toggle a legend entry, zoom, pan, and export a PNG. On Heatmap, calculate a small build's marginals and verify node hover/click. Start a larger Beam search and press **Cancel Simulation** to check that cancellation is noticed at the next progress checkpoint. Use **Export Performance Report** to capture a CSV with the last operation, caches, GC collections, fingerprint, and commit. The Performance tab's repaint timing is a live diagnostic; headless batchmode does not provide a reliable IMGUI idle/repaint benchmark or visual-layout verification.

Further gains are most likely in production enemy generation and in avoiding complete analytical capture for every passive candidate while preserving the exact objective ranking. The reusable actor remains an isolated production `StatsComponent`/`PlayerController`, not a new formula implementation. No parallel worker execution was added because the relevant Unity production adapters are main-thread-dependent.
