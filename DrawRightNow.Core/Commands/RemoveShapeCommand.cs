using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Commands;

/// <summary>
/// Удаление фигуры (ластик-нож, Delete). Undo возвращает её на прежнюю позицию
/// </summary>
public sealed class RemoveShapeCommand : IUndoableCommand
{
    private readonly CanvasModel _canvas;
    private readonly IShape _shape;
    private int _index = -1;

    public RemoveShapeCommand(CanvasModel canvas, IShape shape)
    {
        _canvas = canvas;
        _shape = shape;
    }

    public void Do()
    {
        _index = _canvas.IndexOf(_shape);
        if (_index >= 0) _canvas.Remove(_shape);
    }

    public void Undo()
    {
        if (_index >= 0) _canvas.Insert(_index, _shape);
    }
}