namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// Снимок прямоугольного региона экрана. Растровый: хранит "сырые" пиксели
/// (BGRA, формат GetDIBits) — рендер преобразует в SKImage и применяет
/// blur-фильтр через GPU
/// </summary>
public sealed class BlurShape : IShape
{
    private RectF _placement;

    public BlurShape(RectF placement, byte[] bgraPixels, int pixelWidth, int pixelHeight, float sigma)
    {
        Id = Guid.NewGuid();
        _placement = placement;
        BgraPixels = bgraPixels ?? throw new ArgumentNullException(nameof(bgraPixels));
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        Sigma = sigma;
    }

    public Guid Id { get; }
    public byte[] BgraPixels { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public float Sigma { get; }

    public RectF Bounds => _placement;

    public bool HitTest(PointF p, float tolerance)
        => _placement.Inflate(tolerance).Contains(p);

    public void Translate(float dx, float dy)
    {
        _placement = new RectF(
            _placement.Left + dx, _placement.Top + dy,
            _placement.Right + dx, _placement.Bottom + dy);
    }
}