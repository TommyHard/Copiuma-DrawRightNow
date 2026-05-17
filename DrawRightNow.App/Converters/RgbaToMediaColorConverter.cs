using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DrawRightNow.Core.Models;

using Color = System.Windows.Media.Color;

namespace DrawRightNow.App.Converters;

/// <summary>
/// Конвертирует ColorRgba (из Core.Models) в System.Windows.Media.Color
/// </summary>
public sealed class RgbaToMediaColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ColorRgba c)
        {
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}