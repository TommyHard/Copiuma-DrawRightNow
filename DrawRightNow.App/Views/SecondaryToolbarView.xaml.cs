using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace DrawRightNow.App.Views;

public partial class SecondaryToolbarView : UserControl
{
    private OptionsView? _optionsWindow;

    public SecondaryToolbarView() => InitializeComponent();

    private void OptionsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_optionsWindow != null && _optionsWindow.IsLoaded)
        {
            _optionsWindow.Activate();
            return;
        }

        _optionsWindow = new OptionsView { DataContext = this.DataContext };
        var owner = Window.GetWindow(this);
        if (owner != null) _optionsWindow.Owner = owner;
        _optionsWindow.Show();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Window.GetWindow(this)?.Close();

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        => Window.GetWindow(this)?.Hide();
}