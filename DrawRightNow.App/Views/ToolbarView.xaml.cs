using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace DrawRightNow.App.Views;

public partial class ToolbarView : UserControl
{
    public ToolbarView() => InitializeComponent();

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