using DrawRightNow.App.Services;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.Core.ViewModels;
using DrawRightNow.Interop;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;

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

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        _vm.PropertyChanged += OnViewModelPropertyChanged;

        SourceInitialized += OnSourceInitialized;
        MouseMove += OnAnyMouseMove;
        KeyDown += OnKeyDown;

        Loaded += (_, _) => HideToolbar(animated: false);
        Closing += OnClosing;

        InputBindings.Add(new KeyBinding(_vm.UndoCommand, new KeyGesture(Key.Z, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(_vm.RedoCommand, new KeyGesture(Key.Y, ModifierKeys.Control)));
    }

    // ---- HWND / extended styles ----

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        OverlayWindowHelper.Apply(_hwnd, clickThrough: !_vm.IsDrawingEnabled);

        _vm.ScreenServices = new WpfScreenServices(this);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsDrawingEnabled))
        {
            OverlayWindowHelper.SetClickThrough(_hwnd, enabled: !_vm.IsDrawingEnabled);
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

        Canvas.SetLeft(TextEditor, ts.Position.X);
        Canvas.SetTop(TextEditor, ts.Position.Y - ts.FontSize * 1.1);
        TextEditor.FontSize = ts.FontSize;
        TextEditor.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(ts.Color.A, ts.Color.R, ts.Color.G, ts.Color.B));
        TextEditor.Text = ts.Text;
        TextEditor.Visibility = Visibility.Visible;

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

    // ---- Toolbar auto-show ----

    private bool _toolbarVisible;

    private void HoverStrip_MouseEnter(object sender, MouseEventArgs e) => ShowToolbar();

    private void OnAnyMouseMove(object sender, MouseEventArgs e)
    {
        if (_vm.IsToolbarPinned) return;
        var p = e.GetPosition(this);
        var overToolbar = p.Y < (ToolbarHost.ActualHeight + ToolbarHost.Margin.Top + 12);
        if (_toolbarVisible && !overToolbar) HideToolbar();
    }

    private void ShowToolbar()
    {
        if (_toolbarVisible) return;
        _toolbarVisible = true;
        ToolbarHost.BeginAnimation(OpacityProperty,
            new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(120)));
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
    }
}