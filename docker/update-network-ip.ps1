<#
.SYNOPSIS
Updates the LAN IP used by coturn and the MAUI sample after switching networks (Wi-Fi, mobile
hotspot, etc.), and restarts the coturn container so it picks up the change immediately.

.DESCRIPTION
Two files need to agree on the host machine's current LAN IP, and a stale value in either one
causes a *silent* mid-call failure (ICE/TURN relay candidates unreachable), not a loud
connection-time error - see docker/coturn/turnserver.conf's external-ip comment for the full
story:
  - docker/coturn/turnserver.conf's external-ip (what coturn advertises as the relay candidate
    address)
  - samples/Ringly.Samples.Maui/MauiProgram.cs's CurrentLanHostAddress constant (what the MAUI
    app on a real Android device uses to reach Asterisk/coturn)

This script detects the current IPv4 address on the given network adapter, updates both files,
and restarts the coturn container - replacing the previous three-step manual process (find IP,
edit turnserver.conf, edit MauiProgram.cs, restart coturn) with one command.

.PARAMETER InterfaceAlias
Which network adapter to read the IP from. Defaults to "Wi-Fi" - pass a different value (e.g.
"Ethernet") if testing over a different adapter. Run Get-NetIPAddress yourself first if unsure
which alias your current network shows up under.

.EXAMPLE
.\docker\update-network-ip.ps1

.EXAMPLE
.\docker\update-network-ip.ps1 -InterfaceAlias "Ethernet"
#>
param(
    [string]$InterfaceAlias = "Wi-Fi"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$turnConf = Join-Path $PSScriptRoot "coturn\turnserver.conf"
$mauiProgram = Join-Path $repoRoot "samples\Ringly.Samples.Maui\MauiProgram.cs"
$composeFile = Join-Path $PSScriptRoot "docker-compose.yml"

$ip = Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias $InterfaceAlias -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notmatch '^169\.254\.' } |
    Select-Object -First 1 -ExpandProperty IPAddress

if (-not $ip) {
    throw "Could not find a routable IPv4 address on interface '$InterfaceAlias'. Run Get-NetIPAddress to see available adapters, then pass -InterfaceAlias."
}

Write-Host "Detected LAN IP: $ip (interface: $InterfaceAlias)"

# turnserver.conf: replace the whole external-ip=... line.
$turnConfContent = Get-Content $turnConf -Raw
$updatedTurnConf = $turnConfContent -replace '(?m)^external-ip=.*$', "external-ip=$ip"
if ($updatedTurnConf -eq $turnConfContent) {
    Write-Warning "No external-ip= line found/changed in $turnConf - check the file wasn't restructured."
}
Set-Content -Path $turnConf -Value $updatedTurnConf -NoNewline
Write-Host "Updated $turnConf"

# MauiProgram.cs: replace only the string literal on the line right after the "ringly:lan-ip"
# marker comment, so this keeps working even if the constant gets renamed again later.
$mauiContent = Get-Content $mauiProgram -Raw
$pattern = '(// ringly:lan-ip\r?\n\s*const string \w+ = ")[^"]*(";)'
$evaluator = [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $m.Groups[1].Value + $ip + $m.Groups[2].Value }
$updatedMauiContent = [System.Text.RegularExpressions.Regex]::Replace($mauiContent, $pattern, $evaluator)
if ($updatedMauiContent -eq $mauiContent -and $mauiContent -notmatch [regex]::Escape($ip)) {
    Write-Warning "ringly:lan-ip marker not found in $mauiProgram - check the file wasn't restructured."
}
Set-Content -Path $mauiProgram -Value $updatedMauiContent -NoNewline
Write-Host "Updated $mauiProgram"

Write-Host "Restarting coturn..."
docker compose -f $composeFile restart coturn

Write-Host ""
Write-Host "Done. Rebuild/redeploy the MAUI app (Windows + Android) to pick up the new host address."
