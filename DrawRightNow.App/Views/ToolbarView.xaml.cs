using System.Windows;
using System.Windows.Controls;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.ViewModels;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace DrawRightNow.App.Views;

public partial class ToolbarView : UserControl
{
    public ToolbarView() => InitializeComponent();

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is not Button { Tag: string name }) return;

        vm.StrokeColor = name switch
        {
            "Red" => ColorRgba.Red,
            "Green" => ColorRgba.Green,
            "Blue" => ColorRgba.Blue,
            "Yellow" => ColorRgba.Yellow,
            "Black" => ColorRgba.Black,
            "White" => ColorRgba.White,
            _ => vm.StrokeColor
        };
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Window.GetWindow(this)?.Close();

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        => Window.GetWindow(this)?.Hide();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mw) mw.SaveCanvasAs();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mw) mw.CopyCanvasToClipboard();
    }

    private void Language_Click(object sender, RoutedEventArgs e)
        => DrawRightNow.App.Services.LocalizationManager.Toggle();
}