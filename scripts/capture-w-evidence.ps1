Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type -ReferencedAssemblies "System.Windows.Forms.dll", "System.Drawing.dll" -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class WinDriverW {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
'@ -ErrorAction SilentlyContinue

function Set-Size([int]$w, [int]$h) {
    $p = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) {
        [WinDriverW]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
        [WinDriverW]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
        [WinDriverW]::MoveWindow($p.MainWindowHandle, 0, 0, $w, $h, $true) | Out-Null
        Start-Sleep -Milliseconds 600
    }
}

function Snap-Win([string]$path) {
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/capture-window.ps1 -OutputPath $path
    Write-Host "SAVED: $path"
}

# Restart App on InstanceDetailPage, tab resourcepacks
Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
$env:VAZER_STARTUP_PAGE = "InstanceDetailPage"
$env:VAZER_STARTUP_TAB = "resourcepacks"

Start-Process "src/Vaz.WinUI/bin/x64/Debug/net8.0-windows10.0.26100.0/win-x64/Vaz.WinUI.exe"
Start-Sleep -Seconds 3

# 1. Capture W4 Cleaned Header at 1025x533
Set-Size 1025 533
Snap-Win "docs/evidence/W4-header-cleaned-1025x533.png"

# 2. Open Inspector Drawer on Resource Packs at 1025x533
$p = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "Xem chi tiết")
$detailBtn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
if ($detailBtn) {
    $pattern = $detailBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    Start-Sleep -Milliseconds 600
}

# 3. Capture W2: Active Column + Open Drawer at 1025x533
Snap-Win "docs/evidence/W2-active-column-drawer-open-1025x533.png"

# 4. Open ... Overflow menu on active card
$moreCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "Tùy chọn khác")
$moreBtn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $moreCond)
if ($moreBtn) {
    $pattern = $moreBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    Start-Sleep -Milliseconds 600
}
# Capture W2: Open ... menu at 1025x533
Snap-Win "docs/evidence/W2-overflow-menu-open-1025x533.png"

# Close menu/drawer by pressing Escape
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
Start-Sleep -Milliseconds 400

# 5. Resize to 960x530 (W3)
Set-Size 960 530

# Reopen drawer at 960x530
$detailBtn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
if ($detailBtn) {
    $pattern = $detailBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    Start-Sleep -Milliseconds 600
}
# Capture W3: RP with open drawer at 960x530
Snap-Win "docs/evidence/W3-rp-open-drawer-960x530.png"
Snap-Win "docs/evidence/W2-active-column-drawer-open-960x530.png"

# Reopen ... menu at 960x530
$moreBtn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $moreCond)
if ($moreBtn) {
    $pattern = $moreBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    Start-Sleep -Milliseconds 600
}
Snap-Win "docs/evidence/W2-overflow-menu-open-960x530.png"

# Close menu and drawer
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
Start-Sleep -Milliseconds 400

# Switch to Shaders tab via F3 or TabItem
[System.Windows.Forms.SendKeys]::SendWait("{F3}")
Start-Sleep -Milliseconds 1000

# 6. Capture W3: Shaders at 960x530
Snap-Win "docs/evidence/W3-shaders-960x530.png"

# 7. Resize to 1025x533 for Shaders
Set-Size 1025 533

# Click filter chip "Fantasy" or "Hiệu năng" (which has 2 shader results)
# In ResourcePackShaderData:
# Complementary Reimagined (Fantasy), Complementary Shaders (Hiệu năng, Fantasy), Sildur's Vibrant (Hiệu năng)
# Clicking "Fantasy" gives Complementary Reimagined + Complementary Shaders (exact 2 results!)
$radioCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::RadioButton)
$radios = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $radioCond)
foreach ($r in $radios) {
    if ($r.Current.Name -eq "Fantasy") {
        $pattern = $r.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $pattern.Select()
        Start-Sleep -Milliseconds 600
        break
    }
}
# Capture W5: Shaders 2 filter results with UniformGridLayout at 1025x533
Snap-Win "docs/evidence/W5-shaders-filtered-2results-1025x533.png"

Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
Write-Host "ALL CAPTURES COMPLETED"
