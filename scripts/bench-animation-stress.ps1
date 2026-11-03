param(
    [int]$Cycles = 10
)

# 1. Kill existing
Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

# 2. Launch app
$env:VAZER_STARTUP_PAGE = "InstanceDetailPage"
$env:VAZER_STARTUP_INSTANCE = "test fabric"
$env:VAZER_STARTUP_TAB = "resourcepacks"

$exePath = "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe"
$proc = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds 2

# Pin P/Invoke for window foreground & input
$csharp = @"
using System;
using System.Runtime.InteropServices;

public class WinInput {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const byte VK_CONTROL = 0x11;
    public const byte VK_TAB = 0x09;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    public static void SendCtrlTab() {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_TAB, 0, 0, UIntPtr.Zero);
        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
"@
Add-Type -TypeDefinition $csharp -ErrorAction SilentlyContinue

[WinInput]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 300

$samples = [System.Collections.Generic.List[PSCustomObject]]::new()
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$cpuCores = [Environment]::ProcessorCount
$lastCpuTime = $proc.TotalProcessorTime
$lastWallTime = $stopwatch.Elapsed

for ($i = 0; $i -lt $Cycles; $i++) {
    # Send Ctrl+Tab to cycle tabs and trigger animations
    [WinInput]::SendCtrlTab()

    for ($sub = 0; $sub -lt 4; $sub++) {
        Start-Sleep -Milliseconds 250
        $proc.Refresh()
        if ($proc.HasExited) { break }

        $nowCpuTime = $proc.TotalProcessorTime
        $nowWallTime = $stopwatch.Elapsed
        $cpuUsedMs = ($nowCpuTime - $lastCpuTime).TotalMilliseconds
        $wallPassedMs = ($nowWallTime - $lastWallTime).TotalMilliseconds
        $cpuPercent = 0.0
        if ($wallPassedMs -gt 0) {
            $cpuPercent = [Math]::Round(($cpuUsedMs / ($wallPassedMs * $cpuCores)) * 100, 2)
        }
        $lastCpuTime = $nowCpuTime
        $lastWallTime = $nowWallTime

        $samples.Add([PSCustomObject]@{
            ElapsedSec = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
            CpuPercent = $cpuPercent
            WorkingSetMB = [Math]::Round($proc.WorkingSet64 / 1MB, 2)
            PrivateMB = [Math]::Round($proc.PrivateMemorySize64 / 1MB, 2)
        })
    }
}

if (-not $proc.HasExited) {
    $proc.Kill()
}

$cpuValues = $samples | ForEach-Object { $_.CpuPercent }
$wsValues = $samples | ForEach-Object { $_.WorkingSetMB }
$privValues = $samples | ForEach-Object { $_.PrivateMB }

$avgCpu = [Math]::Round(($cpuValues | Measure-Object -Average).Average, 2)
$maxCpu = [Math]::Round(($cpuValues | Measure-Object -Maximum).Maximum, 2)
$startRam = $wsValues[0]
$peakRam = [Math]::Round(($wsValues | Measure-Object -Maximum).Maximum, 2)
$endRam = $wsValues[-1]
$deltaRam = [Math]::Round($endRam - $startRam, 2)
$peakPrivate = [Math]::Round(($privValues | Measure-Object -Maximum).Maximum, 2)

[PSCustomObject]@{
    TestType = "Active Animation Stress ($Cycles tab switch cycles)"
    AvgCpu = "$avgCpu %"
    PeakCpu = "$maxCpu %"
    StartWorkingSet = "$startRam MB"
    PeakWorkingSet = "$peakRam MB"
    EndWorkingSet = "$endRam MB"
    DeltaWorkingSet = "$deltaRam MB"
    PeakPrivateMemory = "$peakPrivate MB"
    SampleCount = $samples.Count
} | Format-List
