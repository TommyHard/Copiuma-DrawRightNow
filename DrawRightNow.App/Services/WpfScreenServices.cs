using DrawRightNow.Core.Models;
using DrawRightNow.Core.Services;
using DrawRightNow.Interop;
using System.Windows;
using System.Windows.Threading;

namespace DrawRightNow.App.Services;

/// <summary>
/// WPF-обвязка над Interop.ScreenCapture. Главная задача — на время
/// захвата региона скрывать overlay-окно, чтобы собственные штрихи
/// не попадали в кадр
/// </summary>
public sealed class WpfScreenServices : IScreenServices
{
    private readonly Window _overlay;

    public WpfScreenServices(Window overlay)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
    }

    public ColorRgba GetPixel(int screenX, int screenY)
    {
        var (r, g, b) = ScreenCapture.GetPixel(screenX, screenY);
        return new ColorRgba(r, g, b, 0xFF);
    }

    public byte[] CaptureRegionBgra(int screenX, int screenY, int width, int height)
    {
        var prevOpacity = _overlay.Opacity;
        _overlay.Opacity = 0.0;

        Flush(DispatcherPriority.Render);
        Flush(DispatcherPriority.Loaded);
        Thread.Sleep(30);

        byte[] pixels;
        try
        {
            pixels = ScreenCapture.CaptureRegion(screenX, screenY, width, height);
        }
        finally
        {
            _overlay.Opacity = prevOpacity;
        }
        return pixels;
    }

    private void Flush(DispatcherPriority priority)
    {
        _overlay.Dispatcher.Invoke(() => { }, priority);
    }

    public byte[] CaptureLiveRegionBgra(int screenX, int screenY, int width, int height)
    {
        return ScreenCapture.CaptureRegion(screenX, screenY, width, height);
    }
}