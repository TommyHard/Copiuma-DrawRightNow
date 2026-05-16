using System;

namespace DrawRightNow.Core.Services;

/// <summary>
/// Источник свежих кадров экрана для «живого» blur. Реализация в App-слое
/// (polling BitBlt 30fps + WDA_EXCLUDEFROMCAPTURE) или, в перспективе,
/// через Windows Graphics Capture API. Контракт намеренно простой — отдаёт
/// сырые BGRA-пиксели прямоугольного региона
/// </summary>
public interface IFrameProvider
{
    /// <summary>
    /// Текущая «версия» кадра. Меняется каждый раз, когда сервис
    /// обновляет внутренний буфер. Рендер использует для инвалидации
    /// SKImage-кэша
    /// </summary>
    long FrameVersion { get; }

    /// <summary>
    /// Извлечь BGRA-пиксели региона из последнего кадра. Возвращает null,
    /// если регион вне границ или кадров ещё нет
    /// </summary>
    byte[]? TryGetRegionBgra(int screenX, int screenY, int width, int height);

    /// <summary>
    /// Подписка хотя бы одного клиента (live-blur). Сервис может включить
    /// захват только когда клиенты есть, и выключить, когда счётчик = 0
    /// </summary>
    IDisposable Subscribe();

    /// <summary>
    /// Событие нового кадра — DrawingSurface слушает, чтобы вызвать InvalidateVisual
    /// </summary>
    event Action? FrameUpdated;
}