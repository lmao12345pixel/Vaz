param(
    [switch]$Runtime
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Host "==> static check: build" -ForegroundColor Cyan
    dotnet build Vaz.sln --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "STATIC CHECK FAILED: build" -ForegroundColor Red
        exit 1
    }

    Write-Host "==> static check: test" -ForegroundColor Cyan
    dotnet test Vaz.sln --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "STATIC CHECK FAILED: test" -ForegroundColor Red
        exit 1
    }

    Write-Host "==> static check: agent config" -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "validate-agents.ps1")
    if ($LASTEXITCODE -ne 0) {
        Write-Host "STATIC CHECK FAILED: agent config" -ForegroundColor Red
        exit 1
    }

    Write-Host "STATIC CHECK PASSED: build + test + agent config" -ForegroundColor Green
    Write-Host "NOTE: static checks do NOT prove UI/flow behavior." -ForegroundColor Yellow

    if ($Runtime) {
        Write-Host ""
        Write-Host "==> runtime check" -ForegroundColor Cyan
        & (Join-Path $PSScriptRoot "check-runtime.ps1")
        if ($LASTEXITCODE -ne 0) {
            Write-Host "CHECK FAILED: runtime evidence unavailable or failing" -ForegroundColor Red
            exit $LASTEXITCODE
        }
        Write-Host "CHECK PASSED: static + runtime web" -ForegroundColor Green
    }
    else {
        Write-Host "Runtime check not requested. Use -Runtime or 'npm run check:runtime'." -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}
