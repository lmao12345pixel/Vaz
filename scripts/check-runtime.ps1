$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$playwrightModule = Join-Path $repoRoot "node_modules\@playwright\test"
$configPath = Join-Path $repoRoot "tests\web-e2e\playwright.config.ts"

Write-Host "Runtime scope: web prototype (Playwright). WinUI is plan-only." -ForegroundColor Cyan

if (-not (Test-Path -LiteralPath $playwrightModule) -or -not (Test-Path -LiteralPath $configPath)) {
    Write-Host ""
    Write-Host "RUNTIME CHECK NOT_CONFIGURED: Playwright runner is not installed or configured." -ForegroundColor Yellow
    Write-Host "  Missing: node_modules\@playwright\test and/or tests\web-e2e\playwright.config.ts"
    Write-Host "  Install: npm install"
    Write-Host "           npx playwright install chromium"
    Write-Host "  Then run: npm run check:runtime"
    Write-Host ""
    Write-Host "WINUI RUNTIME: PLAN ONLY - no automated runner. UI/flow criteria must be manually verified or BLOCKED." -ForegroundColor Yellow
    exit 2
}

Push-Location $repoRoot
try {
    Write-Host "==> web runtime: Playwright" -ForegroundColor Cyan
    npx --no-install playwright test --config tests/web-e2e/playwright.config.ts
    if ($LASTEXITCODE -ne 0) {
        Write-Host "RUNTIME CHECK FAILED: web Playwright" -ForegroundColor Red
        exit 1
    }

    Write-Host "WEB RUNTIME PASSED: Playwright" -ForegroundColor Green
    Write-Host "WINUI RUNTIME: PLAN ONLY - no automated runner. Do not claim WinUI UI PASS from this result." -ForegroundColor Yellow
}
finally {
    Pop-Location
}
