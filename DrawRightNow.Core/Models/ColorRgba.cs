using System.Globalization;

namespace DrawRightNow.Core.Models;

/// <summary>
/// Представление цвета. Используется в Core, конвертируется
/// в SKColor / System.Windows.Media.Color на уровнях рендера / UI
/// </summary>
public readonly record struct ColorRgba(byte R, byte G, byte B, byte A)
{
    public static readonly ColorRgba Red = new(0xE7, 0x4C, 0x3C, 0xFF);
    public static readonly ColorRgba Green = new(0x2E, 0xCC, 0x71, 0xFF);
    public static readonly ColorRgba Blue = new(0x34, 0x98, 0xDB, 0xFF);
    public static readonly ColorRgba Yellow = new(0xF1, 0xC4, 0x0F, 0xFF);
    public static readonly ColorRgba Black = new(0x00, 0x00, 0x00, 0xFF);
    public static readonly ColorRgba White = new(0xFF, 0xFF, 0xFF, 0xFF);

    public uint ToArgb() => (uint)(A << 24 | R << 16 | G << 8 | B);

    public ColorRgba WithAlpha(byte alpha) => new(R, G, B, alpha);

    public static ColorRgba FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) throw new ArgumentException("hex");
        var h = hex.TrimStart('#');
        if (h.Length == 6) h = "FF" + h;
        if (h.Length != 8) throw new FormatException($"Bad hex color: {hex}");
        var argb = uint.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new ColorRgba(
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF),
            (byte)((argb >> 24) & 0xFF));
    }
}