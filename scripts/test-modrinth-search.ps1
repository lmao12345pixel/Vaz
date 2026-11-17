# Real-world Modrinth API search scenario tests (Bước 1 - TÌM MOD)
param([string]$OutFile = ".agent/mods-test-raw.txt")

$ErrorActionPreference = 'Continue'
$ua = 'Vazer/1.0.0 (runtime-evidence-test; +https://vazer.app)'
$base = 'https://api.modrinth.com/v2'
$lines = @()
function Log($msg) { Write-Host $msg; $global:lines += $msg }

# Scenario helper: GET search, capture status + count + first slug + elapsed ms
function Test-Search($name, $query, $extraFacets, $limit) {
    $q = [uri]::EscapeDataString($query)
    $url = "$base/search?query=$([uri]::EscapeDataString($query))&limit=$limit" + $(if ($extraFacets) { "&facets=$([uri]::EscapeDataString($extraFacets))" })
    try {
        $sw = [Diagnostics.Stopwatch]::StartNew()
        $r = Invoke-WebRequest -Uri $url -UserAgent $ua -TimeoutSec 30 -UseBasicParsing -ErrorAction Stop
        $sw.Stop()
        $json = ($r.Content | ConvertFrom-Json)
        $n = $json.hits.Count
        $first = if ($n -gt 0) { $json.hits[0].slug } else { '(none)' }
        Log "SM|PASS: $name | HTTP $($r.StatusCode) | hits=$n | first=$first | $([int]$sw.Elapsed.TotalMilliseconds)ms"
    } catch {
        $code = '-'
        try { $code = [int]$_.Exception.Response.StatusCode } catch {}
        Log "SM|FAIL: $name | HTTP $code | $($_.Exception.Message.Substring(0,[Math]::Min(120,$_.Exception.Message.Length)))"
    }
}
function Test-Url($name, $url) {
    try {
        $sw = [Diagnostics.Stopwatch]::StartNew()
        $r = Invoke-WebRequest -Uri $url -UserAgent $ua -TimeoutSec 30 -UseBasicParsing -ErrorAction Stop
        $sw.Stop()
        $n = 0; try { $n = ($r.Content | ConvertFrom-Json).hits.Count } catch {}
        Log "SM|PASS: $name | HTTP $($r.StatusCode) | hits=$n | $([int]$sw.Elapsed.TotalMilliseconds)ms"
    } catch {
        $code = '-'
        try { $code = [int]$_.Exception.Response.StatusCode } catch {}
        Log "SM|FAIL: $name | HTTP $code | $($_.Exception.Message.Substring(0,[Math]::Min(120,$_.Exception.Message.Length)))"
    }
}

$facMod = '[["project_type:mod"]]'
$facLoader = '[["project_type:mod"],["categories:fabric"]]'
$facVersion = '[["project_type:mod"],["versions:1.21.1"]]'

Test-Search 'SM-01 keyword-with-results' 'sodium' $facMod 20
Test-Search 'SM-02 keyword-no-results' 'zzz-nosuchmod-qqq-2026' $facMod 20
Test-Search 'SM-03 keyword-vietnamese' 'tối ưu hiệu năng' $facMod 20
Test-Search 'SM-04 keyword-special-chars' 'a%b&c=d+f' $facMod 20
$long = 'x' * 500
Test-Search 'SM-05 keyword-500-chars' $long $facMod 20
Test-Search 'SM-06 facets-loader-fabric' 'sodium' $facLoader 20
Test-Search 'SM-07 facets-version-1.21.1' 'sodium' $facVersion 20
Test-Url 'SM-08 empty-query' "$base/search?query=&limit=20&facets=$([uri]::EscapeDataString($facMod))"
Test-Url 'SM-09 pagination-offset-100' "$base/search?query=sodium&limit=20&offset=100&facets=$([uri]::EscapeDataString($facMod))"
Test-Url 'SM-10 pagination-offset-beyond' "$base/search?query=sodium&limit=20&offset=100000&facets=$([uri]::EscapeDataString($facMod))"
Test-Url 'SM-11 index-downloads' "$base/search?query=sodium&limit=20&index=downloads&facets=$([uri]::EscapeDataString($facMod))"
Test-Url 'SM-12 limit-max-100' "$base/search?query=sodium&limit=100&facets=$([uri]::EscapeDataString($facMod))"
Test-Url 'SM-13 limit-over-100' "$base/search?query=sodium&limit=101&facets=$([uri]::EscapeDataString($facMod))"
Test-Url 'SM-14 limit-zero' "$base/search?query=sodium&limit=0&facets=$([uri]::EscapeDataString($facMod))"

$global:lines | Out-File -FilePath $OutFile -Encoding utf8
Write-Host "RAW-SAVED: $OutFile"
