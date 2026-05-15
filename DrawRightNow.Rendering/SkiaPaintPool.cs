using System;
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
    private readonly Dictionary<int, SKPaint> _strokePool = new();
    private readonly Dictionary<int, SKPaint> _fillPool = new();

    public SKPaint GetStroke(SKColor color, float width, bool antialias, SKBlendMode blend, SKStrokeCap cap)
    {
        var key = HashCode.Combine((int)(uint)color, width, antialias, (int)blend, (int)cap);
        if (!_strokePool.TryGetValue(key, out var paint))
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
            _strokePool[key] = paint;
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

    public SKPaint GetFill(SKColor color, bool antialias, SKBlendMode blend = SKBlendMode.SrcOver)
    {
        var key = HashCode.Combine((int)(uint)color, antialias, (int)blend);
        if (!_fillPool.TryGetValue(key, out var paint))
        {
            paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = antialias,
                Color = color,
                BlendMode = blend
            };
            _fillPool[key] = paint;
        }
        else
        {
            paint.Color = color;
            paint.IsAntialias = antialias;
            paint.BlendMode = blend;
        }
        return paint;
    }

    /// <summary>
    /// Совместимость со старыми вызовами — алиас на GetStroke
    /// </summary>
    public SKPaint Get(SKColor color, float width, bool antialias, SKBlendMode blend, SKStrokeCap cap)
        => GetStroke(color, width, antialias, blend, cap);

    public void Clear()
    {
        foreach (var p in _strokePool.Values) p.Dispose();
        foreach (var p in _fillPool.Values) p.Dispose();
        _strokePool.Clear();
        _fillPool.Clear();
    }
}