Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class WinMouse {
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
    }
}
'@

$sw = [System.Diagnostics.Stopwatch]::StartNew()
[WinMouse]::Click(861, 216)
Write-Host "Clicked card at 861,216"

# Wait for InstanceDetailPage to load
Start-Sleep -Milliseconds 2500
$sw.Stop()
Write-Host "Elapsed time: $($sw.ElapsedMilliseconds) ms"

& 'scripts/capture-app-window.ps1' -W 1025 -H 533 -OutPath "docs/evidence/LD-03-load-500-mods-1025x533.png"
& 'scripts/capture-app-window.ps1' -W 960 -H 530 -OutPath "docs/evidence/LD-03-load-500-mods-960x530.png"
