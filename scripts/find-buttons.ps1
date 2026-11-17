Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output "No process"; exit }

$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$btns = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)

foreach ($b in $btns) {
    $rect = $b.Current.BoundingRectangle
    Write-Output "Button: '$($b.Current.Name)' | Rect: $($rect.X),$($rect.Y),$($rect.Width),$($rect.Height)"
}
