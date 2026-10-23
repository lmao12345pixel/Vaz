#!/usr/bin/env pwsh
# V.A.Z — validate team agent frontmatter and permissions.
# Fails (exit 1) if any required agent file is missing, malformed, or has
# broader permissions than its role allows.

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$agentsDir = Join-Path $repoRoot ".opencode\agents\team"

$expected = [ordered]@{
    "product-owner" = @{ edit = "deny";  shell = "deny" }
    "planner"       = @{ edit = "deny";  shell = "deny" }
    "developer"     = @{ edit = "allow"; shell = "allow" }
    "tester"        = @{ edit = "deny";  shell = "allow" }
    "reviewer"      = @{ edit = "deny";  shell = "deny" }
    "qa-lead"       = @{ edit = "deny";  shell = "deny" }
}

$errors = [System.Collections.Generic.List[string]]::new()
$checked = 0

foreach ($name in $expected.Keys) {
    $path = Join-Path $agentsDir "$name.md"
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
    if ($front -notmatch "(?m)^mode:\s*subagent\s*$") {
        $errors.Add("${name}: mode must be 'subagent'")
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
        $want = if ($action -eq "subagent") { "deny" } else { $expected[$name][$action] }
        if ($effect -ne $want) {
            $errors.Add("${name}: $action effect is '$effect', expected '$want'")
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

Write-Host "Agent validation PASSED: $checked/$($expected.Count) file(s)."
exit 0
