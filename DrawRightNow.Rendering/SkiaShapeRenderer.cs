using System;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;
using SkiaSharp;

namespace DrawRightNow.Rendering;

/// <summary>
/// Платформо-независимый рендер: получает SKCanvas и любую IShape, рисует.
/// Конкретный хостинг (SKElement / OpenGL / Direct2D) живёт в Controls/
/// </summary>
public sealed class SkiaShapeRenderer
{
    private readonly SkiaPaintPool _pool = new();
    private readonly SKPath _scratch = new();
    // Кэш SKFont — типичных размеров шрифта десяток
    private readonly System.Collections.Generic.Dictionary<int, SKFont> _fonts = new();
    // Кэш SKImage для BlurShape (ключ — Guid фигуры; одна фигура — один image)
    private readonly System.Collections.Generic.Dictionary<Guid, SKImage> _blurImages = new();
    private SKTypeface? _typeface;

    public void Draw(SKCanvas canvas, IShape shape)
    {
        switch (shape)
        {
            case StrokeShape s: DrawStroke(canvas, s); break;
            case RectangleShape r: DrawRectangle(canvas, r); break;
            case EllipseShape e: DrawEllipse(canvas, e); break;
            case LineShape l: DrawLine(canvas, l); break;
            case ArrowShape a: DrawArrow(canvas, a); break;
            case TextShape t: DrawText(canvas, t); break;
            case BlurShape bl: DrawBlur(canvas, bl); break;
            case LiveBlurShape lb: DrawLiveBlur(canvas, lb); break;
        }
    }

    // ---- Штрихи ----

    private void DrawStroke(SKCanvas canvas, StrokeShape s)
    {
        if (s.Points.Count == 0) return;

        var (antialias, blend, color) = s.Kind switch
        {
            StrokeKind.Pencil => (false, SKBlendMode.SrcOver, ToSk(s.Color)),
            StrokeKind.Brush => (true, SKBlendMode.SrcOver, ToSk(s.Color)),
            StrokeKind.Marker => (true, SKBlendMode.Multiply, ToSk(s.Color.WithAlpha(0x80))),
            StrokeKind.Eraser => (true, SKBlendMode.DstOut, new SKColor(0, 0, 0, 0xFF)),
            _ => (true, SKBlendMode.SrcOver, ToSk(s.Color))
        };

        var paint = _pool.GetStroke(color, s.Width, antialias, blend, SKStrokeCap.Round);

        if (s.Points.Count == 1)
        {
            var p = s.Points[0];
            canvas.DrawPoint(p.X, p.Y, paint);
            return;
        }

        _scratch.Reset();
        var first = s.Points[0];
        _scratch.MoveTo(first.X, first.Y);
        for (int i = 1; i < s.Points.Count; i++)
        {
            var pt = s.Points[i];
            _scratch.LineTo(pt.X, pt.Y);
        }
        canvas.DrawPath(_scratch, paint);
    }

    // ---- Геометрические фигуры ----

    private void DrawRectangle(SKCanvas canvas, RectangleShape r)
    {
        var b = r.Bounds;
        var rect = new SKRect(b.Left, b.Top, b.Right, b.Bottom);
        if (r.Style.HasFill)
            canvas.DrawRect(rect, _pool.GetFill(ToSk(r.Style.FillColor), antialias: true));
        canvas.DrawRect(rect,
            _pool.GetStroke(ToSk(r.Style.StrokeColor), r.Style.StrokeWidth, true, SKBlendMode.SrcOver, SKStrokeCap.Square));
    }

    private void DrawEllipse(SKCanvas canvas, EllipseShape e)
    {
        var b = e.Bounds;
        var rect = new SKRect(b.Left, b.Top, b.Right, b.Bottom);
        if (e.Style.HasFill)
            canvas.DrawOval(rect, _pool.GetFill(ToSk(e.Style.FillColor), antialias: true));
        canvas.DrawOval(rect,
            _pool.GetStroke(ToSk(e.Style.StrokeColor), e.Style.StrokeWidth, true, SKBlendMode.SrcOver, SKStrokeCap.Round));
    }

    private void DrawLine(SKCanvas canvas, LineShape l)
    {
        var paint = _pool.GetStroke(
            ToSk(l.Style.StrokeColor), l.Style.StrokeWidth, true, SKBlendMode.SrcOver, SKStrokeCap.Round);
        canvas.DrawLine(l.Start.X, l.Start.Y, l.End.X, l.End.Y, paint);
    }

    private void DrawArrow(SKCanvas canvas, ArrowShape a)
    {
        var paint = _pool.GetStroke(
            ToSk(a.Style.StrokeColor), a.Style.StrokeWidth, true, SKBlendMode.SrcOver, SKStrokeCap.Round);

        canvas.DrawLine(a.Start.X, a.Start.Y, a.End.X, a.End.Y, paint);

        // Наконечник: две короткие линии под углом 25 от направления стрелки
        var dx = a.End.X - a.Start.X;
        var dy = a.End.Y - a.Start.Y;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1e-3f) return;

        // Размер наконечника зависит от длины (с минимумом и максимумом)
        var head = MathF.Min(MathF.Max(12f, a.Style.StrokeWidth * 4f), len * 0.35f);
        var ux = dx / len;
        var uy = dy / len;

        const float cos = 0.9063078f;  // cos 25
        const float sin = 0.4226183f;  // sin 25

        // Поворачиваем единичный вектор (ux,uy) на +-25 и берём его "назад" (минус)
        var lx = -(ux * cos + uy * sin);
        var ly = -(uy * cos - ux * sin);
        var rx = -(ux * cos - uy * sin);
        var ry = -(uy * cos + ux * sin);

        canvas.DrawLine(a.End.X, a.End.Y, a.End.X + lx * head, a.End.Y + ly * head, paint);
        canvas.DrawLine(a.End.X, a.End.Y, a.End.X + rx * head, a.End.Y + ry * head, paint);
    }

    // ---- Текст ----

    private void DrawText(SKCanvas canvas, TextShape t)
    {
        if (string.IsNullOrEmpty(t.Text)) return;

        var font = GetFont(t.FontSize);
        using var paint = new SKPaint(font)
        {
            Color = ToSk(t.Color),
            IsAntialias = true
        };
        // SkiaSharp 2.x: SKCanvas.DrawText(string, x, baseline, font, paint)
        canvas.DrawText(t.Text, t.Position.X, t.Position.Y, paint);
    }

    private SKFont GetFont(float size)
    {
        var key = (int)MathF.Round(size * 4f);  // шаг 0.25 пикселя
        if (!_fonts.TryGetValue(key, out var font))
        {
            _typeface ??= SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
                          ?? SKTypeface.Default;
            font = new SKFont(_typeface, size) { Edging = SKFontEdging.SubpixelAntialias };
            _fonts[key] = font;
        }
        return font;
    }

    // ---- Blur ----

    private void DrawBlur(SKCanvas canvas, BlurShape b)
    {
        if (!_blurImages.TryGetValue(b.Id, out var img))
        {
            // Импорт BGRA байтов в SKImage. У формы placement в логических
            // (window-local) координатах, а пиксели — в physical screen
            var info = new SKImageInfo(b.PixelWidth, b.PixelHeight,
                                       SKColorType.Bgra8888, SKAlphaType.Opaque);
            using var data = SKData.CreateCopy(b.BgraPixels);
            img = SKImage.FromPixelData(info, data, info.RowBytes)
                  ?? throw new InvalidOperationException("SKImage.FromPixelData вернул null");
            _blurImages[b.Id] = img;
        }

        var bb = b.Bounds;
        var dest = new SKRect(bb.Left, bb.Top, bb.Right, bb.Bottom);

        using var blurFilter = SKImageFilter.CreateBlur(b.Sigma, b.Sigma);
        using var paint = new SKPaint { ImageFilter = blurFilter, IsAntialias = true };

        canvas.DrawImage(img, dest, paint);
    }

    private readonly System.Collections.Generic.Dictionary<Guid, (long version, SKImage img)> _liveBlurCache = new();

    private void DrawLiveBlur(SKCanvas canvas, LiveBlurShape b)
    {
        // Используем кэш: пересоздаём SKImage только при смене FrameVersion
        var ver = b.Provider.FrameVersion;
        if (!_liveBlurCache.TryGetValue(b.Id, out var entry) || entry.version != ver)
        {
            var pixels = b.CurrentFrameBgra();
            if (pixels is null || pixels.Length == 0) return;

            entry.img?.Dispose();

            var info = new SKImageInfo(b.Width, b.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            using var data = SKData.CreateCopy(pixels);
            var img = SKImage.FromPixelData(info, data, info.RowBytes);
            if (img is null) return;

            entry = (ver, img);
            _liveBlurCache[b.Id] = entry;
        }

        var bb = b.Bounds;
        var dest = new SKRect(bb.Left, bb.Top, bb.Right, bb.Bottom);

        using var blurFilter = SKImageFilter.CreateBlur(b.Sigma, b.Sigma);
        using var paint = new SKPaint { ImageFilter = blurFilter, IsAntialias = true };

        canvas.DrawImage(entry.img, dest, paint);
    }

    /// <summary>
    /// Освободить SKImage-кэш, если фигура удалена (вызывает Canvas.Changed)
    /// </summary>
    public void EvictBlurCache(Guid shapeId)
    {
        if (_blurImages.TryGetValue(shapeId, out var img))
        {
            img.Dispose();
            _blurImages.Remove(shapeId);
        }
        if (_liveBlurCache.TryGetValue(shapeId, out var live))
        {
            live.img?.Dispose();
            _liveBlurCache.Remove(shapeId);
        }
    }

    private static SKColor ToSk(ColorRgba c) => new(c.R, c.G, c.B, c.A);
}