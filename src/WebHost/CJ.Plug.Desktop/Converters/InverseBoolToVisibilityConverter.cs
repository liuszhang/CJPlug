using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CJ.Plug.Desktop.Converters;

/// <summary>
/// 反向 BooleanToVisibilityConverter：true → Collapsed，false → Visible。
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
            return v != Visibility.Visible;
        return false;
    }
}
