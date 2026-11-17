<#
.SYNOPSIS
    Bi-directional validation for evidence images in docs/evidence:
    (1) Every image file in docs/evidence is referenced in at least one docs/evidence/*.md file.
    (2) Every image file referenced in docs/evidence/*.md exists on disk in docs/evidence.
#>

[CmdletBinding()]
param()

$evidenceDir = Join-Path $PSScriptRoot '..\docs\evidence'
if (-not (Test-Path $evidenceDir)) {
    Write-Error "Directory does not exist: $evidenceDir"
    exit 1
}

$evidenceDir = (Resolve-Path $evidenceDir).Path

# 1. Collect all image files on disk
$imageExtensions = @('.png', '.jpg', '.jpeg', '.webp', '.gif')
$diskImages = Get-ChildItem -Path $evidenceDir -File |
    Where-Object { $imageExtensions -contains $_.Extension.ToLowerInvariant() } |
    Select-Object -ExpandProperty Name

Write-Host "Total disk images found: $($diskImages.Count)"

# 2. Collect all markdown files in docs/evidence
$mdFiles = Get-ChildItem -Path $evidenceDir -Filter '*.md' -File
Write-Host "Total markdown files found: $($mdFiles.Count)"

$referencedImages = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

foreach ($md in $mdFiles) {
    $content = Get-Content -Path $md.FullName -Raw
    $matches = [regex]::Matches($content, '([a-zA-Z0-9_\-\.]+\.(?:png|jpg|jpeg|webp|gif))')
    foreach ($m in $matches) {
        $imgName = [System.IO.Path]::GetFileName($m.Groups[1].Value)
        [void]$referencedImages.Add($imgName)
    }
}

Write-Host "Total referenced images in docs: $($referencedImages.Count)"

$hasError = $false

# Direction 1: Are all disk images referenced in docs?
Write-Host "`n--- [Direction 1] Checking if all disk images are referenced in docs ---"
$unreferenced = @()
foreach ($img in $diskImages) {
    if (-not $referencedImages.Contains($img)) {
        $unreferenced += $img
    }
}

if ($unreferenced.Count -gt 0) {
    Write-Warning "Found $($unreferenced.Count) images on disk NOT referenced in any markdown doc:"
    foreach ($u in $unreferenced) {
        Write-Warning "  - $u"
    }
    $hasError = $true
} else {
    Write-Host "PASS: 100% of images on disk are referenced in markdown files." -ForegroundColor Green
}

# Direction 2: Do all referenced images exist on disk?
Write-Host "`n--- [Direction 2] Checking if all referenced images exist on disk ---"
$missingOnDisk = @()
foreach ($ref in $referencedImages) {
    $imgPath = Join-Path $evidenceDir $ref
    if (-not (Test-Path $imgPath)) {
        $missingOnDisk += $ref
    }
}

if ($missingOnDisk.Count -gt 0) {
    Write-Error "Found $($missingOnDisk.Count) referenced images that do NOT exist on disk:"
    foreach ($m in $missingOnDisk) {
        Write-Error "  - $m"
    }
    $hasError = $true
} else {
    Write-Host "PASS: 100% of referenced images exist on disk." -ForegroundColor Green
}

if ($hasError) {
    Write-Host "`nVerdict: FAIL (Image reference mismatch detected)" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nVerdict: PASS (All images and references match 100%)" -ForegroundColor Green
    exit 0
}
