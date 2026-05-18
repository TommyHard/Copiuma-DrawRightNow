using DrawRightNow.Core.Commands;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Shapes;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.Core.Mvvm;
using DrawRightNow.Core.Services;
using System.Collections.ObjectModel;

namespace DrawRightNow.Core.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly RelayCommand _undoCmd;
    private readonly RelayCommand _redoCmd;
    private readonly RelayCommand _clearCmd;
    private readonly RelayCommand _cycleDockCmd;

    private ITool? _tool;
    private bool _isOptionsOpen;
    private ToolType _activeTool = ToolType.None;
    private ColorRgba _strokeColor = ColorRgba.Red;
    private float _strokeWidth = 3f;
    private bool _isToolbarLocked = false;
    private bool _toolbarTranslucent = false;
    private int _overlayDimAlpha = 20;
    private bool _fillEnabled = false;
    private bool _isColorPickerOpen;
    private bool _isMonitorPickerOpen;
    private MonitorInfo? _selectedMonitor;
    private TextShape? _editingText;

    private IShape? _movingShape;
    private PointF _moveLastPoint;
    private float _moveTotalDx;
    private float _moveTotalDy;

    private bool _blurInProgress;
    private PointF _blurStartLocal;
    private PointF _blurStartScreen;
    private RectangleShape? _blurPreview;

    private const float BlurMaxSigma = 100;

    public AppSettings Settings { get; } = AppSettings.Load();

    public bool IsToolVisible(ToolType tool) => Settings.VisibleTools.Contains(tool);

    public MainViewModel()
    {
        Canvas = new CanvasModel();
        History = new CommandHistory();

        _undoCmd = new RelayCommand(_ => History.Undo(), _ => History.CanUndo);
        _redoCmd = new RelayCommand(_ => History.Redo(), _ => History.CanRedo);
        _clearCmd = new RelayCommand(_ => Clear(), _ => Canvas.Shapes.Count > 0);

        // Для циклического переключения позиции
        _cycleDockCmd = new RelayCommand(_ => CycleToolbarDock());

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

    public IScreenServices? ScreenServices { get; set; }

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
    public bool Show_AreaFill { get => IsToolVisible(ToolType.AreaFill); set => ToggleToolVisibility(ToolType.AreaFill, value); }
    public bool Show_Eyedropper { get => IsToolVisible(ToolType.Eyedropper); set => ToggleToolVisibility(ToolType.Eyedropper, value); }
    public bool Show_Blur { get => IsToolVisible(ToolType.Blur); set => ToggleToolVisibility(ToolType.Blur, value); }

    public bool ShowInTray
    {
        get => Settings.ShowInTray;
        set { Settings.ShowInTray = value; Settings.Save(); OnPropertyChanged(); }
    }

    public bool Show_WidthSlider
    {
        get => Settings.ShowWidthSlider;
        set
        {
            if (Settings.ShowWidthSlider == value) return;
            Settings.ShowWidthSlider = value;
            Settings.Save();
            OnPropertyChanged();
        }
    }

    public bool Show_ClearTool
    {
        get => Settings.ShowClearTool;
        set
        {
            if (Settings.ShowClearTool == value) return;
            Settings.ShowClearTool = value;
            Settings.Save();
            OnPropertyChanged();
        }
    }

    public bool Show_SaveTool
    {
        get => Settings.ShowSaveTool;
        set
        {
            if (Settings.ShowSaveTool == value) return;
            Settings.ShowSaveTool = value;
            Settings.Save();
            OnPropertyChanged();
        }
    }

    public bool Show_ClipboardTool
    {
        get => Settings.ShowClipboardTool;
        set
        {
            if (Settings.ShowClipboardTool == value) return;
            Settings.ShowClipboardTool = value;
            Settings.Save();
            OnPropertyChanged();
        }
    }

    public bool Show_ChangePositionTool
    {
        get => Settings.ShowChangePositionTool;
        set
        {
            if (Settings.ShowChangePositionTool == value) return;
            Settings.ShowChangePositionTool = value;
            Settings.Save();
            OnPropertyChanged();
        }
    }

    public bool Show_FadingMode
    {
        get => Settings.ShowFadingMode;
        set
        {
            if (Settings.ShowFadingMode == value) return;
            Settings.ShowFadingMode = value;
            Settings.Save();
            OnPropertyChanged();
        }
    }

    public RelayCommand UndoCommand => _undoCmd;
    public RelayCommand RedoCommand => _redoCmd;
    public RelayCommand ClearCommand => _clearCmd;
    public RelayCommand CycleDockCommand => _cycleDockCmd;

    // Свойство для привязки позиции панели
    public string ToolbarDock
    {
        get => Settings.ToolbarDock;
        set
        {
            if (Settings.ToolbarDock != value)
            {
                Settings.ToolbarDock = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }
    }

    private void CycleToolbarDock()
    {
        var docks = new[] { "BottomRight", "BottomLeft", "TopLeft", "TopRight" };
        int idx = Array.IndexOf(docks, ToolbarDock);
        if (idx == -1)
        {
            idx = 3;
        }

        ToolbarDock = docks[(idx + 1) % docks.Length];
    }

    public ToolType ActiveTool
    {
        get => _activeTool;
        set
        {
            if (SetField(ref _activeTool, value))
            {
                OnPropertyChanged(nameof(ActiveToolDisplayName));
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

    public bool IsToolbarLocked
    {
        get => _isToolbarLocked;
        set => SetField(ref _isToolbarLocked, value);
    }

    public bool RememberToolbarPosition
    {
        get => Settings.RememberToolbarPosition;
        set
        {
            if (Settings.RememberToolbarPosition == value) return;
            Settings.RememberToolbarPosition = value;
            Settings.Save();
            OnPropertyChanged();
        }
    }

    public ObservableCollection<MonitorInfo> Monitors { get; } = new();

    public MonitorInfo? SelectedMonitor
    {
        get => _selectedMonitor;
        set => SetField(ref _selectedMonitor, value);
    }

    public bool IsMonitorPickerOpen
    {
        get => _isMonitorPickerOpen;
        set => SetField(ref _isMonitorPickerOpen, value);
    }

    public bool ToolbarTranslucent
    {
        get => _toolbarTranslucent;
        set => SetField(ref _toolbarTranslucent, value);
    }

    public int OverlayDimAlpha
    {
        get => _overlayDimAlpha;
        set
        {
            var clamped = value < 0 ? 0 : (value > 100 ? 100 : value);
            if (SetField(ref _overlayDimAlpha, clamped))
                OnPropertyChanged(nameof(OverlayDimOpacity));
        }
    }

    public double OverlayDimOpacity => _overlayDimAlpha / 100.0;

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
        if (_activeTool == ToolType.None) return;

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

    public void NotifySettingsChanged()
    {
        OnPropertyChanged(nameof(Settings));
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

    private void HandleKnifeClick(PointF p)
    {
        var victim = HitTestTopmost(p, tolerance: 4f);
        if (victim is null) return;
        History.Execute(new RemoveShapeCommand(Canvas, victim));
    }

    private IShape? HitTestTopmost(PointF p, float tolerance)
    {
        for (int i = Canvas.Shapes.Count - 1; i >= 0; i--)
        {
            if (Canvas.Shapes[i].HitTest(p, tolerance))
                return Canvas.Shapes[i];
        }
        return null;
    }

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

    private void HandleEyedropperClick(PointF screen)
    {
        var svc = ScreenServices;
        if (svc is null) return;
        var c = svc.GetPixel((int)screen.X, (int)screen.Y);
        StrokeColor = c;

        // Отключение после выбора цвета
        ActiveTool = ToolType.None;
    }

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

        var pixels = svc.CaptureRegionBgra(sx, sy, w, h);
        if (pixels.Length == 0) return;

        var blur = new BlurShape(localRect, pixels, w, h, BlurMaxSigma);
        History.Execute(new AddShapeCommand(Canvas, blur));
    }

    private string _editOriginalText = string.Empty;
    private float _editOriginalFontSize = 0;
    private bool _editIsExisting = false;

    public void BeginEditExisting(TextShape shape)
    {
        if (shape is null) return;
        _editOriginalText = shape.Text;
        _editOriginalFontSize = shape.FontSize;
        _editIsExisting = true;
        EditingText = shape;
    }

    public void ChangeEditingFontSize(float delta)
    {
        var ts = _editingText;
        if (ts is null) return;
        var s = ts.FontSize + delta;
        if (s < 8f) s = 8f;
        if (s > 256f) s = 256f;
        ts.FontSize = s;
        Canvas.UpdatePending();
    }

    public void CommitTextEditing(string text)
    {
        var ts = _editingText;
        if (ts is null) return;

        if (_editIsExisting)
        {
            var newText = text ?? string.Empty;
            var newFontSize = ts.FontSize;

            if (string.IsNullOrEmpty(newText))
            {
                ts.Text = _editOriginalText;
                ts.FontSize = _editOriginalFontSize;
                Canvas.UpdatePending();
            }
            else if (newText != _editOriginalText || newFontSize != _editOriginalFontSize)
            {
                ts.Text = _editOriginalText;
                ts.FontSize = _editOriginalFontSize;
                History.Execute(new EditTextCommand(Canvas, ts,
                    _editOriginalText, _editOriginalFontSize,
                    newText, newFontSize));
            }
            ResetEditState();
            EditingText = null;
            return;
        }

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

        if (_editIsExisting)
        {
            var ts = _editingText;
            ts.Text = _editOriginalText;
            ts.FontSize = _editOriginalFontSize;
            Canvas.UpdatePending();
            ResetEditState();
            EditingText = null;
            return;
        }

        History.Undo();
        EditingText = null;
    }

    private void ResetEditState()
    {
        _editIsExisting = false;
        _editOriginalText = string.Empty;
        _editOriginalFontSize = 0;
    }

    public TextShape? HitTestTextAt(PointF p, float tolerance = 4f)
    {
        for (int i = Canvas.Shapes.Count - 1; i >= 0; i--)
        {
            if (Canvas.Shapes[i] is TextShape ts &&
                !string.IsNullOrWhiteSpace(ts.Text) &&
                ts.HitTest(p, tolerance))
            {
                return ts;
            }
        }
        return null;
    }

    public void Clear()
    {
        Canvas.Clear();
        History.Clear();
    }
}