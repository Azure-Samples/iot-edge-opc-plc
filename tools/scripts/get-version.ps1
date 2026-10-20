<#
 .SYNOPSIS
    Sets CI version build variables and/or returns version information.

 .DESCRIPTION
    The script is a wrapper around any versioning tool we use and abstracts it from
    the rest of the build system.
#>

try {
    $repositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    [xml] $packages = Get-Content -Raw (Join-Path $repositoryRoot 'Directory.Packages.props')
    $toolVersion = ($packages.Project.ItemGroup.PackageVersion |
        Where-Object Include -eq 'Nerdbank.GitVersioning').Version
    $toolDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "opcplc-nbgv-$toolVersion"
    $toolName = if ($IsWindows) { 'nbgv.exe' } else { 'nbgv' }
    $toolPath = Join-Path $toolDirectory $toolName
    if (-not (Test-Path -LiteralPath $toolPath)) {
        & dotnet tool install nbgv --tool-path $toolDirectory --version $toolVersion | Out-Host
        if ($LastExitCode -ne 0) {
            throw "Unable to install nbgv $toolVersion."
        }
    }

    $props = (& $toolPath @("get-version", "-f", "json")) | ConvertFrom-Json
    if ($LastExitCode -ne 0) {
        throw "Error: 'nbgv get-version -f json' failed with $($LastExitCode)."
    }

    return [pscustomobject] @{
        Full = $props.CloudBuildAllVars.NBGV_NuGetPackageVersion
        Prefix = $props.CloudBuildAllVars.NBGV_SimpleVersion
        ToolPath = $toolPath
    }
}
catch {
    throw
}
