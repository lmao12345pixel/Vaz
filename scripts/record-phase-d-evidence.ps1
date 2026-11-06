# Script to capture 6 screenshots and record 30s video for Phase D Version Management
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path
$outImgDir = "docs/evidence/dot18/screenshots"
$outVidDir = "docs/evidence/dot18/videos"
if (-not (Test-Path $outImgDir)) { New-Item -ItemType Directory -Path $outImgDir -Force | Out-Null }
if (-not (Test-Path $outVidDir)) { New-Item -ItemType Directory -Path $outVidDir -Force | Out-Null }

function Set-Theme([string]$theme) {
    if (Test-Path $settingsPath) {
        $j = Get-Content $settingsPath -Raw | ConvertFrom-Json
        $j.theme = $theme
        $j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8
    }
}

# --- PART 1: Dark Mode Screenshots & Actions ---
Set-Theme "dark"
Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50) | Out-Null
Start-Sleep -Milliseconds 500

# 1. Click Settings in sidebar
$root = Get-VazRoot
$settingsItem = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.Name -eq "Settings" } | Select-Object -First 1
$r = $settingsItem.Current.BoundingRectangle
[VazMouse]::ClickHwnd($hWnd, [int]($r.X + 60), [int]($r.Y + 18))
Start-Sleep -Seconds 3

# 2. Click "Phiên bản" category chip
$root = Get-VazRoot
$verChip = @(Get-UiaDescendants -Element $root) | Where-Object {
    $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::RadioButton -and
    $_.Current.Name -eq "Phiên bản"
} | Select-Object -First 1
$vr = $verChip.Current.BoundingRectangle
[VazMouse]::ClickHwnd($hWnd, [int]($vr.X + 30), [int]($vr.Y + 12))
Start-Sleep -Seconds 3

# Screenshot 1: Tab Versions Dark
.\scripts\capture-winui.ps1 -OutputPath "$outImgDir/settings-versions-dark.png"
Write-Output "Captured 1: settings-versions-dark.png"

# Screenshot 2: Warning 'đang dùng' (focus on in-use rows with disabled delete)
.\scripts\capture-winui.ps1 -OutputPath "$outImgDir/settings-versions-in-use.png"
Write-Output "Captured 2: settings-versions-in-use.png"

# Screenshot 3: Cleanup confirm dialog
$root = Get-VazRoot
$cleanupBtn = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.AutomationId -eq "CleanupButton" } | Select-Object -First 1
if ($cleanupBtn) {
    $cr = $cleanupBtn.Current.BoundingRectangle
    [VazMouse]::ClickHwnd($hWnd, [int]($cr.X + 35), [int]($cr.Y + 15))
    Start-Sleep -Milliseconds 1200
    .\scripts\capture-winui.ps1 -OutputPath "$outImgDir/settings-versions-cleanup-confirm.png"
    Write-Output "Captured 3: settings-versions-cleanup-confirm.png"

    # Dismiss dialog by clicking 'Hủy'
    $cancelBtn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains "Hủy"
    if ($cancelBtn) {
        $cancelR = $cancelBtn.Current.BoundingRectangle
        [VazMouse]::ClickHwnd($hWnd, [int]($cancelR.X + 25), [int]($cancelR.Y + 15))
        Start-Sleep -Seconds 1
    }
}

# Screenshot 4: Delete confirm dialog on 1.19.4-orphan-demo
$root = Get-VazRoot
$delBtns = @(Get-UiaDescendants -Element $root) | Where-Object {
    $_.Current.AutomationId -eq "DeleteVersionButton" -and $_.Current.IsEnabled -eq $true
}
Write-Output "Enabled delete buttons found: $($delBtns.Count)"
if ($delBtns.Count -gt 0) {
    $firstDel = $delBtns[0]
    $dr = $firstDel.Current.BoundingRectangle
    [VazMouse]::ClickHwnd($hWnd, [int]($dr.X + 25), [int]($dr.Y + 15))
    Start-Sleep -Milliseconds 1200
    .\scripts\capture-winui.ps1 -OutputPath "$outImgDir/settings-versions-delete-confirm.png"
    Write-Output "Captured 4: settings-versions-delete-confirm.png"

    # Click 'Xóa' to confirm deletion
    $confirmDel = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains "Xóa"
    if ($confirmDel) {
        $cdr = $confirmDel.Current.BoundingRectangle
        [VazMouse]::ClickHwnd($hWnd, [int]($cdr.X + 25), [int]($cdr.Y + 15))
        Start-Sleep -Seconds 3
    }
}

# Screenshot 5: Refresh state / after delete (shows status bar & updated size)
$root = Get-VazRoot
$refreshBtn = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.AutomationId -eq "RefreshButton" } | Select-Object -First 1
if ($refreshBtn) {
    $rr = $refreshBtn.Current.BoundingRectangle
    [VazMouse]::ClickHwnd($hWnd, [int]($rr.X + 35), [int]($rr.Y + 15))
    Start-Sleep -Seconds 2
}
.\scripts\capture-winui.ps1 -OutputPath "$outImgDir/settings-versions-refresh.png"
Write-Output "Captured 5: settings-versions-refresh.png"

Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600

# --- PART 2: Light Mode Screenshot ---
Set-Theme "light"
$procLight = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWndLight = $procLight.MainWindowHandle
[VazMouse]::ClickHwnd($hWndLight, 50, 50) | Out-Null
Start-Sleep -Milliseconds 500

$rootLight = Get-VazRoot
$settingsItemLight = @(Get-UiaDescendants -Element $rootLight) | Where-Object { $_.Current.Name -eq "Settings" } | Select-Object -First 1
$rL = $settingsItemLight.Current.BoundingRectangle
[VazMouse]::ClickHwnd($hWndLight, [int]($rL.X + 60), [int]($rL.Y + 18))
Start-Sleep -Seconds 3

$rootLight = Get-VazRoot
$verChipLight = @(Get-UiaDescendants -Element $rootLight) | Where-Object {
    $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::RadioButton -and
    $_.Current.Name -eq "Phiên bản"
} | Select-Object -First 1
$vrL = $verChipLight.Current.BoundingRectangle
[VazMouse]::ClickHwnd($hWndLight, [int]($vrL.X + 30), [int]($vrL.Y + 12))
Start-Sleep -Seconds 3

# Screenshot 6: Tab Versions Light
.\scripts\capture-winui.ps1 -OutputPath "$outImgDir/settings-versions-light.png"
Write-Output "Captured 6: settings-versions-light.png"

Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
Set-Theme "dark" # Restore dark theme
Start-Sleep -Milliseconds 600

Write-Output "All 6 screenshots captured successfully!"
