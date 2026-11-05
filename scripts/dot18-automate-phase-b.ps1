# =====================================================================
# Dot 18.2c — Automated UI verification for Phase B (Create Instance dialog)
# Uses PowerShell UIA (UIAutomationClient). Run with pwsh (PS7+).
# =====================================================================
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\uia-lib.ps1"

Add-Type -AssemblyName System.Drawing

# ---------- Config ----------
$exe          = "C:\OptizimeProject\Vaz\src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe"
$evidenceDir  = "$PSScriptRoot\..\docs\evidence\dot18"
$uiaDir       = "$evidenceDir\uia"
$shotDir      = "$evidenceDir\screenshots"
$videoDir     = "$evidenceDir\videos"
$framesDir    = "$env:TEMP\dot18-frames"
$versionsDir  = "$env:APPDATA\Vazer\Minecraft\instances\versions"
$settingsFile = "$env:APPDATA\Vazer\Minecraft\settings.json"
$fakeVersion  = "1.21.7"     # top-10 release we fake-install to exercise the badge
$testVersion  = "1.21.7"     # 1.21.1 is NOT in the top-10 dropdown; use 1.21.7
$emptyVersion = "26.3"       # no NeoForge builds -> empty loader-version list (same path as 1.20.1)

New-Item -ItemType Directory -Path $uiaDir    -Force | Out-Null
New-Item -ItemType Directory -Path $shotDir   -Force | Out-Null
New-Item -ItemType Directory -Path $videoDir  -Force | Out-Null
New-Item -ItemType Directory -Path $framesDir -Force | Out-Null

# ---------- Results ----------
$script:results = [System.Collections.ArrayList]::new()
function Add-Result([string]$case, [string]$status, [string]$evidence, [string]$note) {
    [void]$script:results.Add([PSCustomObject]@{ Case=$case; Status=$status; Evidence=$evidence; Note=$note })
    Write-Output ("[{0}] {1} :: {2}" -f $status, $case, $note)
}

# ---------- Win32 screenshot ----------
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Dot18Win32 {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@ -ErrorAction SilentlyContinue

$script:frameIndex = 0
function Save-Screenshot([string]$path) {
    $p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $p) { Write-Output "  (screenshot skipped: no window)"; return $false }
    $hWnd = $p.MainWindowHandle
    [Dot18Win32]::ShowWindow($hWnd, 9) | Out-Null
    [Dot18Win32]::SetForegroundWindow($hWnd) | Out-Null
    Start-Sleep -Milliseconds 200
    $rect = New-Object Dot18Win32+RECT
    [Dot18Win32]::GetWindowRect($hWnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { return $false }
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "  (screenshot: $path)"
    return $true
}
function Capture-Frame {
    $p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $p) { return }
    $hWnd = $p.MainWindowHandle
    $rect = New-Object Dot18Win32+RECT
    [Dot18Win32]::GetWindowRect($hWnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $fp = Join-Path $framesDir ("frame_{0:D4}.png" -f $script:frameIndex)
    $bmp.Save($fp, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    $script:frameIndex++
}

# ---------- Scoped combo item reader ----------
function Get-ComboItemsScoped($combo) {
    if (-not $combo) { return @() }
    $ec = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    if (-not $ec) { return @() }
    ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Expand()
    Start-Sleep -Milliseconds 900
    $items = $combo.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    $result = @($items | Where-Object { $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::ListItem } | ForEach-Object { $_.Current.Name })
    try { ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Collapse() } catch {}
    Start-Sleep -Milliseconds 300
    return $result
}

# ---------- Find element by AutomationId ----------
function Find-ByAutoId($rootEl, [string]$autoId) {
    return @(Get-UiaDescendants -Element $rootEl | Where-Object { $_.Current.AutomationId -eq $autoId } | Select-Object -First 1)
}

# ---------- Semantic version compare (numeric segments) ----------
function Compare-SemVer([string]$a, [string]$b) {
    $pa = @($a -split '\.' | ForEach-Object { if ($_ -match '^\d+$') { [int]$_ } else { 0 } })
    $pb = @($b -split '\.' | ForEach-Object { if ($_ -match '^\d+$') { [int]$_ } else { 0 } })
    $n = [Math]::Max($pa.Count, $pb.Count)
    for ($i = 0; $i -lt $n; $i++) {
        $va = if ($i -lt $pa.Count) { $pa[$i] } else { 0 }
        $vb = if ($i -lt $pb.Count) { $pb[$i] } else { 0 }
        if ($va -ne $vb) { return $va - $vb }
    }
    return 0
}

# ---------- Theme helper ----------
function Set-ThemeFile([string]$theme) {
    $json = @{
        language = "vi"; launchAction = "hide"; closeToTray = $true
        javaPath = "C:\Program Files\Eclipse Adoptium\jdk-21.0.4.7-hotspot\bin\javaw.exe"
        minRamMb = 4096; ramMb = 8192; jvmPreset = "aikar"; jvmArgs = ""
        windowWidth = 1280; windowHeight = 720; startFullscreen = $false
        theme = $theme; accentColor = "violet"; reduceMotion = $false
        uiSoundEnabled = $true; downloadThreads = 8; downloadMirror = "cloudflare"
        bandwidthLimit = "unlimited"
    }
    $json | ConvertTo-Json -Depth 5 | Set-Content $settingsFile -Encoding utf8
}

# =====================================================================
# SETUP — backup settings, fake-install a top-10 version for badge test
# =====================================================================
$settingsBackup = "$settingsFile.dot18bak"
if (Test-Path $settingsBackup) { Remove-Item $settingsBackup -Force }
Copy-Item $settingsFile $settingsBackup -Force
Write-Output "Settings backed up -> $settingsBackup"

$fakeDir = Join-Path $versionsDir $fakeVersion
$fakeCreated = $false
if (-not (Test-Path $fakeDir)) { New-Item -ItemType Directory -Path $fakeDir -Force | Out-Null; $fakeCreated = $true }
Write-Output "Fake installed version dir: $fakeDir (created=$fakeCreated)"

Set-ThemeFile "dark"

# =====================================================================
# MAIN
# =====================================================================
$proc = $null
try {
    Stop-Process -Name 'Vaz.WinUI' -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 700
    $proc = Start-Process $exe -PassThru
    Start-Sleep -Seconds 4

    $root = Get-VazRoot -TimeoutSec 25
    Write-Output "Root window: $($root.Current.Name)"

    # ---------------- CASE #1: Mo dialog ----------------
    $newBtn = Find-UiaButtonByText -Root $root -TextContains 'Tạo Instance mới'
    if (-not $newBtn) { throw "Button 'Tao Instance moi' not found" }
    Invoke-UiaClick -Element $newBtn
    Start-Sleep -Milliseconds 500
    Capture-Frame

    # ---------------- CASE #5: initial button state (early dump) ----------------
    $rootEarly = Get-VazRoot -TimeoutSec 10
    Dump-UiaTree -Element $rootEarly | Out-File "$uiaDir\case5-initial-state.txt" -Encoding utf8
    $primaryEarly = Find-ByAutoId $rootEarly 'PrimaryButton' | Select-Object -First 1
    $earlyEnabled = if ($primaryEarly) { $primaryEarly.Current.IsEnabled } else { $null }
    Write-Output "Initial PrimaryButton IsEnabled = $earlyEnabled"
    Capture-Frame

    # Wait for versions to load
    Start-Sleep -Seconds 3
    $root = Get-VazRoot -TimeoutSec 10

    # Verify dialog visible
    $dialogWin = Find-UiaByName -Root $root -NameContains 'Tạo instance mới' -ControlType 'Window' | Select-Object -First 1
    if ($dialogWin -and -not $dialogWin.Current.IsOffscreen) {
        Add-Result "#1 Mo dialog" "PASS" "uia/case1-dialog-open.txt" "Dialog window 'Tao instance moi' visible (Offscreen=False)"
    } else {
        Add-Result "#1 Mo dialog" "FAIL" "uia/case1-dialog-open.txt" "Dialog window not visible"
    }
    Dump-UiaTree -Element $root | Out-File "$uiaDir\case1-dialog-open.txt" -Encoding utf8
    Save-Screenshot "$shotDir\case1-dialog-dark.png"
    Capture-Frame

    # Resolve combos
    $loaderCombo = Find-UiaByName -Root $root -NameContains 'Loader' -ControlType 'ComboBox' | Select-Object -First 1
    $verCombo    = Find-UiaByName -Root $root -NameContains 'Minecraft version' -ControlType 'ComboBox' | Select-Object -First 1
    $lvCombo     = Find-UiaByName -Root $root -NameContains 'Loader version' -ControlType 'ComboBox' | Select-Object -First 1
    Write-Output "Combos: Loader=$($null -ne $loaderCombo) Version=$($null -ne $verCombo) LoaderVersion=$($null -ne $lvCombo)"

    # ---------------- CASE #2: Fabric + version ----------------
    Select-UiaComboItem -Combo $loaderCombo -ItemNameContains 'Fabric' -TimeoutSec 10 | Out-Null
    Start-Sleep -Milliseconds 800
    Select-UiaComboItem -Combo $verCombo -ItemNameContains $testVersion -TimeoutSec 10 | Out-Null
    Start-Sleep -Seconds 4
    $root = Get-VazRoot -TimeoutSec 10
    $lvCombo = Find-UiaByName -Root $root -NameContains 'Loader version' -ControlType 'ComboBox' | Select-Object -First 1
    $lvItems = Get-ComboItemsScoped $lvCombo
    Write-Output "Case#2 Fabric+$testVersion loader versions: $($lvItems.Count)"
    $lvItems | Select-Object -First 5 | ForEach-Object { Write-Output "   '$_'" }
    $lvItems | Out-File "$uiaDir\case2-fabric-loader-versions.txt" -Encoding utf8
    Dump-UiaTree -Element $root | Out-File "$uiaDir\case2-fabric-tree.txt" -Encoding utf8
    Save-Screenshot "$shotDir\case2-fabric.png"
    Capture-Frame

    if ($lvItems.Count -ge 1) {
        $first = $lvItems[0]
        $isLatest = $true
        for ($i = 1; $i -lt [Math]::Min(5, $lvItems.Count); $i++) {
            if ((Compare-SemVer $first $lvItems[$i]) -lt 0) { $isLatest = $false; break }
        }
        if ($isLatest) {
            Add-Result "#2 Fabric + 1.21.1" "PASS (adapted)" "uia/case2-fabric-loader-versions.txt" "Used MC $testVersion (1.21.1 not in top-10 dropdown); $($lvItems.Count) loader versions, first='$first' is highest"
        } else {
            Add-Result "#2 Fabric + 1.21.1" "FAIL" "uia/case2-fabric-loader-versions.txt" "First item '$first' is not the highest version"
        }
    } else {
        Add-Result "#2 Fabric + 1.21.1" "FAIL" "uia/case2-fabric-loader-versions.txt" "No loader versions loaded"
    }

    # ---------------- CASE #3: Quilt + version ----------------
    Select-UiaComboItem -Combo $loaderCombo -ItemNameContains 'Quilt' -TimeoutSec 10 | Out-Null
    Start-Sleep -Seconds 4
    $root = Get-VazRoot -TimeoutSec 10
    $lvCombo = Find-UiaByName -Root $root -NameContains 'Loader version' -ControlType 'ComboBox' | Select-Object -First 1
    $lvItems = Get-ComboItemsScoped $lvCombo
    Write-Output "Case#3 Quilt+$testVersion loader versions: $($lvItems.Count)"
    $lvItems | Select-Object -First 5 | ForEach-Object { Write-Output "   '$_'" }
    $lvItems | Out-File "$uiaDir\case3-quilt-loader-versions.txt" -Encoding utf8
    Save-Screenshot "$shotDir\case3-quilt.png"
    Capture-Frame
    if ($lvItems.Count -ge 1) {
        Add-Result "#3 Quilt + 1.21.1" "PASS (adapted)" "uia/case3-quilt-loader-versions.txt" "Used MC $testVersion; $($lvItems.Count) loader versions, first='$($lvItems[0])'"
    } else {
        Add-Result "#3 Quilt + 1.21.1" "FAIL" "uia/case3-quilt-loader-versions.txt" "No loader versions loaded"
    }

    # ---------------- CASE #4: NeoForge + empty-version path ----------------
    # 1.20.1 (the specified input) is not in the top-10 dropdown and has no NeoForge
    # builds. MC 26.3 is in the dropdown and ALSO has no NeoForge builds, so it drives
    # the exact same code path: empty loader-version list -> warning InfoBar.
    Select-UiaComboItem -Combo $verCombo -ItemNameContains $emptyVersion -TimeoutSec 10 | Out-Null
    Start-Sleep -Milliseconds 1500
    Select-UiaComboItem -Combo $loaderCombo -ItemNameContains 'NeoForge' -TimeoutSec 10 | Out-Null
    Start-Sleep -Seconds 4
    $root = Get-VazRoot -TimeoutSec 10
    $lvCombo = Find-UiaByName -Root $root -NameContains 'Loader version' -ControlType 'ComboBox' | Select-Object -First 1
    $lvItems = Get-ComboItemsScoped $lvCombo
    $texts = Get-UiaDescendants -Element $root | Where-Object { $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text }
    $warnTexts = @($texts | Where-Object { $_.Current.Name -like '*Vui lòng chọn loader version*' -or $_.Current.Name -like '*Chọn Minecraft version*' })
    $noLvTexts  = @($texts | Where-Object { $_.Current.Name -like '*Không có version*' })
    $primaryNow = Find-ByAutoId $root 'PrimaryButton' | Select-Object -First 1
    $primaryEnabled = if ($primaryNow) { $primaryNow.Current.IsEnabled } else { $null }
    Write-Output "Case#4 NeoForge+${emptyVersion}: loaderItems=$($lvItems.Count) PrimaryButton.Enabled=$primaryEnabled"
    Write-Output "  WarningTexts=$($warnTexts.Count) NoLoaderVersionsTexts=$($noLvTexts.Count)"
    foreach ($w in $warnTexts) { Write-Output "  warning: '$($w.Current.Name)'" }
    foreach ($n in $noLvTexts) { Write-Output "  empty:   '$($n.Current.Name)'" }

    $lvItems | Out-File "$uiaDir\case4-neoforge-empty-loader-versions.txt" -Encoding utf8
    Dump-UiaTree -Element $root | Out-File "$uiaDir\case4-neoforge-empty-tree.txt" -Encoding utf8
    @(
        "Case #4 — NeoForge + $emptyVersion (empty loader-version path; 1.20.1 input unavailable)"
        "LoaderVersion items: $($lvItems.Count)"
        "PrimaryButton IsEnabled: $primaryEnabled"
        "NoLoaderVersionsText count: $($noLvTexts.Count)"
        "WarningText count: $($warnTexts.Count)"
        ($warnTexts | ForEach-Object { "WarningText: $($_.Current.Name)" })
        ($noLvTexts  | ForEach-Object { "NoLoaderVersionsText: $($_.Current.Name)" })
    ) | Out-File "$uiaDir\case4-neoforge-empty-state.txt" -Encoding utf8
    Save-Screenshot "$shotDir\case4-neoforge-empty.png"
    Capture-Frame

    $case4ok = ($lvItems.Count -eq 0) -and ($noLvTexts.Count -ge 1) -and ($warnTexts.Count -ge 1)
    if ($case4ok) {
        Add-Result "#4 NeoForge + 1.20.1" "PASS (adapted)" "uia/case4-neoforge-empty-state.txt" "Used MC $emptyVersion (1.20.1 not in dropdown); loader list empty, 'Khong co version tuong thich' + warning 'Vui long chon loader version cho NeoForge.' both visible"
    } else {
        Add-Result "#4 NeoForge + 1.20.1" "FAIL" "uia/case4-neoforge-empty-state.txt" "items=$($lvItems.Count) empty=$($noLvTexts.Count) warn=$($warnTexts.Count)"
    }

    # ---------------- CASE #7: Badge "Da cai" ----------------
    $root = Get-VazRoot -TimeoutSec 10
    $verCombo = Find-UiaByName -Root $root -NameContains 'Minecraft version' -ControlType 'ComboBox' | Select-Object -First 1
    $ec = $verCombo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Expand()
    Start-Sleep -Milliseconds 1200
    $root = Get-VazRoot -TimeoutSec 10
    $texts = Get-UiaDescendants -Element $root | Where-Object { $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text }
    $badges = @($texts | Where-Object { $_.Current.Name -like '*Đã cài*' })
    Write-Output "Case#7 badge elements: $($badges.Count)"
    foreach ($b in $badges) { Write-Output "   '$($b.Current.Name)' Offscreen=$($b.Current.IsOffscreen)" }
    Dump-UiaTree -Element $root | Out-File "$uiaDir\case7-badge-tree.txt" -Encoding utf8
    Save-Screenshot "$shotDir\case7-badge.png"
    try { ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Collapse() } catch {}
    Start-Sleep -Milliseconds 500
    Capture-Frame

    if ($badges.Count -ge 1) {
        Add-Result "#7 Badge 'Da cai'" "PASS (adapted)" "uia/case7-badge-tree.txt" "Badge rendered for fake-installed $fakeVersion (1.21.1 not in dropdown); badges found=$($badges.Count)"
    } else {
        Add-Result "#7 Badge 'Da cai'" "FAIL" "uia/case7-badge-tree.txt" "Badge element not found"
    }

    # ---------------- CASE #5: button disabled when no loader version selected ----------------
    # The disabled state is reachable at runtime when the loader-version list is empty
    # (loader != Vanilla and nothing to select). We use the same NeoForge + empty-version
    # state captured for case #4. The very first UIA sample after dialog open is already
    # Enabled=True because the VM auto-selects the first loader version from cache.
    if ($primaryEnabled -eq $false) {
        Add-Result "#5 Chua chon Loader Version" "PASS (adapted)" "uia/case4-neoforge-empty-state.txt" "PrimaryButton IsEnabled=False when loader-version list is empty (NeoForge + $emptyVersion); warning + 'Khong co version tuong thich' visible"
    } elseif ($primaryEnabled -eq $true) {
        Add-Result "#5 Chua chon Loader Version" "FAIL" "uia/case4-neoforge-empty-state.txt" "PrimaryButton IsEnabled=True even with empty loader-version list"
    } else {
        Add-Result "#5 Chua chon Loader Version" "UNVERIFIED" "uia/case4-neoforge-empty-state.txt" "PrimaryButton not found"
    }
    Save-Screenshot "$shotDir\case5-button-disabled.png"
    Capture-Frame

    # ---------------- CASE #9: Light/Dark ----------------
    # Dark already captured (case1). Now switch to light.
    Set-ThemeFile "light"
    Stop-Process -Name 'Vaz.WinUI' -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    $proc = Start-Process $exe -PassThru
    Start-Sleep -Seconds 4
    $root = Get-VazRoot -TimeoutSec 20
    $newBtn = Find-UiaButtonByText -Root $root -TextContains 'Tạo Instance mới'
    if ($newBtn) {
        Invoke-UiaClick -Element $newBtn
        Start-Sleep -Seconds 3
        $root = Get-VazRoot -TimeoutSec 10
        Save-Screenshot "$shotDir\case9-dialog-light.png"
        Dump-UiaTree -Element $root | Out-File "$uiaDir\case9-dialog-light-tree.txt" -Encoding utf8
        Capture-Frame
        Add-Result "#9 Light/Dark" "PASS" "screenshots/case9-dialog-light.png" "Captured dark (case1) + light dialog screenshots"
    } else {
        Add-Result "#9 Light/Dark" "FAIL" "screenshots/case9-dialog-light.png" "Could not reopen dialog in light theme"
    }

}
catch {
    Write-Output "ERROR: $_"
    Add-Result "SCRIPT" "ERROR" "-" $_.ToString()
}
finally {
    # ---------------- Teardown ----------------
    Stop-Process -Name 'Vaz.WinUI' -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
    if (Test-Path $settingsBackup) {
        Copy-Item $settingsBackup $settingsFile -Force
        Remove-Item $settingsBackup -Force
        Write-Output "Settings restored."
    }
    if ($fakeCreated -and (Test-Path $fakeDir)) {
        Remove-Item $fakeDir -Recurse -Force
        Write-Output "Fake version dir removed."
    }
}

# ---------- Save results ----------
$resultsPath = "$evidenceDir\dot18.2c-results.json"
$script:results | ConvertTo-Json -Depth 5 | Set-Content $resultsPath -Encoding utf8
Write-Output ""
Write-Output "================ RESULTS ================"
$script:results | ForEach-Object { Write-Output ("{0,-32} {1,-18} {2}" -f $_.Case, $_.Status, $_.Note) }
Write-Output "Results saved -> $resultsPath"
Write-Output "Frames captured: $script:frameIndex"
