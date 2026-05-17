using System;
using DrawRightNow.Core.Commands;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.Core.Mvvm;
using DrawRightNow.Core.Services;

namespace DrawRightNow.Core.ViewModels;

/// <summary>
/// Главная ViewModel. Содержит canvas, историю Undo/Redo, активный
/// инструмент и текущие настройки (цвет, толщина). Платформо-независима;
/// специальные инструменты (Knife, Move, Eyedropper, Blur) маршрутизируются
/// здесь же по типу ActiveTool
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly RelayCommand _undoCmd;
    private readonly RelayCommand _redoCmd;
    private readonly RelayCommand _clearCmd;

    private ITool? _tool;
    private bool _isOptionsOpen;
    private ToolType _activeTool = ToolType.Pencil;
    private ColorRgba _strokeColor = ColorRgba.Red;
    private float _strokeWidth = 3f;
    private bool _isToolbarPinned = true;        // По умолчанию тулбар закреплён — иначе при запуске не видно ничего
    private bool _isDrawingEnabled = true;
    private bool _toolbarTranslucent = false;    // Тогл "панель сквозь" — opacity 1.0/0.45
    private bool _overlayDimEnabled = true;     // Видимая полупрозрачная подложка холста
    private bool _fillEnabled = false;
    private bool _liveBlurEnabled = false;
    private bool _isColorPickerOpen;
    private TextShape? _editingText;

    // Move state
    private IShape? _movingShape;
    private PointF _moveLastPoint;
    private float _moveTotalDx;
    private float _moveTotalDy;

    private bool _blurInProgress;
    private PointF _blurStartLocal;
    private PointF _blurStartScreen;
    private RectangleShape? _blurPreview;
    private float _blurSigma = 16f;

    public AppSettings Settings { get; } = AppSettings.Load();

    public bool IsToolVisible(ToolType tool) => Settings.VisibleTools.Contains(tool);

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

        Canvas.Changed += (_, _) => _clearCmd.RaiseCanExecuteChanged();
    }

    public CanvasModel Canvas { get; }
    public CommandHistory History { get; }

    /// <summary>
    /// Сервисы уровня экрана (захват пикселя/региона). Инжектится из App-слоя
    /// после создания окна (нужен HWND). Без них Eyedropper/Blur просто
    /// no-op — приложение не падает
    /// </summary>
    public IScreenServices? ScreenServices { get; set; }

    /// <summary>
    /// Источник «живого» кадра экрана. Опциональный — без него Live-blur
    /// автоматически переключается на статический снимок
    /// </summary>
    public IFrameProvider? FrameProvider { get; set; }

    /// <summary>
    /// Когда true, инструмент Blur создаёт LiveBlurShape (видео под областью
    /// продолжает двигаться размыто). Иначе — статический снимок (BlurShape)
    /// </summary>
    public bool LiveBlurEnabled
    {
        get => _liveBlurEnabled;
        set => SetField(ref _liveBlurEnabled, value);
    }

    public bool IsOptionsOpen
    {
        get => _isOptionsOpen;
        set => SetField(ref _isOptionsOpen, value);
    }

    public void ToggleToolVisibility(ToolType tool, bool isVisible)
    {
        if (isVisible && !Settings.VisibleTools.Contains(tool)) Settings.VisibleTools.Add(tool);
        else if (!isVisible) Settings.VisibleTools.Remove(tool);

        Settings.Save();
        OnPropertyChanged($"Show_{tool}");
    }

    public bool IsColorPickerOpen
    {
        get => _isColorPickerOpen;
        set => SetField(ref _isColorPickerOpen, value);
    }

    public bool Show_Pencil { get => IsToolVisible(ToolType.Pencil); set => ToggleToolVisibility(ToolType.Pencil, value); }
    public bool Show_Brush { get => IsToolVisible(ToolType.Brush); set => ToggleToolVisibility(ToolType.Brush, value); }
    public bool Show_Marker { get => IsToolVisible(ToolType.Marker); set => ToggleToolVisibility(ToolType.Marker, value); }
    public bool Show_Eraser { get => IsToolVisible(ToolType.Eraser); set => ToggleToolVisibility(ToolType.Eraser, value); }
    public bool Show_Rectangle { get => IsToolVisible(ToolType.Rectangle); set => ToggleToolVisibility(ToolType.Rectangle, value); }
    public bool Show_Ellipse { get => IsToolVisible(ToolType.Ellipse); set => ToggleToolVisibility(ToolType.Ellipse, value); }
    public bool Show_Line { get => IsToolVisible(ToolType.Line); set => ToggleToolVisibility(ToolType.Line, value); }
    public bool Show_Arrow { get => IsToolVisible(ToolType.Arrow); set => ToggleToolVisibility(ToolType.Arrow, value); }
    public bool Show_Text { get => IsToolVisible(ToolType.Text); set => ToggleToolVisibility(ToolType.Text, value); }
    public bool Show_KnifeDelete { get => IsToolVisible(ToolType.KnifeDelete); set => ToggleToolVisibility(ToolType.KnifeDelete, value); }
    public bool Show_Move { get => IsToolVisible(ToolType.Move); set => ToggleToolVisibility(ToolType.Move, value); }
    public bool Show_Eyedropper { get => IsToolVisible(ToolType.Eyedropper); set => ToggleToolVisibility(ToolType.Eyedropper, value); }
    public bool Show_Blur { get => IsToolVisible(ToolType.Blur); set => ToggleToolVisibility(ToolType.Blur, value); }

    public bool ShowInTray
    {
        get => Settings.ShowInTray;
        set { Settings.ShowInTray = value; Settings.Save(); OnPropertyChanged(); }
    }

    public RelayCommand UndoCommand => _undoCmd;
    public RelayCommand RedoCommand => _redoCmd;
    public RelayCommand ClearCommand => _clearCmd;

    public ToolType ActiveTool
    {
        get => _activeTool;
        set
        {
            if (SetField(ref _activeTool, value))
            {
                OnPropertyChanged(nameof(ActiveToolDisplayName));

                // Включаем или выключаем режим рисования в зависимости от выбранного инструмента
                if (_activeTool == ToolType.None)
                {
                    IsDrawingEnabled = false;
                }
                else if (!IsDrawingEnabled)
                {
                    IsDrawingEnabled = true;
                }
            }
        }
    }

    public string ActiveToolDisplayName => _activeTool.ToString();

    public ColorRgba StrokeColor
    {
        get => _strokeColor;
        set
        {
            if (SetField(ref _strokeColor, value))
            {
                OnPropertyChanged(nameof(StrokeColorHex));
                OnPropertyChanged(nameof(StrokeColorR));
                OnPropertyChanged(nameof(StrokeColorG));
                OnPropertyChanged(nameof(StrokeColorB));
            }
        }
    }

    public string StrokeColorHex
    {
        get => $"#{_strokeColor.R:X2}{_strokeColor.G:X2}{_strokeColor.B:X2}";
        set
        {
            try { StrokeColor = ColorRgba.FromHex(value).WithAlpha(_strokeColor.A); } catch { /* ignore */ }
        }
    }

    public byte StrokeColorR
    {
        get => _strokeColor.R;
        set => StrokeColor = new ColorRgba(value, _strokeColor.G, _strokeColor.B, _strokeColor.A);
    }

    public byte StrokeColorG
    {
        get => _strokeColor.G;
        set => StrokeColor = new ColorRgba(_strokeColor.R, value, _strokeColor.B, _strokeColor.A);
    }

    public byte StrokeColorB
    {
        get => _strokeColor.B;
        set => StrokeColor = new ColorRgba(_strokeColor.R, _strokeColor.G, value, _strokeColor.A);
    }

    public float StrokeWidth
    {
        get => _strokeWidth;
        set => SetField(ref _strokeWidth, value);
    }

    /// <summary>
    /// Sigma (радиус) для blur-фильтра
    /// </summary>
    public float BlurSigma
    {
        get => _blurSigma;
        set => SetField(ref _blurSigma, value);
    }

    public bool IsToolbarPinned
    {
        get => _isToolbarPinned;
        set => SetField(ref _isToolbarPinned, value);
    }

    /// <summary>
    /// Полупрозрачность тулбара
    /// </summary>
    public bool ToolbarTranslucent
    {
        get => _toolbarTranslucent;
        set => SetField(ref _toolbarTranslucent, value);
    }

    /// <summary>
    /// Делает холст слегка видимым (тонкий dark-tint), чтобы пользователь
    /// понимал, где можно рисовать. Не путать с IsDrawingEnabled
    /// </summary>
    public bool OverlayDimEnabled
    {
        get => _overlayDimEnabled;
        set => SetField(ref _overlayDimEnabled, value);
    }

    public bool IsDrawingEnabled
    {
        get => _isDrawingEnabled;
        set => SetField(ref _isDrawingEnabled, value);
    }

    /// <summary>
    /// Заливка для геометрических фигур (Rectangle/Ellipse). Цвет заливки —
    /// текущий StrokeColor с альфой 0x60. Включается в тулбаре
    /// </summary>
    public bool FillEnabled
    {
        get => _fillEnabled;
        set => SetField(ref _fillEnabled, value);
    }

    public TextShape? EditingText
    {
        get => _editingText;
        private set => SetField(ref _editingText, value);
    }

    public void OnInputDown(PointF local, PointF screen)
    {
        if (!_isDrawingEnabled) return;

        switch (_activeTool)
        {
            case ToolType.KnifeDelete:
                HandleKnifeClick(local);
                return;
            case ToolType.Eyedropper:
                HandleEyedropperClick(screen);
                return;
            case ToolType.Move:
                BeginMove(local);
                return;
            case ToolType.Blur:
                BeginBlur(local, screen);
                return;
        }

        BeginStroke(local);
    }

    public void OnInputMove(PointF local, PointF screen, bool constrain = false)
    {
        if (_movingShape is not null) { ContinueMove(local); return; }
        if (_blurInProgress) { ContinueBlur(local, constrain); return; }
        if (_tool is not null) { ContinueStroke(local, constrain); return; }
    }

    public void OnInputUp(PointF local, PointF screen, bool constrain = false)
    {
        if (_movingShape is not null) { EndMove(local); return; }
        if (_blurInProgress) { EndBlur(local, screen, constrain); return; }
        if (_tool is not null) { EndStroke(local, constrain); return; }
    }

    // ---- ITool-flow (Pencil/Brush/Marker/Eraser/Rect/Ellipse/Line/Arrow/Text) ----

    private void BeginStroke(PointF p)
    {
        if (!ToolFactory.IsImplemented(_activeTool)) return;
        _tool = ToolFactory.Create(_activeTool);
        var settings = new ToolSettings(_strokeColor, _strokeWidth, _fillEnabled);
        var shape = _tool.OnPointerDown(p, settings);
        if (shape is not null) Canvas.BeginPending(shape);
    }

    private void ContinueStroke(PointF p, bool constrain)
    {
        if (_tool is null) return;
        _tool.OnPointerMove(p, constrain);
        Canvas.UpdatePending();
    }

    private void EndStroke(PointF p, bool constrain)
    {
        if (_tool is null) return;
        IShape? shape = _tool.OnPointerUp(p, constrain);
        _tool = null;
        Canvas.CancelPending();
        if (shape is null) return;
        History.Execute(new AddShapeCommand(Canvas, shape));

        if (shape is TextShape ts && string.IsNullOrEmpty(ts.Text))
            EditingText = ts;
    }

    // ---- Knife ----

    private void HandleKnifeClick(PointF p)
    {
        var victim = HitTestTopmost(p, tolerance: 4f);
        if (victim is null) return;
        History.Execute(new RemoveShapeCommand(Canvas, victim));
    }

    /// <summary>
    /// Идём с конца — последняя добавленная фигура "выше" по Z
    /// </summary>
    private IShape? HitTestTopmost(PointF p, float tolerance)
    {
        for (int i = Canvas.Shapes.Count - 1; i >= 0; i--)
        {
            if (Canvas.Shapes[i].HitTest(p, tolerance))
                return Canvas.Shapes[i];
        }
        return null;
    }

    // ---- Move ----

    private void BeginMove(PointF p)
    {
        _movingShape = HitTestTopmost(p, tolerance: 6f);
        _moveLastPoint = p;
        _moveTotalDx = 0f;
        _moveTotalDy = 0f;
    }

    private void ContinueMove(PointF p)
    {
        var s = _movingShape;
        if (s is null) return;
        var dx = p.X - _moveLastPoint.X;
        var dy = p.Y - _moveLastPoint.Y;
        s.Translate(dx, dy);
        _moveTotalDx += dx;
        _moveTotalDy += dy;
        _moveLastPoint = p;
        Canvas.UpdatePending();
    }

    private void EndMove(PointF p)
    {
        var s = _movingShape;
        _movingShape = null;
        if (s is null) return;

        if (_moveTotalDx != 0f || _moveTotalDy != 0f)
        {
            s.Translate(-_moveTotalDx, -_moveTotalDy);
            History.Execute(new MoveShapeCommand(Canvas, s, _moveTotalDx, _moveTotalDy));
        }
    }

    // ---- Eyedropper ----

    private void HandleEyedropperClick(PointF screen)
    {
        var svc = ScreenServices;
        if (svc is null) return;
        var c = svc.GetPixel((int)screen.X, (int)screen.Y);
        StrokeColor = c;
    }

    // ---- Blur (двух-точечный, с захватом региона на отпускании) ----

    private void BeginBlur(PointF local, PointF screen)
    {
        _blurInProgress = true;
        _blurStartLocal = local;
        _blurStartScreen = screen;
        _blurPreview = new RectangleShape(local,
            ShapeStyle.StrokeOnly(_strokeColor, MathF.Max(1f, _strokeWidth * 0.5f)));
        Canvas.BeginPending(_blurPreview);
    }

    private void ContinueBlur(PointF local, bool constrain)
    {
        if (_blurPreview is null) return;

        if (constrain)
        {
            var dx = local.X - _blurStartLocal.X;
            var dy = local.Y - _blurStartLocal.Y;
            var max = MathF.Max(MathF.Abs(dx), MathF.Abs(dy));
            float signX = dx >= 0 ? 1f : -1f;
            float signY = dy >= 0 ? 1f : -1f;
            local = new PointF(_blurStartLocal.X + signX * max, _blurStartLocal.Y + signY * max);
        }

        _blurPreview.SetEnd(local);
        Canvas.UpdatePending();
    }

    private void EndBlur(PointF local, PointF screen, bool constrain)
    {
        if (constrain)
        {
            var dx = local.X - _blurStartLocal.X;
            var dy = local.Y - _blurStartLocal.Y;
            var max = MathF.Max(MathF.Abs(dx), MathF.Abs(dy));
            float signX = dx >= 0 ? 1f : -1f;
            float signY = dy >= 0 ? 1f : -1f;
            var newLocal = new PointF(_blurStartLocal.X + signX * max, _blurStartLocal.Y + signY * max);

            screen = new PointF(_blurStartScreen.X + (newLocal.X - _blurStartLocal.X),
                                _blurStartScreen.Y + (newLocal.Y - _blurStartLocal.Y));
            local = newLocal;
        }

        var preview = _blurPreview;
        _blurPreview = null;
        _blurInProgress = false;
        Canvas.CancelPending();

        if (preview is null) return;

        // Размеры
        var lx = MathF.Min(_blurStartLocal.X, local.X);
        var ly = MathF.Min(_blurStartLocal.Y, local.Y);
        var lr = MathF.Max(_blurStartLocal.X, local.X);
        var lb = MathF.Max(_blurStartLocal.Y, local.Y);
        var localRect = new RectF(lx, ly, lr, lb);
        if (localRect.Width < 4f || localRect.Height < 4f) return;

        var sx = (int)MathF.Min(_blurStartScreen.X, screen.X);
        var sy = (int)MathF.Min(_blurStartScreen.Y, screen.Y);
        var w = (int)(MathF.Max(_blurStartScreen.X, screen.X) - sx);
        var h = (int)(MathF.Max(_blurStartScreen.Y, screen.Y) - sy);

        var svc = ScreenServices;
        if (svc is null || w <= 0 || h <= 0) return;

        if (_liveBlurEnabled && FrameProvider is not null)
        {
            var live = new LiveBlurShape(localRect, sx, sy, w, h, _blurSigma, FrameProvider);
            History.Execute(new AddShapeCommand(Canvas, live));
            return;
        }

        var pixels = svc.CaptureRegionBgra(sx, sy, w, h);
        if (pixels.Length == 0) return;

        var blur = new BlurShape(localRect, pixels, w, h, _blurSigma);
        History.Execute(new AddShapeCommand(Canvas, blur));
    }

    // ---- Текст ----

    public void CommitTextEditing(string text)
    {
        var ts = _editingText;
        if (ts is null) return;
        if (string.IsNullOrWhiteSpace(text))
        {
            History.Undo();
        }
        else
        {
            ts.Text = text;
            Canvas.UpdatePending();
        }
        EditingText = null;
    }

    public void CancelTextEditing()
    {
        if (_editingText is null) return;
        History.Undo();
        EditingText = null;
    }

    public void Clear()
    {
        Canvas.Clear();
        History.Clear();
    }

    // ---- Совместимость со старым API ----
    public void BeginStrokeAt(PointF p) => OnInputDown(p, p);
    public void ContinueStrokeAt(PointF p) => OnInputMove(p, p);
    public void EndStrokeAt(PointF p) => OnInputUp(p, p);
}