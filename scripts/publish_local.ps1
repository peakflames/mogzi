#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Publishes the Mogzi.TUI project locally to the dist directory.

.DESCRIPTION
    This script publishes the Mogzi.TUI project using dotnet publish.
    It can be run from any directory and will always find the correct project path.
    The output will be placed in the dist directory at the project root.

.EXAMPLE
    ./scripts/publish_local.ps1
    Publishes the project to the dist directory.

.EXAMPLE
    pwsh /path/to/mogzi/scripts/publish_local.ps1
    Publishes the project when run from any directory.
#>

# Get the directory where this script is located
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Get the project root directory (one level up from scripts)
$ProjectRoot = Split-Path -Parent $ScriptDir

# Define paths
$ProjectFile = Join-Path $ProjectRoot "src/Mogzi.TUI/Mogzi.TUI.csproj"
$OutputDir = Join-Path $ProjectRoot "dist"

# Verify the project file exists
if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

Write-Host "Publishing Mogzi.TUI project..." -ForegroundColor Green
Write-Host "Project: $ProjectFile" -ForegroundColor Cyan
Write-Host "Output:  $OutputDir" -ForegroundColor Cyan

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Created output directory: $OutputDir" -ForegroundColor Yellow
}

# Run dotnet publish
try {
    $publishArgs = @(
        "publish"
        $ProjectFile
        "--output"
        $OutputDir
        "--configuration"
        "Release"
        "--verbosity"
        "minimal"
    )
    
    Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Publish completed successfully!" -ForegroundColor Green
        Write-Host "Output location: $OutputDir" -ForegroundColor Cyan
    } else {
        Write-Error "❌ Publish failed with exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
} catch {
    Write-Error "❌ An error occurred during publish: $($_.Exception.Message)"
    exit 1
}