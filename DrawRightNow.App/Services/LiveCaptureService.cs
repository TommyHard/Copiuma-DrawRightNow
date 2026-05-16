using System;
using System.Threading;
using DrawRightNow.Core.Services;
using DrawRightNow.Interop;
using Timer = System.Threading.Timer;

namespace DrawRightNow.App.Services;

/// <summary>
/// Polling-реализация IFrameProvider: каждые ~33 мс делает BitBlt всего
/// первичного экрана в общий буфер. Подписчики (LiveBlurShape) выбирают
/// из него свои регионы. Захват включается только когда есть подписчики
/// (счётчик &gt; 0), что экономит CPU/GPU в простое
/// </summary>
public sealed class LiveCaptureService : IFrameProvider, IDisposable
{
    private readonly object _bufLock = new();
    private byte[] _buffer = Array.Empty<byte>();
    private int _bufWidth;
    private int _bufHeight;
    private long _frameVersion;

    private Timer? _timer;
    private int _subscribers;
    private bool _disposed;

    private readonly int _screenWidth;
    private readonly int _screenHeight;

    public LiveCaptureService(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public long FrameVersion => Interlocked.Read(ref _frameVersion);

    public event Action? FrameUpdated;

    public IDisposable Subscribe()
    {
        if (Interlocked.Increment(ref _subscribers) == 1)
            StartCapture();
        return new Sub(this);
    }

    private void Unsubscribe()
    {
        if (Interlocked.Decrement(ref _subscribers) == 0)
            StopCapture();
    }

    private void StartCapture()
    {
        if (_timer is not null) return;
        _timer = new Timer(_ => CaptureOnce(), null, 0, 33);
    }

    private void StopCapture()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void CaptureOnce()
    {
        if (_disposed) return;
        byte[] pixels;
        try
        {
            pixels = ScreenCapture.CaptureRegion(0, 0, _screenWidth, _screenHeight);
        }
        catch
        {
            return;
        }
        if (pixels.Length == 0) return;

        lock (_bufLock)
        {
            _buffer = pixels;
            _bufWidth = _screenWidth;
            _bufHeight = _screenHeight;
            Interlocked.Increment(ref _frameVersion);
        }
        try { FrameUpdated?.Invoke(); } catch { /* ignore */ }
    }

    public byte[]? TryGetRegionBgra(int screenX, int screenY, int width, int height)
    {
        lock (_bufLock)
        {
            if (_buffer.Length == 0 || width <= 0 || height <= 0) return null;
            if (screenX < 0 || screenY < 0) return null;
            if (screenX + width > _bufWidth) return null;
            if (screenY + height > _bufHeight) return null;

            var stride = _bufWidth * 4;
            var outStride = width * 4;
            var result = new byte[outStride * height];
            for (int row = 0; row < height; row++)
            {
                Buffer.BlockCopy(
                    _buffer,
                    (screenY + row) * stride + screenX * 4,
                    result,
                    row * outStride,
                    outStride);
            }
            return result;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopCapture();
    }

    private sealed class Sub : IDisposable
    {
        private LiveCaptureService? _owner;
        public Sub(LiveCaptureService owner) => _owner = owner;
        public void Dispose()
        {
            var o = Interlocked.Exchange(ref _owner, null);
            o?.Unsubscribe();
        }
    }
}