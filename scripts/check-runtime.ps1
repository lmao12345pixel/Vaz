# Compatibility alias: web runtime evidence is Playwright, created by check-web.ps1.
$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "check-web.ps1")
exit $LASTEXITCODE
