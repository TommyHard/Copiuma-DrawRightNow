using System;

namespace DrawRightNow.Core.Models;

/// <summary>
/// Bounding box. Используется для Hit-Testing и Dirty Rectangles
/// </summary>
public readonly record struct RectF(float Left, float Top, float Right, float Bottom)
{
    public float Width => Right - Left;
    public float Height => Bottom - Top;

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(PointF p)
        => p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom;

    public RectF Inflate(float by)
        => new(Left - by, Top - by, Right + by, Bottom + by);

    public RectF Union(RectF other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;
        return new RectF(
            MathF.Min(Left, other.Left),
            MathF.Min(Top, other.Top),
            MathF.Max(Right, other.Right),
            MathF.Max(Bottom, other.Bottom));
    }

    public static RectF FromPoints(PointF a, PointF b)
        => new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y),
               MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));
}