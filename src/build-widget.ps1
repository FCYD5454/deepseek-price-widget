param([string]$OutFile)

$srcDir = $PSScriptRoot
$root = Split-Path -Parent $srcDir
$src = Join-Path $srcDir 'DeepSeekPriceWidget.cs'
$manifest = Join-Path $srcDir 'app.manifest'
$assets = Join-Path $root 'assets'
if (-not (Test-Path -LiteralPath $assets)) {
    New-Item -ItemType Directory -Path $assets -Force | Out-Null
}
$ico = Join-Path $assets 'app.ico'
if (-not $OutFile) {
    $OutFile = Join-Path $root 'DeepSeekPriceWidget.exe'
}

$t = [System.IO.File]::ReadAllText($src, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText($src, $t, (New-Object System.Text.UTF8Encoding($true)))

Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 32, 32
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)
$bgBrush = New-Object System.Drawing.SolidBrush -ArgumentList ([System.Drawing.Color]::FromArgb(255, 18, 22, 29))
$g.FillEllipse($bgBrush, 1, 1, 30, 30)
$ringPen = New-Object System.Drawing.Pen -ArgumentList ([System.Drawing.Color]::FromArgb(255, 46, 204, 113)), ([single]2)
$g.DrawEllipse($ringPen, 2, 2, 28, 28)
$iconFont = [System.Drawing.Font]::new('Segoe UI', [single]16, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fgBrush = New-Object System.Drawing.SolidBrush -ArgumentList ([System.Drawing.Color]::FromArgb(255, 255, 159, 67))
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = [System.Drawing.StringAlignment]::Center
$fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
$yen = [string][char]0x00A5
$g.DrawString($yen, $iconFont, $fgBrush, [System.Drawing.RectangleF]::new(0, 0, 32, 32), $fmt)
$g.Dispose()
$hicon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hicon)
$fs = [System.IO.File]::Create($ico)
$icon.Save($fs)
$fs.Close()
$bmp.Dispose()

$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf = Join-Path $fw 'WPF'
$refs = @(
    (Join-Path $wpf 'WindowsBase.dll'),
    (Join-Path $wpf 'PresentationCore.dll'),
    (Join-Path $wpf 'PresentationFramework.dll'),
    (Join-Path $fw 'System.Xaml.dll'),
    (Join-Path $fw 'System.Net.Http.dll'),
    (Join-Path $fw 'System.Web.Extensions.dll'),
    (Join-Path $fw 'System.Windows.Forms.dll'),
    (Join-Path $fw 'System.Drawing.dll')
)
foreach ($r in $refs) {
    if (-not (Test-Path -LiteralPath $r)) {
        Write-Output ('MISSING REF: ' + $r)
        exit 1
    }
}
$csc = Join-Path $fw 'csc.exe'
$cscArgs = @('/nologo', '/target:winexe', ('/out:' + $OutFile), ('/win32icon:' + $ico))
if (Test-Path -LiteralPath $manifest) {
    $cscArgs += ('/win32manifest:' + $manifest)
}
foreach ($r in $refs) {
    $cscArgs += ('/r:' + $r)
}
$cscArgs += $src
& $csc $cscArgs
if ($LASTEXITCODE -ne 0) {
    Write-Output ('BUILD FAILED: ' + $LASTEXITCODE)
    exit 1
}
Write-Output ('BUILD OK: ' + $OutFile)
