using System;
using DrawRightNow.Core.Commands;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.Core.Mvvm;

namespace DrawRightNow.Core.ViewModels;

/// <summary>
/// Главная ViewModel. Содержит canvas, историю Undo/Redo, активный
/// инструмент и текущие настройки (цвет, толщина)
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly RelayCommand _undoCmd;
    private readonly RelayCommand _redoCmd;
    private readonly RelayCommand _clearCmd;

    private ITool? _tool;
    private ToolType _activeTool = ToolType.Pencil;
    private ColorRgba _strokeColor = ColorRgba.Red;
    private float _strokeWidth = 3f;
    private bool _isToolbarPinned = false;
    private bool _isDrawingEnabled = true;

    public MainViewModel()
    {
        Canvas = new CanvasModel();
        History = new CommandHistory();

        _undoCmd = new RelayCommand(_ => History.Undo(), _ => History.CanUndo);
        _redoCmd = new RelayCommand(_ => History.Redo(), _ => History.CanRedo);
        _clearCmd = new RelayCommand(_ => Clear(), _ => Canvas.Shapes.Count > 0);

        History.Changed += (_, _) =>
        {
            _undoCmd.RaiseCanExecuteChanged();
            _redoCmd.RaiseCanExecuteChanged();
            _clearCmd.RaiseCanExecuteChanged();
        };

        // Любое изменение canvas также влияет на Clear (потенциально и на Undo
        // редактирующих команд)
        Canvas.Changed += (_, _) => _clearCmd.RaiseCanExecuteChanged();
    }

    public CanvasModel Canvas { get; }
    public CommandHistory History { get; }

    public RelayCommand UndoCommand => _undoCmd;
    public RelayCommand RedoCommand => _redoCmd;
    public RelayCommand ClearCommand => _clearCmd;

    public ToolType ActiveTool
    {
        get => _activeTool;
        set
        {
            if (SetField(ref _activeTool, value))
                OnPropertyChanged(nameof(ActiveToolDisplayName));
        }
    }

    public string ActiveToolDisplayName => _activeTool.ToString();

    public ColorRgba StrokeColor
    {
        get => _strokeColor;
        set => SetField(ref _strokeColor, value);
    }

    public float StrokeWidth
    {
        get => _strokeWidth;
        set => SetField(ref _strokeWidth, value);
    }

    public bool IsToolbarPinned
    {
        get => _isToolbarPinned;
        set => SetField(ref _isToolbarPinned, value);
    }

    /// <summary>
    /// Когда false — окно должно быть "прозрачным для кликов" (WS_EX_TRANSPARENT),
    /// чтобы пользователь мог взаимодействовать с программами под overlay
    /// </summary>
    public bool IsDrawingEnabled
    {
        get => _isDrawingEnabled;
        set => SetField(ref _isDrawingEnabled, value);
    }

    // ---- Драйверы ввода ----

    public void BeginStroke(PointF p)
    {
        if (!_isDrawingEnabled) return;
        if (!ToolFactory.IsImplemented(_activeTool)) return;

        _tool = ToolFactory.Create(_activeTool);
        var settings = new ToolSettings(_strokeColor, _strokeWidth);
        var shape = _tool.OnPointerDown(p, settings);
        if (shape is not null) Canvas.BeginPending(shape);
    }

    public void ContinueStroke(PointF p)
    {
        if (_tool is null) return;
        _tool.OnPointerMove(p);
        Canvas.UpdatePending();
    }

    public void EndStroke(PointF p)
    {
        if (_tool is null) return;
        var shape = _tool.OnPointerUp(p);
        _tool = null;
        if (shape is null)
        {
            Canvas.CancelPending();
            return;
        }
        Canvas.CancelPending();          // снимаем "черновую" отрисовку
        History.Execute(new AddShapeCommand(Canvas, shape));
    }

    public void Clear()
    {
        // Полная очистка: фиксируем в истории как одну составную команду,
        // чтобы Undo возвращал всё разом
        Canvas.Clear();
        History.Clear();
    }
}