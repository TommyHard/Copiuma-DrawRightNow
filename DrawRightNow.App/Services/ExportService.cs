using DrawRightNow.Core.Models;
using DrawRightNow.Core.Services;
using DrawRightNow.Rendering;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Clipboard = System.Windows.Clipboard;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace DrawRightNow.App.Services;

public sealed class ExportService
{
    private readonly Window _overlay;
    private readonly CanvasModel _canvas;
    private readonly IScreenServices _screenServices;

    public ExportService(Window overlay, CanvasModel canvas, IScreenServices screenServices)
    {
        _overlay = overlay;
        _canvas = canvas;
        _screenServices = screenServices;
    }

    public bool SaveAs()
    {
        var dlg = new SaveFileDialog
        {
            FileName = $"DrawRightNow_{DateTime.Now:yyyyMMdd_HHmmss}.png",
            Filter = "PNG|*.png|JPEG|*.jpg;*.jpeg|PNG (с фоном)|*.png|JPEG (с фоном)|*.jpg;*.jpeg",
            AddExtension = true
        };
        if (dlg.ShowDialog(_overlay) != true) return false;

        var (w, h) = GetCanvasPixelSize();
        var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();

        bool withBackground = dlg.FilterIndex == 3 || dlg.FilterIndex == 4;
        byte[]? bgraBg = null;

        if (withBackground && _screenServices != null)
        {
            Thread.Sleep(150);

            var left = (int)_overlay.Left;
            var top = (int)_overlay.Top;
            bgraBg = _screenServices.CaptureRegionBgra(left, top, w, h);
        }

        bool isJpeg = dlg.FilterIndex == 2 || dlg.FilterIndex == 4 || ext is ".jpg" or ".jpeg";

        byte[] bytes = isJpeg
            ? CanvasExporter.EncodeJpeg(_canvas, w, h, bgraBg)
            : CanvasExporter.EncodePng(_canvas, w, h, bgraBg);

        File.WriteAllBytes(dlg.FileName, bytes);
        return true;
    }

    public void CopyToClipboard()
    {
        var (w, h) = GetCanvasPixelSize();
        var bytes = CanvasExporter.EncodePng(_canvas, w, h);

        using var ms = new MemoryStream(bytes);
        var decoder = new PngBitmapDecoder(ms,
            BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        Clipboard.SetImage(decoder.Frames[0]);
    }

    private (int W, int H) GetCanvasPixelSize()
    {
        var w = (int)Math.Max(1, _overlay.ActualWidth);
        var h = (int)Math.Max(1, _overlay.ActualHeight);
        return (w, h);
    }
}