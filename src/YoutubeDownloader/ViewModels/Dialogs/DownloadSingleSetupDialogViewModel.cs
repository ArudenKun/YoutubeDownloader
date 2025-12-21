using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Utilities;
using YoutubeDownloader.ViewModels.Components;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.ViewModels.Dialogs;

public sealed partial class DownloadSingleSetupDialogViewModel
    : DialogViewModel<DownloadViewModel>,
        ITransientDependency
{
    [ObservableProperty]
    public partial IVideo? Video { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<VideoDownloadOption>? AvailableDownloadOptions { get; set; }

    [ObservableProperty]
    public partial VideoDownloadOption? SelectedDownloadOption { get; set; }

    [RelayCommand]
    private async Task CopyTitleAsync()
    {
        await Clipboard.SetTextAsync(Video?.Title);
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (Video is null || SelectedDownloadOption is null)
            return;

        var container = SelectedDownloadOption.Container;

        var filePath = await StorageProvider.PromptSaveFilePathAsync(
            [
                new FilePickerFileType($"{container.Name} file")
                {
                    Patterns = [$"*.{container.Name}"],
                },
            ],
            FileNameTemplate.Apply(SettingsService.Youtube.FileNameTemplate, Video, container)
        );

        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // Download does not start immediately, so lock in the file path to avoid conflicts
        Directory.CreateDirectoryForFile(filePath);
        await File.WriteAllBytesAsync(filePath, []);

        SettingsService.Youtube.LastContainer = container;

        var vm = LazyServiceProvider.GetRequiredService<DownloadViewModel>();
        vm.Video = Video;
        vm.DownloadOption = SelectedDownloadOption;
        vm.FilePath = filePath;
        Close(vm);
    }
}
