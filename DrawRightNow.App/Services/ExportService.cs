using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using DrawRightNow.Core.Models;
using DrawRightNow.Rendering;
using Microsoft.Win32;
using Clipboard = System.Windows.Clipboard;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace DrawRightNow.App.Services;

/// <summary>
/// WPF-обвязка экспорта холста: SaveFileDialog + Clipboard
/// Внутреннюю растеризацию делает CanvasExporter из проекта Rendering
/// </summary>
public sealed class ExportService
{
    private readonly Window _overlay;
    private readonly CanvasModel _canvas;

    public ExportService(Window overlay, CanvasModel canvas)
    {
        _overlay = overlay;
        _canvas = canvas;
    }

    /// <summary>
    /// PNG (прозрачный) либо JPEG (с белым фоном) — выбирается по расширению
    /// </summary>
    public bool SaveAs()
    {
        var dlg = new SaveFileDialog
        {
            FileName = $"DrawRightNow_{DateTime.Now:yyyyMMdd_HHmmss}.png",
            Filter = "PNG (прозрачный фон)|*.png|JPEG (белый фон)|*.jpg;*.jpeg",
            AddExtension = true
        };
        if (dlg.ShowDialog(_overlay) != true) return false;

        var (w, h) = GetCanvasPixelSize();
        var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
        byte[] bytes = ext is ".jpg" or ".jpeg"
            ? CanvasExporter.EncodeJpeg(_canvas, w, h)
            : CanvasExporter.EncodePng(_canvas, w, h);

        File.WriteAllBytes(dlg.FileName, bytes);
        return true;
    }

    /// <summary>
    /// Копирование PNG-снимка холста в системный буфер обмена
    /// </summary>
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