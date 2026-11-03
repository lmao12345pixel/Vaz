$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    $playwrightModule = Join-Path $repoRoot "node_modules\@playwright\test"
    $configPath = Join-Path $repoRoot "playwright.config.ts"

    Write-Host "Web scope: docs/design-preview served by tests/web-e2e/serve.mjs" -ForegroundColor Cyan

    if (-not (Test-Path -LiteralPath $playwrightModule) -or -not (Test-Path -LiteralPath $configPath)) {
        Write-Host ""
        Write-Host "WEB CHECK NOT_CONFIGURED: Playwright runner or config is missing." -ForegroundColor Yellow
        Write-Host "  Missing: node_modules\@playwright\test and/or playwright.config.ts"
        Write-Host "  Install: npm install"
        Write-Host "           npx playwright install chromium"
        Write-Host "  Then run: npm run check:web"
        Write-Host ""
        exit 2
    }

    Write-Host "==> npx playwright test" -ForegroundColor Cyan
    npx --no-install playwright test
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WEB CHECK FAILED: Playwright tests failed." -ForegroundColor Red
        exit 1
    }

    Write-Host "WEB CHECK PASSED: Playwright (chromium + mobile-chrome)" -ForegroundColor Green
}
finally {
    Pop-Location
}
