using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Инструмент "Текст". В отличие от прочих инструментов жест — это просто клик:
/// PointerDown создаёт фигуру с пустым текстом, PointerMove/PointerUp ничего
/// не делают. ViewModel переводит UI в режим инлайн-редактирования, когда
/// возвращаемая фигура — TextShape
/// </summary>
public sealed class TextTool : ITool
{
    private TextShape? _current;

    public ToolType Type => ToolType.Text;

    public IShape? OnPointerDown(PointF p, ToolSettings settings)
    {
        var fontSize = MathF.Max(12f, settings.Width * 6f);
        _current = new TextShape(p, string.Empty, settings.Color, fontSize);
        return _current;
    }

    public void OnPointerMove(PointF p, bool constrain = false) { /* ignore */ }

    public IShape? OnPointerUp(PointF p, bool constrain = false)
    {
        var result = _current;
        _current = null;
        return result;
    }
}