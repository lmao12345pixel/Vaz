# Smoke: launch app, open detail page of selected instance, screenshot (verifies "Đổi phiên bản" button renders)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path
$j = Get-Content $settingsPath -Raw | ConvertFrom-Json
$j.theme = "dark"
$j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50) | Out-Null
Start-Sleep -Milliseconds 300

# Open selected instance's detail page: the bottom bar shows selected instance; click its big card? Actually Instances page: click the selected instance card text
$root = Get-VazRoot
$cardTexts = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text -and $_.Current.Name -eq 'Forge 1.20.1 #2' }
Write-Output "card texts found: $($cardTexts.Count)"
if ($cardTexts.Count -gt 0) {
    $t = $cardTexts[0]
    [VazMouse]::ClickHwnd($hWnd, [int]($t.Current.BoundingRectangle.X + 60), [int]($t.Current.BoundingRectangle.Y + 10)) | Out-Null
    Start-Sleep -Seconds 4
}

.\scripts\capture-winui.ps1 -OutputPath "docs/evidence/dot18/screenshots/instance-detail-change-version-button.png"
Get-Process -Name "Vaz.WinUI" | Select-Object Id, MainWindowTitle | Format-Table | Out-String
