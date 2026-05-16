using DrawRightNow.Core.Models.Tools;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace DrawRightNow.App.Converters;

/// <summary>
/// Связывает ToggleButton.IsChecked с MainViewModel.ActiveTool
/// ConverterParameter = имя ToolType ("Pencil", etc.)
/// </summary>
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
        if (value is bool b && b && parameter is string name &&
            Enum.TryParse<ToolType>(name, out var target))
        {
            return target;
        }
        return Binding.DoNothing;
    }
}