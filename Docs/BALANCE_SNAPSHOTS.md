# Balance Snapshots

Open **Balance Workbench → Balance Snapshots**. Add the current player, combat, or drop scenario to a named, user-editable suite. Save/load the suite as JSON. **Capture Before**, make a controlled production authoring change, then **Capture After**. A/B comparison shows numeric and relative deltas. **Compare to Current** reruns a loaded Before suite against the current production data. The highlight percentage is your reporting preference, not a game-balance rule.

Snapshots save the suite, summarized metrics, timestamp, Git commit, and production fingerprint. Export Before/After JSON and comparison CSV. Loading a snapshot from JSON supports continuing a comparison after closing the Editor. A stale warning appears when the saved fingerprint differs from current production.

Suite sources include player analytical, headless combat, enemy median DPS, production drops, sensitivity maximum impact, best legal affix objective delta, loot upgrades per 100 kills, and crafting success rate. Choose small sample counts in a suite you intend to recapture frequently. **Open Source** on a comparison row restores the saved scenario in its owning Workbench tab. Snapshots do not retain enormous per-fight traces.
