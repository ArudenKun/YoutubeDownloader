using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Utilities;
using YoutubeDownloader.ViewModels.Components;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.ViewModels.Dialogs;

public sealed partial class DownloadMultipleSetupDialogViewModel
    : DialogViewModel<IReadOnlyList<DownloadViewModel>>,
        ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public DownloadMultipleSetupDialogViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<IVideo>? AvailableVideos { get; set; }

    [ObservableProperty]
    public partial Container SelectedContainer { get; set; } = Container.Mp4;

    [ObservableProperty]
    public partial VideoQualityPreference SelectedVideoQualityPreference { get; set; } =
        VideoQualityPreference.Highest;

    public ObservableCollection<IVideo> SelectedVideos { get; } = [];

    public IReadOnlyList<Container> AvailableContainers { get; } =
    [Container.Mp4, Container.WebM, Container.Mp3, new("ogg")];

    public IReadOnlyList<VideoQualityPreference> AvailableVideoQualityPreferences { get; } =
        // Without .AsEnumerable(), the below line throws a compile-time error starting with .NET SDK v9.0.200
        Enum.GetValues<VideoQualityPreference>().AsEnumerable().Reverse().ToArray();

    [RelayCommand]
    private void Initialize()
    {
        SelectedContainer = SettingsService.Youtube.LastContainer;
        SelectedVideoQualityPreference = SettingsService.Youtube.LastVideoQualityPreference;
        SelectedVideos.CollectionChanged += (_, _) => ConfirmCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task CopyTitleAsync()
    {
        await Clipboard.SetTextAsync(Title);
    }

    private bool CanConfirm() => SelectedVideos.Any();

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        var dirPath = await StorageProvider.PromptDirectoryPathAsync();
        if (string.IsNullOrWhiteSpace(dirPath))
            return;

        var downloads = new List<DownloadViewModel>();
        for (var i = 0; i < SelectedVideos.Count; i++)
        {
            var video = SelectedVideos[i];

            var baseFilePath = Path.Combine(
                dirPath,
                FileNameTemplate.Apply(
                    SettingsService.Youtube.FileNameTemplate,
                    video,
                    SelectedContainer,
                    (i + 1).ToString().PadLeft(SelectedVideos.Count.ToString().Length, '0')
                )
            );

            if (SettingsService.Youtube.ShouldSkipExistingFiles && File.Exists(baseFilePath))
                continue;

            var filePath = Path.EnsureUniqueFilePath(baseFilePath);

            // Download does not start immediately, so lock in the file path to avoid conflicts
            Directory.CreateDirectoryForFile(filePath);
            await File.WriteAllBytesAsync(filePath, []);

            var vm = _serviceProvider.GetRequiredService<DownloadViewModel>();
            vm.Video = video;
            vm.DownloadPreference = new VideoDownloadPreference(
                SelectedContainer,
                SelectedVideoQualityPreference
            );
            vm.FilePath = filePath;
            downloads.Add(vm);
        }

        SettingsService.Youtube.LastContainer = SelectedContainer;
        SettingsService.Youtube.LastVideoQualityPreference = SelectedVideoQualityPreference;
        Close(downloads);
    }
}
