namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Прямоугольник по двум противоположным углам
/// </summary>
public sealed class RectangleShape : TwoPointShape
{
    public RectangleShape(PointF start, ShapeStyle style) : base(start, style) { }
}