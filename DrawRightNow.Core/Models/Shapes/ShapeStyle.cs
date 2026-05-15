namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Стилистические параметры геометрической фигуры. FillColor.A = 0
/// означает "без заливки" (только обводка)
/// </summary>
public readonly record struct ShapeStyle(ColorRgba StrokeColor, ColorRgba FillColor, float StrokeWidth)
{
    public bool HasFill => FillColor.A != 0;

    public static ShapeStyle StrokeOnly(ColorRgba color, float width)
        => new(color, new ColorRgba(0, 0, 0, 0), width);
}