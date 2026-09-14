$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../..'
$receiver = Get-Content -LiteralPath (Join-Path $root 'Assets/Scripts/DamageReceiver.cs') -Raw
$harness = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'DamageReceiverHitHarness.cs') -Raw
Add-Type -TypeDefinition ("#pragma warning disable 0649`n" + $receiver + "`n" + $harness) -Language CSharp
[DamageReceiverHitHarness]::Run()
