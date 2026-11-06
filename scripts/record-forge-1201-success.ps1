param(
    [string]$OutVideo = "docs/evidence/dot18/videos/wizard-forge-1.20.1-create.mp4",
    [string]$OutStep3 = "docs/evidence/dot18/screenshots/wizard-step3-forge.png",
    [string]$OutSuccess = "docs/evidence/dot18/screenshots/wizard-forge-success.png"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$videoDir = Split-Path $OutVideo -Parent
if ($videoDir -and -not (Test-Path $videoDir)) { New-Item -ItemType Directory -Path $videoDir -Force | Out-Null }
$shotDir = Split-Path $OutStep3 -Parent
if ($shotDir -and -not (Test-Path $shotDir)) { New-Item -ItemType Directory -Path $shotDir -Force | Out-Null }

$tempFramesDir = Join-Path ([System.IO.Path]::GetTempPath()) ("vaz_forge_ok_" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempFramesDir -Force | Out-Null

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path

# dark theme
$j = Get-Content $settingsPath -Raw | ConvertFrom-Json
$j.theme = "dark"
$j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50) | Out-Null
Start-Sleep -Milliseconds 300

$w = 1100
$h = 670
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$script:frameIndex = 0

function Snap([int]$count = 1, [int]$delayMs = 100) {
    for ($k = 0; $k -lt $count; $k++) {
        $gfx.CopyFromScreen(0, 30, 0, 0, (New-Object System.Drawing.Size($w, $h)))
        $filePath = Join-Path $tempFramesDir ("frame_{0:D4}.png" -f $script:frameIndex)
        $bmp.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)
        $script:frameIndex++
        if ($delayMs -gt 0) { Start-Sleep -Milliseconds $delayMs }
    }
}

function SnapWhile{
    param([scriptblock]$Condition, [int]$MaxWaitSec, [int]$PollMs = 1000)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $MaxWaitSec) {
        Snap 1 0
        Start-Sleep -Milliseconds $PollMs
        if (& $Condition) { return $true }
    }
    return $false
}

function Find-Step2CreateButton {
    @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object {
        $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false
    } | Select-Object -First 1
}

$succeeded = $false
try {
    # 1. main window
    Snap 10 100

    # 2. open dialog
    $btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
    if (-not $btn) { throw "Create Instance button not found" }
    Invoke-UiaClick -Element $btn
    Snap 12 100

    # 3. search 1.20.1
    $root = Get-VazRoot
    $sb = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
    if ($sb) {
        Click-MouseAt -X ([int]($sb.Current.BoundingRectangle.X + 30)) -Y ([int]($sb.Current.BoundingRectangle.Y + 18)) -HWnd $hWnd
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait("1.20.1")
        Snap 10 100
    }

    # 4. select first card
    $root = Get-VazRoot
    $cb = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.AutomationId -eq 'CardButton' } | Select-Object -First 1
    if ($cb) {
        [VazMouse]::ClickHwnd($hWnd, [int]($cb.Current.BoundingRectangle.X + 80), [int]($cb.Current.BoundingRectangle.Y + 80)) | Out-Null
        Snap 10 100
    }

    # 5. step1 next -> step2
    $b = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($b) {
        [VazMouse]::ClickHwnd($hWnd, [int]($b.Current.BoundingRectangle.X + 50), [int]($b.Current.BoundingRectangle.Y + 18)) | Out-Null
        Snap 12 100
    }

    # 6. step2: click Forge, wait for version combo to populate
    $forgeb = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ForgeButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if ($forgeb) {
        [VazMouse]::ClickHwnd($hWnd, [int]($forgeb.Current.BoundingRectangle.X + 37), [int]($forgeb.Current.BoundingRectangle.Y + 50)) | Out-Null
    } else { throw "ForgeButton not found on Step 2" }

    $versionReady = SnapWhile -Condition {
        @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object {
            $_.Current.AutomationId -eq 'PreviewLoader' -and $_.Current.Name -like 'Forge 1.20.1-*'
        } | Select-Object -First 1
    } -MaxWaitSec 90 -PollMs 1000
    if (-not $versionReady) { throw "Forge preview/loader version did not populate within 90s" }
    Snap 8 100

    # 7. step2 next -> step3
    $b2 = Find-Step2CreateButton
    if ($b2) {
        [VazMouse]::ClickHwnd($hWnd, [int]($b2.Current.BoundingRectangle.X + 55), [int]($b2.Current.BoundingRectangle.Y + 18)) | Out-Null
        Snap 12 100
    }

    # 8. step3: verify and screenshot; also verify default name is "Forge ..."
    $nameBox = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'NameBox' } | Select-Object -First 1
    if ($nameBox) { Write-Output "DefaultName: $($nameBox.Current.Name)" }
    .\scripts\capture-winui.ps1 -OutputPath $OutStep3
    Snap 10 100

    # 9. click "Tạo Instance"
    $s3 = Find-Step2CreateButton
    if ($s3) {
        [VazMouse]::ClickHwnd($hWnd, [int]($s3.Current.BoundingRectangle.X + 85), [int]($s3.Current.BoundingRectangle.Y + 19)) | Out-Null
        Write-Output "Clicked Step 3 Tạo Instance at $($s3.Current.BoundingRectangle)"
    } else { throw "Step 3 CreateButton not found" }

    # 10. WAIT for installer for real (up to 9 minutes), capture frames at ~1fps
    $succeeded = SnapWhile -Condition {
        @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object {
            $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text -and
            $_.Current.Name -like '*Đã tạo thành công*'
        } | Select-Object -First 1
    } -MaxWaitSec 540 -PollMs 1000

    if ($succeeded) {
        Write-Output "SUCCESS detected by UIA"
        .\scripts\capture-winui.ps1 -OutputPath $OutSuccess
        Snap 12 100
        # let the dialog auto-close (2.5s) and capture main window with new instance
        Start-Sleep -Seconds 5
        .\scripts\capture-winui.ps1 -OutputPath ("docs/evidence/dot18/screenshots/wizard-main-after-forge.png")
        Snap 10 100
    } else {
        Write-Output "WARNING: success text NOT detected within 9 minutes"
    }
} finally {
    $gfx.Dispose()
    $bmp.Dispose()
    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
}

Write-Output "Frames captured: $script:frameIndex"
$inputPattern = Join-Path $tempFramesDir "frame_%04d.png"
if (Test-Path $OutVideo) { Remove-Item $OutVideo -Force }
& ffmpeg -y -framerate 3 -i $inputPattern -c:v libx264 -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" $OutVideo 2>&1 | Select-Object -Last 2
Remove-Item $tempFramesDir -Recurse -Force
if (Test-Path $OutVideo) {
    $dur = (Get-Item $OutVideo).Length
    Write-Output "DONE: $OutVideo ($dur bytes, approx $([math]::Round($script:frameIndex / 3, 1))s at 3fps)"
}
if (-not $succeeded) { Write-Output "RESULT: FAILED_TO_VERIFY_SUCCESS" }
