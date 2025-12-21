using System.Globalization;
using Avalonia.Data.Converters;
using YoutubeDownloader.Utilities;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Converters;

public class VideoToHighestQualityThumbnailUrlStringConverter
    : SingletonBase<VideoToHighestQualityThumbnailUrlStringConverter>,
        IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is IVideo video ? video.Thumbnails.TryGetWithHighestResolution()?.Url : null;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
