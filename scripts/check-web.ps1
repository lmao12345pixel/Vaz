$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Host "==> web check: required files" -ForegroundColor Cyan
    $required = @(
        "docs\design-preview\index.html",
        "docs\design-preview\styles.css",
        "docs\design-preview\app.js",
        "docs\design\tokens.json",
        "docs\contracts\settings.contract.md",
        "docs\contracts\library.contract.md",
        "tests\web-e2e\playwright.config.ts",
        "tests\web-e2e\vaz.spec.ts"
    )

    $missing = @()
    foreach ($path in $required) {
        if (-not (Test-Path -LiteralPath $path)) {
            $missing += $path
        }
    }
    if ($missing.Count -gt 0) {
        Write-Host "WEB CHECK FAILED: missing required file(s)" -ForegroundColor Red
        foreach ($path in $missing) {
            Write-Host "  - $path" -ForegroundColor Red
        }
        exit 1
    }

    Write-Host "==> web check: design tokens" -ForegroundColor Cyan
    $tokens = Get-Content -LiteralPath "docs\design\tokens.json" -Raw | ConvertFrom-Json
    $tokenPaths = @(
        "color.surface.dark",
        "color.surface.light",
        "color.surfaceElevated.dark",
        "color.surfaceElevated.light",
        "color.primary.dark",
        "color.primary.light",
        "color.danger.dark",
        "color.danger.light",
        "spacing.xs",
        "spacing.sm",
        "spacing.md",
        "spacing.lg",
        "radius.sm",
        "radius.md",
        "radius.lg"
    )

    $tokenErrors = @()
    foreach ($tokenPath in $tokenPaths) {
        $value = $tokens
        foreach ($part in $tokenPath.Split(".")) {
            if ($null -eq $value) { break }
            $property = $value.PSObject.Properties[$part]
            if ($null -eq $property) {
                $value = $null
                break
            }
            $value = $property.Value
        }
        if ($null -eq $value -or ([string]$value).Trim().Length -eq 0) {
            $tokenErrors += $tokenPath
        }
    }
    if ($tokenErrors.Count -gt 0) {
        Write-Host "WEB CHECK FAILED: missing design token(s)" -ForegroundColor Red
        foreach ($tokenPath in $tokenErrors) {
            Write-Host "  - $tokenPath" -ForegroundColor Red
        }
        exit 1
    }

    Write-Host "==> web check: contracts" -ForegroundColor Cyan
    $contracts = @(Get-ChildItem -LiteralPath "docs\contracts" -Filter "*.contract.md" -File)
    if ($contracts.Count -lt 2) {
        Write-Host "WEB CHECK FAILED: expected at least 2 feature contracts in docs\contracts" -ForegroundColor Red
        exit 1
    }

    $contractErrors = @()
    $validStatuses = @("WEB_SPEC", "WEB_VALIDATED", "APP_IMPLEMENTED", "PARITY_VERIFIED")
    foreach ($contract in $contracts) {
        $text = Get-Content -LiteralPath $contract.FullName -Raw
        if ($text -notmatch "(?m)^id:\s*\S") {
            $contractErrors += "$($contract.Name): missing id"
        }
        $statusMatch = [regex]::Match($text, "(?m)^status:\s*(\S+)\s*$")
        if (-not $statusMatch.Success) {
            $contractErrors += "$($contract.Name): missing status"
        }
        elseif ($validStatuses -notcontains $statusMatch.Groups[1].Value) {
            $contractErrors += "$($contract.Name): invalid status '$($statusMatch.Groups[1].Value)'"
        }
    }
    if ($contractErrors.Count -gt 0) {
        Write-Host "WEB CHECK FAILED: invalid contract metadata" -ForegroundColor Red
        foreach ($errorText in $contractErrors) {
            Write-Host "  - $errorText" -ForegroundColor Red
        }
        exit 1
    }

    Write-Host "==> web check: JavaScript syntax" -ForegroundColor Cyan
    $node = Get-Command node -ErrorAction SilentlyContinue
    if ($null -eq $node) {
        Write-Host "WEB CHECK FAILED: node is required for JavaScript syntax checks" -ForegroundColor Red
        exit 1
    }

    node --check docs/design-preview/app.js
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WEB CHECK FAILED: docs/design-preview/app.js syntax" -ForegroundColor Red
        exit 1
    }

    node --check tests/web-e2e/serve.mjs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WEB CHECK FAILED: tests/web-e2e/serve.mjs syntax" -ForegroundColor Red
        exit 1
    }

    Write-Host "WEB CHECK PASSED: contracts + tokens + JavaScript syntax" -ForegroundColor Green
    Write-Host "NOTE: web check does NOT prove UI/flow behavior; use check:runtime for Playwright evidence." -ForegroundColor Yellow
}
finally {
    Pop-Location
}
