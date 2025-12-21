using Microsoft.Extensions.DependencyInjection;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Utilities.Extensions;
using YoutubeDownloader.Services;
using YoutubeDownloader.ViewModels.Components;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.ViewModels.Dialogs;

public static class DialogExtensions
{
    extension(IDialogService dialogService)
    {
        public async Task<DownloadViewModel?> ShowDownloadSingleSetupDialogAsync(
            IVideo video,
            IReadOnlyList<VideoDownloadOption> downloadOptions
        )
        {
            var vm =
                dialogService.ServiceProvider.GetRequiredService<DownloadSingleSetupDialogViewModel>();
            vm.Video = video;
            vm.AvailableDownloadOptions = downloadOptions;
            await dialogService.ShowDialogAsync(vm);
            return vm.DialogResult;
        }

        public async Task<ICollection<DownloadViewModel>> ShowDownloadMultipleSetupDialogAsync(
            string title,
            IReadOnlyList<IVideo> availableVideos,
            bool preselectVideos = true
        )
        {
            var vm =
                dialogService.ServiceProvider.GetRequiredService<DownloadMultipleSetupDialogViewModel>();
            vm.Title = title;
            vm.AvailableVideos = availableVideos;
            if (preselectVideos)
                vm.SelectedVideos.AddRange(availableVideos);
            await dialogService.ShowDialogAsync(vm);
            return vm.DialogResult?.ToArray() ?? [];
        }
    }
}
