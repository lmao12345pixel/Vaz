# Change-version scenario runner (instance "fabric 1.21.1 #4") - ASCII-safe for PS 5.1
# Params:
#   -TargetVersion : text to search in Step 1 (e.g. "1.20.6"). Use "" to keep current selection.
#   -ModChoice     : keep | delete | cancel   (maps: keep=giu mods button, delete=xoa mods button, cancel=Huy button)
#   -ExpectedMc    : mc version expected in instance.json after change (e.g. "1.20.6")
#   -ExpectedLoader: loaderType expected in instance.json after change (fabric|vanilla)
#   -InstanceName  : folder name under instances
#   -OutImage      : final screenshot path
#   -OutWarn       : warning dialog screenshot path
#   -OutStep3      : step 3 screenshot path
#   -OutVideo      : (optional) video output path
param(
    [string]$TargetVersion = "",
    [string]$Step2LoaderId = "",
    [ValidateSet("keep", "delete", "cancel")] [string]$ModChoice = "keep",
    [string]$ExpectedMc = "1.21.1",
    [string]$ExpectedLoader = "fabric",
    [string]$InstanceName = "fabric 1.21.1 #4",
    [string]$OutImage = "docs/evidence/dot18/screenshots/change-version-after.png",
    [string]$OutWarn = "docs/evidence/dot18/screenshots/change-version-warning.png",
    [string]$OutStep3 = "docs/evidence/dot18/screenshots/change-version-step3.png",
    [string]$OutVideo = ""
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

# Unicode literals (PS 5.1 default encoding is not UTF-8)
$ChoiceBtnId = switch ($ModChoice) { "keep" { "KeepModsButton" } "delete" { "DeleteModsButton" } "cancel" { "CancelMigrationButton" } }

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path
$j = Get-Content $settingsPath -Raw | ConvertFrom-Json
$j.theme = "dark"
$j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8

$tempFramesDir = $null
if ($OutVideo) {
    $videoDir = Split-Path $OutVideo -Parent
    if ($videoDir -and -not (Test-Path $videoDir)) { New-Item -ItemType Directory -Path $videoDir -Force | Out-Null }
    $tempFramesDir = Join-Path ([System.IO.Path]::GetTempPath()) ("vaz_chgver_" + [System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempFramesDir -Force | Out-Null
}

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600
$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50) | Out-Null
Start-Sleep -Milliseconds 300

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
if ($tempFramesDir) { Snap 8 100 }

# 1. Open Instances page -> click test instance card (exact text)
$root = Get-VazRoot
$cardText = @(Get-UiaDescendants -Element $root) | Where-Object {
    $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text -and
    $_.Current.Name -eq $InstanceName
} | Select-Object -First 1
if (-not $cardText) { throw "Test instance card not found" }
[VazMouse]::ClickHwnd($hWnd, [int]($cardText.Current.BoundingRectangle.X + 60), [int]($cardText.Current.BoundingRectangle.Y + 10)) | Out-Null
if ($tempFramesDir) { Snap 10 100 } else { Start-Sleep -Seconds 3 }
Write-Output "Opened detail page"

# 2. Click "Doi phien ban" button on the detail header
$btn = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ChangeVersionButton' } | Select-Object -First 1
if (-not $btn) { throw "ChangeVersionButton not found" }
[VazMouse]::ClickHwnd($hWnd, [int]($btn.Current.BoundingRectangle.X + 60), [int]($btn.Current.BoundingRectangle.Y + 18)) | Out-Null
if ($tempFramesDir) { Snap 15 100 } else { Start-Sleep -Seconds 3 }
Write-Output "Wizard opened (change mode)"

# 3. Step 1: optionally search + select target version (exact text)
if ($TargetVersion) {
    $sb = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
    Click-MouseAt -X ([int]($sb.Current.BoundingRectangle.X + 30)) -Y ([int]($sb.Current.BoundingRectangle.Y + 18)) -HWnd $hWnd
    [System.Windows.Forms.SendKeys]::SendWait($TargetVersion)
    if ($tempFramesDir) { Snap 8 100 } else { Start-Sleep -Milliseconds 800 }

    $target = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object {
        $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text -and
        $_.Current.Name -eq $TargetVersion
    } | Select-Object -First 1
    if (-not $target) { throw "Card $TargetVersion not found" }
    [VazMouse]::ClickHwnd($hWnd, [int]($target.Current.BoundingRectangle.X + 60), [int]($target.Current.BoundingRectangle.Y + 60)) | Out-Null
    if ($tempFramesDir) { Snap 8 100 } else { Start-Sleep -Milliseconds 700 }
    Write-Output "Selected card $TargetVersion"
}

# 4. Step 1 -> Step 2
$b1 = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($hWnd, [int]($b1.Current.BoundingRectangle.X + 50), [int]($b1.Current.BoundingRectangle.Y + 18)) | Out-Null
if ($tempFramesDir) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $ready = $false
    while ($sw.Elapsed.TotalSeconds -lt 60) {
        Snap 1 0
        $pr = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'PreviewLoader' } | Select-Object -First 1
        if ($pr -and $pr.Current.Name -like '*Fabric*') { $ready = $true; break }
        Start-Sleep -Milliseconds 600
    }
    Write-Output "Loader preview ready: $ready (T+$($sw.Elapsed.TotalSeconds)s)"
    Snap 6 100
} else {
    Start-Sleep -Seconds 2
}

# 4b. Step 2 (optional): switch loader card by AutomationId
if ($Step2LoaderId) {
    $loaderBtn = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq $Step2LoaderId -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    if (-not $loaderBtn) { throw "Loader card '$Step2LoaderId' not found" }
    $inv3 = $loaderBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    if ($inv3) { $inv3.Invoke() } else {
        [VazMouse]::ClickHwnd($hWnd, [int]($loaderBtn.Current.BoundingRectangle.X + 37), [int]($loaderBtn.Current.BoundingRectangle.Y + 50)) | Out-Null
    }
    if ($tempFramesDir) { Snap 8 100 } else { Start-Sleep -Seconds 2 }
    Write-Output "Switched loader card $Step2LoaderId"
} else {
    if ($tempFramesDir) { Snap 4 100 }
}

# 5. Step 2 -> Step 3
$b2 = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($hWnd, [int]($b2.Current.BoundingRectangle.X + 55), [int]($b2.Current.BoundingRectangle.Y + 18)) | Out-Null
if ($tempFramesDir) { Snap 12 100 } else { Start-Sleep -Seconds 2 }

.\scripts\capture-winui.ps1 -OutputPath $OutStep3
Write-Output "Step 3 captured: $OutStep3"

# 6. Click final action button ("Doi phien ban") via InvokePattern
$b3 = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
$inv = $b3.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
if ($inv) { $inv.Invoke() } else {
    [VazMouse]::ClickHwnd($hWnd, [int]($b3.Current.BoundingRectangle.X + 60), [int]($b3.Current.BoundingRectangle.Y + 19)) | Out-Null
}
Start-Sleep -Milliseconds 1200
if ($tempFramesDir) { Snap 8 100 }

# 7. Warning dialog -> screenshot + click the chosen option
.\scripts\capture-winui.ps1 -OutputPath $OutWarn
Write-Output "Warning captured: $OutWarn (choice: $ModChoice)"

$choiceBtn = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq $ChoiceBtnId } | Select-Object -First 1
if (-not $choiceBtn) { throw "Warning button '$ChoiceBtnId' not found" }
$inv2 = $choiceBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
if ($inv2) { $inv2.Invoke() } else {
    [VazMouse]::ClickHwnd($hWnd, [int]($choiceBtn.Current.BoundingRectangle.X + 40), [int]($choiceBtn.Current.BoundingRectangle.Y + 18)) | Out-Null
}
Write-Output "Clicked option $ModChoice"

if ($ModChoice -eq 'cancel') {
    if ($tempFramesDir) { Snap 12 100 } else { Start-Sleep -Seconds 2 }
    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
    $gfx.Dispose(); $bmp.Dispose()
    Write-Output "Scenario CANCEL done"
    exit 0
}

# 8. Wait until instance.json shows the expected new version/loader (real disk verification)
$instancePath = Join-Path "$env:APPDATA\Vazer\Minecraft\instances" $InstanceName
$metaPath = Join-Path $instancePath "instance.json"
$succeeded = $false
$sw2 = [System.Diagnostics.Stopwatch]::StartNew()
while ($sw2.Elapsed.TotalSeconds -lt 540) {
    if ($tempFramesDir) { Snap 1 0 }
    try {
        if (Test-Path $metaPath) {
            $raw = Get-Content $metaPath -Raw -ErrorAction Stop
            if ($raw -like "*`"$ExpectedMc`"*" -and $raw -like "*`"$ExpectedLoader`"*") { $succeeded = $true; break }
        }
    } catch { }
    Start-Sleep -Milliseconds 1000
}
Write-Output "Disk shows expected version/loader: $succeeded (T+$($sw2.Elapsed.TotalSeconds)s)"
if ($tempFramesDir) { Snap 12 100 } else { Start-Sleep -Seconds 3 }

# dialog auto-closes after 2.5s; capture detail page after reload
Start-Sleep -Seconds 4
.\scripts\capture-winui.ps1 -OutputPath $OutImage
Write-Output "Detail captured: $OutImage"
Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
$gfx.Dispose(); $bmp.Dispose()

# 9. Compile video
if ($tempFramesDir) {
    Write-Output "Frames captured: $script:frameIndex"
    $inputPattern = Join-Path $tempFramesDir "frame_%04d.png"
    if (Test-Path $OutVideo) { Remove-Item $OutVideo -Force }
    & ffmpeg -y -framerate 2 -i $inputPattern -c:v libx264 -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" $OutVideo 2>&1 | Select-Object -Last 2
    Remove-Item $tempFramesDir -Recurse -Force
    Write-Output "DONE: $OutVideo (~$([math]::Round($script:frameIndex / 3, 1))s at 3fps)"
}
if (-not $succeeded) { Write-Output "RESULT: FAILED_TO_VERIFY_SUCCESS"; exit 1 }

