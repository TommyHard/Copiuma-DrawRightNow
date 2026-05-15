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

        // Глобальные горячие клавиши (для активного приложения)
        InputBindings.Add(new KeyBinding(_vm.UndoCommand, new KeyGesture(Key.Z, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(_vm.RedoCommand, new KeyGesture(Key.Y, ModifierKeys.Control)));
    }

    // ---- HWND / extended styles ----

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        OverlayWindowHelper.Apply(_hwnd, clickThrough: !_vm.IsDrawingEnabled);
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
        switch (e.Key)
        {
            case Key.P: _vm.ActiveTool = ToolType.Pencil; e.Handled = true; break;
            case Key.B: _vm.ActiveTool = ToolType.Brush; e.Handled = true; break;
            case Key.M: _vm.ActiveTool = ToolType.Marker; e.Handled = true; break;
            case Key.E: _vm.ActiveTool = ToolType.Eraser; e.Handled = true; break;
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