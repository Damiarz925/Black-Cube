# Defense Calibration

Open **Black-Cube → Balance Workbench → Open → Defense Calibration**.

The read-only resistance-pressure audit derives legal suffix families and tier midpoints from the production ModDatabase and item pools. Its rare-quality reference permits at most two suffixes per item and one copy of a family per item. “All Elemental Resistance” affects Fire, Cold and Lightning, never Void. The displayed suffix count is an investment-pressure example, **not** a reservation in the player gear optimizer. Earlier-level rows use the best legal tier available at that item level, capped at the 75% reference target. Re-run the audit after editing affix data.

For a survival check, select a Player Build and set the single **Combat Level**, trials, seed, duration and **No Renewable Life Recovery** flag, then choose **Run Full Level Sequence**. The simulator follows `WorldProgression.BossStage`: currently nine normal encounters and the boss at stage 10. Life and Mana carry between encounters; encounter-local Rage, buffs and enemy state reset. No-Recovery disables renewable player Life healing only; Mana recovery, damage, Armour, resistance and other normal combat logic remain active.

The result's **Run Context** records the exact Player Level, Combat Level, class/subclass/weapon, requested/completed trials, seed, recovery flag, build hash and production-data fingerprint. Changing controls or data afterward marks the old result **STALE**; it does not relabel the old figures. A cancelled run can have fewer completed trials than requested and must not be interpreted as a full sample. These are measurements of the supplied build, not automatically balanced targets for Life or Armour.
