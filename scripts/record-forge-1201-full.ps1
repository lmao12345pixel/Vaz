param(
    [string]$OutVideo = "docs/evidence/dot18/videos/wizard-forge-1.20.1-create.mp4"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$videoDir = Split-Path $OutVideo -Parent
if ($videoDir -and -not (Test-Path $videoDir)) {
    New-Item -ItemType Directory -Path $videoDir -Force | Out-Null
}

$tempFramesDir = Join-Path ([System.IO.Path]::GetTempPath()) ("vaz_forge_create_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempFramesDir -Force | Out-Null

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path

# Set dark theme
$j = Get-Content $settingsPath -Raw | ConvertFrom-Json
$j.theme = "dark"
$j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8

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
    Snap 15 100

    # 2. Click "Tạo Instance mới" button
    $btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
    Invoke-UiaClick -Element $btn
    Snap 20 100

    # 3. Search 1.20.1 in Step 1
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $sb = $all | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
    if ($sb) {
        Click-MouseAt -X ([int]($sb.Current.BoundingRectangle.X + 30)) -Y ([int]($sb.Current.BoundingRectangle.Y + 18)) -HWnd $hWnd
        Snap 4 100
        [System.Windows.Forms.SendKeys]::SendWait("1.20.1")
        Snap 18 100
    }

    # 4. Select Card 1.20.1
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $cb = $all | Where-Object { $_.Current.AutomationId -eq 'CardButton' } | Select-Object -First 1
    if ($cb) {
        [VazMouse]::ClickHwnd($hWnd, [int]($cb.Current.BoundingRectangle.X + 80), [int]($cb.Current.BoundingRectangle.Y + 80))
        Snap 15 100
    }

    # 5. Click Step 1 Next -> Step 2
    $s1Next = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($s1Next) {
        [VazMouse]::ClickHwnd($hWnd, [int]($s1Next.Current.BoundingRectangle.X + 50), [int]($s1Next.Current.BoundingRectangle.Y + 18))
        Snap 20 100
    }

    # 6. In Step 2: Click Forge button
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $forgeb = $all | Where-Object { $_.Current.AutomationId -eq 'ForgeButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($forgeb) {
        [VazMouse]::ClickHwnd($hWnd, [int]($forgeb.Current.BoundingRectangle.X + 37), [int]($forgeb.Current.BoundingRectangle.Y + 66))
        Snap 25 100
    }

    # 7. Click Step 2 Next -> Step 3
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $s2Next = $all | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($s2Next) {
        [VazMouse]::ClickHwnd($hWnd, [int]($s2Next.Current.BoundingRectangle.X + 55), [int]($s2Next.Current.BoundingRectangle.Y + 18))
        Snap 20 100
    }

    # 8. In Step 3: Showcase preview card
    Snap 15 100

    # 9. In Step 3: Click "Tạo Instance"
    $all = @(Get-UiaDescendants -Element (Get-VazRoot))
    $s3Create = $all | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($s3Create) {
        [VazMouse]::ClickHwnd($hWnd, [int]($s3Create.Current.BoundingRectangle.X + 85), [int]($s3Create.Current.BoundingRectangle.Y + 19))
        
        # Installer runs in background (~45-55s total)
        # We sample frames during install
        for ($s = 0; $s -lt 55; $s++) {
            Snap 2 500
            $procCheck = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue
            if (-not $procCheck) { break }
        }
    }

    # 10. Final frames (main window with new instance)
    Snap 20 100

} finally {
    $gfx.Dispose()
    $bmp.Dispose()
    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
}

# Compile with ffmpeg at 7 fps
Write-Host "Compiling $($script:frameIndex) frames into $OutVideo..."
$inputPattern = Join-Path $tempFramesDir "frame_%04d.png"
if (Test-Path $OutVideo) { Remove-Item $OutVideo -Force }
& ffmpeg -y -framerate 6 -i $inputPattern -c:v libx264 -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" $OutVideo 2>&1 | Select-Object -Last 3
Remove-Item $tempFramesDir -Recurse -Force
Write-Host "DONE: $OutVideo ($((Get-Item $OutVideo).Length) bytes, approx $($script:frameIndex / 6)s)"
