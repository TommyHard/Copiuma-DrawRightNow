using System;
using DrawRightNow.Core.Models;
using SkiaSharp;

namespace DrawRightNow.Rendering;

/// <summary>
/// Снимок всего CanvasModel в SKImage и сохранение в PNG/JPG.
/// Renderer переиспользуется — никаких побочных эффектов на ту копию,
/// что в DrawingSurface (создаём свой instance)
/// </summary>
public static class CanvasExporter
{
    /// <summary>
    /// Снимок canvas заданного размера в SKImage. На прозрачном фоне.
    /// Caller отвечает за Dispose возвращаемого SKImage
    /// </summary>
    public static SKImage Snapshot(CanvasModel canvas, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("SKSurface.Create вернул null");

        var skCanvas = surface.Canvas;
        skCanvas.Clear(SKColors.Transparent);

        var renderer = new SkiaShapeRenderer();
        foreach (var shape in canvas.Shapes)
            renderer.Draw(skCanvas, shape);

        return surface.Snapshot();
    }

    /// <summary>
    /// PNG-байты (прозрачный фон, без потерь)
    /// </summary>
    public static byte[] EncodePng(CanvasModel canvas, int width, int height)
    {
        using var img = Snapshot(canvas, width, height);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// JPEG-байты (белый фон, т.к. формат не поддерживает alpha)
    /// </summary>
    public static byte[] EncodeJpeg(CanvasModel canvas, int width, int height, int quality = 92)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("SKSurface.Create вернул null");

        var sk = surface.Canvas;
        sk.Clear(SKColors.White);
        var renderer = new SkiaShapeRenderer();
        foreach (var s in canvas.Shapes) renderer.Draw(sk, s);

        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }
}