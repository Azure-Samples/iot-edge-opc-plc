[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $PackagesDirectory,
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',
    [string] $DotNet = 'dotnet'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$packageRoot = [IO.Path]::GetFullPath($PackagesDirectory)
if ($packageRoot.Equals($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -or
    $packageRoot.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The package cache must be outside the repository to avoid project item exclusions.'
}
$fallbackRoot = "$packageRoot-fallback"
foreach ($cachePath in @($packageRoot, $fallbackRoot)) {
    if ((Test-Path $cachePath) -and (Get-ChildItem -LiteralPath $cachePath -Force | Select-Object -First 1)) {
        throw "Cold restore requires an empty directory: $cachePath"
    }
    New-Item -ItemType Directory -Path $cachePath -Force | Out-Null
}

Push-Location $repositoryRoot
try {
    & $DotNet restore opcplc.sln --force --no-http-cache --packages $packageRoot `
        "-p:Configuration=$Configuration" -p:UseLocalOpcUaStack=false -p:GeneratePackageOnBuild=false `
        -p:NuGetAudit=true -p:NuGetAuditMode=all -p:TreatWarningsAsErrors=true `
        "-p:RestoreFallbackFolders=$fallbackRoot" "-p:RestoreAdditionalProjectFallbackFolders=$fallbackRoot" `
        -p:DisableImplicitNuGetFallbackFolder=true --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Cold package restore failed with exit code $LASTEXITCODE."
    }

    $versions = [xml](Get-Content -Raw Directory.Packages.props)
    $opcVersions = @($versions.Project.ItemGroup.PackageVersion |
        Where-Object Include -Like 'OPCFoundation.*' |
        ForEach-Object { $_.Version.Trim([char[]]'[]') } | Select-Object -Unique)
    if ($opcVersions.Count -ne 1) {
        throw 'All direct OPC UA packages must use one SDK version.'
    }

    $allowedFolders = @($packageRoot, $fallbackRoot) |
        ForEach-Object { [IO.Path]::GetFullPath($_).TrimEnd([char[]]'\/') }
    $assetFiles = @(
        'src/obj/project.assets.json',
        'tests/obj/project.assets.json',
        'models/BoilerModel1/obj/project.assets.json'
    )
    foreach ($assetFile in $assetFiles) {
        $assets = Get-Content -Raw $assetFile | ConvertFrom-Json
        foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
            if ([IO.Path]::GetFullPath($folder).TrimEnd([char[]]'\/') -notin $allowedFolders) {
                throw "Unexpected package cache in ${assetFile}: $folder"
            }
        }
        $audit = $assets.project.restore.restoreAuditProperties
        if ($audit.enableAudit -ne 'true' -or $audit.auditMode -ne 'all') {
            throw "Package auditing was not enabled for all dependencies in $assetFile."
        }
        $opcLibraries = @($assets.libraries.PSObject.Properties | Where-Object Name -Like 'OPCFoundation.*')
        if ($opcLibraries.Count -eq 0) {
            throw "No OPC UA package dependencies were resolved in $assetFile."
        }
        foreach ($library in $opcLibraries) {
            if ($library.Value.type -ne 'package' -or $library.Name.Split('/')[-1] -ne $opcVersions[0]) {
                throw "Unexpected OPC UA dependency in ${assetFile}: $($library.Name)"
            }
        }
        Write-Host "Verified $assetFile : $($opcLibraries.Count) OPC packages at $($opcVersions[0]); audit enabled."
    }
    if (Get-ChildItem -LiteralPath $fallbackRoot -Force | Select-Object -First 1) {
        throw 'The isolated fallback cache must remain empty.'
    }
    Write-Host 'Cold restore verified with no machine package or fallback caches.'
}
finally {
    Pop-Location
}