<#
 .SYNOPSIS
    Runs the standalone consumer against the exact Release package before publication.
#>
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory,
    [Parameter(Mandatory)]
    [string] $WorkingDirectory,
    [Parameter(Mandatory)]
    [string] $Source
)

$ErrorActionPreference = 'Stop'
$package = & (Join-Path $PSScriptRoot 'get-package.ps1') -PackageDirectory $PackageDirectory
if (Test-Path -LiteralPath $WorkingDirectory) {
    throw "Consumer working directory must not already exist: $WorkingDirectory"
}
$feed = Join-Path $WorkingDirectory 'feed'
$consumer = Join-Path $WorkingDirectory 'consumer'
$cache = Join-Path $WorkingDirectory 'cache'
New-Item -ItemType Directory -Path $feed, $consumer | Out-Null
Copy-Item -LiteralPath $package.Path -Destination $feed
$samples = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'samples'
foreach ($file in @('OpcUaUnitTests.csproj', 'OpcUaUnitTests.cs', 'OpcPlcBase.cs')) {
    Copy-Item -LiteralPath (Join-Path $samples $file) -Destination $consumer
}
$project = Join-Path $consumer 'OpcUaUnitTests.csproj'
$configPath = Join-Path $consumer 'NuGet.Config'
[xml] $config = '<configuration><packageSources><clear /></packageSources></configuration>'
foreach ($item in @(@{ Key = 'validated-package'; Value = $feed }, @{ Key = 'aio-brokers'; Value = $Source })) {
    $entry = $config.CreateElement('add')
    $entry.SetAttribute('key', $item.Key)
    $entry.SetAttribute('value', $item.Value)
    [void] $config.configuration.packageSources.AppendChild($entry)
}
$config.Save($configPath)
& dotnet restore $project --configfile $configPath --packages $cache `
    "-p:OpcPlcPackageVersion=$($package.Version)" -p:NuGetAudit=true -p:NuGetAuditMode=all
if ($LASTEXITCODE -ne 0) {
    throw "Packaged consumer restore failed with exit code $LASTEXITCODE."
}
& dotnet test $project --configuration Release --no-restore "-p:OpcPlcPackageVersion=$($package.Version)"
if ($LASTEXITCODE -ne 0) {
    throw "Packaged consumer tests failed with exit code $LASTEXITCODE."
}
Write-Host "Validated package $($package.Version)."
