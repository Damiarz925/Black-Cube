# Changelog: Recent Gameplay and UI Additions

This document captures **all recent additions/changes** made across the last set of gameplay and UI updates, with implementation-level specificity for testing and inspector setup.

---

## 1) Level Progression / Kill Target / Boss Flow

### What was added/changed
- Standardized per-level kill target before boss spawn to a fixed value of **10**.
- Kill tracking resets at level start.
- Boss spawn decision remains: once kill threshold reached, the **next spawned enemy** becomes boss.
- Boss death triggers zone clear logic and progresses to next level.

### Implementation details
- `ZoneManager.GetEnemiesToKillBeforeBoss(int zoneLevel)` now returns `10` consistently.
- `GameManager.StartZone(int zoneLevel)`:
  - resets `enemiesKilledInZone` and `bossSpawned`
  - fetches threshold from `ZoneManager`
  - synchronizes `zoneManager.zoneLevel = zoneLevel`
- `GameManager.OnEnemyKilled(...)`:
  - increments kill count
  - checks `!bossSpawned && enemiesKilledInZone >= enemiesToKillBeforeBoss`
  - spawns boss when condition passes
  - on boss death, triggers `OnZoneCleared()`

### Debug logging added for this area
- Logs threshold query in `ZoneManager.GetEnemiesToKillBeforeBoss(...)`.
- Logs zone-level sync in `GameManager.StartZone(...)`.
- Logs loot generation + add-to-inventory result, and warns if inventory singleton missing.

---

## 2) Player Death Flow: Death Menu (instead of immediate restart)

### What was added/changed
- Replaced immediate “respawn on death” behavior with a **death menu** flow.
- Death menu now displays:
  - title: `You Died`
  - killer enemy level
  - killer enemy rarity
  - killer weapon main element
- Added one-shot guard to avoid duplicate death handling from repeated callbacks.

### Implementation details
- `GameManager.OnPlayerKilled(...)` now:
  - uses `playerDeathHandled` guard
  - gets killer info from `BattleManager.CurrentEnemyAI`
  - formats and forwards details to `DeathMenuUI.Show(...)`
- `BattleManager` now exposes current enemy via `CurrentEnemyAI`.
- `EnemyAI` now exposes:
  - `EnemyLevel`
  - `WeaponMainElement`
- `HealthComponent.Die()` remains responsible for reporting player death to `GameManager`.

### Debug logging added for this area
- Player death entry log with resolved killer details.
- Warning when death menu UI reference is missing.
- Enemy/player death logs in `HealthComponent.Die()`.

---

## 3) Death Menu Buttons + Actions

### What was added/changed
Added a new `DeathMenuUI` controller with three buttons:
1. **Restart Level**
2. **Return to Main Menu**
3. **Quit Game**

### Implementation details
- `DeathMenuUI` serialized references:
  - `root`
  - `titleText`
  - `detailsText`
  - `restartLevelButton`
  - `returnToMainMenuButton`
  - `quitGameButton`
  - `mainMenuSceneName` (default `MainMenu`)
- `Show(...)`:
  - enables menu root
  - writes “You Died” and killer detail text
  - pauses via `Time.timeScale = 0f`
- `Hide()`:
  - disables menu root
  - resumes via `Time.timeScale = 1f`
- Button handlers:
  - restart -> calls `GameManager.RestartCurrentLevelAfterDeath()`
  - return to main menu -> `SceneManager.LoadScene(mainMenuSceneName)`
  - quit -> `Application.Quit()` (editor-safe debug note remains)

### Debug logging added for this area
- Awake-time logging of configuration (`root`, `mainMenuSceneName`).
- Warning logs for unassigned button references.
- Logs for show/hide transitions.
- Logs for each button click action and target scene load.

---

## 4) Restart Current Level After Death

### What was added/changed
- Added explicit restart method for “same current level” recovery after death menu interaction.

### Implementation details
- `GameManager.RestartCurrentLevelAfterDeath()`:
  - resets `enemiesKilledInZone = 0`
  - resets `bossSpawned = false`
  - clears `playerDeathHandled`
  - hides death menu
  - calls `BattleManager.RespawnPlayerAtLevelStart()`
- `BattleManager.RespawnPlayerAtLevelStart()`:
  - moves player to `playerSpawnPoint` if assigned
  - restores player HP by calling `playerHealth.ReviveToFullLife()`
  - spawns a fresh normal enemy (`SpawnNextEnemy(spawnBoss: false)`)

### Debug logging added for this area
- Logs restart entry from `GameManager`.
- Logs respawn call dispatch.
- Logs spawn point move and position.
- Warning if spawn point or player health refs are missing.
- Logs player revive HP value and new enemy spawn intent.

---

## 5) Main Menu Scaffolding

### What was added/changed
- Added `MainMenuUI` script scaffold for a future main menu canvas.
- Added serialized button hooks for:
  - Achievements
  - New Game
  - Load Game
- No gameplay functionality wired yet (as requested).

### Implementation details
- `MainMenuUI` registers button listeners in `Awake()`.
- Placeholder handlers currently only log clicks.

### Debug logging added for this area
- Awake log with root object name.
- Warning logs for any unassigned button fields.
- Existing click handler debug logs retained.

---

## 6) Inspector Setup Notes (for testing these features)

### Game scene
- `GameManager`:
  - assign `deathMenuUI` (or rely on runtime auto-find)
- `BattleManager`:
  - ensure `player`, `playerSpawnPoint`, `enemySpawnPoint` are assigned
- `DeathMenuUI`:
  - assign root panel + TMP text fields + three buttons
  - set `mainMenuSceneName` to your actual menu scene name

### Main menu scene
- Add `MainMenuUI` to your main menu canvas/controller object.
- Assign `achievementsButton`, `newGameButton`, `loadGameButton`.

---

## 7) Debug Verification Checklist (playmode)

1. Start run: confirm zone start logs show kill threshold and zone-level sync.
2. Kill 10 non-boss enemies: confirm boss-next-spawn log appears.
3. Die to an enemy: confirm death menu appears and logs include killer level/rarity/element.
4. Click **Restart Level**:
   - time resumes
   - player teleports to spawn point
   - HP restored to full
   - enemy kill counter reset behavior observed via logs
   - new normal enemy spawns
5. Click **Return to Main Menu**: verify target scene name log and scene load.
6. Click **Quit Game** in build: verify app closes (editor will only log).

