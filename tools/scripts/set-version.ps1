<#
 .SYNOPSIS
    Sets CI version build variables and/or returns version information.

 .DESCRIPTION
    The script is a wrapper around any versioning tool we use and abstracts it from
    the rest of the build system.
#>

$version = & (Join-Path $PSScriptRoot "get-version.ps1")

# Export version variables without using the four-component assembly version as the build number.
& $version.ToolPath @("cloud", "-c", "-a", "--skip-cloud-build-number")
if ($LastExitCode -ne 0) {
   throw "Error: 'nbgv cloud -c -a --skip-cloud-build-number' failed with $($LastExitCode)."
}

# Set build environment version numbers in pipeline context
Write-Host "Setting version build variables:"

Write-Host "##vso[build.updatebuildnumber]$($version.Full)"
Write-Host "##vso[task.setvariable variable=Version_Full;isOutput=true]$($version.Full)"
Write-Host "##vso[task.setvariable variable=Version_Prefix;isOutput=true]$($version.Prefix)"
