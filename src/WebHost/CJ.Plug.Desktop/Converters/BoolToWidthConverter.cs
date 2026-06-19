using System;
using System.Globalization;
using System.Windows.Data;

namespace CJ.Plug.Desktop.Converters;

/// <summary>
/// 布尔值到宽度的转换器：true=收起(窄)，false=展开(宽)。
/// 参数可指定窄宽度，默认 64；展开宽度硬编码 220。
/// </summary>
public class BoolToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var collapsed = value is true;
        if (collapsed)
        {
            if (parameter is double d) return d;
            if (parameter is string s && double.TryParse(s, out var parsed)) return parsed;
            return 64.0;
        }
        return 220.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
