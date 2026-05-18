using DrawRightNow.App.Services;
using DrawRightNow.Core.Models;
using System.Windows;
using Application = System.Windows.Application;

namespace DrawRightNow.App;

public partial class App : Application
{
    // Глобальный mutex запрещает запуск второго экземпляра процесса
    private static Mutex? _singleInstanceMutex;
    private const string SingleInstanceMutexName =
        @"Global\Copiuma.DrawRightNow.SingleInstance.{B6F0C7D2-9C9A-4B40-9C5F-9C46A6E3D511}";

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            try { _singleInstanceMutex.Dispose(); } catch { }
            _singleInstanceMutex = null;
            Shutdown(0);
            return;
        }

        var settings = AppSettings.Load();
        LocalizationManager.SetLanguage(settings.Language);

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_singleInstanceMutex is not null)
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
        }
        catch (ApplicationException) { /* ignore */ }
        catch (ObjectDisposedException) { /* ignore */ }

        base.OnExit(e);
    }
}