> Historical results: the additive-more rule in this milestone was superseded on 2026-09-08. See MORE-DAMAGE-MILESTONE.md for the current independent-roll formula and validation.

# Attack, stats and Legendary milestone

Implementation and bounded verification complete. Unity 6000.6.0f1, SampleScene. Existing changes preserved.

- PASS: shared noncritical attack/display calculation, immediate gear and skill refresh, zero-stat filtering, collapsible categories, status source/chance gating, Legendary five-Scrap yield and safeguards, and lethal-hit/DOT successor protection. Reproduce outside Play with **Black Cube > Play Checks > Verify Attack Stats and Status Gating**. Detailed assertions: `attack-stats-check.txt`.
- PASS: live stats UI showed separate chances, damage modifiers and duration sections. Armor changed primary damage 140 to 160; swapping to the Fire weapon changed it to 60. Collapsed Damage and Status categories retained their state.
- PASS: final compilation including Preview Legendary Tooltip. Editor assembly timestamp followed the final fixture source; Unity loaded its new menu and successfully entered fresh Play, with no C# compilation errors in Editor.log.
- PASS: final live Legendary action. Outside Play choose **Black Cube > Play Checks > Preview Legendary Tooltip**. Observe one Scrap x1 slot and Legendary Ring with **SCRAP ITEM / +5 SCRAP**. Focus the tooltip card body, then click the Scrap button. Observed ring and tooltip disappear, leaving exactly one Scrap x6 slot. Initial injected clicks did not activate the button; after card-body focus the action succeeded. No gameplay code changes were needed.
- PASS: exited Play using the toolbar. Edit-mode Forest1 view restored, no fixture scene saved, and no Assets/Scenes diff. No code was edited during Play.

Residual limitations: Chill/Shock responses remain existing placeholders with no production effect assets; gating was tested using disposable definitions. Existing Poison/Void stat mappings and ailment percentage scaling were not redesigned. NOT RUN: standalone build, exhaustive resolutions, or stress testing. Previously passed suites were not repeated for the final tooltip verification.

No work remains in this scoped milestone or is continuing.
