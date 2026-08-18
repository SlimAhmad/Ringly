<#
.SYNOPSIS
Updates the LAN IP used by coturn and the MAUI sample after switching networks (Wi-Fi, mobile
hotspot, etc.), and restarts the coturn container so it picks up the change immediately.

.DESCRIPTION
Four files need to agree on the host machine's current LAN IP, and a stale value in any one of
them causes a *silent* mid-call failure (ICE/TURN candidates pointing at an address unreachable
from a real device), not a loud connection-time error - see docker/coturn/turnserver.conf's
external-ip comment, docker/asterisk/config/pjsip.conf's external_media_address comment, and
docker/asterisk/config/rtp.conf's externaddr comment for the full story:
  - docker/coturn/turnserver.conf's external-ip (what coturn advertises as the relay candidate
    address)
  - docker/asterisk/config/pjsip.conf's external_media_address/external_signaling_address (what
    Asterisk's SIP/SDP layer advertises as ITS candidate address - Asterisk terminates/relays
    media per-leg for webrtc=yes endpoints, so its own container-internal IP being unreachable
    from outside Docker is just as real a failure mode as coturn's)
  - docker/asterisk/config/rtp.conf's externaddr (the RTP/ICE engine's OWN, completely separate
    NAT-traversal setting - pjsip.conf's external_media_address only rewrites the SDP's "c="
    line; the actual "a=candidate:...typ host" line real clients dial into comes from here)
  - samples/Ringly.Samples.Maui/MauiProgram.cs's CurrentLanHostAddress constant (what the MAUI
    app on a real Android device uses to reach Asterisk/coturn)

This script detects the current IPv4 address on the given network adapter, updates all four
files, and rebuilds+recreates the coturn and asterisk containers - replacing the previous
multi-step manual process (find IP, edit files, rebuild containers) with one command.

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
$pjsipConf = Join-Path $PSScriptRoot "asterisk\config\pjsip.conf"
$rtpConf = Join-Path $PSScriptRoot "asterisk\config\rtp.conf"
$mauiProgram = Join-Path $repoRoot "samples\Ringly.Samples.Maui\MauiProgram.cs"
$composeFile = Join-Path $PSScriptRoot "docker-compose.yml"

$ip = Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias $InterfaceAlias -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notmatch '^169\.254\.' } |
    Select-Object -First 1 -ExpandProperty IPAddress

if (-not $ip) {
    throw "Could not find a routable IPv4 address on interface '$InterfaceAlias'. Run Get-NetIPAddress to see available adapters, then pass -InterfaceAlias."
}

Write-Host "Detected LAN IP: $ip (interface: $InterfaceAlias)"

# turnserver.conf: replace the whole external-ip=... line. Checking the RESULT contains the
# target line (not "did the content change") avoids a false-positive warning when the IP happens
# to already be correct - a same-value replace leaves $updated identical to the original even
# though the regex matched and did its job.
$turnConfContent = Get-Content $turnConf -Raw
$updatedTurnConf = $turnConfContent -replace '(?m)^external-ip=.*$', "external-ip=$ip"
if ($updatedTurnConf -notmatch "(?m)^external-ip=$([regex]::Escape($ip))$") {
    Write-Warning "No external-ip= line found in $turnConf - check the file wasn't restructured."
}
Set-Content -Path $turnConf -Value $updatedTurnConf -NoNewline
Write-Host "Updated $turnConf"

# pjsip.conf: same "ringly:lan-ip" marker convention as MauiProgram.cs below, but there are two
# values on two separate lines right after the marker (external_media_address and
# external_signaling_address), both needing the same new IP.
$pjsipContent = Get-Content $pjsipConf -Raw
$pjsipPattern = '(; ringly:lan-ip\r?\n\[transport-udp\]\r?\ntype = transport\r?\nprotocol = udp\r?\nbind = 0\.0\.0\.0\r?\nexternal_media_address = )[^\r\n]*(\r?\nexternal_signaling_address = )[^\r\n]*'
$pjsipEvaluator = [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $m.Groups[1].Value + $ip + $m.Groups[2].Value + $ip }
$updatedPjsipContent = [System.Text.RegularExpressions.Regex]::Replace($pjsipContent, $pjsipPattern, $pjsipEvaluator)
if ($updatedPjsipContent -eq $pjsipContent -and $pjsipContent -notmatch [regex]::Escape($ip)) {
    Write-Warning "ringly:lan-ip marker not found in $pjsipConf - check the file wasn't restructured."
}
Set-Content -Path $pjsipConf -Value $updatedPjsipContent -NoNewline
Write-Host "Updated $pjsipConf"

# rtp.conf: same marker convention, single externaddr=... line right after it.
$rtpContent = Get-Content $rtpConf -Raw
$updatedRtpContent = $rtpContent -replace '(?m)(?<=; ringly:lan-ip\r?\n)externaddr=.*$', "externaddr=$ip"
if ($updatedRtpContent -eq $rtpContent -and $rtpContent -notmatch [regex]::Escape($ip)) {
    Write-Warning "ringly:lan-ip marker not found in $rtpConf - check the file wasn't restructured."
}
Set-Content -Path $rtpConf -Value $updatedRtpContent -NoNewline
Write-Host "Updated $rtpConf"

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

# NOT "restart" for either container - both coturn/Dockerfile and asterisk/Dockerfile COPY their
# config into the image at build time rather than bind-mounting it, so a plain restart brings the
# container back up on the OLD image with the OLD baked-in values. Confirmed live (twice - once
# for coturn, then again for Asterisk's own pjsip.conf): every previous "restart" in this script
# and by hand appeared to succeed (clean startup logs) while the running container silently kept
# serving stale config from a much earlier build - calls were routing media to an unreachable
# address the whole time, with no error at connection time, only broken/silent audio and choppy
# video partway into calls.
#
# Also explicitly removing the container and image before rebuilding, not just "up -d --build" -
# confirmed live (twice) that Docker Desktop can leave the *running* container silently serving a
# stale pre-fix image even after a "successful" --build/--force-recreate, most likely a
# containerd-vs-classic image store disagreement about what the "latest" tag currently points to.
# A plain "docker run" of the freshly-built image showed the correct content while the
# compose-managed container did not, until the old container AND image were removed outright first.
Write-Host "Rebuilding coturn and asterisk (removing old containers/images first - see comment above for why a plain restart or --build alone isn't reliable enough)..."
docker compose -f $composeFile stop coturn asterisk
docker rm -f docker-coturn-1 docker-asterisk-1 2>$null
docker rmi docker-coturn:latest docker-asterisk:latest 2>$null
docker compose -f $composeFile up -d --build coturn asterisk

Write-Host ""
Write-Host "Done. Rebuild/redeploy the MAUI app (Windows + Android) to pick up the new host address."
