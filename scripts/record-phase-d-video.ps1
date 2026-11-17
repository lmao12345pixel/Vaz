# Record 30s video for Phase D: Settings -> Versions Tab -> Delete orphan version -> Confirm -> Verify
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path
$outVidPath = "docs/evidence/dot18/videos/version-management-delete-orphan.mp4"
$videoDir = Split-Path $outVidPath -Parent
if (-not (Test-Path $videoDir)) { New-Item -ItemType Directory -Path $videoDir -Force | Out-Null }

# Setup dummy orphan version 1.18.2-orphan-demo (10 MB)
$vdir = "$env:APPDATA\Vazer\Minecraft\instances\versions\1.18.2-orphan-demo"
New-Item -ItemType Directory -Path $vdir -Force | Out-Null
Set-Content (Join-Path $vdir "1.18.2-orphan-demo.json") '{"id":"1.18.2-orphan-demo","type":"release"}' -Encoding utf8
$dummy = New-Object byte[] (10 * 1024 * 1024)
[System.IO.File]::WriteAllBytes((Join-Path $vdir "1.18.2-orphan-demo.jar"), $dummy)

# Ensure dark theme
if (Test-Path $settingsPath) {
    $j = Get-Content $settingsPath -Raw | ConvertFrom-Json
    $j.theme = "dark"
    $j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8
}

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

$tempFramesDir = Join-Path ([System.IO.Path]::GetTempPath()) ("vaz_ver_vid_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempFramesDir -Force | Out-Null

$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50) | Out-Null
Start-Sleep -Milliseconds 500

$script:frameIndex = 0
$w = 1100
$h = 670
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)

function Snap([int]$count = 1, [int]$delayMs = 100) {
    for ($k = 0; $k -lt $count; $k++) {
        $gfx.CopyFromScreen(0, 30, 0, 0, (New-Object System.Drawing.Size($w, $h)))
        $filePath = Join-Path $tempFramesDir ("frame_{0:D4}.png" -f $script:frameIndex)
        $bmp.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)
        $script:frameIndex++
        if ($delayMs -gt 0) { Start-Sleep -Milliseconds $delayMs }
    }
}

Write-Output "Recording started..."
# 1. Main Instances page
Snap 10 100

# 2. Click Settings on left sidebar
$root = Get-VazRoot
$settingsItem = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.Name -eq "Settings" } | Select-Object -First 1
$r = $settingsItem.Current.BoundingRectangle
[VazMouse]::ClickHwnd($hWnd, [int]($r.X + 60), [int]($r.Y + 18))
Snap 12 100

# 3. Settings page loaded -> Click "Phiên bản" tab
$root = Get-VazRoot
$verChip = @(Get-UiaDescendants -Element $root) | Where-Object {
    $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::RadioButton -and
    $_.Current.Name -eq "Phiên bản"
} | Select-Object -First 1
$vr = $verChip.Current.BoundingRectangle
[VazMouse]::ClickHwnd($hWnd, [int]($vr.X + 30), [int]($vr.Y + 12))
Snap 15 100

# 4. Versions tab loaded -> Find and click "Xóa" on 1.18.2-orphan-demo
$root = Get-VazRoot
$delBtns = @(Get-UiaDescendants -Element $root) | Where-Object {
    $_.Current.AutomationId -eq "DeleteVersionButton" -and $_.Current.IsEnabled -eq $true
}
Write-Output "Found $($delBtns.Count) enabled delete buttons"
if ($delBtns.Count -gt 0) {
    $targetDel = $delBtns[0]
    $dr = $targetDel.Current.BoundingRectangle
    [VazMouse]::ClickHwnd($hWnd, [int]($dr.X + 25), [int]($dr.Y + 15))
    Snap 15 100

    # 5. Confirm dialog opened -> Click 'Xóa' (PrimaryButton)
    $confirmDel = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object {
        $_.Current.AutomationId -eq "PrimaryButton" -or ($_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Button -and $_.Current.Name -eq "Xóa")
    } | Select-Object -First 1

    if ($confirmDel) {
        $cdr = $confirmDel.Current.BoundingRectangle
        [VazMouse]::ClickHwnd($hWnd, [int]($cdr.X + 25), [int]($cdr.Y + 15))
        Snap 20 100
    }
}

# 6. Click 'Làm mới'
$root = Get-VazRoot
$refreshBtn = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.AutomationId -eq "RefreshButton" } | Select-Object -First 1
if ($refreshBtn) {
    $rr = $refreshBtn.Current.BoundingRectangle
    [VazMouse]::ClickHwnd($hWnd, [int]($rr.X + 35), [int]($rr.Y + 15))
    Snap 15 100
}

# Final hold
Snap 10 100

Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
$gfx.Dispose(); $bmp.Dispose()

Write-Output "Frames captured: $script:frameIndex"
$inputPattern = Join-Path $tempFramesDir "frame_%04d.png"
if (Test-Path $outVidPath) { Remove-Item $outVidPath -Force }

# Target duration ~30s: $fps = $script:frameIndex / 30.0
$fps = [math]::Max(2, [math]::Round($script:frameIndex / 30.0, 2))
Write-Output "Compiling video at $fps fps..."
& ffmpeg -y -framerate $fps -i $inputPattern -c:v libx264 -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" $outVidPath 2>&1 | Select-Object -Last 2

Remove-Item $tempFramesDir -Recurse -Force
Write-Output "DONE: $outVidPath"
& ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1 $outVidPath
