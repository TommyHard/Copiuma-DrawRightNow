using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DrawRightNow.Core.Models.Tools;

namespace DrawRightNow.Core.Models;

public sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DrawRightNow", "settings.json");

    public string Language { get; set; } = "ru";
    public bool ShowInTray { get; set; } = true;

    public List<ToolType> VisibleTools { get; set; } = new()
    {
        ToolType.Pencil, ToolType.Brush, ToolType.Marker, ToolType.Eraser,
        ToolType.Rectangle, ToolType.Ellipse, ToolType.Line, ToolType.Arrow,
        ToolType.Text, ToolType.KnifeDelete, ToolType.Move, ToolType.Eyedropper, ToolType.Blur
    };

    public Dictionary<string, HotkeyConfig> Hotkeys { get; set; } = new()
    {
        { "ToggleOverlay", new HotkeyConfig { Modifiers = 3, VirtualKey = 0x44, DisplayText = "Ctrl + Alt + D" } },
        { "ToggleDrawing", new HotkeyConfig { Modifiers = 3, VirtualKey = 0x45, DisplayText = "Ctrl + Alt + E" } },
        { "Undo",          new HotkeyConfig { Modifiers = 3, VirtualKey = 0x5A, DisplayText = "Ctrl + Alt + Z" } },
        { "Redo",          new HotkeyConfig { Modifiers = 3, VirtualKey = 0x59, DisplayText = "Ctrl + Alt + Y" } },
        { "Copy",          new HotkeyConfig { Modifiers = 3, VirtualKey = 0x43, DisplayText = "Ctrl + Alt + C" } }
    };

    // ТЕПЕРЬ ТУТ КЛЮЧ STRING
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