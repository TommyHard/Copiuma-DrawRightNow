using System;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Сопоставляет ToolType с конкретной реализацией ITool. На каждое
/// нажатие создаётся новый instance
/// </summary>
public static class ToolFactory
{
    public static ITool Create(ToolType type) => type switch
    {
        ToolType.Pencil => new PencilTool(StrokeKind.Pencil),
        ToolType.Brush => new PencilTool(StrokeKind.Brush),
        ToolType.Marker => new PencilTool(StrokeKind.Marker),
        ToolType.Eraser => new PencilTool(StrokeKind.Eraser),
        _ => throw new NotSupportedException(
            $"Инструмент {type} ещё не реализован")
    };

    public static bool IsImplemented(ToolType type) => type
        is ToolType.Pencil or ToolType.Brush or ToolType.Marker or ToolType.Eraser;
}