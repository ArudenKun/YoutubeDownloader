using System.Globalization;
using Avalonia.Data.Converters;

namespace YoutubeDownloader.Converters;

public class EqualityConverter : IValueConverter
{
    private readonly bool _isInverted;

    public EqualityConverter(bool isInverted)
    {
        _isInverted = isInverted;
    }

    public static EqualityConverter Equality { get; } = new(false);
    public static EqualityConverter IsNotEqual { get; } = new(true);

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => EqualityComparer<object>.Default.Equals(value, parameter) != _isInverted;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
