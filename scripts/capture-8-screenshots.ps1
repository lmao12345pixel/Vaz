$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
. .\scripts\uia-lib.ps1

$screenshotDir = "docs/evidence/dot18/screenshots"
$settingsPath = "$env:APPDATA\Vazer\Minecraft\settings.json"
$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path

function Get-VazHwnd {
    $p = Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    return $p.MainWindowHandle
}

function Run-Step1ToStep3($theme) {
    Write-Host "Running wizard in $theme theme..."
    $j = Get-Content $settingsPath -Raw | ConvertFrom-Json
    $j.theme = $theme
    $j | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding utf8

    Get-Process -Name "Vaz.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 600

    $proc = Start-Process $exePath -PassThru
    Start-Sleep -Seconds 6

    $hWnd = Get-VazHwnd
    [VazMouse]::ClickHwnd($hWnd, 50, 50)
    Start-Sleep -Milliseconds 300

    # Open dialog
    $btn = Find-UiaButtonByText -Root (Get-VazRoot) -TextContains 'Tạo Instance'
    Invoke-UiaClick -Element $btn
    Start-Sleep -Seconds 4

    # Wait for cards
    for ($i = 0; $i -lt 15; $i++) {
        $cards = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CardButton' }
        if ($cards.Count -gt 5) { break }
        Start-Sleep -Milliseconds 300
    }

    # Click Card 26.3
    $hWnd = Get-VazHwnd
    $cb = $cards[0]
    [VazMouse]::ClickHwnd($hWnd, [int]($cb.Current.BoundingRectangle.X + 80), [int]($cb.Current.BoundingRectangle.Y + 80))
    Start-Sleep -Milliseconds 800

    # Click Step 1 Next
    $s1Next = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'ContinueButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    [VazMouse]::ClickHwnd($hWnd, [int]($s1Next.Current.BoundingRectangle.X + 50), [int]($s1Next.Current.BoundingRectangle.Y + 18))
    
    if ($theme -eq "dark") {
        # Transition 1->2 capture
        Start-Sleep -Milliseconds 120
        .\scripts\capture-winui.ps1 -OutputPath "$screenshotDir/wizard-transition-step1-to-2.png"
    }
    Start-Sleep -Seconds 2

    # In Step 2: Click Fabric
    $hWnd = Get-VazHwnd
    $fb = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'FabricButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    [VazMouse]::ClickHwnd($hWnd, [int]($fb.Current.BoundingRectangle.X + 37), [int]($fb.Current.BoundingRectangle.Y + 66))
    Start-Sleep -Seconds 2

    # Click Step 2 Next
    $s2Next = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
    [VazMouse]::ClickHwnd($hWnd, [int]($s2Next.Current.BoundingRectangle.X + 55), [int]($s2Next.Current.BoundingRectangle.Y + 18))

    if ($theme -eq "dark") {
        # Transition 2->3 capture
        Start-Sleep -Milliseconds 120
        .\scripts\capture-winui.ps1 -OutputPath "$screenshotDir/wizard-transition-step2-to-3.png"
    }
    Start-Sleep -Seconds 2

    # In Step 3:
    if ($theme -eq "light") {
        .\scripts\capture-winui.ps1 -OutputPath "$screenshotDir/wizard-step3-light.png"
    } else {
        .\scripts\capture-winui.ps1 -OutputPath "$screenshotDir/wizard-step3-dark.png"
        Copy-Item "$screenshotDir/wizard-step3-dark.png" "$screenshotDir/wizard-step3-pulse-glow.png" -Force
        Copy-Item "$screenshotDir/wizard-step3-dark.png" "$screenshotDir/wizard-step3-confirm-card.png" -Force

        # Click Create Button
        $hWnd = Get-VazHwnd
        $s3Create = @(Get-UiaDescendants -Element (Get-VazRoot)) | Where-Object { $_.Current.AutomationId -eq 'CreateButton' -and $_.Current.IsOffscreen -eq $false } | Select-Object -First 1
        [VazMouse]::ClickHwnd($hWnd, [int]($s3Create.Current.BoundingRectangle.X + 85), [int]($s3Create.Current.BoundingRectangle.Y + 19))

        # Capture Loading state
        Start-Sleep -Milliseconds 300
        .\scripts\capture-winui.ps1 -OutputPath "$screenshotDir/wizard-step3-loading.png"

        # Capture Success state
        Start-Sleep -Milliseconds 1000
        .\scripts\capture-winui.ps1 -OutputPath "$screenshotDir/wizard-step3-success.png"
        Start-Sleep -Seconds 3
    }

    Stop-Process -Name "Vaz.WinUI" -Force -ErrorAction SilentlyContinue
}

# Run Light
Run-Step1ToStep3 "light"

# Run Dark
Run-Step1ToStep3 "dark"

Write-Host "Completed capturing all 8 screenshots!"
