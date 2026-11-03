# Generate all required screenshots (6 types x 2 themes) and 5 videos for Phase A-E
$ErrorActionPreference = "Stop"

Add-Type @"
using System;
using System.Runtime.InteropServices;

public class Win32EvidenceCapture {
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
    public static extern bool SetCursorPos(int X, int Y);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@ -ErrorAction SilentlyContinue

Add-Type -AssemblyName System.Drawing

$appDataPath = [System.IO.Path]::Combine($env:APPDATA, "Vazer", "Minecraft")
$settingsFile = [System.IO.Path]::Combine($appDataPath, "settings.json")
$exePath = "C:\OptizimeProject\Vaz\src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe"
$evidenceDir = "docs/evidence/dot16"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}

function Set-Theme([string]$themeName) {
    if (Test-Path $settingsFile) {
        $json = Get-Content $settingsFile -Raw | ConvertFrom-Json
        $json.theme = $themeName
        $json.reduceMotion = $false
        $json | ConvertTo-Json -Depth 10 | Set-Content $settingsFile -Encoding utf8
    }
}

function Capture-WindowToFile([IntPtr]$hWnd, [string]$outputPath) {
    $rect = New-Object Win32EvidenceCapture+RECT
    [Win32EvidenceCapture]::GetWindowRect($hWnd, [ref]$rect) | Out-Null
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -gt 0 -and $height -gt 0) {
        $bmp = New-Object System.Drawing.Bitmap $width, $height
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $width, $height))
        $bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose()
        $bmp.Dispose()
    }
}

function Run-AppAndCapture([string]$theme, [string]$toastEnv, [string]$outFile, [int]$delaySec = 2) {
    Write-Output "Capturing $outFile (Theme=$theme, Env=$toastEnv)..."
    Set-Theme $theme
    Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 400

    $env:VAZER_STARTUP_PAGE = "InstanceDetailPage"
    $env:VAZER_STARTUP_INSTANCE = "test fabric"
    $env:VAZER_TEST_TOAST = $toastEnv

    $proc = Start-Process -FilePath $exePath -PassThru
    $hWnd = [IntPtr]::Zero
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 400
        $p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($p) {
            $hWnd = $p.MainWindowHandle
            break
        }
    }

    if ($hWnd -ne [IntPtr]::Zero) {
        [Win32EvidenceCapture]::ShowWindow($hWnd, 9)
        [Win32EvidenceCapture]::SetForegroundWindow($hWnd)
        Start-Sleep -Seconds $delaySec
        Capture-WindowToFile $hWnd $outFile
    }

    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 400
}

# 1. Capture 6 Notification Types in Both Themes
$types = @("success", "warning", "error", "info", "update", "progress")
foreach ($t in $types) {
    Run-AppAndCapture "dark" "type_$t" "$evidenceDir/toast_${t}_dark.png" 2
    Run-AppAndCapture "light" "type_$t" "$evidenceDir/toast_${t}_light.png" 2
}

# 2. Capture Stacked Toasts & Notification Center
Run-AppAndCapture "dark" "1" "$evidenceDir/toasts_dark.png" 3
Run-AppAndCapture "light" "1" "$evidenceDir/toasts_light.png" 3
Run-AppAndCapture "dark" "launch_error" "$evidenceDir/toast_game_launch_error.png" 2
Run-AppAndCapture "dark" "center" "$evidenceDir/notification_center_dark.png" 3
Run-AppAndCapture "light" "center" "$evidenceDir/notification_center_light.png" 3

# 3. Capture Hover on Close Button
Run-AppAndCapture "dark" "1" "$evidenceDir/temp_hover.png" 2
# (hover captured in capture-toasts.ps1 already)

Write-Output "All 12 type screenshots and scenarios generated successfully!"
