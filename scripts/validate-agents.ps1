#!/usr/bin/env pwsh
# V.A.Z - validate agent frontmatter, permissions, and mandatory testing rules.
# Fails (exit 1) if any required agent file is missing, malformed, has broader
# permissions than its role allows, or lacks required testing-rule phrases.

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$agentsRoot = Join-Path $repoRoot ".opencode\agents"

$agents = @(
    [pscustomobject]@{ Name = "product-owner"; Path = "team\product-owner.md"; Mode = "subagent"; Edit = "deny";  Shell = "deny";  Subagent = "deny";  Required = @() },
    [pscustomobject]@{ Name = "planner";       Path = "team\planner.md";       Mode = "subagent"; Edit = "deny";  Shell = "deny";  Subagent = "deny";  Required = @() },
    [pscustomobject]@{ Name = "developer";     Path = "team\developer.md";     Mode = "subagent"; Edit = "allow"; Shell = "allow"; Subagent = "deny";  Required = @("Developer Report", "AUTOMATED", "RUNTIME", "MANUAL_REQUIRED", "BLOCKED") },
    [pscustomobject]@{ Name = "tester";        Path = "team\tester.md";        Mode = "subagent"; Edit = "deny";  Shell = "allow"; Subagent = "deny";  Required = @() },
    [pscustomobject]@{ Name = "reviewer";      Path = "team\reviewer.md";      Mode = "subagent"; Edit = "deny";  Shell = "deny";  Subagent = "deny";  Required = @() },
    [pscustomobject]@{ Name = "qa-lead";       Path = "team\qa-lead.md";       Mode = "subagent"; Edit = "deny";  Shell = "deny";  Subagent = "deny";  Required = @("Verification Contract", "RUNTIME", "MANUAL_REQUIRED", "BLOCKED", "PENDING", "PARITY_VERIFIED") },
    [pscustomobject]@{ Name = "verifier";      Path = "verifier.md";           Mode = "subagent"; Edit = "deny";  Shell = "allow"; Subagent = "deny";  Required = @("No evidence, no PASS", "Verification Contract", "RUNTIME", "MANUAL_REQUIRED", "BLOCKED") },
    [pscustomobject]@{ Name = "web-developer"; Path = "web-developer.md";      Mode = "subagent"; Edit = "allow"; Shell = "allow"; Subagent = "deny";  Required = @("Developer Report", "WEB_SPEC", "WEB_VALIDATED", "tests/web-e2e", "docs/design/tokens.json") },
    [pscustomobject]@{ Name = "web-verifier";  Path = "web-verifier.md";       Mode = "subagent"; Edit = "deny";  Shell = "allow"; Subagent = "deny";  Required = @("No evidence, no PASS", "Playwright", "RUNTIME", "BLOCKED", "WEB_VALIDATED") },
    [pscustomobject]@{ Name = "app-developer"; Path = "app-developer.md";      Mode = "subagent"; Edit = "allow"; Shell = "allow"; Subagent = "deny";  Required = @("Developer Report", "WEB_VALIDATED", "PARITY", "docs/design/tokens.json") },
    [pscustomobject]@{ Name = "app-verifier";  Path = "app-verifier.md";       Mode = "subagent"; Edit = "deny";  Shell = "allow"; Subagent = "deny";  Required = @("No evidence, no PASS", "RUNTIME", "MANUAL_REQUIRED", "BLOCKED", "APP_IMPLEMENTED", "PARITY_VERIFIED") },
    [pscustomobject]@{ Name = "orchestrator";  Path = "orchestrator.md";       Mode = "primary";  Edit = "deny";  Shell = "allow"; Subagent = "allow"; Required = @("Verification Contract", "AUTOMATED", "RUNTIME", "MANUAL_REQUIRED", "BLOCKED", "PENDING", "WEB_SPEC", "WEB_VALIDATED", "APP_IMPLEMENTED", "PARITY_VERIFIED", "check:web", "Verifier") }
)

$errors = [System.Collections.Generic.List[string]]::new()
$checked = 0

foreach ($agent in $agents) {
    $name = $agent.Name
    $path = Join-Path $agentsRoot $agent.Path
    if (-not (Test-Path -LiteralPath $path)) {
        $errors.Add("${name}: file not found at $path")
        continue
    }

    $text = Get-Content -LiteralPath $path -Raw
    if (-not $text.StartsWith("---")) {
        $errors.Add("${name}: missing YAML frontmatter")
        continue
    }

    $close = $text.IndexOf("---", 3)
    if ($close -lt 0) {
        $errors.Add("${name}: frontmatter is not closed")
        continue
    }
    $front = $text.Substring(3, $close - 3)

    if ($front -notmatch "(?m)^description:\s*\S") {
        $errors.Add("${name}: missing description")
    }
    if ($front -notmatch ("(?m)^mode:\s*" + [regex]::Escape($agent.Mode) + "\s*$")) {
        $errors.Add("${name}: mode must be '${agent.Mode}'")
    }
    if ($front -notmatch "(?m)^temperature:\s*[0-9]+(\.[0-9]+)?\s*$") {
        $errors.Add("${name}: missing temperature")
    }
    if ($front -notmatch "(?m)^permissions:\s*$") {
        $errors.Add("${name}: missing permissions block")
    }

    foreach ($action in @("edit", "shell", "subagent")) {
        $pattern = "(?m)^[ \t]*-[ \t]*action:[ \t]*" + $action + "[ \t]*\r?\n[ \t]*resource:[^\r\n]*\r?\n[ \t]*effect:[ \t]*(\w+)[ \t]*$"
        $match = [regex]::Match($front, $pattern)
        if (-not $match.Success) {
            $errors.Add("${name}: missing permission rule for '$action'")
            continue
        }

        $effect = $match.Groups[1].Value
        $want = $agent.$action
        if ($effect -ne $want) {
            $errors.Add("${name}: $action effect is '$effect', expected '$want'")
        }
    }

    foreach ($phrase in $agent.Required) {
        if ($text -notmatch [regex]::Escape($phrase)) {
            $errors.Add("${name}: missing required testing-rule phrase '$phrase'")
        }
    }

    $checked++
    Write-Host "  OK  $name"
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Agent validation FAILED:" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host "  - $e" -ForegroundColor Red
    }
    exit 1
}

Write-Host "Agent validation PASSED: $checked/$($agents.Count) file(s)."
exit 0
