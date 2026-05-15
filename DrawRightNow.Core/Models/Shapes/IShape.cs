using System;

namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Базовый интерфейс векторного элемента
/// </summary>
public interface IShape
{
    Guid Id { get; }
    RectF Bounds { get; }

    bool HitTest(PointF p, float tolerance);

    /// <summary>
    /// Сдвиг всей фигуры. Используется инструментом Move и MoveShapeCommand
    /// (Undo через Translate(-dx, -dy))
    /// </summary>
    void Translate(float dx, float dy);
}
