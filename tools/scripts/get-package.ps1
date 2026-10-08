<#
 .SYNOPSIS
    Returns the single OPC PLC package and its nuspec version from an artifact directory.
#>
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory,
    [switch] $RequirePreview
)

$ErrorActionPreference = 'Stop'
$packages = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter 'Microsoft.IoTEdge.OpcPlc.*.nupkg' -File)
if ($packages.Count -ne 1) {
    throw "Expected exactly one OPC PLC package in $PackageDirectory; found $($packages.Count)."
}

$archive = [System.IO.Compression.ZipFile]::OpenRead($packages[0].FullName)
try {
    $nuspecs = @($archive.Entries | Where-Object FullName -like '*.nuspec')
    if ($nuspecs.Count -ne 1) {
        throw 'Expected exactly one nuspec in the package.'
    }
    $reader = [System.IO.StreamReader]::new($nuspecs[0].Open())
    try {
        [xml] $nuspec = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
    if ($nuspec.package.metadata.id -ne 'Microsoft.IoTEdge.OpcPlc') {
        throw 'Unexpected package identity.'
    }
    $version = [string] $nuspec.package.metadata.version
    if ($RequirePreview -and $version -notmatch '^2\.16\.0-preview\.\d+$') {
        throw "Preview publication requires 2.16.0-preview.<number>; found $version."
    }
    $debugDependencies = @($nuspec.SelectNodes("//*[local-name()='dependency']") |
        Where-Object { $_.id -like 'OPCFoundation.*.Debug' })
    if ($RequirePreview -and $debugDependencies.Count -gt 0) {
        throw 'Preview publication requires the Release package, not Debug SDK dependencies.'
    }
    return [pscustomobject]@{ Path = $packages[0].FullName; Version = $version }
}
finally {
    $archive.Dispose()
}
