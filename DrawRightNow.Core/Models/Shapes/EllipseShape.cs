namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Эллипс, вписанный в bounding box между Start и End
/// </summary>
public sealed class EllipseShape : TwoPointShape
{
    public EllipseShape(PointF start, ShapeStyle style) : base(start, style) { }
}