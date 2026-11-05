# Records high-fidelity 30s MP4 video demonstrating Wizard Step 1 + Step 2 with all 8 animations.
# Output: docs/evidence/dot18/videos/wizard-step1-2.mp4
param(
    [string]$OutFile = "docs/evidence/dot18/videos/wizard-step1-2.mp4"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$videoDir = Split-Path $OutFile -Parent
if ($videoDir -and -not (Test-Path $videoDir)) {
    New-Item -ItemType Directory -Path $videoDir -Force | Out-Null
}

$tempFramesDir = Join-Path ([System.IO.Path]::GetTempPath()) ("vaz_wiz_rec_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempFramesDir -Force | Out-Null

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path
$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6

$hWnd = $proc.MainWindowHandle
$root = Get-VazRoot

# Ensure window at (0, 30) for screen bounds
[VazMouse]::ClickHwnd($hWnd, 50, 50)
Start-Sleep -Milliseconds 300

$rect = New-Object System.Drawing.Rectangle
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
    # 1. Main window initial state (10 frames = 1s)
    Snap 10 100

    # 2. Click "Tạo Instance mới" button
    $btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
    if ($btn) { Invoke-UiaClick -Element $btn }
    # Step 1 entrance animation (#1)
    Snap 20 100

    # 3. Hover over cards (#2 Hover Lift)
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $cardBtns = @($all | Where-Object { $_.Current.AutomationId -eq 'CardButton' })
    if ($cardBtns.Count -ge 3) {
        $r0 = $cardBtns[0].Current.BoundingRectangle
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($r0.X + 80), [int]($r0.Y + 80))
        Snap 8 100

        $r1 = $cardBtns[1].Current.BoundingRectangle
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($r1.X + 80), [int]($r1.Y + 80))
        Snap 8 100

        $r2 = $cardBtns[2].Current.BoundingRectangle
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($r2.X + 80), [int]($r2.Y + 80))
        Snap 8 100
    }

    # 4. Search focus + typing "1.21" (#6 search focus glow, #7 search filter)
    $sb = $all | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
    if ($sb) {
        $sbr = $sb.Current.BoundingRectangle
        Click-MouseAt -X ([int]($sbr.X + 30)) -Y ([int]($sbr.Y + 18)) -HWnd $hWnd
        Snap 4 100
        [System.Windows.Forms.SendKeys]::SendWait("1")
        Snap 4 100
        [System.Windows.Forms.SendKeys]::SendWait(".")
        Snap 4 100
        [System.Windows.Forms.SendKeys]::SendWait("2")
        Snap 4 100
        [System.Windows.Forms.SendKeys]::SendWait("1")
        Snap 12 100

        # Clear search
        [System.Windows.Forms.SendKeys]::SendWait("^{A}{BACKSPACE}")
        Snap 10 100
    }

    # 5. Select Card 26.3 (#3 Card selected checkmark bounce, #4 click)
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $cb0 = $all | Where-Object { $_.Current.AutomationId -eq 'CardButton' } | Select-Object -First 1
    if ($cb0) {
        $cr = $cb0.Current.BoundingRectangle
        Click-MouseAt -X ([int]($cr.X + $cr.Width / 2)) -Y ([int]($cr.Y + $cr.Height / 2)) -HWnd $hWnd
        Snap 10 100
    }

    # 6. Click "Tiếp tục →" to switch to Step 2
    $nextBtn = Find-UiaByName -Root (Get-VazRoot) -NameContains 'Tiếp tục' -ControlType 'Button' | Select-Object -First 1
    if ($nextBtn) {
        $nr = $nextBtn.Current.BoundingRectangle
        Click-MouseAt -X ([int]($nr.X + $nr.Width / 2)) -Y ([int]($nr.Y + $nr.Height / 2)) -HWnd $hWnd
        # Step 2 entrance
        Snap 15 100
    }

    # 7. Step 2: Click Fabric (#5 logo spin, #8 dropdown slide)
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $fb = $all | Where-Object { $_.Current.AutomationId -eq 'FabricButton' } | Select-Object -First 1
    if ($fb) {
        $fr = $fb.Current.BoundingRectangle
        Click-MouseAt -X ([int]($fr.X + $fr.Width / 2)) -Y ([int]($fr.Y + $fr.Height / 2)) -HWnd $hWnd
        Snap 18 100
    }

    # 8. Step 2: Click Quilt
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $qb = $all | Where-Object { $_.Current.AutomationId -eq 'QuiltButton' } | Select-Object -First 1
    if ($qb) {
        $qr = $qb.Current.BoundingRectangle
        Click-MouseAt -X ([int]($qr.X + $qr.Width / 2)) -Y ([int]($qr.Y + $qr.Height / 2)) -HWnd $hWnd
        Snap 15 100
    }

    # 9. Step 2: Click NeoForge
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $nfb = $all | Where-Object { $_.Current.AutomationId -eq 'NeoForgeButton' } | Select-Object -First 1
    if ($nfb) {
        $nfr = $nfb.Current.BoundingRectangle
        Click-MouseAt -X ([int]($nfr.X + $nfr.Width / 2)) -Y ([int]($nfr.Y + $nfr.Height / 2)) -HWnd $hWnd
        Snap 15 100
    }

    # 10. Step 2: Click Vanilla
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $vb = $all | Where-Object { $_.Current.AutomationId -eq 'VanillaButton' } | Select-Object -First 1
    if ($vb) {
        $vr = $vb.Current.BoundingRectangle
        Click-MouseAt -X ([int]($vr.X + $vr.Width / 2)) -Y ([int]($vr.Y + $vr.Height / 2)) -HWnd $hWnd
        Snap 15 100
    }

    # 11. Click "← Quay lại"
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $bb = $all | Where-Object { $_.Current.AutomationId -eq 'BackButton' } | Select-Object -First 1
    if ($bb) {
        $bbr = $bb.Current.BoundingRectangle
        Click-MouseAt -X ([int]($bbr.X + $bbr.Width / 2)) -Y ([int]($bbr.Y + $bbr.Height / 2)) -HWnd $hWnd
        Snap 15 100
    }

    # 12. Final pause
    Snap 10 100

} finally {
    $gfx.Dispose()
    $bmp.Dispose()
    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
}

# Compile with ffmpeg
Write-Host "Compiling $($script:frameIndex) frames into $OutFile..."
$inputPattern = Join-Path $tempFramesDir "frame_%04d.png"
if (Test-Path $OutFile) { Remove-Item $OutFile -Force }

& ffmpeg -y -framerate 8 -i $inputPattern -c:v libx264 -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" $OutFile 2>&1 | Select-Object -Last 3

Remove-Item $tempFramesDir -Recurse -Force
Write-Host "DONE: $OutFile (size: $((Get-Item $OutFile).Length) bytes)"
