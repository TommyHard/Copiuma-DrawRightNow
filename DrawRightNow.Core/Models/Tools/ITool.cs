using DrawRightNow.Core.Models.Shapes;

namespace DrawRightNow.Core.Models.Tools;

/// <summary>
/// Контракт активного инструмента. Преобразует ввод (нажатие/движение/отпускание)
/// в новую/обновляемую фигуру (или команду удаления, у ластика)
/// </summary>
public interface ITool
{
    ToolType Type { get; }

    /// <summary>
    /// Начало взаимодействия. Возвращает новую "черновую" фигуру либо null
    /// </summary>
    StrokeShape? OnPointerDown(PointF p, ToolSettings settings);

    /// <summary>
    /// Продолжение жеста
    /// Должен работать без аллокаций
    /// </summary>
    void OnPointerMove(PointF p);

    /// <summary>
    /// Завершение жеста. Возвращает финальную фигуру (или null, если жест отменён)
    /// </summary>
    StrokeShape? OnPointerUp(PointF p);
}