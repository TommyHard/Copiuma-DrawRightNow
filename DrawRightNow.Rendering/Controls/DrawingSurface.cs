using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.Core.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Input;

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

    // ---- Ctrl+LMB изменение размера кисти ----
    private bool _adjustingBrush;
    private System.Windows.Point _brushAdjustStart;   // позиция начала жеста (в координатах Surface)
    private float _brushStartWidth;                   // StrokeWidth при начале жеста
    private float _brushPreviewWidth;                 // текущая "превью" ширина

    private PointF _hoverPosition;
    private bool _isMouseInside;

    /// <summary>
    /// Минимальная и максимальная ширина кисти при подгонке
    /// </summary>
    private const float BrushMin = 1f;
    private const float BrushMax = 64f;

    /// <summary>
    /// Сколько пикселей сдвига даёт +1 к ширине кисти. Меньше = чувствительнее
    /// </summary>
    private const float BrushAdjustPixelsPerUnit = 2f;

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
            old.PropertyChanged -= self.OnViewModelPropertyChanged;
        }

        if (e.NewValue is MainViewModel @new)
        {
            @new.Canvas.Changed += self.OnCanvasChanged;
            @new.PropertyChanged += self.OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ActiveTool) ||
            e.PropertyName == nameof(MainViewModel.StrokeWidth))
        {
            InvalidateVisual();
        }
    }

    private void OnCanvasChanged(object? sender, EventArgs e) => InvalidateVisual();

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        _isMouseInside = true;
        if (ViewModel?.ActiveTool == ToolType.Eraser) InvalidateVisual();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _isMouseInside = false;
        if (ViewModel?.ActiveTool == ToolType.Eraser) InvalidateVisual();
    }

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

        if (_adjustingBrush)
        {
            using var fill = new SKPaint
            {
                Color = new SKColor(0x34, 0x98, 0xDB, 0x40),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            using var stroke = new SKPaint
            {
                Color = new SKColor(0xFF, 0xFF, 0xFF, 0xE0),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f
            };
            float r = _brushPreviewWidth * 0.5f;
            canvas.DrawCircle((float)_brushAdjustStart.X, (float)_brushAdjustStart.Y, r, fill);
            canvas.DrawCircle((float)_brushAdjustStart.X, (float)_brushAdjustStart.Y, r, stroke);

            using var font = new SKFont(SKTypeface.Default, 13f) { Edging = SKFontEdging.SubpixelAntialias };
            using var textPaint = new SKPaint(font)
            {
                Color = SKColors.White,
                IsAntialias = true
            };
            var label = $"{_brushPreviewWidth:0.#} px";
            canvas.DrawText(label, (float)_brushAdjustStart.X + r + 8, (float)_brushAdjustStart.Y + 4, textPaint);
        }
        else if (vm.ActiveTool == ToolType.Eraser && _isMouseInside)
        {
            // Отрисовка превью зоны Eraze
            using var fill = new SKPaint
            {
                Color = new SKColor(255, 100, 100, 30),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            using var stroke = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 200),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f
            };

            float r = vm.StrokeWidth * 0.5f;

            canvas.DrawCircle(_hoverPosition.X, _hoverPosition.Y, r, fill);
            canvas.DrawCircle(_hoverPosition.X, _hoverPosition.Y, r, stroke);
        }
    }

    // ---- Mouse input ----

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton != MouseButton.Left) return;
        var vm = ViewModel; if (vm is null) return;

        var local = e.GetPosition(this);

        // Двойной клик инструментом Text по существующей TextShape -> редактирование
        if (e.ClickCount >= 1 && vm.ActiveTool == ToolType.Text)
        {
            var hit = vm.HitTestTextAt(ToCanvas(local), tolerance: 6f);
            if (hit is not null)
            {
                vm.BeginEditExisting(hit);
                e.Handled = true;
                return;
            }
        }

        // Ctrl+ЛКМ -> подгонка размера кисти. Сохраняем стартовую
        // ширину и точку якоря, перехватываем мышь, перерисовываем превью
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _adjustingBrush = true;
            _brushAdjustStart = local;
            _brushStartWidth = vm.StrokeWidth;
            _brushPreviewWidth = vm.StrokeWidth;
            CaptureMouse();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        CaptureMouse();
        var screen = PointToScreen(local);
        vm.OnInputDown(ToCanvas(local), ToCanvas(screen));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var vm = ViewModel; if (vm is null) return;

        var local = e.GetPosition(this);
        _hoverPosition = ToCanvas(local);

        if (_adjustingBrush)
        {
            var p = e.GetPosition(this);
            var dx = (float)(p.X - _brushAdjustStart.X);
            var newWidth = _brushStartWidth + dx / BrushAdjustPixelsPerUnit;
            if (newWidth < BrushMin) newWidth = BrushMin;
            if (newWidth > BrushMax) newWidth = BrushMax;
            _brushPreviewWidth = newWidth;
            vm.StrokeWidth = newWidth;
            InvalidateVisual();
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (vm.ActiveTool == ToolType.Eraser)
            {
                InvalidateVisual();
            }
            return;
        }

        var screen = PointToScreen(local);
        bool constrain = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        vm.OnInputMove(ToCanvas(local), ToCanvas(screen), constrain);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton != MouseButton.Left) return;
        var vm = ViewModel; if (vm is null) return;

        // Завершаем режим подгонки кисти — отпускаем capture и убираем preview
        if (_adjustingBrush)
        {
            _adjustingBrush = false;
            if (IsMouseCaptured) ReleaseMouseCapture();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

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