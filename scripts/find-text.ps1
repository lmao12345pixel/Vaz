Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output "No process"; exit }

$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
$texts = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)

foreach ($t in $texts) {
    $rect = $t.Current.BoundingRectangle
    if ($t.Current.Name) {
        Write-Output "Text: '$($t.Current.Name)' | Rect: $($rect.X),$($rect.Y),$($rect.Width),$($rect.Height)"
    }
}
