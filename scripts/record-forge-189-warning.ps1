param(
    [string]$OutImage = "docs/evidence/dot18/screenshots/wizard-forge-1.8.9-warning.png",
    [string]$OutVideo = "docs/evidence/dot18/videos/wizard-forge-1.8.9-warning.mp4"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$videoDir = Split-Path $OutVideo -Parent
if ($videoDir -and -not (Test-Path $videoDir)) {
    New-Item -ItemType Directory -Path $videoDir -Force | Out-Null
}

$tempFramesDir = Join-Path ([System.IO.Path]::GetTempPath()) ("vaz_forge_189_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempFramesDir -Force | Out-Null

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50)
Start-Sleep -Milliseconds 300

$w = 1100
$h = 700
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$frameIndex = 0

function Snap([int]$count = 1, [int]$delayMs = 100) {
    for ($k = 0; $k -lt $count; $k++) {
        $gfx.CopyFromScreen(0, 30, 0, 0, (New-Object System.Drawing.Size($w, $h)))
        $filePath = Join-Path $tempFramesDir ("frame_{0:D4}.png" -f $script:frameIndex)
        $bmp.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)
        $script:frameIndex++
        if ($delayMs -gt 0) { Start-Sleep -Milliseconds $delayMs }
    }
}

try {
    # 1. Main window initial
    Snap 10 100

    # 2. Click "Tạo Instance mới" button
    $btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
    Invoke-UiaClick -Element $btn
    Snap 15 100

    # 3. Search 1.8.9 in Step 1
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $sb = $all | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
    if ($sb) {
        Click-MouseAt -X ([int]($sb.Current.BoundingRectangle.X + 30)) -Y ([int]($sb.Current.BoundingRectangle.Y + 18)) -HWnd $hWnd
        Snap 5 100
        [System.Windows.Forms.SendKeys]::SendWait("1.8.9")
        Snap 15 100
    }

    # 4. Select Card 1.8.9
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $cb = $all | Where-Object { $_.Current.AutomationId -eq 'CardButton' } | Select-Object -First 1
    if ($cb) {
        [VazMouse]::ClickHwnd($hWnd, [int]($cb.Current.BoundingRectangle.X + 80), [int]($cb.Current.BoundingRectangle.Y + 80))
        Snap 10 100
    }

    # 5. Click Step 1 Next
    $s1Next = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($s1Next) {
        [VazMouse]::ClickHwnd($hWnd, [int]($s1Next.Current.BoundingRectangle.X + 50), [int]($s1Next.Current.BoundingRectangle.Y + 18))
        Snap 15 100
    }

    # 6. In Step 2: Click Forge button
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $forgeb = $all | Where-Object { $_.Current.AutomationId -eq 'ForgeButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($forgeb) {
        [VazMouse]::ClickHwnd($hWnd, [int]($forgeb.Current.BoundingRectangle.X + 37), [int]($forgeb.Current.BoundingRectangle.Y + 66))
        Snap 20 100
    }

    # 7. Check warning and disabled button, hold for video
    Snap 30 100

    # Save screenshot of warning
    .\scripts\capture-winui.ps1 -OutputPath $OutImage

} finally {
    $gfx.Dispose()
    $bmp.Dispose()
    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
}

# Compile with ffmpeg at 7 fps (~15-20s video)
$inputPattern = Join-Path $tempFramesDir "frame_%04d.png"
if (Test-Path $OutVideo) { Remove-Item $OutVideo -Force }
& ffmpeg -y -framerate 7 -i $inputPattern -c:v libx264 -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" $OutVideo 2>&1 | Select-Object -Last 3
Remove-Item $tempFramesDir -Recurse -Force
Write-Host "DONE: $OutVideo and $OutImage"
