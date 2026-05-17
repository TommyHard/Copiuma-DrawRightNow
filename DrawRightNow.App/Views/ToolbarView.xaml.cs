using DrawRightNow.Core.ViewModels;
using System.Windows;
using System.Windows.Input;
using UserControl = System.Windows.Controls.UserControl;

namespace DrawRightNow.App.Views;

public partial class ToolbarView : UserControl
{
    private OptionsView? _optionsWindow;
    private System.DateTime _colorPickerClosedTime = System.DateTime.MinValue;

    public ToolbarView() => InitializeComponent();

    private void OptionsButton_Click(object sender, RoutedEventArgs e)
    {
        // Если уже открыто — просто делаем его активным
        if (_optionsWindow != null && _optionsWindow.IsLoaded)
        {
            _optionsWindow.Activate();
            return;
        }

        _optionsWindow = new OptionsView { DataContext = this.DataContext };

        // Привязываем к главному окну, чтобы Опции отображались поверх Overlay
        var owner = Window.GetWindow(this);
        if (owner != null) _optionsWindow.Owner = owner;

        _optionsWindow.Show();
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