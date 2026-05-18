namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Общий базовый класс для двухточечных фигур (Rectangle, Ellipse, Line, Arrow)
/// </summary>
public abstract class TwoPointShape : IShape
{
    protected TwoPointShape(PointF start, ShapeStyle style)
    {
        Id = Guid.NewGuid();
        Start = start;
        End = start;
        Style = style;
    }

    public Guid Id { get; }
    public PointF Start { get; private set; }
    public PointF End { get; private set; }
    public ShapeStyle Style { get; }

    public RectF Bounds => RectF.FromPoints(Start, End);

    public void SetEnd(PointF p) => End = p;

    public virtual bool HitTest(PointF p, float tolerance)
        => Bounds.Inflate(tolerance).Contains(p);

    public void Translate(float dx, float dy)
    {
        Start = new PointF(Start.X + dx, Start.Y + dy);
        End = new PointF(End.X + dx, End.Y + dy);
    }
}