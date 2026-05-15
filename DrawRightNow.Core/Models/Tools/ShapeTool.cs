using System;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Универсальный инструмент-"2 точки": на mouse down фиксируется первая точка,
/// на mouse move обновляется вторая, на mouse up — фигура коммитится
/// Поддерживает Rectangle / Ellipse / Line / Arrow
/// </summary>
public sealed class ShapeTool : ITool
{
    private readonly Func<PointF, ShapeStyle, TwoPointShape> _ctor;
    private TwoPointShape? _current;

    public ShapeTool(ToolType type)
    {
        Type = type;
        _ctor = type switch
        {
            ToolType.Rectangle => (p, s) => new RectangleShape(p, s),
            ToolType.Ellipse => (p, s) => new EllipseShape(p, s),
            ToolType.Line => (p, s) => new LineShape(p, s),
            ToolType.Arrow => (p, s) => new ArrowShape(p, s),
            _ => throw new ArgumentException($"ShapeTool не поддерживает {type}", nameof(type))
        };
    }

    public ToolType Type { get; }

    public IShape? OnPointerDown(PointF p, ToolSettings settings)
    {
        var style = ShapeStyle.StrokeOnly(settings.Color, settings.Width);
        _current = _ctor(p, style);
        return _current;
    }

    public void OnPointerMove(PointF p)
    {
        _current?.SetEnd(p);
    }

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