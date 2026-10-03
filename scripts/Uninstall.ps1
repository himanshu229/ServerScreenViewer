[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$InstallDirectory = "$env:ProgramFiles\ServerScreenViewer",
    [switch]$RemoveUserConfiguration
)

$ErrorActionPreference = 'Stop'
Get-Process ServerScreenViewer -ErrorAction SilentlyContinue | Stop-Process -Force
Unregister-ScheduledTask -TaskName 'Server Screen Viewer' -Confirm:$false -ErrorAction SilentlyContinue

if (Test-Path $InstallDirectory) {
    Remove-Item $InstallDirectory -Recurse -Force
}
if ($RemoveUserConfiguration) {
    Remove-Item "$env:LOCALAPPDATA\ServerScreenViewer" -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host 'Server Screen Viewer was uninstalled.' -ForegroundColor Green
