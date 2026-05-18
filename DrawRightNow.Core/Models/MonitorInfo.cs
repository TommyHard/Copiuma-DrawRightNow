namespace DrawRightNow.Core.Models;

/// <summary>
/// Информация о мониторе для multi-monitor режима
/// Координаты задаются в WPF DIP-единицах (logical units),
/// чтобы их можно было напрямую присвоить Window.Left/Top/Width/Height
/// </summary>
public sealed class MonitorInfo
{
    public int Index { get; set; }

    /// <summary>
    /// Отображаемое имя пункта в списке
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public bool IsPrimary { get; set; }

    /// <summary>
    /// true — специальный пункт "все мониторы" (виртуальный экран)
    /// </summary>
    public bool IsAllMonitors { get; set; }

    public override string ToString() => DisplayName;
}