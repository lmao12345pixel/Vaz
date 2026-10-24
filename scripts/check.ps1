param(
    [switch]$Web,
    [switch]$Runtime,
    [switch]$All
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

$runStatic = $All -or (-not $Web -and -not $Runtime)
$runWeb = $All -or $Web
$runRuntime = $All -or $Runtime

try {
    if ($runStatic) {
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
    }

    if ($runWeb) {
        Write-Host ""
        Write-Host "==> web check" -ForegroundColor Cyan
        & (Join-Path $PSScriptRoot "check-web.ps1")
        if ($LASTEXITCODE -ne 0) {
            Write-Host "WEB CHECK FAILED" -ForegroundColor Red
            exit 1
        }
    }

    if ($runRuntime) {
        Write-Host ""
        Write-Host "==> runtime check" -ForegroundColor Cyan
        & (Join-Path $PSScriptRoot "check-runtime.ps1")
        if ($LASTEXITCODE -ne 0) {
            Write-Host "RUNTIME CHECK FAILED OR NOT CONFIGURED (exit $LASTEXITCODE)" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }

    if (-not $runStatic -and -not $runWeb -and -not $runRuntime) {
        Write-Host "No check selected. Use -Web, -Runtime, or -All." -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}
