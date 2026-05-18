using DrawRightNow.Core.Models;

namespace DrawRightNow.Core.Services;

/// <summary>
/// Услуги уровня экрана, нужные инструментам Eyedropper и Blur.
/// Реализуется в App-слое (доступен HWND overlay-окна для временного
/// скрытия в момент захвата); Core держит только контракт
/// </summary>
public interface IScreenServices
{
    /// <summary>
    /// Цвет пикселя в screen-координатах. Используется Eyedropper
    /// </summary>
    ColorRgba GetPixel(int screenX, int screenY);

    /// <summary>
    /// Снимок прямоугольного региона. Возвращает "сырые" пиксели BGRA32 (GetDIBits),
    /// width = pixelWidth, stride = pixelWidth*4. Длина массива = stride*pixelHeight.
    /// Реализация должна сама скрыть overlay на время снимка, чтобы
    /// собственные штрихи не попали в кадр
    /// </summary>
    byte[] CaptureRegionBgra(int screenX, int screenY, int width, int height);

    byte[] CaptureLiveRegionBgra(int screenX, int screenY, int width, int height);
}