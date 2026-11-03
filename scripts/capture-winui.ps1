param(
    [string]$OutputPath = "docs/evidence/dot15/screenshot.png"
)

Add-Type @"
using System;
using System.Runtime.InteropServices;

public class Win32 {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@

Add-Type -AssemblyName System.Drawing

$p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) {
    Write-Error "Vaz.WinUI process not found or has no MainWindowHandle"
    exit 1
}

$hWnd = $p.MainWindowHandle
[Win32]::ShowWindow($hWnd, 9) # SW_RESTORE
[Win32]::SetForegroundWindow($hWnd)
Start-Sleep -Milliseconds 300

$rect = New-Object Win32+RECT
[Win32]::GetWindowRect($hWnd, [ref]$rect) | Out-Null

$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top

if ($width -le 0 -or $height -le 0) {
    Write-Error "Invalid window size: ${width}x${height}"
    exit 1
}

$bmp = New-Object System.Drawing.Bitmap $width, $height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $width, $height))

$outDir = [System.IO.Path]::GetDirectoryName($OutputPath)
if (-not [string]::IsNullOrEmpty($outDir) -and -not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

$bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()

Write-Output "Screenshot captured: $OutputPath (${width}x${height})"
