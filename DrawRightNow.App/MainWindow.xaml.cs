using DrawRightNow.App.Services;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.Core.ViewModels;
using DrawRightNow.Interop;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace DrawRightNow.App;

/// <summary>
/// Окно-overlay. Делает:
///   1. Настраивает Win32 extended styles (TOPMOST/LAYERED/etc.)
///   2. Прячет/показывает toolbar в зависимости от наведения мыши и Pin
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private IntPtr _hwnd;
    private TrayIconService? _tray;
    private HotkeyManager? _hotkeys;
    private ExportService? _export;

    private bool _isInitializing = true;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        for (int i = 0; i < 25; i++)
        {
            var rect = new System.Windows.Shapes.Rectangle();
            rect.Fill = new System.Windows.Media.SolidColorBrush();
            PixelGrid.Children.Add(rect);
        }

        _vm.PropertyChanged += OnViewModelPropertyChanged;

        SourceInitialized += OnSourceInitialized;
        MouseMove += OnAnyMouseMove;
        KeyDown += OnKeyDown;

        Loaded += (_, _) =>
        {
            UpdateToolbarState(animated: false);

            if (_vm.RememberToolbarPosition)
            {
                ToolbarTransform.X = _vm.Settings.ToolbarOffsetX;
                ToolbarTransform.Y = _vm.Settings.ToolbarOffsetY;
            }

            UpdateCursorForActiveTool();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isInitializing = false;
            }), System.Windows.Threading.DispatcherPriority.Background);
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

        IntPtr currentExStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE);
        long newExStyle = currentExStyle.ToInt64() | (long)NativeMethods.WS_EX_TOOLWINDOW;
        NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(newExStyle));

        RebuildMonitorList();

        var source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(NcHitTestHook);

        _vm.ScreenServices = new WpfScreenServices(this);

        _tray = new TrayIconService(this);
        _tray.IsVisible = _vm.ShowInTray;

        _export = new ExportService(this, _vm.Canvas, _vm.ScreenServices);

        _hotkeys = new HotkeyManager(_hwnd);
        source?.AddHook(_hotkeys.WndProc);
        UpdateGlobalHotkeys();

        const uint VK_D = 0x44, VK_Z = 0x5A, VK_Y = 0x59, VK_C = 0x43;
        const uint CTRL_ALT = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;

        _hotkeys.Register(1, CTRL_ALT, VK_D, () =>
        {
            if (IsVisible) Hide(); else { Show(); Activate(); }
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
    /// Per-pixel click-through: клики пропускаем сквозь
    /// вернуть рисование)
    /// </summary>
    private IntPtr NcHitTestHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.WM_NCHITTEST) return IntPtr.Zero;

        int lp = lParam.ToInt32();
        int sx = (short)(lp & 0xFFFF);
        int sy = (short)((lp >> 16) & 0xFFFF);

        var local = PointFromScreen(new System.Windows.Point(sx, sy));

        bool overInteractive = false;

        if (_vm.ActiveTool != ToolType.None)
        {
            overInteractive = true;
        }

        if (_vm.EditingText is not null) overInteractive = true;

        // Проверка: находится ли мышь над реальным положением перетащенной панели
        if (ToolbarHost != null && ToolbarHost.IsDescendantOf(this) && ToolbarHost.ActualWidth > 0)
        {
            var bounds = ToolbarHost.TransformToAncestor(this).TransformBounds(new Rect(0, 0, ToolbarHost.ActualWidth, ToolbarHost.ActualHeight));
            bounds.Inflate(20, 20);
            if (bounds.Contains(local)) overInteractive = true;
        }

        handled = true;
        return overInteractive
            ? new IntPtr(NativeMethods.HTCLIENT)
            : new IntPtr(NativeMethods.HTTRANSPARENT);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedMonitor))
        {
            ApplyMonitorSelection();
        }
        else if (e.PropertyName == nameof(MainViewModel.ToolbarTranslucent))
        {
            UpdateToolbarState();
        }
        else if (e.PropertyName == nameof(MainViewModel.EditingText))
        {
            ShowOrHideTextEditor();
        }
        else if (e.PropertyName == nameof(MainViewModel.ActiveTool))
        {
            UpdateEyedropperOverlayVisibility();
            UpdateCursorForActiveTool();
        }
        else if (e.PropertyName == nameof(MainViewModel.ShowInTray))
        {
            if (_tray != null) _tray.IsVisible = _vm.ShowInTray;
        }
    }

    private void UpdateEyedropperOverlayVisibility()
    {
        EyedropperOverlay.Visibility = _vm.ActiveTool == ToolType.Eyedropper
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// Меняет курсор холста (Surface) в зависимости от выбранного инструмента.
    /// На кнопках toolbar курсор остаётся Hand из стиля — Cursor родительского
    /// окна не наследуется насильно через ChildrenInheritCursor
    /// </summary>
    private void UpdateCursorForActiveTool()
    {
        var cursor = _vm.ActiveTool switch
        {
            ToolType.Pencil => Cursors.Pen,
            ToolType.Brush => Cursors.Pen,
            ToolType.Marker => Cursors.Pen,
            ToolType.Eraser => Cursors.Cross,
            ToolType.KnifeDelete => Cursors.Cross,
            ToolType.Move => Cursors.SizeAll,
            ToolType.Text => Cursors.IBeam,
            ToolType.Rectangle => Cursors.Cross,
            ToolType.Ellipse => Cursors.Cross,
            ToolType.Line => Cursors.Cross,
            ToolType.Arrow => Cursors.Cross,
            ToolType.Blur => Cursors.Cross,
            ToolType.Eyedropper => Cursors.Cross,
            ToolType.AreaFill => Cursors.Cross,
            _ => Cursors.Arrow
        };
        Surface.Cursor = cursor;
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

    /// <summary>
    /// Ctrl+колесо мыши в открытом редакторе текста меняет размер шрифта
    /// существующей фигуры. Вне Ctrl — обычная прокрутка TextBox
    /// </summary>
    private void TextEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
        if (_vm.EditingText is null) return;

        float delta = e.Delta > 0 ? 2f : -2f;
        _vm.ChangeEditingFontSize(delta);

        if (_vm.EditingText is { } ts)
        {
            TextEditor.FontSize = ts.FontSize;
            Canvas.SetTop(TextEditor, ts.Position.Y - ts.FontSize * 1.1);
        }
        e.Handled = true;
    }

    // ---- Экспорт ----
    internal void SaveCanvasAs() => _export?.SaveAs();
    internal void CopyCanvasToClipboard() => _export?.CopyToClipboard();

    // ---- Toolbar auto-show ----
    private bool _isHoveringToolbar;

    private void OnAnyMouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(this);

        bool overToolbar = false;

        if (ToolbarHost != null && ToolbarHost.IsDescendantOf(this) && ToolbarHost.ActualWidth > 0)
        {
            var bounds = ToolbarHost.TransformToAncestor(this).TransformBounds(new Rect(0, 0, ToolbarHost.ActualWidth, ToolbarHost.ActualHeight));
            bounds.Inflate(20, 20);
            overToolbar = bounds.Contains(p);
        }

        if (overToolbar != _isHoveringToolbar)
        {
            _isHoveringToolbar = overToolbar;
            UpdateToolbarState();
        }

        if (_vm.ActiveTool == ToolType.Eyedropper && _vm.ScreenServices is not null)
        {
            var screen = PointToScreen(p);
            int cx = (int)screen.X;
            int cy = (int)screen.Y;

            // Регион 5x5
            var pixels = _vm.ScreenServices.CaptureLiveRegionBgra(cx - 2, cy - 2, 5, 5);

            if (pixels.Length == 100)
            {
                for (int i = 0; i < 25; i++)
                {
                    int offset = i * 4;
                    byte b = pixels[offset];
                    byte g = pixels[offset + 1];
                    byte r = pixels[offset + 2];
                    byte a = pixels[offset + 3];

                    var rect = (System.Windows.Shapes.Rectangle)PixelGrid.Children[i];
                    var brush = (System.Windows.Media.SolidColorBrush)rect.Fill;
                    brush.Color = System.Windows.Media.Color.FromArgb(a, r, g, b);
                }
            }

            Canvas.SetLeft(EyedropperPreview, p.X);
            Canvas.SetTop(EyedropperPreview, p.Y);
        }
    }

    private void UpdateToolbarState(bool animated = true)
    {
        ToolbarHost.IsHitTestVisible = true;

        double target = (_isHoveringToolbar || !_vm.ToolbarTranslucent) ? 1.0 : 0.45;

        if (animated)
        {
            ToolbarHost.BeginAnimation(OpacityProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(120)));
        }
        else
        {
            ToolbarHost.BeginAnimation(OpacityProperty, null);
            ToolbarHost.Opacity = target;
        }
    }

    // ---- Hotkeys (когда окно в фокусе) ----

    public void UpdateGlobalHotkeys()
    {
        if (_hotkeys == null) return;

        _hotkeys.ClearAll();

        var h = _vm.Settings.Hotkeys;

        if (h.TryGetValue("ToggleOverlay", out var c1))
            _hotkeys.Register(1, c1.Modifiers, c1.VirtualKey, () => { if (IsVisible) Hide(); else { Show(); Activate(); } });

        if (h.TryGetValue("Undo", out var c3))
            _hotkeys.Register(3, c3.Modifiers, c3.VirtualKey, () => { if (_vm.UndoCommand.CanExecute(null)) _vm.UndoCommand.Execute(null); });

        if (h.TryGetValue("Redo", out var c4))
            _hotkeys.Register(4, c4.Modifiers, c4.VirtualKey, () => { if (_vm.RedoCommand.CanExecute(null)) _vm.RedoCommand.Execute(null); });

        if (h.TryGetValue("Copy", out var c5))
            _hotkeys.Register(5, c5.Modifiers, c5.VirtualKey, () => CopyCanvasToClipboard());
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm.EditingText is not null) return;

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

        var modifiers = Keyboard.Modifiers;
        string pressedCombo = "";

        if (modifiers.HasFlag(ModifierKeys.Control)) pressedCombo += "Ctrl + ";
        if (modifiers.HasFlag(ModifierKeys.Alt)) pressedCombo += "Alt + ";
        if (modifiers.HasFlag(ModifierKeys.Shift)) pressedCombo += "Shift + ";

        pressedCombo += key.ToString();

        foreach (var kvp in _vm.Settings.ToolHotkeys)
        {
            if (kvp.Value == pressedCombo && Enum.TryParse<ToolType>(kvp.Key, out var tool))
            {
                _vm.ActiveTool = tool;
                e.Handled = true;
                return;
            }
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_vm.RememberToolbarPosition)
        {
            _vm.Settings.ToolbarOffsetX = ToolbarTransform.X;
            _vm.Settings.ToolbarOffsetY = ToolbarTransform.Y;
            _vm.Settings.Save();
        }

        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _hotkeys?.Dispose(); _hotkeys = null;
        _tray?.Dispose(); _tray = null;
    }

    // ---- Перетаскивание Toolbar ----

    private bool _isDraggingToolbar;
    private System.Windows.Point _toolbarDragStart;

    private void ToolbarHost_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm.IsToolbarLocked) return;
        if (IsInsidePopup(e.OriginalSource as DependencyObject)) return;

        _isDraggingToolbar = true;
        _toolbarDragStart = e.GetPosition(this);
        ToolbarHost.CaptureMouse();
        e.Handled = true;
    }

    /// <summary>
    /// Поднимается по визуальному и логическому дереву от <paramref name="d"/>
    /// и возвращает true, если по пути встречается элемент <see cref="System.Windows.Controls.Primitives.Popup"/>
    /// </summary>
    private static bool IsInsidePopup(DependencyObject? d)
    {
        while (d is not null)
        {
            if (d is System.Windows.Controls.Primitives.Popup) return true;

            var parent = d is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(d)
                : null;

            parent ??= LogicalTreeHelper.GetParent(d);
            d = parent;
        }
        return false;
    }

    private void ToolbarHost_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingToolbar)
        {
            var p = e.GetPosition(this);
            var dx = p.X - _toolbarDragStart.X;
            var dy = p.Y - _toolbarDragStart.Y;
            _toolbarDragStart = p;

            ToolbarTransform.X += dx;
            ToolbarTransform.Y += dy;
        }
    }

    private void ToolbarHost_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingToolbar)
        {
            _isDraggingToolbar = false;
            ToolbarHost.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    // ---- Multi-monitor ----

    /// <summary>
    /// Перечисляет физические мониторы и заполняет MainViewModel.Monitors.
    /// Координаты конвертируются из пикселей в DIP (WPF logical units),
    /// чтобы их можно было присвоить Window.Left/Top/Width/Height
    /// </summary>
    private void RebuildMonitorList()
    {
        var dpi = GetDpiScale();
        _vm.Monitors.Clear();

        var allMonitors = new MonitorInfo
        {
            Index = -1,
            DisplayName = (TryFindResource("Mon_All") as string) ?? "Все мониторы",
            Left = SystemParameters.VirtualScreenLeft,
            Top = SystemParameters.VirtualScreenTop,
            Width = SystemParameters.VirtualScreenWidth,
            Height = SystemParameters.VirtualScreenHeight,
            IsAllMonitors = true,
            IsPrimary = false
        };
        _vm.Monitors.Add(allMonitors);

        var screens = WinFormsScreen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            var caption = (TryFindResource("Mon_Display") as string) ?? "Монитор";
            var primaryTag = s.Primary ? " (" + ((TryFindResource("Mon_Primary") as string) ?? "Основной") + ")" : "";

            _vm.Monitors.Add(new MonitorInfo
            {
                Index = i,
                DisplayName = $"{caption} {i + 1}{primaryTag}  —  {s.Bounds.Width}×{s.Bounds.Height}",
                Left = s.Bounds.Left / dpi.X,
                Top = s.Bounds.Top / dpi.Y,
                Width = s.Bounds.Width / dpi.X,
                Height = s.Bounds.Height / dpi.Y,
                IsPrimary = s.Primary,
                IsAllMonitors = false
            });
        }

        _vm.SelectedMonitor = _vm.Monitors.FirstOrDefault(m => m.IsPrimary) ?? allMonitors;
    }

    /// <summary>
    /// Перемещает/растягивает overlay так, чтобы он покрывал выбранный
    /// MonitorInfo. Сбрасывает оффсет перетащенного toolbar, чтобы
    /// он не уехал "за пределы" нового экрана
    /// </summary>
    private void ApplyMonitorSelection()
    {
        var m = _vm.SelectedMonitor;
        if (m is null) return;

        if (WindowState != WindowState.Normal) WindowState = WindowState.Normal;

        Left = m.Left;
        Top = m.Top;
        Width = m.Width;
        Height = m.Height;

        if (!_isInitializing)
        {
            ToolbarTransform.X = 0;
            ToolbarTransform.Y = 0;
        }
        else if (_vm.RememberToolbarPosition)
        {
            ToolbarTransform.X = _vm.Settings.ToolbarOffsetX;
            ToolbarTransform.Y = _vm.Settings.ToolbarOffsetY;
        }

        _vm.IsMonitorPickerOpen = false;
    }

    /// <summary>
    /// Возвращает текущий DPI-scale окна (для конвертации пиксели -> DIP)
    /// </summary>
    private (double X, double Y) GetDpiScale()
    {
        try
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var sx = dpi.DpiScaleX <= 0 ? 1.0 : dpi.DpiScaleX;
            var sy = dpi.DpiScaleY <= 0 ? 1.0 : dpi.DpiScaleY;
            return (sx, sy);
        }
        catch
        {
            return (1.0, 1.0);
        }
    }
}