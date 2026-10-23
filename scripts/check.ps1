#!/usr/bin/env pwsh
# V.A.Z — mandatory check: build + unit tests + agent config validation.
# Run from anywhere:  .\scripts\check.ps1
# Or via npm alias:   npm run check

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    Write-Host "==> dotnet build Vaz.sln"
    dotnet build Vaz.sln --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "BUILD FAILED (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "==> dotnet test Vaz.sln"
    dotnet test Vaz.sln --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "TEST FAILED (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "==> agent frontmatter validation"
    & (Join-Path $PSScriptRoot "validate-agents.ps1")
    if ($LASTEXITCODE -ne 0) {
        Write-Host "AGENT VALIDATION FAILED (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "CHECK PASSED: build + test + agent config" -ForegroundColor Green
}
finally {
    Pop-Location
}
