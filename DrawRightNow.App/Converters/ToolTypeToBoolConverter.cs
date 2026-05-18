using DrawRightNow.Core.Models.Tools;
using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace DrawRightNow.App.Converters;

public sealed class ToolTypeToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ToolType current && parameter is string name &&
            Enum.TryParse<ToolType>(name, out var target))
        {
            return current == target;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && parameter is string name &&
            Enum.TryParse<ToolType>(name, out var target))
        {
            return isChecked ? target : ToolType.None;
        }

        return Binding.DoNothing;
    }
}