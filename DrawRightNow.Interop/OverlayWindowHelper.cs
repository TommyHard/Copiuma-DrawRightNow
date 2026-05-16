using System;

namespace DrawRightNow.Interop;

/// <summary>
/// Утилита настройки расширенных стилей окна-overlay
/// </summary>
public static class OverlayWindowHelper
{
    public static void Apply(IntPtr hwnd, bool clickThrough)
    {
        if (hwnd == IntPtr.Zero) return;

        var ex = (uint)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();

        ex |= NativeMethods.WS_EX_TOPMOST
            | NativeMethods.WS_EX_TOOLWINDOW;

        ex &= ~NativeMethods.WS_EX_TRANSPARENT;
        ex &= ~NativeMethods.WS_EX_LAYERED;
        ex &= ~NativeMethods.WS_EX_NOACTIVATE;

        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr((long)ex));

        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    /// <summary>
    /// Реальное переключение click-through
    /// делается в MainWindow через WM_NCHITTEST — важно, чтобы
    /// тулбар продолжал ловить клики даже в режиме "клики сквозь окно"
    /// </summary>
    public static void SetClickThrough(IntPtr hwnd, bool enabled) { /* ignore */ }

    /// <summary>
    /// WDA_EXCLUDEFROMCAPTURE: исключает overlay из BitBlt/WGC
    /// </summary>
    public static void ExcludeFromCapture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        if (!NativeMethods.SetWindowDisplayAffinity(hwnd, NativeMethods.WDA_EXCLUDEFROMCAPTURE))
        {
            NativeMethods.SetWindowDisplayAffinity(hwnd, NativeMethods.WDA_NONE);
        }
    }
}