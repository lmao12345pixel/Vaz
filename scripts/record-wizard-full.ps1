param(
    [string]$OutVideo = "docs/evidence/dot18/videos/wizard-step3.mp4"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$videoDir = Split-Path $OutVideo -Parent
if ($videoDir -and -not (Test-Path $videoDir)) {
    New-Item -ItemType Directory -Path $videoDir -Force | Out-Null
}

$screenshotDir = "docs/evidence/dot18/screenshots"
if (-not (Test-Path $screenshotDir)) {
    New-Item -ItemType Directory -Path $screenshotDir -Force | Out-Null
}

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path

# ==========================================
# PART 1: Capture Step 3 in Light Theme
# ==========================================
Write-Host "Part 1: Capturing Step 3 in Light theme..."
$j = Get-Content $settingsPath -Raw | ConvertFrom-Json
$j.theme = "light"
$j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

$procLight = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
[VazMouse]::ClickHwnd($procLight.MainWindowHandle, 50, 50)
Start-Sleep -Milliseconds 300

# Open Dialog
$btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
if ($btn) { Invoke-UiaClick -Element $btn }
Start-Sleep -Seconds 4

# Step 1: Click 26.3
$cb = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CardButton' } | Select-Object -First 1
[VazMouse]::ClickHwnd($procLight.MainWindowHandle, [int]($cb.Current.BoundingRectangle.X + 80), [int]($cb.Current.BoundingRectangle.Y + 80))
Start-Sleep -Milliseconds 800

# Step 1 Next
$s1Next = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($procLight.MainWindowHandle, [int]($s1Next.Current.BoundingRectangle.X + 50), [int]($s1Next.Current.BoundingRectangle.Y + 18))
Start-Sleep -Seconds 2

# Step 2: Click Fabric
$fb = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'FabricButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($procLight.MainWindowHandle, [int]($fb.Current.BoundingRectangle.X + 37), [int]($fb.Current.BoundingRectangle.Y + 66))
Start-Sleep -Seconds 2

# Step 2 Next
$s2Next = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($procLight.MainWindowHandle, [int]($s2Next.Current.BoundingRectangle.X + 55), [int]($s2Next.Current.BoundingRectangle.Y + 18))
Start-Sleep -Seconds 2

# Save Step 3 Light
.\scripts\capture-winui.ps1 -OutputPath "$screenshotDir/wizard-step3-light.png"
Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600

# ==========================================
# PART 2: Video Recording in Dark Theme (~45s)
# ==========================================
Write-Host "Part 2: Recording ~45s full wizard video in Dark theme..."
$j = Get-Content $settingsPath -Raw | ConvertFrom-Json
$j.theme = "dark"
$j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8

$tempFramesDir = Join-Path ([System.IO.Path]::GetTempPath()) ("vaz_wiz_full_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempFramesDir -Force | Out-Null

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

function Snap([int]$count = 1, [int]$delayMs = 100, [string]$saveAs = "") {
    for ($k = 0; $k -lt $count; $k++) {
        $gfx.CopyFromScreen(0, 30, 0, 0, (New-Object System.Drawing.Size($w, $h)))
        $filePath = Join-Path $tempFramesDir ("frame_{0:D4}.png" -f $script:frameIndex)
        $bmp.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)
        if ($saveAs -and $k -eq 0) {
            $bmp.Save($saveAs, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        $script:frameIndex++
        if ($delayMs -gt 0) { Start-Sleep -Milliseconds $delayMs }
    }
}

try {
    # 1. Main window initial (20 frames)
    Snap 20 100

    # 2. Click "Tạo Instance mới" button
    $btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
    if ($btn) { Invoke-UiaClick -Element $btn }
    # Step 1 entrance (#1, #11)
    Snap 25 100

    # 3. Hover over cards (#2 Hover Lift)
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $cardBtns = @($all | Where-Object { $_.Current.AutomationId -eq 'CardButton' })
    if ($cardBtns.Count -ge 3) {
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($cardBtns[0].Current.BoundingRectangle.X + 80), [int]($cardBtns[0].Current.BoundingRectangle.Y + 80))
        Snap 10 100
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($cardBtns[1].Current.BoundingRectangle.X + 80), [int]($cardBtns[1].Current.BoundingRectangle.Y + 80))
        Snap 10 100
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($cardBtns[2].Current.BoundingRectangle.X + 80), [int]($cardBtns[2].Current.BoundingRectangle.Y + 80))
        Snap 10 100
    }

    # 4. Search focus + typing "1.21" (#6, #7)
    $sb = $all | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
    if ($sb) {
        Click-MouseAt -X ([int]($sb.Current.BoundingRectangle.X + 30)) -Y ([int]($sb.Current.BoundingRectangle.Y + 18)) -HWnd $hWnd
        Snap 5 100
        [System.Windows.Forms.SendKeys]::SendWait("1.21")
        Snap 25 100
        [System.Windows.Forms.SendKeys]::SendWait("^{A}{BACKSPACE}")
        Snap 15 100
    }

    # 5. Select Card 26.3 (#3, #4)
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $cb = $all | Where-Object { $_.Current.AutomationId -eq 'CardButton' } | Select-Object -First 1
    if ($cb) {
        [VazMouse]::ClickHwnd($hWnd, [int]($cb.Current.BoundingRectangle.X + 80), [int]($cb.Current.BoundingRectangle.Y + 80))
        Snap 20 100
    }

    # 6. Click Step 1 Next -> Step 2 (#1 forward, #3 indicator dot 2)
    $s1Next = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($s1Next) {
        [VazMouse]::ClickHwnd($hWnd, [int]($s1Next.Current.BoundingRectangle.X + 50), [int]($s1Next.Current.BoundingRectangle.Y + 18))
        Snap 28 100
    }

    # 7. In Step 2: Select Fabric (#5 logo spin, #8 dropdown slide)
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $fb = $all | Where-Object { $_.Current.AutomationId -eq 'FabricButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($fb) {
        [VazMouse]::ClickHwnd($hWnd, [int]($fb.Current.BoundingRectangle.X + 37), [int]($fb.Current.BoundingRectangle.Y + 66))
        Snap 30 100
    }

    # 8. Click Step 2 Next -> Step 3 (#1 forward, #3 indicator dot 3)
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $s2Next = $all | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($s2Next) {
        [VazMouse]::ClickHwnd($hWnd, [int]($s2Next.Current.BoundingRectangle.X + 55), [int]($s2Next.Current.BoundingRectangle.Y + 18))
        Snap 30 100
    }

    # 9. In Step 3: Idle to showcase preview card & pulse glow (#6 glow pulse)
    Snap 25 100

    # 10. Click "Tạo Instance" in Step 3 (#5 press, #7 morph, #8-9 progress/loading)
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $s3Create = $all | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($s3Create) {
        [VazMouse]::ClickHwnd($hWnd, [int]($s3Create.Current.BoundingRectangle.X + 85), [int]($s3Create.Current.BoundingRectangle.Y + 19))
        Snap 25 100
    }

    # 11. Success state & auto-close (#10 checkmark draw)
    Snap 25 100

    # 12. Main window with new instance
    Snap 20 100

} finally {
    $gfx.Dispose()
    $bmp.Dispose()
    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
}

# Compile with ffmpeg at 8 fps (300-360 frames = ~40-45s)
Write-Host "Compiling $($script:frameIndex) frames into $OutVideo..."
$inputPattern = Join-Path $tempFramesDir "frame_%04d.png"
if (Test-Path $OutVideo) { Remove-Item $OutVideo -Force }

& ffmpeg -y -framerate 7 -i $inputPattern -c:v libx264 -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" $OutVideo 2>&1 | Select-Object -Last 3

Remove-Item $tempFramesDir -Recurse -Force
Write-Host "DONE: $OutVideo ($((Get-Item $OutVideo).Length) bytes, approx $($script:frameIndex / 7)s)"
