using DrawRightNow.Core.Models;
using SkiaSharp;

namespace DrawRightNow.Rendering;

/// <summary>
/// Снимок всего CanvasModel в SKImage и сохранение в PNG/JPG
/// </summary>
public static class CanvasExporter
{
    /// <summary>
    /// Снимок canvas заданного размера в SKImage.
    /// Если передан bgraBackground, отрисовывается в качестве подложки
    /// </summary>
    public static SKImage Snapshot(CanvasModel canvas, int width, int height, byte[]? bgraBackground = null)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("SKSurface.Create вернул null");

        var skCanvas = surface.Canvas;

        // Если передан фон экрана - рисуем
        if (bgraBackground != null && bgraBackground.Length > 0)
        {
            var bgInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            using var bgData = SKData.CreateCopy(bgraBackground);
            using var bgImage = SKImage.FromPixelData(bgInfo, bgData, width * 4);
            if (bgImage != null)
            {
                skCanvas.DrawImage(bgImage, 0, 0);
            }
            else
            {
                skCanvas.Clear(SKColors.Transparent);
            }
        }
        else
        {
            skCanvas.Clear(SKColors.Transparent);
        }

        var renderer = new SkiaShapeRenderer();
        foreach (var shape in canvas.Shapes)
            renderer.Draw(skCanvas, shape);

        return surface.Snapshot();
    }

    /// <summary>
    /// PNG-байты (с фоном или прозрачный, без потерь)
    /// </summary>
    public static byte[] EncodePng(CanvasModel canvas, int width, int height, byte[]? bgraBackground = null)
    {
        using var img = Snapshot(canvas, width, height, bgraBackground);
        using var encodedData = img.Encode(SKEncodedImageFormat.Png, 100);
        return encodedData.ToArray();
    }

    /// <summary>
    /// JPEG-байты (с фоном экрана или белый фон по умолчанию)
    /// </summary>
    public static byte[] EncodeJpeg(CanvasModel canvas, int width, int height, byte[]? bgraBackground = null, int quality = 92)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("SKSurface.Create вернул null");

        var skCanvas = surface.Canvas;

        // Отрисовка фона для JPEG
        if (bgraBackground != null && bgraBackground.Length > 0)
        {
            var bgInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            using var bgData = SKData.CreateCopy(bgraBackground);
            using var bgImage = SKImage.FromPixelData(bgInfo, bgData, width * 4);
            if (bgImage != null)
            {
                skCanvas.DrawImage(bgImage, 0, 0);
            }
            else
            {
                skCanvas.Clear(SKColors.White);
            }
        }
        else
        {
            skCanvas.Clear(SKColors.White);
        }

        var renderer = new SkiaShapeRenderer();
        foreach (var s in canvas.Shapes)
            renderer.Draw(skCanvas, s);

        using var img = surface.Snapshot();
        using var encodedData = img.Encode(SKEncodedImageFormat.Jpeg, quality);
        return encodedData.ToArray();
    }
}