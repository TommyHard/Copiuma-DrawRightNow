namespace DrawRightNow.Core.Commands;

/// <summary>
/// Стек действий для Undo/Redo. Очищает Redo при появлении нового действия,
/// уведомляет подписчиков (RaiseCanExecuteChanged во ViewModel)
/// </summary>
public sealed class CommandHistory
{
    private readonly Stack<IUndoableCommand> _undo = new();
    private readonly Stack<IUndoableCommand> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public event EventHandler? Changed;

    public void Execute(IUndoableCommand command)
    {
        command.Do();
        _undo.Push(command);
        _redo.Clear();
        OnChanged();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var c = _undo.Pop();
        c.Undo();
        _redo.Push(c);
        OnChanged();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var c = _redo.Pop();
        c.Do();
        _undo.Push(c);
        OnChanged();
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}