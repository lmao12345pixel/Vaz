$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path

Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

$proc = Start-Process $exePath -PassThru
Start-Sleep -Seconds 6
$hWnd = $proc.MainWindowHandle
[VazMouse]::ClickHwnd($hWnd, 50, 50)
Start-Sleep -Milliseconds 300

# Open Dialog
$btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
Invoke-UiaClick -Element $btn
Start-Sleep -Seconds 4

# Search 1.20.1 in Step 1
$all = @(Get-UiaDescendants -Element (Get-VazRoot))
$sb = $all | Where-Object { $_.Current.AutomationId -eq 'SearchBox' } | Select-Object -First 1
if ($sb) {
    Click-MouseAt -X ([int]($sb.Current.BoundingRectangle.X + 30)) -Y ([int]($sb.Current.BoundingRectangle.Y + 18)) -HWnd $hWnd
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.SendKeys]::SendWait("1.20.1")
    Start-Sleep -Milliseconds 800
}

# Select Card 1.20.1
$all = @(Get-UiaDescendants -Element (Get-VazRoot))
$cb = $all | Where-Object { $_.Current.AutomationId -eq 'CardButton' } | Select-Object -First 1
if ($cb) {
    [VazMouse]::ClickHwnd($hWnd, [int]($cb.Current.BoundingRectangle.X + 80), [int]($cb.Current.BoundingRectangle.Y + 80))
    Start-Sleep -Milliseconds 800
}

# Click Step 1 Next
$s1Next = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
if ($s1Next) {
    [VazMouse]::ClickHwnd($hWnd, [int]($s1Next.Current.BoundingRectangle.X + 50), [int]($s1Next.Current.BoundingRectangle.Y + 18))
    Start-Sleep -Seconds 2
}

# In Step 2: Click Forge button
$all = @(Get-UiaDescendants -Element (Get-VazRoot))
$forgeb = $all | Where-Object { $_.Current.AutomationId -eq 'ForgeButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
Write-Output "Forge button found: $($null -ne $forgeb) Rect: $($forgeb.Current.BoundingRectangle)"
if ($forgeb) {
    [VazMouse]::ClickHwnd($hWnd, [int]($forgeb.Current.BoundingRectangle.X + 37), [int]($forgeb.Current.BoundingRectangle.Y + 50))
    Start-Sleep -Seconds 2
}

# Click Step 2 Next
$all = @(Get-UiaDescendants -Element (Get-VazRoot))
$s2Next = $all | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
Write-Output "Step 2 Next: IsEnabled=$($s2Next.Current.IsEnabled) Rect=$($s2Next.Current.BoundingRectangle)"
if ($s2Next) {
    [VazMouse]::ClickHwnd($hWnd, [int]($s2Next.Current.BoundingRectangle.X + 55), [int]($s2Next.Current.BoundingRectangle.Y + 18))
    Start-Sleep -Seconds 2
}

# Check Step 3
.\scripts\capture-winui.ps1 -OutputPath 'docs/evidence/dot18/screenshots/wizard-step3-forge-dark.png'

# In Step 3: Click "Tạo Instance"
$all = @(Get-UiaDescendants -Element (Get-VazRoot))
$s3Create = $all | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
Write-Output "Step 3 Create: IsEnabled=$($s3Create.Current.IsEnabled) Rect=$($s3Create.Current.BoundingRectangle)"
if ($s3Create) {
    [VazMouse]::ClickHwnd($hWnd, [int]($s3Create.Current.BoundingRectangle.X + 85), [int]($s3Create.Current.BoundingRectangle.Y + 19))
    Start-Sleep -Seconds 5
}

Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
