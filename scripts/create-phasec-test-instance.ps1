# Create a Fabric 1.21.1 instance named "Phase C Test 1.21.1" (auto-default name keeps) via wizard automation
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50) | Out-Null
Start-Sleep -Milliseconds 300

$btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
if (-not $btn) { throw "Create button not found" }
Invoke-UiaClick -Element $btn
Start-Sleep -Seconds 3

# Search 1.21.1
$root = Get-VazRoot
$sb = @(Get-UiaDescendants -Element $root) | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
Click-MouseAt -X ([int]($sb.Current.BoundingRectangle.X + 30)) -Y ([int]($sb.Current.BoundingRectangle.Y + 18)) -HWnd $hWnd
[System.Windows.Forms.SendKeys]::SendWait("1.21.1")
Start-Sleep -Milliseconds 800

# Select the card whose text is exactly "1.21.1" (avoid matching 1.21.11)
$root = Get-VazRoot
$target = @(Get-UiaDescendants -Element $root) | Where-Object {
    $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text -and
    $_.Current.Name -eq '1.21.1'
} | Select-Object -First 1
if (-not $target) { throw "Card 1.21.1 not found" }
[VazMouse]::ClickHwnd($hWnd, [int]($target.Current.BoundingRectangle.X + 60), [int]($target.Current.BoundingRectangle.Y + 60)) | Out-Null
Start-Sleep -Milliseconds 800

# Step 1 -> Step 2
$b1 = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($hWnd, [int]($b1.Current.BoundingRectangle.X + 50), [int]($b1.Current.BoundingRectangle.Y + 18)) | Out-Null
Start-Sleep -Seconds 2

# Step 2: Fabric is default; Tiếp tục
$b2 = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($hWnd, [int]($b2.Current.BoundingRectangle.X + 55), [int]($b2.Current.BoundingRectangle.Y + 18)) | Out-Null
Start-Sleep -Seconds 2

# Step 3: Tạo Instance
$b3 = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($hWnd, [int]($b3.Current.BoundingRectangle.X + 85), [int]($b3.Current.BoundingRectangle.Y + 19)) | Out-Null
Write-Output "Clicked create. Waiting for install..."

$sw = [System.Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 180) {
    Start-Sleep -Seconds 5
    $ok = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object {
        $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text -and $_.Current.Name -like '*Đã tạo thành công*'
    }
    if ($ok) { Write-Output "Instance created in $($sw.Elapsed.TotalSeconds)s"; break }
}
Start-Sleep -Seconds 4
Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
