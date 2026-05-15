using System;

namespace DrawRightNow.Interop;

/// <summary>
/// Утилита настройки расширенных стилей окна-overlay
/// </summary>
public static class OverlayWindowHelper
{
    /// <summary>
    /// Базовые флаги overlay: TOPMOST + LAYERED + TOOLWINDOW + NOACTIVATE
    /// При clickThrough = true дополнительно ставится WS_EX_TRANSPARENT
    /// </summary>
    public static void Apply(IntPtr hwnd, bool clickThrough)
    {
        if (hwnd == IntPtr.Zero) return;

        var ex = (uint)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();

        ex |= NativeMethods.WS_EX_LAYERED
            | NativeMethods.WS_EX_TOPMOST
            | NativeMethods.WS_EX_TOOLWINDOW
            | NativeMethods.WS_EX_NOACTIVATE;

        if (clickThrough) ex |= NativeMethods.WS_EX_TRANSPARENT;
        else ex &= ~NativeMethods.WS_EX_TRANSPARENT;

        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr((long)ex));

        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    /// <summary>
    /// Динамическое переключение режима "клики проходят насквозь"
    /// </summary>
    public static void SetClickThrough(IntPtr hwnd, bool enabled)
    {
        if (hwnd == IntPtr.Zero) return;
        var ex = (uint)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        if (enabled) ex |= NativeMethods.WS_EX_TRANSPARENT;
        else ex &= ~NativeMethods.WS_EX_TRANSPARENT;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr((long)ex));
    }
}