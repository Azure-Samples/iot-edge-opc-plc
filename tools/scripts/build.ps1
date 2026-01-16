<#
 .SYNOPSIS
    Builds docker images from definition files in folder or the entire tree

 .DESCRIPTION
    The script traverses the build root to find all folders with an container.json
    file builds each one

 .PARAMETER Path
    The root folder to start traversing the repository from (Optional).

 .PARAMETER Debug
    Whether to build debug images.
#>

Param(
    [string] $Path = $null,
    [switch] $Debug
)

$BuildRoot = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "*.sln"

if ([string]::IsNullOrEmpty($Path)) {
    $Path = $BuildRoot
}

# Traverse from build root and find all container.json metadata files and build
Get-ChildItem $Path -Recurse -Include "container.json" `
| ForEach-Object {

    # Get root
    $dockerFolder = $_.DirectoryName.Replace($BuildRoot, "")
    if ([string]::IsNullOrEmpty($dockerFolder)) {
        $dockerFolder = "."
    }
    else {
        $dockerFolder = $dockerFolder.Substring(1)
    }

    $metadata = Get-Content -Raw -Path (join-path $_.DirectoryName "container.json") `
    | ConvertFrom-Json

    $dockerfileName = "Dockerfile.release"
    if ($Debug.IsPresent) {
        $dockerfileName = "Dockerfile.debug"
    }
    $dockerfilePath = Join-Path $BuildRoot $dockerfileName

    $imageName = $metadata.name
    if ($Debug.IsPresent) {
        $imageName += ":debug"
    }
    else {
        $imageName += ":latest"
    }

    Write-Host "Building $imageName using $dockerfilePath context $BuildRoot"
    docker build -f $dockerfilePath -t $imageName $BuildRoot
    if ($LastExitCode -ne 0) {
        throw "Docker build failed with exit code $LastExitCode"
    }
}
