<#
 .SYNOPSIS
    Creates buildjob matrix based on the specified file names in the tree.

 .DESCRIPTION
    The script traverses the build root to find all folders with a matching
    file and populates the matrix.  The matrix is used to spawn jobs that
    run on multiple different environments for each file.  E.g. build all
    solution files on all platforms, run tests per particular folder of
    the tree, etc.

 .PARAMETER BuildRoot
    The root folder to start traversing the repository from.

 .PARAMETER FileName
    File patterns to match defining the folders and files in the matrix

 .PARAMETER JobPrefix
    Optional name prefix for each job
#>

Param(
    [string] $BuildRoot = $null,
    [string] $FileName = $null,
    [string] $JobPrefix = ""
)

if ([string]::IsNullOrEmpty($BuildRoot)) {
    $BuildRoot = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "*.sln"
}

if ([string]::IsNullOrEmpty($FileName)) {
    $FileName = "Directory.Build.props"
}
if (![string]::IsNullOrEmpty($JobPrefix)) {
    $JobPrefix = "$($JobPrefix)-"
}

# https://docs.microsoft.com/en-us/azure/devops/pipelines/agents/hosted?view=azure-devops&tabs=yaml#software
$agents = @{
    windows = "windows-2022"
    linux = "ubuntu-22.04"
}

$jobMatrix = @{}

# Traverse from build root and find all files to create job matrix
Get-ChildItem $BuildRoot -Recurse `
    | Where-Object Name -like $FileName `
    | ForEach-Object {

    $fullFolder = $_.DirectoryName.Replace("\", "/")
    $folder = $_.DirectoryName.Replace($BuildRoot, "").Replace("\", "/").TrimStart("/")
    $file = $_.FullName.Replace($BuildRoot, "").Replace("\", "/").TrimStart("/")
    if ([string]::IsNullOrEmpty($folder)) {
        $postFix = ""
    }
    else {
        $postFix = $folder.Replace("/", "-")
        $postFix = "$($postFix)-"
    }
    $agents.keys | ForEach-Object {
        $jobName = "$($JobPrefix)$($postFix)$($_)"
        $jobMatrix.Add($jobName, @{
            "poolImage" = $agents.Item($_)
            "folder" = $folder
            "fullFolder" = $fullFolder
            "file" = $file
            "agent" = $($_)
        })
    }
}

# Set pipeline variable
Write-Host ("##vso[task.setVariable variable=jobMatrix;isOutput=true] {0}" `
    -f ($jobMatrix | ConvertTo-Json -Compress))
