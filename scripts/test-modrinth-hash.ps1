# Real Modrinth hash-lookup test: download real jar, verify sha512, POST /version_files
param([string]$OutFile = ".agent/mods-hash-raw.txt", [string]$TmpDir = ".agent/mods-hash-tmp")

$ErrorActionPreference = 'Continue'
$ua = 'Vazer/1.0.0 (+https://vazer.app)'
$base = 'https://api.modrinth.com/v2'
$lines = @()
function Log($m) { Write-Host $m; $global:lines += $m }

if (-not (Test-Path $TmpDir)) { New-Item -ItemType Directory -Path $TmpDir | Out-Null }

# 1) Pick latest sodium fabric 1.21.1 primary file
$lv = [uri]::EscapeDataString('[["project_type:mod"],["categories:fabric"],["versions:1.21.1"]]')
$v = Invoke-RestMethod -Uri "$base/search?query=sodium&limit=1&facets=$lv" -UserAgent $ua -TimeoutSec 30
$slug = $v.hits[0].slug
$ver = Invoke-RestMethod -Uri "$base/project/$slug/version?loaders=%5B%22fabric%22%5D&game_versions=%5B%221.21.1%22%5D" -UserAgent $ua -TimeoutSec 30
$primary = $ver[0].files | Where-Object { $_.primary } | Select-Object -First 1
$apiSha = $primary.hashes.sha512
Log "HA-01 primary-file | slug=$slug | file=$($primary.filename) | api_sha512_len=$($apiSha.Length) | downloads=$($ver[0].downloads) | url-host=$([uri]$primary.url | Select-Object -ExpandProperty Host)"

# 2) Download and verify sha512 matches API
$dest = Join-Path $TmpDir $primary.filename
Invoke-WebRequest -Uri $primary.url -UserAgent $ua -TimeoutSec 300 -OutFile $dest
$localSha = (Get-FileHash -Path $dest -Algorithm SHA512).Hash.ToLowerInvariant()
$match = ($localSha -eq $apiSha.ToLowerInvariant())
Log "HA-02 download+sha512-match | tmp=$dest | size=$((Get-Item $dest).Length)B | match=$match"

# 3) POST /version_files bulk hash lookup (BULK endpoint)
$body = @{ hashes = @($localSha); algorithm = 'sha512' } | ConvertTo-Json
try {
    $r = Invoke-RestMethod -Uri "$base/version_files" -Method Post -Body $body -ContentType 'application/json' -UserAgent $ua -TimeoutSec 30
    $key = ($r.PSObject.Properties.Name)[0]
    $ver0 = $r.$key
    Log "HA-03 POST /version_files | best:name.proto=$($ver0.name) | returned_version_id=$($ver0.version_number) | project_id=$($ver0.project_id) | keys=$($r.PSObject.Properties.Name.Count)"
} catch {
    Log "HA-03 FAIL | $($_.Exception.Message)"
}

# 4) Fake hash must NOT resolve (duplicate-hash detection sanity)
$fake = 'deadbeef' * 16
$body2 = @{ hashes = @($fake); algorithm = 'sha512' } | ConvertTo-Json
try {
    $r2 = Invoke-RestMethod -Uri "$base/version_files" -Method Post -Body $body2 -ContentType 'application/json' -UserAgent $ua -TimeoutSec 30
    Log "HA-04 fake-hash | resolved_count=$(@($r2.PSObject.Properties.Name).Count) | expected=0"
} catch {
    Log "HA-04 FAIL | $($_.Exception.Message)"
}

Remove-Item -Recurse -Force $TmpDir -ErrorAction SilentlyContinue
$global:lines | Out-File -FilePath $OutFile -Encoding utf8
Write-Host "RAW-SAVED: $OutFile"
