using System;
using DrawRightNow.Core.Services;

namespace DrawRightNow.Core.Models.Shapes;

/// <summary>
/// "Живая" версия BlurShape: вместо одного снимка хранит ссылку на
/// IFrameProvider и каждые ~33мс показывает свежий кадр под областью.
/// Реализует IDisposable, чтобы освобождать подписку на захват
/// </summary>
public sealed class LiveBlurShape : IShape, IDisposable
{
    private RectF _placement;
    private int _screenX, _screenY;
    private readonly IDisposable _subscription;
    private bool _disposed;

    public LiveBlurShape(
        RectF placement,
        int screenX, int screenY,
        int width, int height,
        float sigma,
        IFrameProvider provider)
    {
        Id = Guid.NewGuid();
        _placement = placement;
        _screenX = screenX;
        _screenY = screenY;
        Width = width;
        Height = height;
        Sigma = sigma;
        Provider = provider;
        _subscription = provider.Subscribe();
    }

    public Guid Id { get; }
    public IFrameProvider Provider { get; }
    public int Width { get; }
    public int Height { get; }
    public float Sigma { get; }
    public int ScreenX => _screenX;
    public int ScreenY => _screenY;

    public RectF Bounds => _placement;

    public bool HitTest(PointF p, float tolerance)
        => _placement.Inflate(tolerance).Contains(p);

    public void Translate(float dx, float dy)
    {
        _placement = new RectF(
            _placement.Left + dx, _placement.Top + dy,
            _placement.Right + dx, _placement.Bottom + dy);
        _screenX += (int)dx;
        _screenY += (int)dy;
    }

    public byte[]? CurrentFrameBgra()
        => Provider.TryGetRegionBgra(_screenX, _screenY, Width, Height);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscription.Dispose();
    }
}