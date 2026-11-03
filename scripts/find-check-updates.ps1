Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile("C:\Users\TOSHIBA\AppData\Local\Temp\antigravity\after-close.png")

for ($x = 180; $x -le 520; $x += 20) {
    $c = $bmp.GetPixel($x, 244)
    $hex = "#{0:X2}{1:X2}{2:X2}" -f $c.R,$c.G,$c.B
    Write-Host "X=$x : $hex"
}
$bmp.Dispose()
