namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Прямая линия от Start к End. Hit-Testing — расстояние до сегмента
/// </summary>
public sealed class LineShape : TwoPointShape
{
    public LineShape(PointF start, ShapeStyle style) : base(start, style) { }

    public override bool HitTest(PointF p, float tolerance)
    {
        var t = Style.StrokeWidth / 2f + tolerance;
        return SqDistanceToSegment(p, Start, End) <= t * t;
    }

    internal static float SqDistanceToSegment(PointF p, PointF a, PointF b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var apx = p.X - a.X;
        var apy = p.Y - a.Y;

        var len2 = abx * abx + aby * aby;
        var u = len2 > 0 ? (apx * abx + apy * aby) / len2 : 0f;
        if (u < 0f) u = 0f; else if (u > 1f) u = 1f;

        var cx = a.X + abx * u;
        var cy = a.Y + aby * u;
        var dx = p.X - cx;
        var dy = p.Y - cy;
        return dx * dx + dy * dy;
    }
}