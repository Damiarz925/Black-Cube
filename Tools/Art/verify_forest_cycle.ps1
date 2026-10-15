# Developer map: Compiles actual ZoneManager with minimal stubs and checks level boundaries without launching Unity.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
$ErrorActionPreference = 'Stop'
$zoneSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../Assets/Scripts/ZoneManager.cs') -Raw
$forestHarness = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ForestCycleHarness.cs') -Raw
Add-Type -TypeDefinition ("#pragma warning disable 0649`n" + $zoneSource + "`n" + $forestHarness) -Language CSharp
[ForestCycleHarness]::Run()
