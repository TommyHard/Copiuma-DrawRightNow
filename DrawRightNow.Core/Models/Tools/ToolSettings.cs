namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Снимок настроек инструмента на момент начала жеста. Передаётся
/// в ITool.OnPointerDown — внутри жеста не меняется.
/// FillEnabled + FillAlpha заданы для геометрических фигур; рисующие
/// инструменты их игнорируют
/// </summary>
public readonly record struct ToolSettings(
    ColorRgba Color,
    float Width,
    bool FillEnabled = false,
    byte FillAlpha = 0x60);