using System;
using System.Runtime.InteropServices;

namespace DrawRightNow.Interop;

/// <summary>
/// Глобальный keyboard-hook (WH_KEYBOARD_LL). Используется для глобальных
/// хоткеев приложения: переключение инструментов, hide/show UI, toggle
/// click-through. Hook поднимается на отдельном HWND-таймере UI-потока
/// и обязан быть Dispose() при выходе
/// </summary>
public sealed class GlobalKeyboardHook : IDisposable
{
    private readonly NativeMethods.HookProc _proc;
    private IntPtr _hookId;

    public event Action<uint>? KeyDown;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;
        var module = NativeMethods.GetModuleHandleW(null);
        _hookId = NativeMethods.SetWindowsHookExW(NativeMethods.WH_KEYBOARD_LL, _proc, module, 0);
        if (_hookId == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Не удалось установить keyboard hook (WinError {Marshal.GetLastWin32Error()})");
    }

    public void Stop()
    {
        if (_hookId == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            (wParam.ToInt32() == NativeMethods.WM_KEYDOWN ||
             wParam.ToInt32() == NativeMethods.WM_SYSKEYDOWN))
        {
            var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            try { KeyDown?.Invoke(info.vkCode); } catch { /* ignore */ }
        }
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}