[CmdletBinding()]
param(
    [string] $ConnectorPackageVersion = '1.2.0',
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

function Resolve-RequiredPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [Parameter(Mandatory = $true)]
        [string] $Description
    )

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    if ($resolved.Count -ne 1) {
        throw "Expected one path for $Description, found $($resolved.Count)."
    }

    return $resolved[0].Path
}

function Remove-DirectoryInside {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Directory,
        [Parameter(Mandatory = $true)]
        [string] $AllowedRoot
    )

    if (-not (Test-Path -LiteralPath $Directory)) {
        return
    }

    $resolvedDirectory = Resolve-RequiredPath -Path $Directory -Description 'directory to remove'
    $resolvedRoot = Resolve-RequiredPath -Path $AllowedRoot -Description 'allowed root'

    if (-not $resolvedDirectory.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove '$resolvedDirectory' because it is outside '$resolvedRoot'."
    }

    $deletePath = $resolvedDirectory
    if ($IsWindows -or $env:OS -eq 'Windows_NT') {
        $deletePath = "\\?\$resolvedDirectory"
    }

    [System.IO.Directory]::Delete($deletePath, $true)
}

$toolkitRoot = Resolve-RequiredPath -Path (Join-Path $PSScriptRoot '..') -Description 'Toolkit repository root'
$validationProject = Join-Path $toolkitRoot 'validation\MCEGold.Data.Services.Connector.PackageConsumer\MCEGold.Data.Services.Connector.PackageConsumer.csproj'
$nugetConfig = Join-Path $toolkitRoot 'validation\NuGet.config'
$artifactsRoot = Join-Path $toolkitRoot 'artifacts'
$packagesPath = Join-Path $artifactsRoot 'packages'
$validationProjectRoot = Split-Path -Parent $validationProject

foreach ($path in @($validationProject, $nugetConfig)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path not found: $path"
    }
}

if ([string]::IsNullOrWhiteSpace($ConnectorPackageVersion)) {
    throw 'ConnectorPackageVersion is required.'
}

Write-Host "Toolkit repository:   $toolkitRoot"
Write-Host "Configuration:        $Configuration"
Write-Host "Connector package:    MCEGold.Data.Services.Connector/$ConnectorPackageVersion"

Remove-DirectoryInside -Directory $packagesPath -AllowedRoot $toolkitRoot
Remove-DirectoryInside -Directory (Join-Path $validationProjectRoot 'bin') -AllowedRoot $toolkitRoot
Remove-DirectoryInside -Directory (Join-Path $validationProjectRoot 'obj') -AllowedRoot $toolkitRoot
New-Item -ItemType Directory -Path $packagesPath -Force | Out-Null

Write-Host 'Restoring PackageConsumer validation from configured NuGet sources...'
dotnet restore $validationProject --configfile $nugetConfig --packages $packagesPath --no-cache -p:RestoreNoCache=true -p:ConnectorPackageVersion=$ConnectorPackageVersion
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$assetsPath = Join-Path (Split-Path -Parent $validationProject) 'obj\project.assets.json'
if (-not (Test-Path -LiteralPath $assetsPath)) {
    throw "Restore assets file not found: $assetsPath"
}

$assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json
$connectorLibraryName = "MCEGold.Data.Services.Connector/$ConnectorPackageVersion"
if (-not ($assets.libraries.PSObject.Properties.Name -contains $connectorLibraryName)) {
    throw "Restored assets do not contain exact Connector package '$connectorLibraryName'."
}

$connectorLibrary = $assets.libraries.$connectorLibraryName
if ($connectorLibrary.type -ne 'package') {
    throw "Connector dependency resolved as '$($connectorLibrary.type)' instead of package."
}

Write-Host "Restored exact Connector package: $connectorLibraryName"

Write-Host 'Building PackageConsumer validation without restore...'
dotnet build $validationProject --configuration $Configuration --no-restore -m:1 -p:ConnectorPackageVersion=$ConnectorPackageVersion
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

foreach ($framework in @('net8.0', 'net10.0')) {
    Write-Host "Running PackageConsumer validation without restore for $framework..."
    dotnet run --project $validationProject --configuration $Configuration --no-restore --no-build --framework $framework -p:ConnectorPackageVersion=$ConnectorPackageVersion
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host 'Phase 1 package consumer validation completed successfully.'
