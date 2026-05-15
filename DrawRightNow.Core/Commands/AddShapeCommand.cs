using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Commands;

/// <summary>
/// Добавление готовой фигуры на холст. Undo удаляет её обратно
/// </summary>
public sealed class AddShapeCommand : IUndoableCommand
{
    private readonly CanvasModel _canvas;
    private readonly IShape _shape;
    private int _restoreIndex = -1;

    public AddShapeCommand(CanvasModel canvas, IShape shape)
    {
        _canvas = canvas;
        _shape = shape;
    }

    public void Do()
    {
        if (_restoreIndex < 0)
            _canvas.Add(_shape);
        else
            _canvas.Insert(_restoreIndex, _shape);
    }

    public void Undo()
    {
        _restoreIndex = _canvas.IndexOf(_shape);
        _canvas.Remove(_shape);
    }
}