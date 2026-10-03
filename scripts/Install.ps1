[CmdletBinding()]
param(
    [string]$InstallDirectory = "$env:ProgramFiles\ServerScreenViewer",
    [ValidateSet('win-x64','win-arm64')]
    [string]$Runtime = 'win-x64',
    [switch]$StartAtLogon
)

$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run PowerShell as Administrator to install under Program Files.'
}

$publish = Join-Path $env:TEMP "ServerScreenViewer-publish-$PID"
& (Join-Path $PSScriptRoot 'Build.ps1') -Runtime $Runtime -OutputDirectory $publish

New-Item -ItemType Directory -Force -Path $InstallDirectory | Out-Null
Copy-Item "$publish\*" $InstallDirectory -Recurse -Force
Remove-Item $publish -Recurse -Force

$exe = Join-Path $InstallDirectory 'ServerScreenViewer.exe'
if ($StartAtLogon) {
    $taskName = 'Server Screen Viewer'
    $action = New-ScheduledTaskAction -Execute $exe
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $identity.Name
    $taskPrincipal = New-ScheduledTaskPrincipal -UserId $identity.Name -LogonType Interactive -RunLevel Highest
    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $taskPrincipal -Description 'Starts Server Screen Viewer in the signed-in interactive session.' -Force | Out-Null
    Write-Host "Installed and registered '$taskName' to start when $($identity.Name) signs in." -ForegroundColor Green
} else {
    Write-Host 'Installed. Start-at-logon was not enabled.' -ForegroundColor Yellow
}

Start-Process $exe
Write-Host "Application path: $exe" -ForegroundColor Green
Write-Host "Configuration: $env:LOCALAPPDATA\ServerScreenViewer\appsettings.json"
