using DrawRightNow.Core.Models;
using DrawRightNow.Core.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace DrawRightNow.App.Views;

public partial class ColorPickerView : UserControl
{
    private bool _isDraggingWheel;
    private bool _isDraggingValue;
    private bool _isDraggingAlpha;
    private bool _isUpdatingFromVM;

    private ColorHsv _currentHsv;

    public ColorPickerView()
    {
        InitializeComponent();
        WheelImage.Source = GenerateColorWheel(120);
        DataContextChanged += OnDataContextChanged;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e) => UpdateUIFromViewModel();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm) oldVm.PropertyChanged -= Vm_PropertyChanged;
        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += Vm_PropertyChanged;
            UpdateUIFromViewModel();
        }
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.StrokeColor) && !_isDraggingWheel && !_isDraggingValue && !_isDraggingAlpha)
            UpdateUIFromViewModel();
    }

    // --- Кнопки и поля ввода ---
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.IsColorPickerOpen = false;
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            Keyboard.ClearFocus();
        }
    }

    // --- Отрисовка selector'а ---
    private WriteableBitmap GenerateColorWheel(int size)
    {
        var wb = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new uint[size * size];
        double radius = size / 2.0;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x - radius;
                double dy = y - radius;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= radius)
                {
                    double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
                    if (angle < 0) angle += 360;
                    double saturation = distance / radius;
                    var c = new ColorHsv(angle, saturation, 1.0).ToRgba();
                    pixels[y * size + x] = (uint)((255 << 24) | (c.R << 16) | (c.G << 8) | c.B);
                }
            }
        }
        wb.WritePixels(new Int32Rect(0, 0, size, size), pixels, size * 4, 0);
        return wb;
    }

    // --- Поведение мыши ---
    private void Control_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingWheel = _isDraggingValue = _isDraggingAlpha = false;
        Mouse.Capture(null);
    }

    private void Wheel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingWheel = true;
        WheelImage.CaptureMouse();
        UpdateColorFromWheel(e.GetPosition(WheelImage));
    }
    private void Wheel_MouseMove(object sender, MouseEventArgs e) { if (_isDraggingWheel) UpdateColorFromWheel(e.GetPosition(WheelImage)); }

    private void Value_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingValue = true;
        ValueTrack.CaptureMouse();
        UpdateColorFromValue(e.GetPosition(ValueTrack));
    }
    private void Value_MouseMove(object sender, MouseEventArgs e) { if (_isDraggingValue) UpdateColorFromValue(e.GetPosition(ValueTrack)); }

    private void Alpha_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingAlpha = true;
        AlphaTrack.CaptureMouse();
        UpdateColorFromAlpha(e.GetPosition(AlphaTrack));
    }
    private void Alpha_MouseMove(object sender, MouseEventArgs e) { if (_isDraggingAlpha) UpdateColorFromAlpha(e.GetPosition(AlphaTrack)); }

    // --- Обновление цвета из мыши ---
    private void UpdateColorFromWheel(Point p)
    {
        double x = (p.X / 60.0) - 1;
        double y = (p.Y / 60.0) - 1;
        double radius = Math.Sqrt(x * x + y * y);
        if (radius > 1) { x /= radius; y /= radius; radius = 1; }

        double angle = Math.Atan2(y, x) * (180 / Math.PI);
        if (angle < 0) angle += 360;

        SetViewModelColor(new ColorHsv(angle, radius, _currentHsv.V, _currentHsv.A));
    }

    private void UpdateColorFromValue(Point p)
    {
        double v = Math.Clamp(1.0 - (p.Y / 120.0), 0, 1);
        SetViewModelColor(new ColorHsv(_currentHsv.H, _currentHsv.S, v, _currentHsv.A));
    }

    private void UpdateColorFromAlpha(Point p)
    {
        double a = Math.Clamp(1.0 - (p.Y / 120.0), 0, 1);
        SetViewModelColor(new ColorHsv(_currentHsv.H, _currentHsv.S, _currentHsv.V, a));
    }

    private void SetViewModelColor(ColorHsv hsv)
    {
        if (DataContext is not MainViewModel vm) return;
        _isUpdatingFromVM = true;
        _currentHsv = hsv;
        vm.StrokeColor = hsv.ToRgba();
        _isUpdatingFromVM = false;
        UpdateUIElements(hsv, vm.StrokeColor);
    }

    // --- Внешнее обновление (Изменили текст, etc.) ---
    private void UpdateUIFromViewModel()
    {
        if (_isUpdatingFromVM || DataContext is not MainViewModel vm) return;

        var color = vm.StrokeColor;
        var newHsv = ColorHsv.FromRgba(color);

        if (newHsv.S == 0 && _currentHsv.S > 0) newHsv = new ColorHsv(_currentHsv.H, newHsv.S, newHsv.V, newHsv.A);
        if (newHsv.V == 0 && _currentHsv.V > 0) newHsv = new ColorHsv(_currentHsv.H, _currentHsv.S, newHsv.V, newHsv.A);

        _currentHsv = newHsv;
        UpdateUIElements(newHsv, color);
    }

    private void UpdateUIElements(ColorHsv hsv, ColorRgba color)
    {
        double angleRad = hsv.H * Math.PI / 180.0;
        double r = hsv.S * 60;
        Canvas.SetLeft(WheelSelector, Math.Cos(angleRad) * r + 60);
        Canvas.SetTop(WheelSelector, Math.Sin(angleRad) * r + 60);

        double valueY = (1.0 - hsv.V) * 120.0;
        Canvas.SetTop(ValueThumbL, valueY);
        Canvas.SetTop(ValueThumbR, valueY);

        double alphaY = (1.0 - hsv.A) * 120.0;
        Canvas.SetTop(AlphaThumbL, alphaY);
        Canvas.SetTop(AlphaThumbR, alphaY);

        var baseColor = new ColorHsv(hsv.H, hsv.S, 1.0).ToRgba();
        ValueGradStart.Color = Color.FromRgb(baseColor.R, baseColor.G, baseColor.B);

        AlphaGradStart.Color = Color.FromArgb(255, color.R, color.G, color.B);
        AlphaGradEnd.Color = Color.FromArgb(0, color.R, color.G, color.B);

        PreviewBorder.Background = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
    }
}