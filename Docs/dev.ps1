<#
.SYNOPSIS
    Start VitePress development server for local documentation preview.

.DESCRIPTION
    Starts the VitePress dev server with proper host binding to work on all systems.
    Opens the documentation site in your default browser.

.EXAMPLE
    .\dev.ps1
    Starts the dev server and opens browser.

.NOTES
    Use Ctrl+C to stop the server when done.
#>

$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PoshUI Documentation Server" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if npm is installed
$npm = Get-Command npm -ErrorAction SilentlyContinue
if (-not $npm) {
    Write-Host "[ERROR] npm is not installed or not in PATH" -ForegroundColor Red
    Write-Host "        Please install Node.js from https://nodejs.org/" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Check if node_modules exists
$nodeModules = Join-Path $PSScriptRoot "node_modules"
if (-not (Test-Path $nodeModules)) {
    Write-Host "[STEP] Installing dependencies (first time only)..." -ForegroundColor Cyan
    npm install
    Write-Host ""
}

Write-Host "[STEP] Starting VitePress development server..." -ForegroundColor Cyan
Write-Host ""
Write-Host "  The documentation site will be available at:" -ForegroundColor Gray
Write-Host "  - http://localhost:5173/" -ForegroundColor Green
Write-Host "  - http://127.0.0.1:5173/" -ForegroundColor Green
Write-Host "  - http://[::1]:5173/ (IPv6)" -ForegroundColor Green
Write-Host ""
Write-Host "  Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

# Start dev server
npm run docs:dev
