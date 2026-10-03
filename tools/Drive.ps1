# Drives a running Patina window when the App MCP is not loaded: raise it, click at a point measured on a
# capture (window coordinates, chrome included, as Capture-Window.ps1 saves them), or send keys.
# Pattern from memory reference-win32-input-injection: raise with HWND_TOPMOST then SetForegroundWindow,
# absolute mouse_event scaled by the screen size, verify the hit window.
param(
    [Parameter(Mandatory = $true)][ValidateSet('click', 'keys', 'hover', 'resize', 'wheel')][string]$Action,
    [int]$X, [int]$Y,
    [string]$Keys,
    [int]$WaitMs = 600,
    [int]$Width, [int]$Height,
    [int]$Delta = -360,
    [string]$ProcessName = "Patina"
)
$ErrorActionPreference = 'Stop'
Add-Type -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint f);
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
[DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
[DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, int d, IntPtr e);
[DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
[DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint flags);
[StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
[StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
'@ -Name U -Namespace PatinaDrive
[void][PatinaDrive.U]::SetProcessDPIAware()

$proc = Get-Process $ProcessName -ErrorAction Stop | Select-Object -First 1
$hwnd = $proc.MainWindowHandle
[void][PatinaDrive.U]::SetWindowPos($hwnd, [IntPtr](-1), 0, 0, 0, 0, 0x0043)   # TOPMOST, NOMOVE|NOSIZE|SHOWWINDOW
[void][PatinaDrive.U]::SetForegroundWindow($hwnd)
Start-Sleep -Milliseconds 250

if ($Action -in 'click', 'hover', 'wheel') {
    $r = New-Object PatinaDrive.U+RECT
    [void][PatinaDrive.U]::GetWindowRect($hwnd, [ref]$r)
    $sx = $r.Left + $X; $sy = $r.Top + $Y
    $w = [PatinaDrive.U]::GetSystemMetrics(0); $h = [PatinaDrive.U]::GetSystemMetrics(1)
    $ax = [int]($sx * 65535 / ($w - 1)); $ay = [int]($sy * 65535 / ($h - 1))
    [PatinaDrive.U]::mouse_event(0x8001, $ax, $ay, 0, [IntPtr]::Zero)          # MOVE | ABSOLUTE
    Start-Sleep -Milliseconds 120
    $p = New-Object PatinaDrive.U+POINT; $p.X = $sx; $p.Y = $sy
    $hit = [PatinaDrive.U]::GetAncestor([PatinaDrive.U]::WindowFromPoint($p), 2)
    if ($hit -ne $hwnd) { Write-Host "warning: point ($sx,$sy) is over another window" }
    if ($Action -eq 'wheel') {
        [PatinaDrive.U]::mouse_event(0x0800, 0, 0, $Delta, [IntPtr]::Zero)   # WHEEL
    }
    if ($Action -eq 'click') {
        [PatinaDrive.U]::mouse_event(0x8002, $ax, $ay, 0, [IntPtr]::Zero)      # LEFTDOWN
        Start-Sleep -Milliseconds 60
        [PatinaDrive.U]::mouse_event(0x8004, $ax, $ay, 0, [IntPtr]::Zero)      # LEFTUP
    }
}
elseif ($Action -eq 'resize') {
    # Resize twice (target+8, then target): a single large resize can leave stale pixels (runtime gotchas).
    $r = New-Object PatinaDrive.U+RECT
    [void][PatinaDrive.U]::GetWindowRect($hwnd, [ref]$r)
    [void][PatinaDrive.U]::SetWindowPos($hwnd, [IntPtr]::Zero, $r.Left, $r.Top, $Width + 8, $Height + 8, 0x0014)
    Start-Sleep -Milliseconds 900
    [void][PatinaDrive.U]::SetWindowPos($hwnd, [IntPtr]::Zero, $r.Left, $r.Top, $Width, $Height, 0x0014)
}
elseif ($Action -eq 'keys') {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.SendKeys]::SendWait($Keys)
}
Start-Sleep -Milliseconds $WaitMs
[void][PatinaDrive.U]::SetWindowPos($hwnd, [IntPtr](-2), 0, 0, 0, 0, 0x0003)   # NOTOPMOST
Write-Host "$Action done"
