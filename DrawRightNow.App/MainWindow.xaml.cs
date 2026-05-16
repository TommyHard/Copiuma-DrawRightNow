using DrawRightNow.App.Services;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.Core.ViewModels;
using DrawRightNow.Interop;
using DrawRightNow.Rendering;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DrawRightNow.App;

/// <summary>
/// Окно-overlay. Делает три вещи:
///   1. Настраивает Win32 extended styles (TOPMOST/LAYERED/etc.)
///   2. Слушает свойство IsDrawingEnabled и переключает WS_EX_TRANSPARENT,
///      реализуя "PC interaction mode"
///   3. Прячет/показывает toolbar в зависимости от наведения мыши и Pin
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private IntPtr _hwnd;
    private TrayIconService? _tray;
    private LiveCaptureService? _liveCapture;
    private HotkeyManager? _hotkeys;
    private ExportService? _export;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        _vm.PropertyChanged += OnViewModelPropertyChanged;

        SourceInitialized += OnSourceInitialized;
        MouseMove += OnAnyMouseMove;
        KeyDown += OnKeyDown;

        Loaded += (_, _) =>
        {
            // По умолчанию IsToolbarPinned=true -> панель видна сразу.
            // Если пользователь снимет «закрепить», панель будет авто-скрываться
            if (_vm.IsToolbarPinned) ShowToolbar(); else HideToolbar(animated: false);
            ApplyToolbarOpacity();
            ApplyDimLayerVisibility();
        };
        Closing += OnClosing;

        // Глобальные горячие клавиши (для активного приложения)
        InputBindings.Add(new KeyBinding(_vm.UndoCommand, new KeyGesture(Key.Z, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(_vm.RedoCommand, new KeyGesture(Key.Y, ModifierKeys.Control)));
    }

    // ---- HWND / extended styles ----

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        OverlayWindowHelper.Apply(_hwnd, clickThrough: !_vm.IsDrawingEnabled);

        var source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(NcHitTestHook);

        // HWND готов — поднимаем сервисы
        _vm.ScreenServices = new WpfScreenServices(this);

        var screenW = (int)SystemParameters.PrimaryScreenWidth;
        var screenH = (int)SystemParameters.PrimaryScreenHeight;
        _liveCapture = new LiveCaptureService(screenW, screenH);
        _vm.FrameProvider = _liveCapture;

        _tray = new TrayIconService(this);
        _export = new ExportService(this, _vm.Canvas);

        // Глобальные хоткеи: Ctrl+Alt+* (Win+Shift+* конфликтует с системой)
        _hotkeys = new HotkeyManager(_hwnd);
        source?.AddHook(_hotkeys.WndProc);

        const uint VK_D = 0x44, VK_E = 0x45, VK_Z = 0x5A, VK_Y = 0x59, VK_C = 0x43;
        const uint CTRL_ALT = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;

        _hotkeys.Register(1, CTRL_ALT, VK_D, () =>
        {
            if (IsVisible) Hide(); else { Show(); Activate(); }
        });
        _hotkeys.Register(2, CTRL_ALT, VK_E, () =>
        {
            _vm.IsDrawingEnabled = !_vm.IsDrawingEnabled;
        });
        _hotkeys.Register(3, CTRL_ALT, VK_Z, () =>
        {
            if (_vm.UndoCommand.CanExecute(null)) _vm.UndoCommand.Execute(null);
        });
        _hotkeys.Register(4, CTRL_ALT, VK_Y, () =>
        {
            if (_vm.RedoCommand.CanExecute(null)) _vm.RedoCommand.Execute(null);
        });
        _hotkeys.Register(5, CTRL_ALT, VK_C, () =>
        {
            CopyCanvasToClipboard();
        });
    }

    /// <summary>
    /// Per-pixel click-through: в режиме «не рисуем» клики пропускаем сквозь
    /// окно, кроме тулбара и верхней hover-полоски (чтобы можно было
    /// вернуть рисование)
    /// </summary>
    private IntPtr NcHitTestHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.WM_NCHITTEST) return IntPtr.Zero;
        if (_vm.IsDrawingEnabled) return IntPtr.Zero;     // обычный hit-test

        // lParam: low = screenX, high = screenY (signed 16 бит)
        int lp = lParam.ToInt32();
        int sx = (short)(lp & 0xFFFF);
        int sy = (short)((lp >> 16) & 0xFFFF);

        var local = PointFromScreen(new System.Windows.Point(sx, sy));

        // Зона, в которой нужно ловить клики, даже когда "click-through" on:
        // верхние ~60 пикселей — туда умещаются и тулбар, и hover-стрип
        const double InteractiveBandHeight = 60.0;
        bool overInteractive = local.Y >= 0 && local.Y < InteractiveBandHeight;

        // Если открыт инлайн-редактор текста — тоже не пропускаем сквозь
        if (_vm.EditingText is not null) overInteractive = true;

        handled = true;
        return overInteractive
            ? new IntPtr(NativeMethods.HTCLIENT)
            : new IntPtr(NativeMethods.HTTRANSPARENT);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsDrawingEnabled))
        {
            // click-through решается в WM_NCHITTEST-hook
        }
        else if (e.PropertyName == nameof(MainViewModel.IsToolbarPinned))
        {
            if (_vm.IsToolbarPinned) ShowToolbar();
            else HideToolbar();
        }
        else if (e.PropertyName == nameof(MainViewModel.EditingText))
        {
            ShowOrHideTextEditor();
        }
        else if (e.PropertyName == nameof(MainViewModel.ActiveTool))
        {
            UpdateEyedropperOverlayVisibility();
        }
        else if (e.PropertyName == nameof(MainViewModel.ToolbarTranslucent))
        {
            ApplyToolbarOpacity();
        }
        else if (e.PropertyName == nameof(MainViewModel.OverlayDimEnabled))
        {
            ApplyDimLayerVisibility();
        }
    }

    private void ApplyToolbarOpacity()
    {
        // 1.0 — обычная, 0.45 — полупрозрачная (чтобы видеть под ней содержимое)
        var target = _vm.ToolbarTranslucent ? 0.45 : 1.0;
        if (_toolbarVisible)
            ToolbarHost.Opacity = target;
        // Если тулбар сейчас скрыт — анимация opacity активирует его сама,
        // здесь только запоминаем целевой уровень. ShowToolbar() читает _vm
    }

    private void ApplyDimLayerVisibility()
    {
        DimLayer.Visibility = _vm.OverlayDimEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateEyedropperOverlayVisibility()
    {
        EyedropperOverlay.Visibility = _vm.ActiveTool == ToolType.Eyedropper
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // ---- Инлайн-редактор текста (TextTool) ----

    private void ShowOrHideTextEditor()
    {
        var ts = _vm.EditingText;
        if (ts is null)
        {
            TextEditor.Visibility = Visibility.Collapsed;
            return;
        }

        // Позиционируем TextBox над baseline-точкой текста
        Canvas.SetLeft(TextEditor, ts.Position.X);
        Canvas.SetTop(TextEditor, ts.Position.Y - ts.FontSize * 1.1);
        TextEditor.FontSize = ts.FontSize;
        TextEditor.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(ts.Color.A, ts.Color.R, ts.Color.G, ts.Color.B));
        TextEditor.Text = ts.Text;
        TextEditor.Visibility = Visibility.Visible;

        // Откладываем установку фокуса до завершения layout-pass
        Dispatcher.BeginInvoke(new Action(() =>
        {
            TextEditor.Focus();
            Keyboard.Focus(TextEditor);
            TextEditor.CaretIndex = TextEditor.Text.Length;
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void TextEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            _vm.CommitTextEditing(TextEditor.Text);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            _vm.CancelTextEditing();
        }
    }

    private void TextEditor_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_vm.EditingText is not null)
            _vm.CommitTextEditing(TextEditor.Text);
    }

    // ---- Экспорт (доступен из тулбара и через Win+Shift+C) ----

    internal void SaveCanvasAs() => _export?.SaveAs();
    internal void CopyCanvasToClipboard() => _export?.CopyToClipboard();

    // ---- Toolbar auto-show ----

    private bool _toolbarVisible;

    private void HoverStrip_MouseEnter(object sender, MouseEventArgs e) => ShowToolbar();

    private void OnAnyMouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(this);

        // Скрытие/появление тулбара
        if (!_vm.IsToolbarPinned)
        {
            var overToolbar = p.Y < (ToolbarHost.ActualHeight + ToolbarHost.Margin.Top + 12);
            if (_toolbarVisible && !overToolbar) HideToolbar();
        }

        // Live-превью цвета для Eyedropper
        if (_vm.ActiveTool == ToolType.Eyedropper && _vm.ScreenServices is not null)
        {
            var screen = PointToScreen(p);
            var c = _vm.ScreenServices.GetPixel((int)screen.X, (int)screen.Y);

            EyedropperSwatch.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B));

            Canvas.SetLeft(EyedropperPreview, p.X);
            Canvas.SetTop(EyedropperPreview, p.Y);
        }
    }

    private void ShowToolbar()
    {
        if (_toolbarVisible) return;
        _toolbarVisible = true;
        var target = _vm.ToolbarTranslucent ? 0.45 : 1.0;
        ToolbarHost.BeginAnimation(OpacityProperty,
            new DoubleAnimation(target, TimeSpan.FromMilliseconds(120)));
        ToolbarHost.IsHitTestVisible = true;
    }

    private void HideToolbar(bool animated = true)
    {
        _toolbarVisible = false;
        if (_vm.IsToolbarPinned) { ShowToolbar(); return; }

        if (animated)
        {
            ToolbarHost.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180)));
        }
        else
        {
            ToolbarHost.Opacity = 0.0;
        }
        ToolbarHost.IsHitTestVisible = false;
    }

    // ---- Hotkeys (когда окно в фокусе) ----

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm.EditingText is not null) return;

        switch (e.Key)
        {
            case Key.P: _vm.ActiveTool = ToolType.Pencil; e.Handled = true; break;
            case Key.B: _vm.ActiveTool = ToolType.Brush; e.Handled = true; break;
            case Key.M: _vm.ActiveTool = ToolType.Marker; e.Handled = true; break;
            case Key.E: _vm.ActiveTool = ToolType.Eraser; e.Handled = true; break;
            case Key.R: _vm.ActiveTool = ToolType.Rectangle; e.Handled = true; break;
            case Key.O: _vm.ActiveTool = ToolType.Ellipse; e.Handled = true; break;
            case Key.L: _vm.ActiveTool = ToolType.Line; e.Handled = true; break;
            case Key.A: _vm.ActiveTool = ToolType.Arrow; e.Handled = true; break;
            case Key.T: _vm.ActiveTool = ToolType.Text; e.Handled = true; break;
            case Key.K: _vm.ActiveTool = ToolType.KnifeDelete; e.Handled = true; break;
            case Key.V: _vm.ActiveTool = ToolType.Move; e.Handled = true; break;
            case Key.I: _vm.ActiveTool = ToolType.Eyedropper; e.Handled = true; break;
            case Key.U: _vm.ActiveTool = ToolType.Blur; e.Handled = true; break;
            case Key.F8:
                _vm.IsDrawingEnabled = !_vm.IsDrawingEnabled;
                e.Handled = true;
                break;
            case Key.Escape:
                if (_vm.IsToolbarPinned) _vm.IsToolbarPinned = false;
                else Close();
                e.Handled = true;
                break;
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _hotkeys?.Dispose(); _hotkeys = null;
        _tray?.Dispose(); _tray = null;
        _liveCapture?.Dispose(); _liveCapture = null;
    }
}