param(
    [ValidateSet("static", "web", "all")]
    [string]$Scope = "static"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

function Invoke-StaticCheck {
    Write-Host "==> static: build" -ForegroundColor Cyan
    dotnet build Vaz.sln --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "STATIC CHECK FAILED: build" -ForegroundColor Red
        exit 1
    }

    Write-Host "==> static: test" -ForegroundColor Cyan
    dotnet test Vaz.sln --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "STATIC CHECK FAILED: test" -ForegroundColor Red
        exit 1
    }

    Write-Host "==> static: agent config" -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "validate-agents.ps1")
    if ($LASTEXITCODE -ne 0) {
        Write-Host "STATIC CHECK FAILED: agent config" -ForegroundColor Red
        exit 1
    }

    Write-Host "STATIC CHECK PASSED: build + test + agent config" -ForegroundColor Green
    Write-Host "NOTE: static checks do NOT prove UI/flow behavior." -ForegroundColor Yellow
}

function Invoke-WebCheck {
    Write-Host "==> web: Playwright" -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "check-web.ps1")
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WEB CHECK FAILED: Playwright evidence unavailable or failing" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

try {
    Write-Host "Check scope: $Scope" -ForegroundColor Cyan

    if ($Scope -eq "static" -or $Scope -eq "all") {
        Invoke-StaticCheck
    }

    if ($Scope -eq "web" -or $Scope -eq "all") {
        Invoke-WebCheck
    }

    Write-Host "CHECK PASSED: $Scope" -ForegroundColor Green
}
finally {
    Pop-Location
}
