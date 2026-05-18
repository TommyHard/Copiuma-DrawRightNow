using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Commands;

/// <summary>
/// Изменение содержимого и/или размера шрифта существующей TextShape.
/// Используется при редактировании по двойному клику (инструмент Text).
/// В отличие от Add/Remove, фигура остаётся в коллекции — меняются только
/// её поля Text и FontSize
/// </summary>
public sealed class EditTextCommand : IUndoableCommand
{
    private readonly CanvasModel _canvas;
    private readonly TextShape _shape;
    private readonly string _oldText;
    private readonly float _oldFontSize;
    private readonly string _newText;
    private readonly float _newFontSize;

    public EditTextCommand(CanvasModel canvas, TextShape shape,
                           string oldText, float oldFontSize,
                           string newText, float newFontSize)
    {
        _canvas = canvas;
        _shape = shape;
        _oldText = oldText ?? string.Empty;
        _oldFontSize = oldFontSize;
        _newText = newText ?? string.Empty;
        _newFontSize = newFontSize;
    }

    public void Do()
    {
        _shape.Text = _newText;
        _shape.FontSize = _newFontSize;
        _canvas.UpdatePending();
    }

    public void Undo()
    {
        _shape.Text = _oldText;
        _shape.FontSize = _oldFontSize;
        _canvas.UpdatePending();
    }
}