using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Commands;

/// <summary>
/// Перемещение фигуры на (dx, dy). Команда фиксируется только после
/// завершения жеста Move, чтобы Undo возвращал её одним шагом, а не
/// поточечно
/// </summary>
public sealed class MoveShapeCommand : IUndoableCommand
{
    private readonly CanvasModel _canvas;
    private readonly IShape _shape;
    private readonly float _dx;
    private readonly float _dy;

    public MoveShapeCommand(CanvasModel canvas, IShape shape, float dx, float dy)
    {
        _canvas = canvas;
        _shape = shape;
        _dx = dx;
        _dy = dy;
    }

    public void Do()
    {
        _shape.Translate(_dx, _dy);
        _canvas.UpdatePending();
    }

    public void Undo()
    {
        _shape.Translate(-_dx, -_dy);
        _canvas.UpdatePending();
    }
}