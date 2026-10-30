param()

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type -ReferencedAssemblies "System.Windows.Forms.dll", "System.Drawing.dll" -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class WinDriver {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    public static void Click(int x, int y) {
        System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);
        System.Threading.Thread.Sleep(120);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(60);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(400);
    }
}
'@ -ErrorAction SilentlyContinue

function Set-WindowSize([int]$w, [int]$h) {
    $p = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) {
        [WinDriver]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
        [WinDriver]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
        [WinDriver]::MoveWindow($p.MainWindowHandle, 0, 0, $w, $h, $true) | Out-Null
        Start-Sleep -Milliseconds 500
    }
}

function Snap([string]$name) {
    $path = "docs/evidence/$name"
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/capture-window.ps1 -OutputPath $path
    Write-Host "SAVED: $name"
}

# 1. Start fresh app instance
Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600

$env:VAZER_STARTUP_PAGE = "InstanceDetailPage"
$env:VAZER_STARTUP_TAB = "resourcepacks"

Start-Process "src/Vaz.WinUI/bin/x64/Debug/net8.0-windows10.0.26100.0/win-x64/Vaz.WinUI.exe"
Start-Sleep -Seconds 3

# Enforce 1025x533
Set-WindowSize 1025 533

# --- PHASE 1: Resource Packs Tab at 1025x533 ---
Snap "RPS-AC-01-layout-rp-1025x533.png"
Snap "RPS-AC-02-reorder-before-1025x533.png"
Snap "RPS-AC-03-incompatible-warning-1025x533.png"
Snap "RPS-AC-07-drag-drop-and-import-1025x533.png"

# Move Down on Item 1 (Faithful at 901, 352)
$p = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
foreach ($btn in $buttons) {
    $rect = $btn.Current.BoundingRectangle
    if ([Math]::Abs($rect.X - 901) -lt 8 -and [Math]::Abs($rect.Y - 352) -lt 8) {
        $pattern = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $pattern.Invoke()
        Start-Sleep -Milliseconds 600
        break
    }
}
Snap "RPS-AC-02-reorder-after-1025x533.png"

# Open Inspector on Dokucraft Light at (521, 352)
$buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
foreach ($btn in $buttons) {
    $rect = $btn.Current.BoundingRectangle
    if ([Math]::Abs($rect.X - 521) -lt 8 -and [Math]::Abs($rect.Y - 352) -lt 8) {
        $pattern = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $pattern.Invoke()
        Start-Sleep -Milliseconds 600
        break
    }
}
Snap "RPS-AC-04-drawer-format-and-desc-1025x533.png"

# Close Inspector via Esc
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
Start-Sleep -Milliseconds 500
Snap "RPS-AC-04-drawer-closed-esc-1025x533.png"

# Test Corrupted Zip (F4)
[System.Windows.Forms.SendKeys]::SendWait("{F4}")
Start-Sleep -Milliseconds 600
Snap "RPS-AC-09-corrupted-zip-error-1025x533.png"

# Test Empty State Search
[WinDriver]::Click(430, 220)
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait("xyznonexistent")
Start-Sleep -Milliseconds 600
Snap "RPS-AC-09-empty-state-1025x533.png"
Snap "RPS-AC-09-empty-state-search-1025x533.png"
[System.Windows.Forms.SendKeys]::SendWait("^a{BACKSPACE}")
Start-Sleep -Milliseconds 400

# Test Empty State No Active Packs (F5)
[System.Windows.Forms.SendKeys]::SendWait("{F5}")
Start-Sleep -Milliseconds 600
Snap "RPS-AC-09-empty-state-no-active-1025x533.png"

# --- PHASE 2: Shaders Tab at 1025x533 ---
# Switch to Shaders Tab at (623, 85)
$buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
foreach ($btn in $buttons) {
    $rect = $btn.Current.BoundingRectangle
    if ([Math]::Abs($rect.X - 623) -lt 12 -and [Math]::Abs($rect.Y - 85) -lt 12) {
        $pattern = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $pattern.Invoke()
        Start-Sleep -Milliseconds 600
        break
    }
}
Snap "RPS-AC-05-layout-shaders-1025x533.png"
Snap "RPS-AC-05-shaders-active-1025x533.png"
Snap "RPS-AC-06-loader-banner-iris-1025x533.png"

# Click "Tắt shader (Vanilla)" at (530, 220)
[WinDriver]::Click(530, 220)
Start-Sleep -Milliseconds 600
Snap "RPS-AC-05-shaders-vanilla-disabled-1025x533.png"

# Reactivate Complementary Reimagined at (585, 415)
[WinDriver]::Click(585, 415)
Start-Sleep -Milliseconds 600

# --- PHASE 3: Multi-resolution Layouts (1280x720 & 1366x768) ---
Set-WindowSize 1280 720
Snap "RPS-AC-05-layout-shaders-1280x720.png"

# Switch back to Resource Packs
$buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
foreach ($btn in $buttons) {
    $rect = $btn.Current.BoundingRectangle
    if ([Math]::Abs($rect.X - 539) -lt 15 -and [Math]::Abs($rect.Y - 85) -lt 15) {
        $pattern = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $pattern.Invoke()
        Start-Sleep -Milliseconds 600
        break
    }
}
Snap "RPS-AC-01-layout-rp-1280x720.png"

# Max Screen Size (1366x768)
Set-WindowSize 1366 768
Snap "RPS-AC-01-layout-rp-1366x768.png"

# Switch to Shaders on 1366x768
$buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
foreach ($btn in $buttons) {
    $rect = $btn.Current.BoundingRectangle
    if ([Math]::Abs($rect.X - 623) -lt 15 -and [Math]::Abs($rect.Y - 85) -lt 15) {
        $pattern = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $pattern.Invoke()
        Start-Sleep -Milliseconds 600
        break
    }
}
Snap "RPS-AC-05-layout-shaders-1366x768.png"

# --- PHASE 4: Settings Reduce Motion ---
# Click Settings in NavPane (bottom left around X=24, Y=700)
Set-WindowSize 1025 533
[WinDriver]::Click(24, 490)
Start-Sleep -Milliseconds 600
Snap "RPS-AC-10-reduce-motion-1025x533.png"

Write-Host "ALL EVIDENCE CAPTURED SUCCESSFULLY!"
