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
            old.Canvas.Changed -= self.OnCanvasChanged;

        if (e.NewValue is MainViewModel @new)
            @new.Canvas.Changed += self.OnCanvasChanged;
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
        vm.OnInputMove(ToCanvas(local), ToCanvas(screen));
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton != MouseButton.Left) return;
        var vm = ViewModel; if (vm is null) return;

        var local = e.GetPosition(this);
        var screen = PointToScreen(local);
        vm.OnInputUp(ToCanvas(local), ToCanvas(screen));
        if (IsMouseCaptured) ReleaseMouseCapture();
        e.Handled = true;
    }

    private static PointF ToCanvas(System.Windows.Point p)
        => new((float)p.X, (float)p.Y);
}