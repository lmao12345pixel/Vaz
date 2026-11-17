# Phase B UIA helpers - dung cho dot18.2c automated verification.
# Load: . .\scripts\uia-lib.ps1
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

if (-not ([System.Management.Automation.PSTypeName]'VazMouse').Type) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public class VazMouse {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(150);
        mouse_event(0x02, 0, 0, 0, 0); // DOWN
        System.Threading.Thread.Sleep(60);
        mouse_event(0x04, 0, 0, 0, 0); // UP
    }
    public static void ClickHwnd(IntPtr hWnd, int x, int y) {
        ShowWindow(hWnd, 9);
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(300);
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(150);
        mouse_event(0x02, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(80);
        mouse_event(0x04, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(300);
    }
}
"@
}

function Click-MouseAt {
    param([int]$X, [int]$Y, [IntPtr]$HWnd = [IntPtr]::Zero)
    if ($HWnd -ne [IntPtr]::Zero) {
        [VazMouse]::ClickHwnd($HWnd, $X, $Y)
    } else {
        [VazMouse]::Click($X, $Y)
    }
}

function Get-VazRoot {
    param([int]$TimeoutSec = 15)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        $p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($p) {
            $root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
            if ($root) { return $root }
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Vaz.WinUI window not found within ${TimeoutSec}s"
}

# Recursively dump UIA tree: ControlType | Name | AutomationId | ClassName | enabled | offscreen | rect
function Dump-UiaTree {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Element,
        [int]$Depth = 0,
        [int]$MaxDepth = 30
    )
    if ($Depth -gt $MaxDepth) { return }
    $c = $Element.Current
    $indent = '  ' * $Depth
    $name = ($c.Name ?? '') -replace "`r?`n", '\n'
    Write-Output ("{0}{1} | Name='{2}' | AutoId='{3}' | Class='{4}' | Enabled={5} | Offscreen={6} | Rect=({7:0},{8:0},{9:0}x{10:0})" -f `
        $indent, $c.ControlType.ProgrammaticName.Replace('ControlType.', ''), $name, $c.AutomationId, $c.ClassName, `
        $c.IsEnabled, $c.IsOffscreen, $c.BoundingRectangle.X, $c.BoundingRectangle.Y, $c.BoundingRectangle.Width, $c.BoundingRectangle.Height)

    $children = $Element.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($child in $children) {
        Dump-UiaTree -Element $child -Depth ($Depth + 1) -MaxDepth $MaxDepth
    }
}

# Recursive walker that works across Popup boundaries (FindAll(Descendants) can return
# nothing once WinUI moves the dialog into a PopupHost).
function Get-UiaDescendants {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Element,
        [int]$Depth = 0,
        [int]$MaxDepth = 40
    )
    if ($Depth -gt $MaxDepth) { return }
    $children = $Element.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($child in $children) {
        $child
        Get-UiaDescendants -Element $child -Depth ($Depth + 1) -MaxDepth $MaxDepth
    }
}

function Test-UiaMatch {
    param([System.Windows.Automation.AutomationElement]$Element, [string]$ControlType)
    if (-not $ControlType) { return $true }
    $ct = [System.Windows.Automation.ControlType]::$ControlType
    return $Element.Current.ControlType -eq $ct
}

function Find-UiaByName {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Root,
        [Parameter(Mandatory = $true)][string]$NameContains,
        [string]$ControlType
    )
    return @(Get-UiaDescendants -Element $Root | Where-Object {
        (Test-UiaMatch -Element $_ -ControlType $ControlType) -and ($_.Current.Name -like "*$NameContains*")
    })
}

function Find-UiaByControlType {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Root,
        [Parameter(Mandatory = $true)][string]$ControlType
    )
    return @(Get-UiaDescendants -Element $Root | Where-Object { Test-UiaMatch -Element $_ -ControlType $ControlType })
}

# Find a Button (any ControlType Button) that has a descendant Text whose Name matches.
function Find-UiaButtonByText {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Root,
        [Parameter(Mandatory = $true)][string]$TextContains
    )
    $texts = Find-UiaByName -Root $Root -NameContains $TextContains -ControlType 'Text'
    foreach ($t in $texts) {
        $el = $t
        for ($i = 0; $i -lt 6; $i++) {
            $el = [System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($el)
            if (-not $el) { break }
            if ($el.Current.ControlType -eq [System.Windows.Automation.ControlType]::Button) { return $el }
        }
    }
    return $null
}

function Invoke-UiaClick {
    param([Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Element)
    $pattern = $Element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    ($pattern -as [System.Windows.Automation.InvokePattern]).Invoke()
}

# Expand ComboBox, pick the ListItem whose Name matches, then collapse.
function Select-UiaComboItem {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Combo,
        [Parameter(Mandatory = $true)][string]$ItemNameContains,
        [int]$TimeoutSec = 10
    )
    $ec = $Combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    if (-not $ec) { throw "ComboBox does not support ExpandCollapsePattern" }
    ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Expand()

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $item = $null
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        Start-Sleep -Milliseconds 200
        $items = Find-UiaByControlType -Root (Get-VazRoot) -ControlType 'ListItem'
        $item = @($items | Where-Object { $_.Current.Name -like "*$ItemNameContains*" }) | Select-Object -First 1
        if ($item) { break }
    }
    if (-not $item) {
        ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Collapse()
        throw "ListItem containing '$ItemNameContains' not found"
    }
    $sel = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    ($sel -as [System.Windows.Automation.SelectionItemPattern]).Select()
    Start-Sleep -Milliseconds 300
    try { ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Collapse() } catch { }
    return $item
}

# List the current items of an expanded ComboBox (must be expanded first), then collapse.
function Get-UiaComboItems {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Combo,
        [int]$TimeoutSec = 10
    )
    $ec = $Combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    if (-not $ec) { throw "ComboBox does not support ExpandCollapsePattern" }
    ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Expand()
    Start-Sleep -Milliseconds 500
    $items = Find-UiaByControlType -Root (Get-VazRoot) -ControlType 'ListItem'
    $result = @($items | ForEach-Object { $_.Current.Name })
    try { ($ec -as [System.Windows.Automation.ExpandCollapsePattern]).Collapse() } catch { }
    return $result
}
