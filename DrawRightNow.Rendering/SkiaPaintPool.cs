using System.Collections.Generic;
using SkiaSharp;

namespace DrawRightNow.Rendering;

/// <summary>
/// Object Pool для SKPaint. Создание SKPaint — тяжёлая операция;
/// вместо аллокации на каждый штрих храним
/// один экземпляр под каждое сочетание режимов и переписываем у него поля
/// </summary>
internal sealed class SkiaPaintPool
{
    private readonly Dictionary<int, SKPaint> _pool = new();

    public SKPaint Get(SKColor color, float width, bool antialias, SKBlendMode blend, SKStrokeCap cap)
    {
        // Композитный ключ — порядка двух десятков уникальных вариантов на сессию
        var key = HashCode.Combine((int)(uint)color, width, antialias, (int)blend, (int)cap);
        if (!_pool.TryGetValue(key, out var paint))
        {
            paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                IsAntialias = antialias,
                Color = color,
                StrokeWidth = width,
                StrokeCap = cap,
                StrokeJoin = SKStrokeJoin.Round,
                BlendMode = blend
            };
            _pool[key] = paint;
        }
        else
        {
            paint.Color = color;
            paint.StrokeWidth = width;
            paint.IsAntialias = antialias;
            paint.BlendMode = blend;
            paint.StrokeCap = cap;
        }
        return paint;
    }

    public void Clear()
    {
        foreach (var p in _pool.Values) p.Dispose();
        _pool.Clear();
    }
}