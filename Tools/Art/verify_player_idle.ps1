# Developer map: Compatibility entry point that runs the current combat/idle harness in verify_combat_animation.ps1.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
# Shared file-only harness: installed idle, attack timing and legacy ghoul poses.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'verify_combat_animation.ps1')
