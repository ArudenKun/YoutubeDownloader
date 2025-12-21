using CommunityToolkit.Mvvm.ComponentModel;

namespace YoutubeDownloader.Options;

public sealed partial class GeneralOptions : ObservableObject
{
    [ObservableProperty]
    public partial bool AutoUpdate { get; set; } = false;

    [ObservableProperty]
    public partial int ParallelLimit { get; set; } = 8;
}
