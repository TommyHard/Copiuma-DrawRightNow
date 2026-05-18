using System.Collections.ObjectModel;
using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models;

/// <summary>
/// Модель холста: упорядоченный список фигур + текущая "черновая" фигура,
/// над которой пользователь сейчас работает
/// </summary>
public sealed class CanvasModel
{
    private readonly ObservableCollection<IShape> _shapes = new();

    public ReadOnlyObservableCollection<IShape> Shapes { get; }
    public IShape? Pending { get; private set; }

    public event EventHandler? Changed;

    public CanvasModel()
    {
        Shapes = new ReadOnlyObservableCollection<IShape>(_shapes);
    }

    public void BeginPending(IShape shape)
    {
        Pending = shape;
        RaiseChanged();
    }

    public void UpdatePending() => RaiseChanged();

    public void CommitPending()
    {
        if (Pending is null) return;
        _shapes.Add(Pending);
        Pending = null;
        RaiseChanged();
    }

    public void CancelPending()
    {
        Pending = null;
        RaiseChanged();
    }

    public void Add(IShape shape)
    {
        _shapes.Add(shape);
        RaiseChanged();
    }

    public bool Remove(IShape shape)
    {
        var ok = _shapes.Remove(shape);
        if (ok) RaiseChanged();
        return ok;
    }

    public void Insert(int index, IShape shape)
    {
        _shapes.Insert(index, shape);
        RaiseChanged();
    }

    public int IndexOf(IShape shape) => _shapes.IndexOf(shape);

    public void Clear()
    {
        _shapes.Clear();
        Pending = null;
        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}