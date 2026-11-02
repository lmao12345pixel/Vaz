Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$code = @"
using System;
using System.Runtime.InteropServices;

public class Win32Capture {
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@
Add-Type -TypeDefinition $code

$outDir = "docs/evidence/dot15/animations"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Capture-Window($proc, $filePath) {
    $rect = New-Object Win32Capture+RECT
    [Win32Capture]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) { return }

    $bmp = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bmp)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($width, $height)))
    $graphics.Dispose()
    $bmp.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "Saved: $filePath"
}

# Ensure Vaz.WinUI stopped
Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "src/Vaz.WinUI/bin/x64/Debug/net8.0-windows10.0.26100.0/win-x64/Vaz.WinUI.exe"
$psi.UseShellExecute = $false
$psi.EnvironmentVariables["VAZER_STARTUP_PAGE"] = "InstanceDetailPage"
$psi.EnvironmentVariables["VAZER_STARTUP_INSTANCE"] = "test fabric"
$psi.EnvironmentVariables["VAZER_STARTUP_TAB"] = "resourcepacks"

$proc = [System.Diagnostics.Process]::Start($psi)

# Wait for window handle
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Milliseconds 300
    $proc.Refresh()
    if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { break }
}

[Win32Capture]::ShowWindow($proc.MainWindowHandle, 9)
[Win32Capture]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Milliseconds 800

$rect = New-Object Win32Capture+RECT
[Win32Capture]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null

# 1. Staggered Entrance / Initial Load
Capture-Window $proc (Join-Path $outDir "01_staggered_entrance_loaded.png")

# 2. Card Hover (Hover over first card at ~350, 480 relative to window)
$hoverX = $rect.Left + 420
$hoverY = $rect.Top + 480
[Win32Capture]::SetCursorPos($hoverX, $hoverY)
Start-Sleep -Milliseconds 300
Capture-Window $proc (Join-Path $outDir "02_card_hover.png")

# 3. Tab Switch Animation (Click on Shaders Tab at top ~ 560, 260)
$tabShadersX = $rect.Left + 560
$tabShadersY = $rect.Top + 260
[Win32Capture]::SetCursorPos($tabShadersX, $tabShadersY)
[Win32Capture]::mouse_event([Win32Capture]::MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
[Win32Capture]::mouse_event([Win32Capture]::MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)

# Capture immediately during transition
Start-Sleep -Milliseconds 80
Capture-Window $proc (Join-Path $outDir "03_tab_switch_transition.png")
Start-Sleep -Milliseconds 300
Capture-Window $proc (Join-Path $outDir "04_tab_switch_completed.png")

# 4. Trigger Delete Dialog on Shaders (Click delete button on first card at ~570, 780)
# Click Shaders first card delete icon
$delX = $rect.Left + 570
$delY = $rect.Top + 780
[Win32Capture]::SetCursorPos($delX, $delY)
[Win32Capture]::mouse_event([Win32Capture]::MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
[Win32Capture]::mouse_event([Win32Capture]::MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
Start-Sleep -Milliseconds 250
Capture-Window $proc (Join-Path $outDir "05_delete_dialog_entrance.png")

$proc.Kill()
Write-Output "Animation sequence capture complete!"
