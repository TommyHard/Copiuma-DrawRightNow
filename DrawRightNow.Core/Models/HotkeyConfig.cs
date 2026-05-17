namespace DrawRightNow.Core.Models;

public sealed class HotkeyConfig
{
    public uint Modifiers { get; set; }
    public uint VirtualKey { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}