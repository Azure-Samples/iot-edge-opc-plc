<#
 .SYNOPSIS
    Builds multiarch containers from the container.json file in the path.

 .DESCRIPTION
    The script requires az to be installed and already logged on to a
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

 .PARAMETER Path
    The folder to build the docker files from

 .PARAMETER Registry
    The name of the registry

 .PARAMETER Subscription
    The subscription to use - otherwise uses default

 .PARAMETER Debug
    Build debug and include debugger into images (where applicable)
#>

Param(
    [string] $Path = $null,
    [string] $Registry = $null,
    [string] $Subscription = $null,
    [switch] $Debug
)

# Check path argument and resolve to full existing path
if ([string]::IsNullOrEmpty($Path)) {
    throw "No docker folder specified."
}
$getroot = (Join-Path $PSScriptRoot "get-root.ps1")
if (!(Test-Path -Path $Path -PathType Container)) {
    $Path = Join-Path (& $getroot -fileName $Path) $Path
}
$Path = Resolve-Path -LiteralPath $Path

# Try get branch name
$branchName = $env:BUILD_SOURCEBRANCH
if (![string]::IsNullOrEmpty($branchName)) {
    if ($branchName.StartsWith("refs/heads/")) {
        $branchName = $branchName.Replace("refs/heads/", "")
    }
    else {
        Write-Warning "'$($branchName)' is not a branch."
        $branchName = $null
    }
}
if ([string]::IsNullOrEmpty($branchName)) {
    try {
        $argumentList = @("rev-parse", "--abbrev-ref", "HEAD")
        $branchName = (& "git" $argumentList 2>&1 | ForEach-Object { "$_" });
        if ($LastExitCode -ne 0) {
            throw "git $($argumentList) failed with $($LastExitCode)."
        }
    }
    catch {
        Write-Warning $_.Exception
        $branchName = $null
    }
}

if ([string]::IsNullOrEmpty($branchName) -or ($branchName -eq "HEAD")) {
    Write-Warning "Not building from a branch - skip image build."
    return
}

# Set namespace name based on branch name
$releaseBuild = $false
$namespace = $branchName
if ($namespace.StartsWith("feature/")) {
    $namespace = $namespace.Replace("feature/", "")
}
elseif ($namespace.StartsWith("release/") -or ($namespace -eq "main")) {
    $namespace = "public"
    $releaseBuild = $true
}
$namespace = $namespace.Replace("_", "/").Substring(0, [Math]::Min($namespace.Length, 24))
$namespace = "$($namespace)/"

if (![string]::IsNullOrEmpty($Registry) -and ($Registry -ne "industrialiot")) {
    # if we build from release or from main and registry is provided we leave namespace empty
    if ($releaseBuild) {
        $namespace = ""
    }
}

# get and set build information from gitversion, git or version content
$latestTag = "latest"
$sourceTag = $env:Version_Prefix
if ([string]::IsNullOrEmpty($sourceTag)) {
    try {
        $version = & (Join-Path $PSScriptRoot "get-version.ps1")
        $sourceTag = $version.Prefix
    }
    catch {
        $sourceTag = $null
    }
}
if (![string]::IsNullOrEmpty($sourceTag)) {
    Write-Host "Using version $($sourceTag) from get-version.ps1"
}
else {
    # Otherwise look at git tag
    if (![string]::IsNullOrEmpty($env:BUILD_SOURCEVERSION)) {
        # Try get current tag
        try {
            $argumentList = @("tag", "--points-at", $env:BUILD_SOURCEVERSION)
            $sourceTag = (& "git" $argumentList 2>&1 | ForEach-Object { "$_" });
            if ($LastExitCode -ne 0) {
                throw "git $($argumentList) failed with $($LastExitCode)."
            }
        }
        catch {
            Write-Error "Error reading tag from $($env:BUILD_SOURCEVERSION)"
            $sourceTag = $null
        }
    }
    if ([string]::IsNullOrEmpty($sourceTag)) {
        throw "Failed getting version from get-version.ps1"
    }
}

# set default subscription
if (![string]::IsNullOrEmpty($Subscription)) {
    Write-Debug "Setting subscription to $($Subscription)"
    $argumentList = @("account", "set", "--subscription", $Subscription)
    & "az" $argumentList 2>&1 | ForEach-Object { Write-Host "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# Check and set registry
if ([string]::IsNullOrEmpty($Registry)) {
    $Registry = $env:BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($Registry)) {
        if ($releaseBuild) {
            # Make sure we do not override latest in release builds - this is done manually later.
            # For opcplc we do not need a manual step
            # $latestTag = "preview"
            $Registry = "industrialiot"
        }
        else {
            $Registry = "industrialiotdev"
        }
        Write-Warning "No registry specified - using $($Registry).azurecr.io."
    }
}

# get registry information
Write-Host "Using container registry $Registry"
$argumentList = @("acr", "show", "--name", $Registry)
$RegistryInfo = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$resourceGroup = $RegistryInfo.resourceGroup
Write-Debug "Using resource group $($resourceGroup)"
# get credentials
$argumentList = @("acr", "update", "-n", $Registry, "--admin-enabled", "$true")
& "az" $argumentList 2>&1 | Out-Null
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$argumentList = @("acr", "credential", "show", "--name", $Registry)
$credentials = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$user = $credentials.username
$password = $credentials.passwords[0].value
Write-Debug "Using User name $($user) and passsword ****"

# Login to registry
Write-Host "Logging in to $Registry.azurecr.io..."
$password | docker login "$Registry.azurecr.io" -u "$user" --password-stdin
if ($LastExitCode -ne 0) {
    throw "docker login failed with $($LastExitCode)."
}

# Get build root - this is the top most folder with .dockerignore
$buildRoot = & $getroot -startDir $Path -fileName ".dockerignore"
# Get meta data
$metadata = Get-Content -Raw -Path (join-path $Path "container.json") `
| ConvertFrom-Json


# Set image name and namespace in acr based on branch and source tag
$imageName = $metadata.name

$tagPostfix = ""
$tagPrefix = ""
if ($Debug.IsPresent) {
    $tagPostfix = "-debug"
}
if (![string]::IsNullOrEmpty($metadata.tag)) {
    $tagPrefix = "$($metadata.tag)-"
}

$fullImageName = "$($Registry).azurecr.io/$($namespace)$($imageName):$($tagPrefix)$($sourceTag)$($tagPostfix)"
Write-Host "Full image name: $($fullImageName)"

Write-Host "Building images in $($Path) in $($buildRoot)"
Write-Host " and pushing to $($Registry)/$($namespace)$($imageName)..."

# Setup for multi-arch builds
Write-Host "Setting up QEMU..."
docker run --privileged --rm tonistiigi/binfmt --install all

Write-Host "Creating and bootstrapping new builder..."
if (!(docker buildx ls | Select-String "mybuilder")) {
    docker buildx create --use --name mybuilder --driver docker-container
} else {
    docker buildx use mybuilder
}
docker buildx inspect --bootstrap

# Adjust build root if we are in src
if ($buildRoot -eq $Path -and (Test-Path (Join-Path $Path "../common.props"))) {
    $buildRoot = Resolve-Path (Join-Path $Path "..")
}

$dockerfile = Join-Path $buildRoot "Dockerfile"
$platforms = "linux/amd64,linux/arm/v7,linux/arm64"

Write-Host "Building $fullImageName for $platforms using $dockerfile context $buildRoot"

$argumentList = @("buildx", "build",
    "--platform", $platforms,
    "--file", $dockerfile,
    "--tag", $fullImageName,
    "--provenance=false",
    "--push"
)
$argumentList += $buildRoot

Write-Host "Running: docker $($argumentList -join ' ')"
& docker $argumentList
if ($LastExitCode -ne 0) {
    throw "Docker build failed with exit code $LastExitCode"
}

Write-Host "Build and push completed successfully."
