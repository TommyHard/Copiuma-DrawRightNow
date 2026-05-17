using DrawRightNow.App.Services;
using DrawRightNow.Core.Models;
using System.Windows;
using Application = System.Windows.Application;

namespace DrawRightNow.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var settings = AppSettings.Load();
        LocalizationManager.SetLanguage(settings.Language);

        base.OnStartup(e);
    }
}