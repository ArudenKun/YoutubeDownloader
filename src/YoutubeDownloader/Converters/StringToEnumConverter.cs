using System.Globalization;
using Avalonia.Data.Converters;
using YoutubeDownloader.Extensions;
using ZLinq;

namespace YoutubeDownloader.Converters;

public class StringToEnumConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return null;
        var list = value.GetType().GetAllValues().AsValueEnumerable();
        return list.FirstOrDefault(vd => Equals(vd, value));
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is null)
            return null;
        var list = value.GetType().GetAllValues().AsValueEnumerable();
        return list.FirstOrDefault(vd => Equals(vd, value));
    }
}
