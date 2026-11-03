Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class DpiCaptureDriver {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
}
'@ -ErrorAction SilentlyContinue

function Capture-Target($tab, $w, $h, $outPath) {
    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
    $env:VAZER_STARTUP_PAGE = "InstanceDetailPage"
    $env:VAZER_STARTUP_TAB = $tab

    $proc = Start-Process "src/Vaz.WinUI/bin/x64/Debug/net8.0-windows10.0.26100.0/win-x64/Vaz.WinUI.exe" -PassThru
    Start-Sleep -Seconds 3

    $p = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) {
        [DpiCaptureDriver]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
        [DpiCaptureDriver]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
        [DpiCaptureDriver]::MoveWindow($p.MainWindowHandle, 0, 0, $w, $h, $true) | Out-Null
        Start-Sleep -Milliseconds 800

        powershell -NoProfile -ExecutionPolicy Bypass -File scripts/capture-window.ps1 -OutputPath $outPath
        Write-Host "SAVED: $outPath"
    }
    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
}

Capture-Target "resourcepacks" 960 530 "docs/evidence/RPS-AC-08-layout-rp-100scale-960x530.png"
Capture-Target "shaders" 960 530 "docs/evidence/RPS-AC-08-layout-shaders-100scale-960x530.png"
Capture-Target "resourcepacks" 1025 533 "docs/evidence/RPS-AC-08-layout-rp-100scale-1025x533.png"
