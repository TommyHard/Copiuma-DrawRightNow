using System;

namespace DrawRightNow.Core.Models;

/// <summary>
/// Лёгкая структура координаты
/// </summary>
public readonly record struct PointF(float X, float Y)
{
    public static readonly PointF Empty = new(0f, 0f);

    public float Distance(PointF other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}