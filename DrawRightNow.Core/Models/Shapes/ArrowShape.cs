namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Стрелка: линия + наконечник в End. Размер наконечника вычисляется
/// рендером пропорционально длине, чтобы не плодить параметры
/// </summary>
public sealed class ArrowShape : TwoPointShape
{
    public ArrowShape(PointF start, ShapeStyle style) : base(start, style) { }

    public override bool HitTest(PointF p, float tolerance)
    {
        var t = Style.StrokeWidth / 2f + tolerance;
        return LineShape.SqDistanceToSegment(p, Start, End) <= t * t;
    }
}