# Crafting Simulator

Open **Balance Workbench → Crafting Simulator**. Load the current build weapon as the starting item. Add target affix conditions (family, strongest acceptable tier number, minimum roll), required match count, and an ordered list of production crafting actions. Set trials, seed, and maximum actions per attempt. A positive budget limits an action; zero means unlimited. The simulator calls production `EquipmentCrafting` and `ModManager` with a deterministic random stream, including real Crafting Potential costs and failure rules.

Results include success rate, Potential and budget failures, action-count and final-match percentiles, per-currency mean/P50/P90/P95/P99 cost, and a replayable action trace (favoring a failed attempt when available). Export the summary CSV and trace JSON. Save/load a policy as JSON or add two to five policies to the comparison list and run them under their saved seeds. Do not treat mixed currency types as a fictitious gold equivalent.

Current scope is ordinary equipment operations. Ancient relic crafting, boss infusion, Empowerment, and Implicit Reforge are not in this simulator. Policy execution is an ordered repeating sequence; there is no bounded automatic policy search yet. Starting-item validity matters: use a real saved build item with an implicit and crafting state.
