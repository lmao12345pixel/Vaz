Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type -ReferencedAssemblies "System.Windows.Forms.dll", "System.Drawing.dll" -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class WinDriver9 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    public const int VK_MENU = 0x12; // ALT key
    public const int VK_DOWN = 0x28; // DOWN key
    public const int VK_UP = 0x26;   // UP key
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public static void SendAltDown() {
        keybd_event((byte)VK_MENU, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        keybd_event((byte)VK_DOWN, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        keybd_event((byte)VK_DOWN, 0, KEYEVENTF_KEYUP, 0);
        System.Threading.Thread.Sleep(50);
        keybd_event((byte)VK_MENU, 0, KEYEVENTF_KEYUP, 0);
        System.Threading.Thread.Sleep(300);
    }
}
'@ -ErrorAction SilentlyContinue

function Set-Size([int]$w, [int]$h) {
    $p = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) {
        [WinDriver9]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
        [WinDriver9]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
        [WinDriver9]::MoveWindow($p.MainWindowHandle, 0, 0, $w, $h, $true) | Out-Null
        Start-Sleep -Milliseconds 600
    }
}

function Snap-Win([string]$path) {
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/capture-window.ps1 -OutputPath $path
    Write-Host "SAVED: $path"
}

# 1. Start app
Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
$env:VAZER_STARTUP_PAGE = "InstanceDetailPage"
$env:VAZER_STARTUP_TAB = "resourcepacks"

$proc = Start-Process "src/Vaz.WinUI/bin/x64/Debug/net8.0-windows10.0.26100.0/win-x64/Vaz.WinUI.exe" -PassThru
Start-Sleep -Seconds 3

Set-Size 1025 533

# 2. Dump UIAutomation properties to W9-uia-dump.txt
$dumpLines = @()
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)

$dumpLines += "=== UI AUTOMATION BUTTON AUDIT (Vaz.WinUI - InstanceDetailPage) ==="
$dumpLines += "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$dumpLines += "Total Buttons Found: $($buttons.Count)`n"

foreach ($b in $buttons) {
    $name = $b.Current.Name
    $help = $b.Current.HelpText
    $autoId = $b.Current.AutomationId
    $rect = $b.Current.BoundingRectangle
    $className = $b.Current.ClassName
    $bX = if ([double]::IsInfinity($rect.X) -or [double]::IsNaN($rect.X)) { -1 } else { [int]$rect.X }
    $bY = if ([double]::IsInfinity($rect.Y) -or [double]::IsNaN($rect.Y)) { -1 } else { [int]$rect.Y }
    $bW = if ([double]::IsInfinity($rect.Width) -or [double]::IsNaN($rect.Width)) { -1 } else { [int]$rect.Width }
    $bH = if ([double]::IsInfinity($rect.Height) -or [double]::IsNaN($rect.Height)) { -1 } else { [int]$rect.Height }
    $dumpLines += "Control: Class='$className' | AutoId='$autoId' | Name='$name' | HelpText/ToolTip='$help' | Bounds=[X=$bX, Y=$bY, W=$bW, H=$bH]"
}

$dumpLines | Out-File -FilePath "docs/evidence/W9-uia-dump.txt" -Encoding utf8
Write-Host "SAVED: docs/evidence/W9-uia-dump.txt"

# 3. Capture W9 Alt+Down Before
Snap-Win "docs/evidence/W9-alt-reorder-before-1025x533.png"

# Send Alt+Down via Win32 keybd_event (Keyboard only)
[WinDriver9]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 200
[WinDriver9]::SendAltDown()
Start-Sleep -Milliseconds 600

# Capture W9 Alt+Down After
Snap-Win "docs/evidence/W9-alt-reorder-after-1025x533.png"

# 4. Press F6 to simulate empty directory
[System.Windows.Forms.SendKeys]::SendWait("{F6}")
Start-Sleep -Milliseconds 600

# Capture RPS-AC-01-empty-directory-1025x533.png
Snap-Win "docs/evidence/RPS-AC-01-empty-directory-1025x533.png"

Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
Write-Host "ALL W9 & EMPTY STATE CAPTURES DONE"
