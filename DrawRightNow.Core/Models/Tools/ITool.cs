using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Контракт активного инструмента. Преобразует ввод (нажатие/движение/отпускание)
/// в новую/обновляемую фигуру.
/// Возвращаемый тип IShape — общий, чтобы один и тот же контракт обслуживал
/// и штрихи (Pencil/Brush/Marker/Eraser), и геометрические фигуры
/// (Rectangle/Ellipse/Line/Arrow), и текст
/// </summary>
public interface ITool
{
    ToolType Type { get; }

    /// <summary>
    /// Начало взаимодействия. Возвращает новую "черновую" фигуру либо null
    /// </summary>
    IShape? OnPointerDown(PointF p, ToolSettings settings);

    /// <summary>
    /// Продолжение жеста. Должен работать без аллокаций
    /// </summary>
    void OnPointerMove(PointF p);

    /// <summary>
    /// Завершение жеста. Возвращает финальную фигуру (или null, если жест отменён)
    /// </summary>
    IShape? OnPointerUp(PointF p);
}