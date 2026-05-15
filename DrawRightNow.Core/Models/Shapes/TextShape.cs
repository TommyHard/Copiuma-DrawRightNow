using System;

namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Текстовая фигура. Text и Position мутируемые — для инлайн-редактирования
/// и перемещения инструментом Move
/// </summary>
public sealed class TextShape : IShape
{
    private string _text;

    public TextShape(PointF position, string text, ColorRgba color, float fontSize)
    {
        Id = Guid.NewGuid();
        Position = position;
        _text = text ?? string.Empty;
        Color = color;
        FontSize = fontSize;
    }

    public Guid Id { get; }
    public PointF Position { get; private set; }
    public ColorRgba Color { get; }
    public float FontSize { get; }

    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public RectF Bounds
    {
        get
        {
            var w = MathF.Max(1f, _text.Length * FontSize * 0.55f);
            var h = FontSize * 1.2f;
            return new RectF(Position.X, Position.Y - h, Position.X + w, Position.Y);
        }
    }

    public bool HitTest(PointF p, float tolerance)
        => Bounds.Inflate(tolerance).Contains(p);

    public void Translate(float dx, float dy)
        => Position = new PointF(Position.X + dx, Position.Y + dy);
}