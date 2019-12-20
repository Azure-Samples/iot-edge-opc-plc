<#
 .SYNOPSIS
    Creates the container build matrix from the mcr.json files in the tree.

 .DESCRIPTION
    The script traverses the build root to find all folders with an mcr.json
    file and populates the matrix to create the individual build jobs.

 .PARAMETER BuildRoot
    The root folder to start traversing the repository from.

 .PARAMETER Build
    If not set the task generates jobs in azure pipeline.
#>

Param(
    [string] $BuildRoot = $null,
    [switch] $Build,
    [switch] $Debug
)

if ([string]::IsNullOrEmpty($BuildRoot)) {
    $BuildRoot = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "*.sln"
}

$acrMatrix = @{}

# Traverse from build root and find all mcr.json metadata files to acr matrix
Get-ChildItem $BuildRoot -Recurse -Include "mcr.json" `
    | ForEach-Object {

    # Get root
    $dockerFolder = $_.DirectoryName.Replace($BuildRoot, "").Substring(1)
    $metadata = Get-Content -Raw -Path $_.FullName | ConvertFrom-Json
    Write-Host "dockerFolder: $dockerFolder"
    Write-Host "metadata: $metadata"
    try {
        $jobName = "$($metadata.name)"
        if (![string]::IsNullOrEmpty($metadata.tag)) {
            $jobName = "$($jobName)/$($metadata.tag)"
        }
        if (![string]::IsNullOrEmpty($jobName)) {
            $acrMatrix.Add($jobName, @{ "dockerFolder" = $dockerFolder })
        }
        Write-Host "WH $($metadata.name) - $($metadata.tag)"
    }
    catch {
        # continue to next
    }
}

Write-Host "build present: $($Build.IsPresent)"

if ($Build.IsPresent) {
    Write-Host "acr matrix values: $($acrMatrix.Values)"
    $acrMatrix.Values | ForEach-Object {
        & (Join-Path $PSScriptRoot "acr-build.ps1") `
            -Path $_.dockerFolder -Debug:$Debug
    }
}
else {
    # Set pipeline variable
    Write-Host ("##vso[task.setVariable variable=acrMatrix;isOutput=true] {0}" `
        -f ($acrMatrix | ConvertTo-Json -Compress))
        Write-Host "set pipeline variable acrmatrix to $(($acrMatrix | ConvertTo-Json -Compress))"
    }
