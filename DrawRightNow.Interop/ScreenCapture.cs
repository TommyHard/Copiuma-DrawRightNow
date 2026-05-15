using System;

namespace DrawRightNow.Interop;

/// <summary>
/// Низкоуровневые операции захвата экрана через GDI. Эта обёртка
/// платформо-зависима (Windows), но не зависит от WPF — поэтому живёт
/// в Interop. App-слой добавляет к ним временное скрытие overlay-окна
/// </summary>
public static class ScreenCapture
{
    /// <summary>
    /// Цвет пикселя десктопа в screen-координатах
    /// </summary>
    public static (byte R, byte G, byte B) GetPixel(int x, int y)
    {
        var dc = NativeMethods.GetDC(IntPtr.Zero);
        if (dc == IntPtr.Zero) return (0, 0, 0);
        try
        {
            var bgr = NativeMethods.GetPixel(dc, x, y);
            // GetPixel: 0x00BBGGRR
            var r = (byte)(bgr & 0xFF);
            var g = (byte)((bgr >> 8) & 0xFF);
            var b = (byte)((bgr >> 16) & 0xFF);
            return (r, g, b);
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, dc);
        }
    }

    /// <summary>
    /// Снимок прямоугольного региона экрана. Возвращает BGRA32 (GetDIBits-формат:
    /// строки сверху-вниз при biHeight &lt; 0). Длина = width*height*4
    /// </summary>
    public static byte[] CaptureRegion(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return Array.Empty<byte>();

        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero) return Array.Empty<byte>();

        IntPtr memDc = IntPtr.Zero;
        IntPtr bmp = IntPtr.Zero;
        IntPtr oldBmp = IntPtr.Zero;
        try
        {
            memDc = NativeMethods.CreateCompatibleDC(screenDc);
            bmp = NativeMethods.CreateCompatibleBitmap(screenDc, width, height);
            if (memDc == IntPtr.Zero || bmp == IntPtr.Zero) return Array.Empty<byte>();

            oldBmp = NativeMethods.SelectObject(memDc, bmp);

            NativeMethods.BitBlt(memDc, 0, 0, width, height, screenDc, x, y,
                NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT);

            var bmi = new NativeMethods.BITMAPINFO
            {
                bmiHeader = new NativeMethods.BITMAPINFOHEADER
                {
                    biSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height,    // top-down
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = NativeMethods.BI_RGB
                }
            };

            var pixels = new byte[width * height * 4];
            NativeMethods.GetDIBits(memDc, bmp, 0, (uint)height, pixels, ref bmi, NativeMethods.DIB_RGB_COLORS);
            return pixels;
        }
        finally
        {
            if (oldBmp != IntPtr.Zero) NativeMethods.SelectObject(memDc, oldBmp);
            if (bmp != IntPtr.Zero) NativeMethods.DeleteObject(bmp);
            if (memDc != IntPtr.Zero) NativeMethods.DeleteDC(memDc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    /// <summary>
    /// Установка alpha слоистого окна (для краткого скрытия overlay при снимке)
    /// </summary>
    public static void SetLayeredAlpha(IntPtr hwnd, byte alpha)
    {
        if (hwnd == IntPtr.Zero) return;
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, alpha, NativeMethods.LWA_ALPHA);
    }
}