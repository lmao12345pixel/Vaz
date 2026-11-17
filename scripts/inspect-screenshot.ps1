param([string]$Path = "C:\Users\TOSHIBA\AppData\Local\Temp\antigravity\current-state.png")
Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile($Path)
Write-Host "Image size: $($bmp.Width)x$($bmp.Height)"
for ($y = 195; $y -le 275; $y += 10) {
    $line = "y=$y : "
    for ($x = 150; $x -le 800; $x += 75) {
        $c = $bmp.GetPixel($x, $y)
        $hex = "#{0:X2}{1:X2}{2:X2}" -f $c.R,$c.G,$c.B
        $line += "($x)=$hex "
    }
    Write-Host $line
}
$bmp.Dispose()
