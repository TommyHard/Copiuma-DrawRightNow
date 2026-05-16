using System.Windows;
using DrawRightNow.App.Services;
using Application = System.Windows.Application;

namespace DrawRightNow.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        LocalizationManager.Initialize();
        base.OnStartup(e);
    }
}