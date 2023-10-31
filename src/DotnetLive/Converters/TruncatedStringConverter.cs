using Avalonia.Data.Converters;
using System.Globalization;

namespace DotnetLive.Converters;

public class TruncatedStringConverter : IValueConverter
{
    private const int OverflowLength = 115;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && str.Length > OverflowLength) {
            return str[..(OverflowLength - 5)] + ". . .";
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
