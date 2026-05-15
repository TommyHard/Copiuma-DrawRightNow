using System;

namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Базовый интерфейс векторного элемента.
/// Конкретные фигуры (Stroke, Rectangle, Text, etc.) реализуют его, а слой
/// рендера принимает на вход IShape и решает, как именно рисовать
/// </summary>
public interface IShape
{
    Guid Id { get; }
    RectF Bounds { get; }
    bool HitTest(PointF p, float tolerance);
}