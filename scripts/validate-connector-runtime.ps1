[CmdletBinding()]
param(
    [string] $ConnectorPackageVersion = '1.2.0',
    [string] $Config,
    [string] $Configuration = 'Release',
    [ValidateSet('smoke', 'requests', 'attach', 'cli', 'full')]
    [string] $Mode = 'full',
    [int] $TimeoutSeconds = 60,
    [int] $PollIntervalSeconds = 2,
    [switch] $RemoveResponse,
    [switch] $CustomRequestRouting,
    [switch] $CustomPublicationRouting,
    [switch] $Net8Smoke
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

if ([string]::IsNullOrWhiteSpace($Config)) {
    $Config = Join-Path $PSScriptRoot '..\configs\connector.config.development.json'
}

$toolkitRoot = Resolve-RequiredPath -Path (Join-Path $PSScriptRoot '..') -Description 'Toolkit repository root'
$configPath = Resolve-RequiredPath -Path $Config -Description 'runtime config'
$runtimeProject = Join-Path $toolkitRoot 'validation\MCEGold.Data.Services.Connector.RuntimeSmoke\MCEGold.Data.Services.Connector.RuntimeSmoke.csproj'
$nugetConfig = Join-Path $toolkitRoot 'validation\NuGet.config'
$artifactsRoot = Join-Path $toolkitRoot 'artifacts'
$packagesPath = Join-Path $artifactsRoot 'packages-runtime'
$runtimeProjectRoot = Split-Path -Parent $runtimeProject

foreach ($path in @($runtimeProject, $nugetConfig)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path not found: $path"
    }
}

$ignored = git -C $toolkitRoot check-ignore -v -- $configPath
if ($LASTEXITCODE -ne 0) {
    throw "Runtime config is not ignored by Git: $configPath"
}
Write-Host 'Runtime config: found and Git-ignored'

if ([string]::IsNullOrWhiteSpace($ConnectorPackageVersion)) {
    throw 'ConnectorPackageVersion is required.'
}

Write-Host "Toolkit repository:   $toolkitRoot"
Write-Host "Configuration:        $Configuration"
Write-Host "Connector package:    MCEGold.Data.Services.Connector/$ConnectorPackageVersion"
Write-Host "Validation mode:      $Mode"

Remove-DirectoryInside -Directory $packagesPath -AllowedRoot $toolkitRoot
Remove-DirectoryInside -Directory (Join-Path $runtimeProjectRoot 'bin') -AllowedRoot $toolkitRoot
Remove-DirectoryInside -Directory (Join-Path $runtimeProjectRoot 'obj') -AllowedRoot $toolkitRoot
New-Item -ItemType Directory -Path $packagesPath -Force | Out-Null

Write-Host 'Restoring RuntimeSmoke validation from configured NuGet sources...'
dotnet restore $runtimeProject --configfile $nugetConfig --packages $packagesPath --no-cache -p:RestoreNoCache=true -p:ConnectorPackageVersion=$ConnectorPackageVersion
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$assetsPath = Join-Path $runtimeProjectRoot 'obj\project.assets.json'
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

$projectText = Get-Content -LiteralPath $runtimeProject -Raw
if ($projectText -match '<ProjectReference' -or $projectText -match '<Reference') {
    throw 'RuntimeSmoke project must not contain ProjectReference or direct Reference entries.'
}

Write-Host "Restored exact Connector package: $connectorLibraryName"
Write-Host "Resolved dependency type: $($connectorLibrary.type)"

Write-Host 'Building RuntimeSmoke validation without restore...'
dotnet build $runtimeProject --configuration $Configuration --no-restore -m:1 -p:ConnectorPackageVersion=$ConnectorPackageVersion
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Running RuntimeSmoke validation without restore...'
$runtimeArgs = @(
    '--config', $configPath,
    '--mode', $Mode,
    '--timeout-seconds', $TimeoutSeconds,
    '--poll-interval-seconds', $PollIntervalSeconds
)
if ($RemoveResponse) { $runtimeArgs += '--remove-response' }
if ($CustomRequestRouting) { $runtimeArgs += '--custom-request-routing' }
if ($CustomPublicationRouting) { $runtimeArgs += '--custom-publication-routing' }
if ($Net8Smoke) { $runtimeArgs += '--net8-smoke' }

dotnet run --project $runtimeProject --configuration $Configuration --no-restore --no-build -- @runtimeArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Runtime validation completed successfully for mode '$Mode'."
