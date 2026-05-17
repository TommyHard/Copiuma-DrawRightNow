using System.Windows;
using WinForms = System.Windows.Forms;

namespace DrawRightNow.App.Services;

/// <summary>
/// Иконка в системном трее + контекстное меню Показать / Скрыть / Выйти
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly Window _window;
    private readonly WinForms.NotifyIcon _icon;
    private bool _disposed;

    public bool IsVisible
    {
        get => _icon.Visible;
        set => _icon.Visible = value;
    }

    public TrayIconService(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));

        _icon = new WinForms.NotifyIcon
        {
            Visible = true,
            Text = "DrawRightNow",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location)
        };

        var menu = new WinForms.ContextMenuStrip();
        var showItem = new WinForms.ToolStripMenuItem("Показать", null, (_, _) => ShowWindow());
        var hideItem = new WinForms.ToolStripMenuItem("Свернуть", null, (_, _) => HideWindow());
        var exitItem = new WinForms.ToolStripMenuItem("Выйти", null, (_, _) => ExitApp());
        menu.Items.Add(showItem);
        menu.Items.Add(hideItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(exitItem);
        _icon.ContextMenuStrip = menu;

        _icon.MouseDoubleClick += (_, _) =>
        {
            if (_window.IsVisible) HideWindow();
            else ShowWindow();
        };
    }

    public void ShowWindow()
    {
        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Maximized;
        _window.Activate();
    }

    public void HideWindow() => _window.Hide();

    private static void ExitApp()
    {
        System.Windows.Application.Current?.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _icon.Visible = false;
        _icon.Dispose();
    }
}