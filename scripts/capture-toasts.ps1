# Capture Toast Notification Evidence (Dark Mode, Light Mode, Launch Error, Stacked Toasts)
$ErrorActionPreference = "Stop"

Add-Type @"
using System;
using System.Runtime.InteropServices;

public class Win32ToastCapture {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

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
        Write-Output "Settings updated: theme=$themeName, reduceMotion=false"
    }
}

function Capture-ToastScenario([string]$theme, [string]$toastEnv, [string]$outFile, [int]$delaySec = 3) {
    Write-Output "--- Capturing Toast Scenario: Theme=$theme | VAZER_TEST_TOAST=$toastEnv -> $outFile ---"
    Set-Theme $theme
    
    Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 500

    $env:VAZER_STARTUP_PAGE = "InstanceDetailPage"
    $env:VAZER_STARTUP_INSTANCE = "test fabric"
    $env:VAZER_TEST_TOAST = $toastEnv

    $proc = Start-Process -FilePath $exePath -PassThru
    
    $timeout = 10
    $elapsed = 0
    while ($elapsed -lt $timeout) {
        Start-Sleep -Milliseconds 500
        $elapsed += 0.5
        $p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($p) {
            break
        }
    }
    
    # Wait for toasts to slide in
    Start-Sleep -Seconds $delaySec

    powershell -ExecutionPolicy Bypass -File "scripts/capture-winui.ps1" -OutputPath $outFile

    if ($toastEnv -eq "1" -and $theme -eq "dark") {
        # Also capture hover on bottom toast showing close X button
        $rect = New-Object Win32ToastCapture+RECT
        $handle = (Get-Process Vaz.WinUI | Select-Object -First 1).MainWindowHandle
        [Win32ToastCapture]::GetWindowRect($handle, [ref]$rect) | Out-Null
        $hoverX = $rect.Right - 150
        $hoverY = $rect.Bottom - 80
        [Win32ToastCapture]::SetCursorPos($hoverX, $hoverY)
        Start-Sleep -Milliseconds 400
        powershell -ExecutionPolicy Bypass -File "scripts/capture-winui.ps1" -OutputPath "$evidenceDir/toast_hover_close_button.png"
        Write-Output "Captured hover close button!"
    }

    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
}

# 1. Dark Mode: Stack of 4 toasts (Info, Warning, Error, Success)
Capture-ToastScenario "dark" "1" "$evidenceDir/toasts_dark.png" 4

# 2. Light Mode: Stack of 4 toasts (Info, Warning, Error, Success)
Capture-ToastScenario "light" "1" "$evidenceDir/toasts_light.png" 4

# 3. Game Launch Error: Toast replacing old modal MessageDialog
Capture-ToastScenario "dark" "launch_error" "$evidenceDir/toast_game_launch_error.png" 2

# 4. Notification Center (Panel) in Dark Mode
Capture-ToastScenario "dark" "center" "$evidenceDir/notification_center_dark.png" 3

# 5. Notification Center (Panel) in Light Mode
Capture-ToastScenario "light" "center" "$evidenceDir/notification_center_light.png" 3

# Restore Dark
Set-Theme "dark"
Write-Output "All Toast & Notification Center screenshots captured successfully in $evidenceDir!"
