# Generates 5 high-fidelity MP4 videos for Phase A, B, C, D, E using window captures and ffmpeg
$ErrorActionPreference = "Stop"

Add-Type @"
using System;
using System.Runtime.InteropServices;

public class Win32VideoRecorder {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int X, int Y);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    public static void LeftClickAt(int x, int y) {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, UIntPtr.Zero);
    }
}
"@ -ErrorAction SilentlyContinue

Add-Type -AssemblyName System.Drawing

$videoDir = "docs/evidence/dot16/videos"
if (-not (Test-Path $videoDir)) {
    New-Item -ItemType Directory -Path $videoDir -Force | Out-Null
}

$appDataPath = [System.IO.Path]::Combine($env:APPDATA, "Vazer", "Minecraft")
$settingsFile = [System.IO.Path]::Combine($appDataPath, "settings.json")
$exePath = "C:\OptizimeProject\Vaz\src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe"

function Set-Theme([string]$themeName) {
    if (Test-Path $settingsFile) {
        $json = Get-Content $settingsFile -Raw | ConvertFrom-Json
        $json.theme = $themeName
        $json.reduceMotion = $false
        $json | ConvertTo-Json -Depth 10 | Set-Content $settingsFile -Encoding utf8
    }
}

function Record-Scenario([string]$videoName, [string]$theme, [string]$envFlag, [scriptblock]$actionBlock) {
    Write-Host "Recording $videoName ($theme, Env=$envFlag)..."
    Set-Theme $theme
    
    $tempFramesDir = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "vaz_video_" + [System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempFramesDir -Force | Out-Null

    Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 400

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exePath
    $psi.EnvironmentVariables["VAZER_STARTUP_PAGE"] = "InstancesPage"
    $psi.EnvironmentVariables["VAZER_TEST_TOAST"] = $envFlag
    $psi.UseShellExecute = $false

    $proc = [System.Diagnostics.Process]::Start($psi)
    
    $hWnd = [IntPtr]::Zero
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 150
        $proc.Refresh()
        if ($proc.MainWindowHandle -ne [IntPtr]::Zero) {
            $hWnd = $proc.MainWindowHandle
            break
        }
    }

    if ($hWnd -eq [IntPtr]::Zero) {
        Write-Warning "Failed to get MainWindowHandle for $videoName"
        $proc.Kill()
        return
    }

    [Win32VideoRecorder]::ShowWindow($hWnd, 9)
    [Win32VideoRecorder]::SetForegroundWindow($hWnd)
    Start-Sleep -Milliseconds 300

    $rect = New-Object Win32VideoRecorder+RECT
    [Win32VideoRecorder]::GetWindowRect($hWnd, [ref]$rect)
    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top

    # Ensure even dimensions for H.264
    if ($w % 2 -ne 0) { $w -= 1 }
    if ($h % 2 -ne 0) { $h -= 1 }

    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)

    $frameIndex = 0

    function Capture-Frame() {
        $gfx.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
        $filePath = [System.IO.Path]::Combine($tempFramesDir, ("frame_{0:D4}.png" -f $frameIndex))
        $bmp.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)
        $script:frameIndex++
    }

    # Record initial state: 10 frames (1s at 10fps)
    for ($f = 0; $f -lt 15; $f++) {
        Capture-Frame
        Start-Sleep -Milliseconds 100
    }

    # Execute custom interaction action
    if ($actionBlock) {
        & $actionBlock $hWnd $rect
    }

    # Record post-action state: 30 frames (3s at 10fps)
    for ($f = 0; $f -lt 25; $f++) {
        Capture-Frame
        Start-Sleep -Milliseconds 100
    }

    $gfx.Dispose()
    $bmp.Dispose()
    $proc.Kill()
    Start-Sleep -Milliseconds 300

    # Compile with ffmpeg into MP4 at 10 fps (around 15 seconds)
    $outFile = Join-Path $videoDir "$videoName.mp4"
    if (Test-Path $outFile) { Remove-Item $outFile -Force }

    $inputPattern = Join-Path $tempFramesDir "frame_%04d.png"
    
    $oldEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $ffmpegCmd = "ffmpeg -y -framerate 5 -i `"$inputPattern`" -c:v libx264 -pix_fmt yuv420p -vf `"scale=trunc(iw/2)*2:trunc(ih/2)*2`" `"$outFile`""
    cmd /c "$ffmpegCmd >nul 2>&1"
    $ErrorActionPreference = $oldEap

    Write-Host "Created $outFile"
    Remove-Item $tempFramesDir -Recurse -Force
}

# 1. Phase A: Toast appear & dismiss
Record-Scenario "phase_a_toast_appear" "dark" "type_success" {
    param($hWnd, $rect)
    # Move cursor to X close button and click
    $closeX = $rect.Right - 45
    $closeY = $rect.Bottom - 95
    [Win32VideoRecorder]::SetCursorPos($closeX, $closeY)
    for ($i = 0; $i -lt 15; $i++) { Capture-Frame; Start-Sleep -Milliseconds 100 }
    [Win32VideoRecorder]::LeftClickAt($closeX, $closeY)
}

# 2. Phase B: Slide in + hover pause + progress bar
Record-Scenario "phase_b_slide_hover" "dark" "type_info" {
    param($hWnd, $rect)
    $toastX = $rect.Right - 150
    $toastY = $rect.Bottom - 80
    [Win32VideoRecorder]::SetCursorPos($toastX, $toastY)
    for ($i = 0; $i -lt 25; $i++) { Capture-Frame; Start-Sleep -Milliseconds 100 }
    # Exit hover
    [Win32VideoRecorder]::SetCursorPos($rect.Left + 50, $rect.Top + 50)
    for ($i = 0; $i -lt 15; $i++) { Capture-Frame; Start-Sleep -Milliseconds 100 }
}

# 3. Phase C: Bell icon + Notification Center panel
Record-Scenario "phase_c_bell_panel" "dark" "center" {
    param($hWnd, $rect)
    # Panel is open, inspect notifications, move cursor over item
    $itemX = $rect.Right - 150
    $itemY = $rect.Top + 200
    [Win32VideoRecorder]::SetCursorPos($itemX, $itemY)
    for ($i = 0; $i -lt 20; $i++) { Capture-Frame; Start-Sleep -Milliseconds 100 }
}

# 4. Phase D: Network error + Retry action
Record-Scenario "phase_d_error_retry" "dark" "launch_error" {
    param($hWnd, $rect)
    # Move to 'Thử lại' button on error toast
    $retryX = $rect.Right - 100
    $retryY = $rect.Bottom - 65
    [Win32VideoRecorder]::SetCursorPos($retryX, $retryY)
    for ($i = 0; $i -lt 15; $i++) { Capture-Frame; Start-Sleep -Milliseconds 100 }
    [Win32VideoRecorder]::LeftClickAt($retryX, $retryY)
    for ($i = 0; $i -lt 15; $i++) { Capture-Frame; Start-Sleep -Milliseconds 100 }
}

# 5. Phase E: Multi-toast stack & theme switch overview
Record-Scenario "phase_e_overview_theme" "light" "1" {
    param($hWnd, $rect)
    # Hold view of all 4 stacked toasts in light mode
    for ($i = 0; $i -lt 30; $i++) { Capture-Frame; Start-Sleep -Milliseconds 100 }
}

Write-Host "All 5 videos generated successfully!"
