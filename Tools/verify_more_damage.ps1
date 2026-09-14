$ErrorActionPreference = 'Stop'
$damageRoot = Split-Path $PSScriptRoot -Parent
$production = @('StatTypes','StatModifiers','StatValue','StatsComponent','RolledMod','Gear','StatusEffects','DamageContext','StatMappings','AilmentCalculator')
$source = "using UnityEngine;`nusing System.Collections.Generic;`nusing System.Linq;`n"
foreach ($name in $production) {
    $code = Get-Content -LiteralPath (Join-Path $damageRoot "Assets/Scripts/$name.cs") -Raw
    $source += ($code -replace '(?m)^using [^;]+;\r?\n', '') + "`n"
}
$harness = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'MoreDamageHarness.cs') -Raw
foreach ($actor in @('Player','Enemy')) {
    $name = if ($actor -eq 'Player') { 'PlayerController' } else { 'EnemyAI' }
    $code = Get-Content -LiteralPath (Join-Path $damageRoot "Assets/Scripts/$name.cs") -Raw
    $start = $code.IndexOf('    public DamageContext BuildNonCriticalAttackContext(')
    $end = $code.IndexOf('    public float GetFinalAttackSpeed()', $start)
    if ($start -lt 0 -or $end -le $start) { throw "Production $actor attack region not found" }
    # Includes complete actual noncritical, critical, base/off-element scaling and crit-chance methods.
    $harness = $harness.Replace("// $($actor.ToUpper())_ATTACK_METHODS", $code.Substring($start,$end-$start))
}
Add-Type -TypeDefinition ($source + $harness) -Language CSharp
$result = [MoreDamageHarness]::Run()
$result | Set-Content -LiteralPath (Join-Path $damageRoot 'ReviewCaptures/more-damage-focused-check.txt')
$result
