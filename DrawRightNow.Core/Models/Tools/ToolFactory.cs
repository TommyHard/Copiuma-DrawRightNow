using System;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Сопоставляет ToolType с конкретной реализацией ITool.
/// На каждое нажатие создаётся новый instance
/// </summary>
public static class ToolFactory
{
    public static ITool Create(ToolType type) => type switch
    {
        ToolType.Pencil => new PencilTool(StrokeKind.Pencil),
        ToolType.Brush => new PencilTool(StrokeKind.Brush),
        ToolType.Marker => new PencilTool(StrokeKind.Marker),
        ToolType.Eraser => new PencilTool(StrokeKind.Eraser),
        ToolType.Rectangle => new ShapeTool(ToolType.Rectangle),
        ToolType.Ellipse => new ShapeTool(ToolType.Ellipse),
        ToolType.Line => new ShapeTool(ToolType.Line),
        ToolType.Arrow => new ShapeTool(ToolType.Arrow),
        ToolType.Text => new TextTool(),
        _ => throw new NotSupportedException(
            $"Инструмент {type} ещё не реализован в текущей версии.")
    };

    public static bool IsImplemented(ToolType type) => type
        is ToolType.Pencil
        or ToolType.Brush
        or ToolType.Marker
        or ToolType.Eraser
        or ToolType.Rectangle
        or ToolType.Ellipse
        or ToolType.Line
        or ToolType.Arrow
        or ToolType.Text;
}