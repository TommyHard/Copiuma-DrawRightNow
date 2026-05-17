using System;
using System.Windows;
using System.Windows.Input;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace DrawRightNow.Rendering.Controls;

/// <summary>
/// WPF-контрол поверх SKElement. Перехватывает ввод мыши, транслирует его
/// в MainViewModel и реагирует на изменение модели перерисовкой
/// </summary>
public class DrawingSurface : SKElement
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(MainViewModel),
            typeof(DrawingSurface),
            new PropertyMetadata(null, OnViewModelChanged));

    private readonly SkiaShapeRenderer _renderer = new();

    public DrawingSurface()
    {
        IsHitTestVisible = true;
        Focusable = false;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (DrawingSurface)d;

        if (e.OldValue is MainViewModel old)
        {
            old.Canvas.Changed -= self.OnCanvasChanged;
            old.PropertyChanged -= self.OnVmPropertyChanged;
            self.UnsubscribeFromFrameProvider(old.FrameProvider);
        }

        if (e.NewValue is MainViewModel @new)
        {
            @new.Canvas.Changed += self.OnCanvasChanged;
            @new.PropertyChanged += self.OnVmPropertyChanged;
            self.SubscribeToFrameProvider(@new.FrameProvider);
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.FrameProvider)) return;
        SubscribeToFrameProvider(ViewModel?.FrameProvider);
    }

    private DrawRightNow.Core.Services.IFrameProvider? _subscribedProvider;
    private void SubscribeToFrameProvider(DrawRightNow.Core.Services.IFrameProvider? p)
    {
        if (ReferenceEquals(_subscribedProvider, p)) return;
        UnsubscribeFromFrameProvider(_subscribedProvider);
        _subscribedProvider = p;
        if (p is not null) p.FrameUpdated += OnFrameUpdated;
    }
    private void UnsubscribeFromFrameProvider(DrawRightNow.Core.Services.IFrameProvider? p)
    {
        if (p is null) return;
        p.FrameUpdated -= OnFrameUpdated;
        if (ReferenceEquals(_subscribedProvider, p)) _subscribedProvider = null;
    }

    private void OnFrameUpdated()
    {
        Dispatcher.BeginInvoke(InvalidateVisual, System.Windows.Threading.DispatcherPriority.Render);
    }

    private void OnCanvasChanged(object? sender, EventArgs e) => InvalidateVisual();

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var vm = ViewModel;
        if (vm is null) return;

        foreach (var shape in vm.Canvas.Shapes)
            _renderer.Draw(canvas, shape);

        if (vm.Canvas.Pending is { } pending)
            _renderer.Draw(canvas, pending);
    }

    // ---- Mouse input ----

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton != MouseButton.Left) return;
        var vm = ViewModel; if (vm is null) return;

        CaptureMouse();
        var local = e.GetPosition(this);
        var screen = PointToScreen(local);
        vm.OnInputDown(ToCanvas(local), ToCanvas(screen));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var vm = ViewModel; if (vm is null) return;

        var local = e.GetPosition(this);
        var screen = PointToScreen(local);
        bool constrain = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        vm.OnInputMove(ToCanvas(local), ToCanvas(screen), constrain);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton != MouseButton.Left) return;
        var vm = ViewModel; if (vm is null) return;

        var local = e.GetPosition(this);
        var screen = PointToScreen(local);
        bool constrain = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        vm.OnInputUp(ToCanvas(local), ToCanvas(screen), constrain);
        if (IsMouseCaptured) ReleaseMouseCapture();
        e.Handled = true;
    }

    private static PointF ToCanvas(System.Windows.Point p)
        => new((float)p.X, (float)p.Y);
}