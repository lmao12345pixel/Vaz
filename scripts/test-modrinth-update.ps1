# scripts/test-modrinth-update.ps1
# Test POST /version_files/update voi file Sodium cu that cho 1.21.1/fabric
[CmdletBinding()]
param(
    [string]$TargetInstanceModsDir = "$env:APPDATA\Vazer\Minecraft\instances\rt-mods-test\mods"
)

$ErrorActionPreference = "Stop"
$userAgent = "vazer-client/1.0.0 (tests@vazer.local)"
$tempDir = Join-Path $env:TEMP "vazer-update-test"
if (-not (Test-Path $tempDir)) { New-Item -ItemType Directory -Path $tempDir -Force | Out-Null }

Write-Host "=== 1. Download old Sodium jar (0.6.0 for 1.21.1 Fabric) ==="
$oldVersionUrl = "https://cdn.modrinth.com/data/AANobbMI/versions/b70slbHV/sodium-fabric-0.6.0%2Bmc1.21.1.jar"
$oldJarPath = Join-Path $tempDir "sodium-fabric-0.6.0+mc1.21.1.jar"

if (-not (Test-Path $oldJarPath)) {
    Write-Host "Downloading $oldVersionUrl -> $oldJarPath"
    Invoke-WebRequest -Uri $oldVersionUrl -OutFile $oldJarPath -Headers @{ "User-Agent" = $userAgent }
} else {
    Write-Host "Reusing cached jar: $oldJarPath"
}

Write-Host "=== 2. Compute SHA-512 ==="
$hasher = [System.Security.Cryptography.SHA512]::Create()
$fileStream = [System.IO.File]::OpenRead($oldJarPath)
$hashBytes = $hasher.ComputeHash($fileStream)
$fileStream.Dispose()
$oldHash = ([System.BitConverter]::ToString($hashBytes) -replace "-","").ToLowerInvariant()
Write-Host "Old SHA-512: $oldHash"

if ($oldHash -ne "f7b92989ba6312352623d0311c5bb71eb8da14ea2e5a070f3a959a1f10cfe68cfc8487cf8dbde9187d849d6c04833df2a974be12949efae8aaffe873815ba7a5") {
    throw "SHA-512 does not match expected Modrinth record!"
}

Write-Host "=== 3. Call POST /version_files/update ==="
$payload = @{
    hashes = @($oldHash)
    algorithm = "sha512"
    loaders = @("fabric")
    game_versions = @("1.21.1")
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://api.modrinth.com/v2/version_files/update" `
    -Method Post `
    -Body $payload `
    -ContentType "application/json" `
    -Headers @{ "User-Agent" = $userAgent }

if (-not $response.$oldHash) {
    throw "API returned no match for hash: $oldHash"
}

$latest = $response.$oldHash
Write-Host "Latest Version Id: $($latest.id)"
Write-Host "Latest Version Number: $($latest.version_number)"
Write-Host "Latest Version Name: $($latest.name)"

$latestFile = $latest.files | Where-Object { $_.primary -eq $true } | Select-Object -First 1
if (-not $latestFile) { $latestFile = $latest.files[0] }

$latestHash = $latestFile.hashes.sha512
Write-Host "Latest SHA-512: $latestHash"

if ($latestHash -eq $oldHash) {
    throw "ERROR: Latest hash equals old hash (update not detected)!"
}

Write-Host "SUCCESS: POST /version_files/update correctly detected that Sodium 0.6.0 has update to $($latest.version_number)"

if (Test-Path $TargetInstanceModsDir) {
    $targetFile = Join-Path $TargetInstanceModsDir "sodium-fabric-0.6.0+mc1.21.1.jar"
    Copy-Item $oldJarPath $targetFile -Force
    Write-Host "Copied old Sodium jar into instance mods dir: $targetFile"
}

Write-Host "=== PASS: Test POST /version_files/update complete ==="
