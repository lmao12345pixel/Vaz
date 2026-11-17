Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile("C:\Users\TOSHIBA\AppData\Local\Temp\antigravity\current-state.png")

# Find the bounding box of green pixels (#10B981 or similar green)
$minX = 9999; $maxX = 0; $minY = 9999; $maxY = 0; $count = 0
for ($y = 220; $y -lt 270; $y++) {
    for ($x = 200; $x -lt 800; $x++) {
        $c = $bmp.GetPixel($x, $y)
        # Check green button color (R is low, G is high around 185, B around 129)
        if ($c.G -gt 150 -and $c.R -lt 50 -and $c.B -gt 100 -and $c.B -lt 160) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
            $count++
        }
    }
}
Write-Host "Green pixels: $count. Box: X=[$minX, $maxX], Y=[$minY, $maxY]"
Write-Host "Center: X=$([int](($minX + $maxX)/2)), Y=$([int](($minY + $maxY)/2))"

# Check other buttons on the same row:
Write-Host "Toolbar row at Y=$([int](($minY + $maxY)/2))"
$bmp.Dispose()
