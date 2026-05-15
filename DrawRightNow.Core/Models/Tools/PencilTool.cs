using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Карандаш/Кисть/Маркер/Ластик. Разница в режиме отрисовки —
/// сохраняется в StrokeKind, а уже рендер выбирает blend mode и AA
/// </summary>
public sealed class PencilTool : ITool
{
    private readonly StrokeKind _kind;
    private StrokeShape? _current;
    private PointF _lastPoint;

    public PencilTool(StrokeKind kind = StrokeKind.Pencil)
    {
        _kind = kind;
        Type = kind switch
        {
            StrokeKind.Brush => ToolType.Brush,
            StrokeKind.Marker => ToolType.Marker,
            StrokeKind.Eraser => ToolType.Eraser,
            _ => ToolType.Pencil
        };
    }

    public ToolType Type { get; }

    public IShape? OnPointerDown(PointF p, ToolSettings settings)
    {
        _current = new StrokeShape(_kind, settings.Color, settings.Width);
        _current.AddPoint(p);
        _lastPoint = p;
        return _current;
    }

    public void OnPointerMove(PointF p)
    {
        if (_current is null) return;
        if (p.Distance(_lastPoint) < 1.5f) return;
        _current.AddPoint(p);
        _lastPoint = p;
    }

    public IShape? OnPointerUp(PointF p)
    {
        if (_current is null) return null;
        if (p.Distance(_lastPoint) >= 0.5f) _current.AddPoint(p);
        var result = _current;
        _current = null;
        return result;
    }
}