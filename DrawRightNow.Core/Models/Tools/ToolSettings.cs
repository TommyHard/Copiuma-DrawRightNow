namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Снимок настроек инструмента на момент начала жеста
/// Передаётся в ITool.OnPointerDown — внутри жеста не меняется
/// </summary>
public readonly record struct ToolSettings(ColorRgba Color, float Width);