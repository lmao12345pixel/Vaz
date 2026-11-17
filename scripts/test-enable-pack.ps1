Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Stop-Process -Name 'Vaz.WinUI' -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

$env:VAZER_STARTUP_PAGE = 'InstanceDetailPage'
$env:VAZER_STARTUP_TAB = 'resourcepacks'

$proc = Start-Process 'src/Vaz.WinUI/bin/x64/Debug/net8.0-windows10.0.26100.0/win-x64/Vaz.WinUI.exe' -PassThru
Start-Sleep -Seconds 3

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$appWin = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)

if ($appWin) {
    Write-Host 'Found app window!'
    $btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    $allBtns = $appWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
    Write-Host "Total buttons: $($allBtns.Count)"

    $targetBtn = $null
    foreach ($b in $allBtns) {
        $name = $b.Current.Name
        if ($name -like "B*Fresh Animations*") {
            $targetBtn = $b
            Write-Host "Matched enable button: $name"
            break
        }
    }

    if ($targetBtn) {
        $inv = $targetBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $inv.Invoke()
        Write-Host "Successfully invoked enable button!"
        Start-Sleep -Seconds 2
    } else {
        Write-Host 'Could not find enable button for Fresh Animations'
    }

    $alive = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
    $isAlive = ($alive -ne $null)
    Write-Host "Process still alive (NO CRASH): $isAlive"

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/capture-window.ps1 -OutputPath 'docs/evidence/RPS-AC-02-pack-enabled-top-priority-1025x533.png'
}

# Check options.txt
$instMods = Join-Path $env:APPDATA 'Vazer\Minecraft\instances\test fabric\options.txt'
if (Test-Path $instMods) {
    $lines = Get-Content $instMods | Where-Object { $_ -like 'resourcePacks:*' }
    Write-Host "options.txt line: $lines"
} else {
    Write-Host "options.txt not found at $instMods"
}

Stop-Process -Name 'Vaz.WinUI' -Force -ErrorAction SilentlyContinue
