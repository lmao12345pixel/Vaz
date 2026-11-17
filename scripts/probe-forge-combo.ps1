# Probe: open wizard, step1 1.20.1, step2 Forge, dump UIA tree to inspect ComboBox state
$ErrorActionPreference = "Stop"
. .\scripts\uia-lib.ps1
Add-Type -AssemblyName System.Windows.Forms

$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path
Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600
$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50) | Out-Null
Start-Sleep -Milliseconds 300

$btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
Invoke-UiaClick -Element $btn
Start-Sleep -Seconds 4

$sb = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
Click-MouseAt -X ([int]($sb.Current.BoundingRectangle.X + 30)) -Y ([int]($sb.Current.BoundingRectangle.Y + 18)) -HWnd $hWnd
[System.Windows.Forms.SendKeys]::SendWait("1.20.1")
Start-Sleep -Milliseconds 900

$cb = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CardButton' } | Select-Object -First 1
[VazMouse]::ClickHwnd($hWnd, [int]($cb.Current.BoundingRectangle.X + 80), [int]($cb.Current.BoundingRectangle.Y + 80)) | Out-Null
Start-Sleep -Milliseconds 800

$b1 = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
[VazMouse]::ClickHwnd($hWnd, [int]($b1.Current.BoundingRectangle.X + 50), [int]($b1.Current.BoundingRectangle.Y + 18)) | Out-Null
Start-Sleep -Seconds 2

$forgeb = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ForgeButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
Write-Output "ForgeButton: $($null -ne $forgeb) $($forgeb.Current.BoundingRectangle)"
[VazMouse]::ClickHwnd($hWnd, [int]($forgeb.Current.BoundingRectangle.X + 37), [int]($forgeb.Current.BoundingRectangle.Y + 50)) | Out-Null
Start-Sleep -Seconds 5

Write-Output "----- UIA dump of window (filtered to ComboBox / elements with '1.20' in name) -----"
Get-UiaDescendants -Element (Get-VazRoot) | ForEach-Object {
    $c = $_.Current
    if ($c.ControlType -eq [System.Windows.Automation.ControlType]::ComboBox -or ($c.Name -like '*1.20*') -or ($c.AutomationId -like '*Loader*')) {
        Write-Output ("CT={0} Name='{1}' AutoId='{2}' Class='{3}' Enabled={4} Offscreen={5} Rect=({6:0},{7:0})" -f $c.ControlType.ProgrammaticName.Replace('ControlType.', ''), $c.Name, $c.AutomationId, $c.ClassName, $c.IsEnabled, $c.IsOffscreen, $c.BoundingRectangle.X, $c.BoundingRectangle.Y)
    }
}
Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
