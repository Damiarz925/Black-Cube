# Repository Baseline

Date: 2026-09-13

This document records the repository cleanup baseline established before further gameplay work.

## Safety snapshot

- Safety branch: `codex/safety-pre-cleanup-20260913`
- Snapshot commit: `e431dd84` (`Safety snapshot before repository cleanup`)
- The snapshot preserves the complete non-ignored working state that existed before cleanup.

## Classification and disposition

### Preserved as intentional project work

- Current gameplay, UI, editor, and test scripts under `Assets/`
- Current scenes, prefabs, materials, ScriptableObjects, and project/package settings
- New Paper Battle character, enemy, environment, animation, damage-number, and UI art
- Documentation, verification tooling, and `ReviewCaptures` evidence/source material

### Retired as obsolete and unreferenced

- `Assets/Store Assets` and its root meta file

The retired Store Asset tree contained 39,833 tracked deletions. A GUID audit collected 20,357 GUIDs from the deleted metas and found zero references to those GUIDs in the remaining current project. The current build scenes and Paper Battle content use the replacement first-party art and prefabs.

### Removed as generated local state

- Plastic SCM workspace metadata in `.plastic/`
- Unity crash-recovery scenes in `Assets/_Recovery/`
- Unity/IDE-generated `.slnx` solution files
- Python `__pycache__` bytecode
- The empty root scratch file `_tmp_tier_levels.csv`

## Ignore policy

The root `.gitignore` now covers Unity caches/build output, IDE files including `.slnx`, Unity recovery scenes, Plastic SCM workspace state, Python bytecode caches, root `_tmp_*` scratch exports, large archive/media exports, and the previously established store-asset exclusions.

No `.gitattributes` or new Git LFS rules were added. The current repository has no active LFS-tracked files, and the largest preserved project files are small enough that introducing LFS during this cleanup would add migration risk without a clear benefit.

## Static validation

- No merge, rebase, cherry-pick, or index-conflict state was present before cleanup.
- Every current file under `Assets/` that requires a Unity meta has one.
- No orphan Unity meta files were found.
- Build settings contain `Assets/Scenes/Main Menu.unity` followed by `Assets/Scenes/SampleScene.unity`.
- No Unity Editor test or build pass was run as part of this repository-only cleanup.

