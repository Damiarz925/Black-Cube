# Balance Reporting

Configure a named suite in **Balance Snapshots**, then open **Balance Report**. **Run Report** executes those scenarios and writes a Markdown report, CSV metric table, PNG chart, and JSON metadata to `ReviewCaptures/BalanceWorkbench`. The Markdown links to its generated data and chart. Cancel via the Workbench footer during a long run.

The report contains exactly the scenarios you placed in the suite, plus any comparison currently displayed in Balance Snapshots. It has no universal balance score and makes no automatic balance edits. Report warnings are limited to technical failures or the comparison threshold configured in Balance Snapshots; designers decide whether a numerical change is desirable.

Current reporting is a compact scenario table and chart, not a customizable widget-layout editor. Add separate scenarios to cover player, enemy, combat, and drops; affix, loot-progression, crafting, and breakpoint results are exported from their own tabs rather than embedded as report widgets in this first pass.
