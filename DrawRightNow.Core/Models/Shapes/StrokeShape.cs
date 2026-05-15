using System;
using System.Collections.Generic;

namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Свободная линия — общий контейнер для Pencil/Brush/Marker
/// Точки хранятся в одном списке (List&lt;PointF&gt;), что даёт компактный
/// массив значений в куче
/// </summary>
public sealed class StrokeShape : IShape
{
    private RectF _bounds;

    public StrokeShape(StrokeKind kind, ColorRgba color, float width)
    {
        Id = Guid.NewGuid();
        Kind = kind;
        Color = color;
        Width = width;
        Points = new List<PointF>(capacity: 64);
    }

    public Guid Id { get; }
    public StrokeKind Kind { get; }
    public ColorRgba Color { get; }
    public float Width { get; }
    public List<PointF> Points { get; }
    public RectF Bounds => _bounds;

    public void AddPoint(PointF p)
    {
        Points.Add(p);
        if (Points.Count == 1)
        {
            _bounds = new RectF(p.X, p.Y, p.X, p.Y);
        }
        else
        {
            _bounds = new RectF(
                MathF.Min(_bounds.Left, p.X),
                MathF.Min(_bounds.Top, p.Y),
                MathF.Max(_bounds.Right, p.X),
                MathF.Max(_bounds.Bottom, p.Y));
        }
    }

    public bool HitTest(PointF p, float tolerance)
    {
        if (!_bounds.Inflate(tolerance).Contains(p))
            return false;

        var t2 = (Width / 2f + tolerance);
        t2 *= t2;
        for (int i = 1; i < Points.Count; i++)
        {
            if (SqDistancePointSegment(p, Points[i - 1], Points[i]) <= t2)
                return true;
        }
        return false;
    }

    public void Translate(float dx, float dy)
    {
        for (int i = 0; i < Points.Count; i++)
        {
            var p = Points[i];
            Points[i] = new PointF(p.X + dx, p.Y + dy);
        }
        _bounds = new RectF(_bounds.Left + dx, _bounds.Top + dy,
                            _bounds.Right + dx, _bounds.Bottom + dy);
    }

    private static float SqDistancePointSegment(PointF p, PointF a, PointF b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var apx = p.X - a.X;
        var apy = p.Y - a.Y;

        var len2 = abx * abx + aby * aby;
        var t = len2 > 0 ? (apx * abx + apy * aby) / len2 : 0f;
        if (t < 0f) t = 0f; else if (t > 1f) t = 1f;

        var cx = a.X + abx * t;
        var cy = a.Y + aby * t;

        var dx = p.X - cx;
        var dy = p.Y - cy;
        return dx * dx + dy * dy;
    }
}

public enum StrokeKind
{
    /// <summary>
    /// Карандаш: фиксированная ширина, без сглаживания
    /// </summary>
    Pencil,
    /// <summary>
    /// Кисть: anti-aliasing
    /// </summary>
    Brush,
    /// <summary>
    /// Маркер: полупрозрачный, режим наложения Multiply
    /// </summary>
    Marker,
    /// <summary>
    /// Ластик: "отрицательный штрих"
    /// </summary>
    Eraser
}