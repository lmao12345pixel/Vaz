# Capture screenshots for Resource Packs & Shaders in both Dark and Light themes
$ErrorActionPreference = "Stop"

$appDataPath = [System.IO.Path]::Combine($env:APPDATA, "Vazer", "Minecraft")
$settingsFile = [System.IO.Path]::Combine($appDataPath, "settings.json")
$exePath = "C:\OptizimeProject\Vaz\src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe"

function Set-Theme([string]$themeName) {
    if (Test-Path $settingsFile) {
        $json = Get-Content $settingsFile -Raw | ConvertFrom-Json
        $json.theme = $themeName
        $json.reduceMotion = $false
        $json | ConvertTo-Json -Depth 10 | Set-Content $settingsFile -Encoding utf8
        Write-Output "Settings updated: theme=$themeName, reduceMotion=false"
    }
}

function Capture-Scenario([string]$tab, [string]$theme, [string]$outFile) {
    Write-Output "--- Capturing Tab: $tab | Theme: $theme -> $outFile ---"
    Set-Theme $theme
    
    Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 500

    $env:VAZER_STARTUP_PAGE = "InstanceDetailPage"
    $env:VAZER_STARTUP_INSTANCE = "test fabric"
    $env:VAZER_STARTUP_TAB = $tab

    $proc = Start-Process -FilePath $exePath -PassThru
    
    $timeout = 10
    $elapsed = 0
    while ($elapsed -lt $timeout) {
        Start-Sleep -Milliseconds 500
        $elapsed += 0.5
        $p = Get-Process Vaz.WinUI -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($p) {
            break
        }
    }
    Start-Sleep -Seconds 2

    powershell -ExecutionPolicy Bypass -File "scripts/capture-winui.ps1" -OutputPath $outFile

    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
}

# 1. Dark Mode
Capture-Scenario "resourcepacks" "dark" "docs/evidence/dot15/resourcepacks_dark.png"
Capture-Scenario "shaders" "dark" "docs/evidence/dot15/shaders_dark.png"

# 2. Light Mode
Capture-Scenario "resourcepacks" "light" "docs/evidence/dot15/resourcepacks_light.png"
Capture-Scenario "shaders" "light" "docs/evidence/dot15/shaders_light.png"

# Restore Dark
Set-Theme "dark"
Write-Output "All 4 screenshots captured successfully!"
