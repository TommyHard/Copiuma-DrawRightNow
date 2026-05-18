namespace DrawRightNow.Interop;

/// <summary>
/// Регистрация глобальных хоткеев через RegisterHotKey.
/// Сообщения WM_HOTKEY получает HWND, переданный в конструктор; маршрутизация
/// id -> callback живёт здесь
/// Сразу после создания HotkeyManager хост должен подписать его на WndProc:
///   <c>HwndSource.FromHwnd(hwnd).AddHook(mgr.WndProc);</c>
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _disposed;

    public HotkeyManager(IntPtr hwnd) => _hwnd = hwnd;

    /// <summary>
    /// Зарегистрировать хоткей. Возвращает true при успехе. id выбирает вызывающий
    /// (произвольный int, должен быть уникальным в рамках процесса)
    /// </summary>
    public bool Register(int id, uint modifiers, uint virtualKey, Action handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        if (!NativeMethods.RegisterHotKey(_hwnd, id, modifiers | NativeMethods.MOD_NOREPEAT, virtualKey))
            return false;
        _handlers[id] = handler;
        return true;
    }

    public void ClearAll()
    {
        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_hwnd, id);
        _handlers.Clear();
    }

    /// <summary>
    /// WndProc-hook: возвращает (IntPtr.Zero, handled=true) для обработанных WM_HOTKEY
    /// </summary>
    public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var h))
        {
            try { h(); } catch { /* ignore */ }
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0) return;
        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_hwnd, id);
        _handlers.Clear();
    }
}