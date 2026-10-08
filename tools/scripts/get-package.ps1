<#
 .SYNOPSIS
    Returns the single OPC PLC package and its nuspec version from an artifact directory.
#>
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory
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
    return [pscustomobject]@{ Path = $packages[0].FullName; Version = $version }
}
finally {
    $archive.Dispose()
}
