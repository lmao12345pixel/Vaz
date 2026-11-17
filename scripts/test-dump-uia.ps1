Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Stop-Process -Name 'Vaz.WinUI' -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
$env:VAZER_STARTUP_PAGE = 'InstanceDetailPage'
$env:VAZER_STARTUP_TAB = 'resourcepacks'
$proc = Start-Process 'src/Vaz.WinUI/bin/x64/Debug/net8.0-windows10.0.26100.0/win-x64/Vaz.WinUI.exe' -PassThru
Start-Sleep -Seconds 3

$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)

Write-Host "Total Buttons found: $($buttons.Count)"
foreach ($b in $buttons) {
    $name = $b.Current.Name
    $help = $b.Current.HelpText
    $autoId = $b.Current.AutomationId
    $rect = $b.Current.BoundingRectangle
    if ($name -or $help) {
        Write-Host "Button: Name='$name' | HelpText='$help' | AutoId='$autoId' | Pos=$([int]$rect.X),$([int]$rect.Y)"
    }
}
Stop-Process -Name 'Vaz.WinUI' -Force -ErrorAction SilentlyContinue
