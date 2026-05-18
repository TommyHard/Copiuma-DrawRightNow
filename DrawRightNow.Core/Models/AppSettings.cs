using System.Text.Json;
using DrawRightNow.Core.Models.Tools;

namespace DrawRightNow.Core.Models;

public sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DrawRightNow", "settings.json");

    public string Language { get; set; } = "en";
    public bool ShowInTray { get; set; } = true;

    /// <summary>
    /// Запоминать положение тулбара (drag-offset) после завершения работы
    /// </summary>
    public bool RememberToolbarPosition { get; set; } = false;

    /// <summary>
    /// Видимость slider для выбора толщины линий
    /// </summary>
    public bool ShowWidthSlider { get; set; } = true;

    /// <summary>
    /// Видимость ClearTool
    /// </summary>
    public bool ShowClearTool { get; set; } = true;

    /// <summary>
    /// Видимость SaveTool (сохранение в файл)
    /// </summary>
    public bool ShowSaveTool { get; set; } = true;

    /// <summary>
    /// Видимость ClipboardTool (сохранение в буфер)
    /// </summary>
    public bool ShowClipboardTool { get; set; } = true;

    /// <summary>
    /// Видимость ChangePositionTool (переключение позиции SecondaryToolbarView)
    /// </summary>
    public bool ShowChangePositionTool { get; set; } = true;

    /// <summary>
    /// Видимость FadingMode (прозрачность панелей без фокуса)
    /// </summary>
    public bool ShowFadingMode { get; set; } = true;

    /// <summary>
    /// Сохранённая сторона привязки вспомогательной панели
    /// </summary>
    public string ToolbarDock { get; set; } = "TopRight";

    /// <summary>
    /// Сохранённый drag-offset панели по X (в DIP)
    /// </summary>
    public double ToolbarOffsetX { get; set; } = 0;

    /// <summary>
    /// Сохранённый drag-offset панели по Y (в DIP)
    /// </summary>
    public double ToolbarOffsetY { get; set; } = 0;

    public List<ToolType> VisibleTools { get; set; } = new()
    {
        ToolType.Pencil, ToolType.Brush, ToolType.Marker, ToolType.Eraser,
        ToolType.Rectangle, ToolType.Ellipse, ToolType.Line, ToolType.Arrow,
        ToolType.Text, ToolType.KnifeDelete, ToolType.Move, ToolType.AreaFill, 
        ToolType.Eyedropper, ToolType.Blur
    };

    public Dictionary<string, HotkeyConfig> Hotkeys { get; set; } = new()
    {
        { "ToggleOverlay", new HotkeyConfig { Modifiers = 3, VirtualKey = 0x44, DisplayText = "Ctrl + Alt + D" } },
        { "Undo",          new HotkeyConfig { Modifiers = 3, VirtualKey = 0x5A, DisplayText = "Ctrl + Alt + Z" } },
        { "Redo",          new HotkeyConfig { Modifiers = 3, VirtualKey = 0x59, DisplayText = "Ctrl + Alt + Y" } },
    };

    public Dictionary<string, string> ToolHotkeys { get; set; } = new()
    {
        { "Pencil", "P" },
        { "Brush", "B" },
        { "Marker", "M" },
        { "Eraser", "E" },
        { "Rectangle", "R" },
        { "Ellipse", "O" },
        { "Line", "L" },
        { "Arrow", "A" },
        { "Text", "T" },
        { "KnifeDelete", "K" },
        { "Move", "V" },
        { "Eyedropper", "I" },
        { "Blur", "U" }
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* ignore */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* ignore */ }
    }
}