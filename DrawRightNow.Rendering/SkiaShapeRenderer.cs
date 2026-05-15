using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;
using SkiaSharp;
using System.Windows.Shapes;

namespace DrawRightNow.Rendering;

/// <summary>
/// Платформо-независимый рендер: получает SKCanvas и любую IShape, рисует
/// Конкретный хостинг (SKElement / OpenGL / Direct2D) живёт в Controls/
/// </summary>
public sealed class SkiaShapeRenderer
{
    private readonly SkiaPaintPool _pool = new();
    private readonly SKPath _scratch = new();

    public void Draw(SKCanvas canvas, IShape shape)
    {
        switch (shape)
        {
            case StrokeShape s:
                DrawStroke(canvas, s);
                break;
        }
    }

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

        var paint = _pool.Get(color, s.Width, antialias, blend, SKStrokeCap.Round);

        if (s.Points.Count == 1)
        {
            // Одна точка — рисуем точкой, чтобы пользователь видел клик
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

    private static SKColor ToSk(ColorRgba c) => new(c.R, c.G, c.B, c.A);
}