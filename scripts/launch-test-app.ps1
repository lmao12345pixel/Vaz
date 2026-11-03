param(
    [string]$Instance = "rt-mods-test",
    [string]$Page = "InstanceDetailPage"
)

Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$exePath = (Resolve-Path "src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe").Path
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exePath
$psi.EnvironmentVariables["VAZER_STARTUP_PAGE"] = $Page
$psi.EnvironmentVariables["VAZER_STARTUP_INSTANCE"] = $Instance
$psi.UseShellExecute = $false
$p = [System.Diagnostics.Process]::Start($psi)
Start-Sleep -Seconds 3
Get-Process Vaz.WinUI
