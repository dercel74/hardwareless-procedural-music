#!/usr/bin/env powershell
<#
.SYNOPSIS
    Install Unity Editor 2021.3.22f1 for the hardwareless project
.DESCRIPTION
    This script automates the installation of Unity Editor 2021.3.22f1 with Windows Build Support
    using Unity Hub's command line interface.
.NOTES
    Generated on November 19, 2025
    Project: hardwareless - Procedural Music System
    Target Unity Version: 2021.3.22f1
#>

param(
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Unity Installation Script for hardwareless project" -ForegroundColor Cyan
Write-Host "Target Version: Unity 2021.3.22f1" -ForegroundColor Yellow
Write-Host ""

# Define paths and version
$unityHubPath = "${env:ProgramFiles}\Unity Hub\Unity Hub.exe"
$targetVersion = "2021.3.22f1"
$projectPath = $PSScriptRoot

# Check if Unity Hub is installed
if (-not (Test-Path $unityHubPath)) {
    Write-Error "Unity Hub not found at: $unityHubPath"
    Write-Host "Please install Unity Hub first:" -ForegroundColor Red
    Write-Host "winget install Unity.UnityHub" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Unity Hub found at: $unityHubPath" -ForegroundColor Green

# Function to run Unity Hub commands
function Invoke-UnityHub {
    param([string]$Arguments)

    Write-Host "Running: Unity Hub $Arguments" -ForegroundColor Cyan
    try {
        $result = & $unityHubPath -- $Arguments.Split(' ') 2>&1
        return $result
    }
    catch {
        Write-Warning "Unity Hub command failed: $_"
        return $null
    }
}

# Check current installations
Write-Host "📋 Checking current Unity installations..." -ForegroundColor Blue
$installations = Invoke-UnityHub "--headless editors"
if ($installations) {
    Write-Host "Current installations:" -ForegroundColor Green
    $installations | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
} else {
    Write-Host "No Unity installations found or unable to query." -ForegroundColor Yellow
}

# Check if target version is already installed
$isInstalled = $installations -match $targetVersion
if ($isInstalled -and -not $Force) {
    Write-Host "✅ Unity $targetVersion appears to be already installed!" -ForegroundColor Green
    Write-Host "Use -Force to reinstall anyway." -ForegroundColor Yellow
} else {
    Write-Host "📦 Installing Unity $targetVersion..." -ForegroundColor Blue
    Write-Host "This may take several minutes depending on your internet connection." -ForegroundColor Yellow

    # Install Unity with Windows Build Support
    $installResult = Invoke-UnityHub "--headless install --version $targetVersion --module windows-il2cpp"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Unity $targetVersion installation completed!" -ForegroundColor Green
    } else {
        Write-Warning "Unity installation may have encountered issues. Exit code: $LASTEXITCODE"
        if ($installResult) {
            Write-Host "Output:" -ForegroundColor Yellow
            $installResult | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        }
    }
}

# Verify installation
Write-Host ""
Write-Host "🔍 Verifying Unity installation..." -ForegroundColor Blue
$newInstallations = Invoke-UnityHub "--headless editors"
$targetInstalled = $newInstallations -match $targetVersion

if ($targetInstalled) {
    Write-Host "✅ Unity $targetVersion is now available!" -ForegroundColor Green
} else {
    Write-Warning "Unity $targetVersion was not found in the installation list."
}

# Project opening instructions
Write-Host ""
Write-Host "🎯 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Open Unity Hub (GUI version)" -ForegroundColor White
Write-Host "2. Click 'Open' or 'Add project from disk'" -ForegroundColor White
Write-Host "3. Navigate to: $projectPath" -ForegroundColor White
Write-Host "4. Select the project folder containing Assets/ and Packages/" -ForegroundColor White
Write-Host "5. Unity will open with version $targetVersion" -ForegroundColor White
Write-Host ""
Write-Host "🎵 Testing the Procedural Music System:" -ForegroundColor Cyan
Write-Host "- Press F9 in Play mode to open the debug HUD" -ForegroundColor White
Write-Host "- Test the countdown display and auto-save functionality" -ForegroundColor White
Write-Host "- See Assets/Documentation/ProceduralMusic.md for full docs" -ForegroundColor White

Write-Host ""
Write-Host "✨ Development environment setup complete!" -ForegroundColor Green
