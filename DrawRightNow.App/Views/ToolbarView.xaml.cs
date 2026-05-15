using System.Windows;
using System.Windows.Controls;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.ViewModels;

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
}