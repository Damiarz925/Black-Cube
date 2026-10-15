# Developer map: Compiles actual selected C# sources with CombatTimingHarness in a fresh PowerShell process and runs assertions. Dependency stubs limit this to logic verification.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
$ErrorActionPreference = 'Stop'
$artRoot = Join-Path $PSScriptRoot '../..'
$combatSource = Get-Content -LiteralPath (Join-Path $artRoot 'Assets/Scripts/BattleManager.cs') -Raw
$spriteSource = Get-Content -LiteralPath (Join-Path $artRoot 'Assets/Scripts/PaperSpriteActor.cs') -Raw
$testSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'CombatTimingHarness.cs') -Raw
$playerSource = Get-Content -LiteralPath (Join-Path $artRoot 'Assets/Scripts/PlayerController.cs') -Raw
$equipMethod = [regex]::Match($playerSource, 'public void EquipWeapon\(Gear weapon\)\s*\{[^}]+\}').Value
$equipGetter = [regex]::Match($playerSource, 'public Gear EquippedWeapon => equippedWeapon;').Value
if (!$equipMethod -or !$equipGetter) { throw 'Actual PlayerController equipment contract was not found.' }
$testSource = $testSource.Replace('// PLAYER_EQUIP_METHOD', $equipMethod).Replace('// PLAYER_EQUIPMENT_GETTER', $equipGetter)
$profiles = (Get-Content -LiteralPath (Join-Path $artRoot 'Assets/Scripts/PaperWeaponVisual.cs') -Raw) + "`n" + (Get-Content -LiteralPath (Join-Path $artRoot 'Assets/Scripts/PaperPlayerAnimationSet.cs') -Raw)
$profiles = $profiles -replace '(?m)^using UnityEngine;\r?\n', ''
$enemyProfile = Get-Content -LiteralPath (Join-Path $artRoot 'Assets/Scripts/PaperEnemyAnimationSet.cs') -Raw
$profiles += "`n" + ($enemyProfile -replace '(?m)^using UnityEngine;\r?\n', '')
$spriteSource = $spriteSource -replace '(?m)^using UnityEngine;\r?\n', ''
Add-Type -TypeDefinition ("#pragma warning disable 0649`n" + $combatSource + "`n" + $spriteSource + "`n" + $profiles + "`n" + $testSource) -Language CSharp
[CombatTimingHarness]::Run()
