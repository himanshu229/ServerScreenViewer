[CmdletBinding()]
param(
    [ValidateSet('win-x64','win-arm64')]
    [string]$Runtime = 'win-x64',
    [string]$OutputDirectory = "$PSScriptRoot\..\publish"
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\ServerScreenViewer\ServerScreenViewer.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 8 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/8.0'
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

dotnet publish $project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $OutputDirectory `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

Write-Host "Published to $((Resolve-Path $OutputDirectory).Path)" -ForegroundColor Green
