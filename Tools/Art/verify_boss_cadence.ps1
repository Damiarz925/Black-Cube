# Developer map: Compiles actual GameManager with production ZoneManager quota and HealthComponent death-claim methods into a file-only encounter progression harness. Does not launch Unity.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
# Compile production progression logic with bounded dependencies; no Unity launch.
$ErrorActionPreference = 'Stop'
$cadenceRoot = Join-Path $PSScriptRoot '../..'
$gameCode = Get-Content (Join-Path $cadenceRoot 'Assets/Scripts/GameManager.cs') -Raw
$zoneCode = Get-Content (Join-Path $cadenceRoot 'Assets/Scripts/ZoneManager.cs') -Raw
$healthCode = Get-Content (Join-Path $cadenceRoot 'Assets/Scripts/HealthComponent.cs') -Raw
$harness = Get-Content (Join-Path $PSScriptRoot 'BossCadenceHarness.cs') -Raw
$quota = [regex]::Match($zoneCode,'public int GetEnemiesToKillBeforeBoss\(int zoneLevel\)\s*\{[\s\S]*?\n    \}').Value
$claim = [regex]::Match($healthCode,'public bool TryClaimEnemyDeath\(\)\s*\{[\s\S]*?\n    \}').Value
if (!$quota -or !$claim) { throw 'Production quota/death-claim method not found' }
$harness = $harness.Replace('// PRODUCTION_QUOTA',$quota).Replace('// PRODUCTION_CLAIM',$claim)
Add-Type -TypeDefinition ("#pragma warning disable 0649`n" + $gameCode + "`n" + $harness) -Language CSharp
[BossCadenceHarness]::Run()
