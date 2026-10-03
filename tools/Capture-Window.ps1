# Captures the app window with PrintWindow(PW_RENDERFULLCONTENT).
# PrintWindow can return TRUE and still hand back a blank bitmap on a Skia GL window,
# so the capture is sampled and falls back to a screen copy when it comes back uniform.
param(
    [Parameter(Mandatory = $true)][int]$ProcessId,
    [Parameter(Mandatory = $true)][string]$Out
)

Add-Type -AssemblyName System.Drawing

Add-Type -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
[DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
[StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
'@ -Name Win32Cap -Namespace Cap

$proc = Get-Process -Id $ProcessId -ErrorAction Stop
$hwnd = $proc.MainWindowHandle
if ($hwnd -eq [IntPtr]::Zero) { throw "Process $ProcessId has no main window (MainWindowHandle is 0)." }
if ([Cap.Win32Cap]::IsIconic($hwnd)) { throw "Window is minimised; restore it before capturing." }

$rect = New-Object Cap.Win32Cap+RECT
[void][Cap.Win32Cap]::GetWindowRect($hwnd, [ref]$rect)
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
if ($w -le 0 -or $h -le 0) { throw "Window rect is empty ($w x $h)." }

$bmp = New-Object System.Drawing.Bitmap $w, $h
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $gfx.GetHdc()
[void][Cap.Win32Cap]::PrintWindow($hwnd, $hdc, 2)  # PW_RENDERFULLCONTENT
$gfx.ReleaseHdc($hdc)
$gfx.Dispose()

# Sample a grid: a success return is not evidence of pixels.
$colors = @()
for ($x = 1; $x -lt 10; $x++) {
    for ($y = 1; $y -lt 10; $y++) {
        $colors += $bmp.GetPixel([int]($w * $x / 10), [int]($h * $y / 10)).ToArgb()
    }
}
if (($colors | Sort-Object -Unique).Count -le 1) {
    Write-Host "PrintWindow returned a uniform bitmap; falling back to screen copy."
    $bmp.Dispose()
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $gfx.Dispose()
}

$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Saved $Out ($w x $h)"
