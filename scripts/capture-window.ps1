param(
    [string]$OutputPath = "docs/evidence/current-window.png"
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WinSnap {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
"@ -ErrorAction SilentlyContinue

$p = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($p) {
    [WinSnap]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 200

    $rect = New-Object WinSnap+RECT
    [WinSnap]::GetWindowRect($p.MainWindowHandle, [ref]$rect)
    $w = [Math]::Max(1, $rect.Right - $rect.Left)
    $h = [Math]::Max(1, $rect.Bottom - $rect.Top)
    
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
    
    $dir = [System.IO.Path]::GetDirectoryName($OutputPath)
    if (-not [string]::IsNullOrEmpty($dir) -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    $bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $gfx.Dispose()
    $bmp.Dispose()
    Write-Host "Captured window: ${w}x${h} to $OutputPath"
}
