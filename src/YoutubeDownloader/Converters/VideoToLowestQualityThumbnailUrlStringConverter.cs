using System.Globalization;
using Avalonia.Data.Converters;
using YoutubeDownloader.Utilities;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Converters;

public class VideoToLowestQualityThumbnailUrlStringConverter
    : SingletonBase<VideoToLowestQualityThumbnailUrlStringConverter>,
        IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is IVideo video ? video.Thumbnails.MinBy(t => t.Resolution.Area)?.Url : null;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
