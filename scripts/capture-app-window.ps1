# Resizes the V.A.Z window to WxH at (0,0) and saves a screen-crop PNG of exactly that size.
param(
  [int]$W = 1025,
  [int]$H = 533,
  [Parameter(Mandatory = $true)][string]$OutPath
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32 {
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$proc = Get-Process -Name 'Vaz.WinUI' -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $proc) { throw 'Vaz.WinUI window not found' }
$hwnd = $proc.MainWindowHandle

[Win32]::ShowWindow($hwnd, 9) | Out-Null # SW_RESTORE
Start-Sleep -Milliseconds 400
[Win32]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 400
# SWP_NOZORDER=0x4, SWP_SHOWWINDOW=0x40
[Win32]::SetWindowPos($hwnd, [IntPtr]::Zero, 0, 0, $W, $H, 0x44) | Out-Null
Start-Sleep -Milliseconds 700
[Win32]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 300

$rect = New-Object Win32+RECT
[Win32]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
$vw = $rect.Right - $rect.Left
$vh = $rect.Bottom - $rect.Top

$bmp = New-Object System.Drawing.Bitmap($vw, $vh)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($vw, $vh)))
$g.Dispose()

$outDir = Split-Path $OutPath -Parent
if ($outDir -and -not (Test-Path $outDir)) { New-Item -ItemType Directory -Force $outDir | Out-Null }
$bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output ("SAVED {0} {1}x{2} rect=({3},{4},{5},{6})" -f $OutPath, $vw, $vh, $rect.Left, $rect.Top, $rect.Right, $rect.Bottom)
