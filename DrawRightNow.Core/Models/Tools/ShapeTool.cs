using System;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Универсальный инструмент-"2 точки": Rectangle / Ellipse / Line / Arrow.
/// Поддерживает заливку через ToolSettings.FillEnabled — цвет заливки
/// берётся как тот же Color с уменьшенной альфой (FillAlpha)
/// </summary>
public sealed class ShapeTool : ITool
{
    private readonly Func<PointF, ShapeStyle, TwoPointShape> _ctor;
    private readonly bool _supportsFill;
    private TwoPointShape? _current;

    public ShapeTool(ToolType type)
    {
        Type = type;
        (_ctor, _supportsFill) = type switch
        {
            ToolType.Rectangle => ((Func<PointF, ShapeStyle, TwoPointShape>)((p, s) => new RectangleShape(p, s)), true),
            ToolType.Ellipse => ((p, s) => new EllipseShape(p, s), true),
            ToolType.Line => ((p, s) => new LineShape(p, s), false),
            ToolType.Arrow => ((p, s) => new ArrowShape(p, s), false),
            _ => throw new ArgumentException($"ShapeTool не поддерживает {type}", nameof(type))
        };
    }

    public ToolType Type { get; }

    public IShape? OnPointerDown(PointF p, ToolSettings settings)
    {
        ShapeStyle style;
        if (_supportsFill && settings.FillEnabled)
        {
            var fill = settings.Color.WithAlpha(settings.FillAlpha);
            style = new ShapeStyle(settings.Color, fill, settings.Width);
        }
        else
        {
            style = ShapeStyle.StrokeOnly(settings.Color, settings.Width);
        }
        _current = _ctor(p, style);
        return _current;
    }

    public void OnPointerMove(PointF p) => _current?.SetEnd(p);

    public IShape? OnPointerUp(PointF p)
    {
        if (_current is null) return null;
        _current.SetEnd(p);
        var b = _current.Bounds;
        if (b.Width < 1f && b.Height < 1f)
        {
            _current = null;
            return null;
        }
        var result = _current;
        _current = null;
        return result;
    }
}