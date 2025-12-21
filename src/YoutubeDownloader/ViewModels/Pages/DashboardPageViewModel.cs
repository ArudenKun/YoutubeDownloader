using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gress;
using Gress.Completable;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using R3;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Resolving;
using YoutubeDownloader.Core.Tagging;
using YoutubeDownloader.Core.Utilities;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Models;
using YoutubeDownloader.ViewModels.Components;
using YoutubeDownloader.ViewModels.Dialogs;
using YoutubeExplode;
using YoutubeExplode.Exceptions;

namespace YoutubeDownloader.ViewModels.Pages;

public sealed partial class DashboardPageViewModel : PageViewModel, ITransientDependency
{
    private readonly YoutubeClient _youtubeClient;

    private readonly ResizableSemaphore _downloadSemaphore = new();
    private readonly AutoResetProgressMuxer _progressMuxer;

    public DashboardPageViewModel(YoutubeClient youtubeClient)
    {
        _youtubeClient = youtubeClient;

        _progressMuxer = Progress.CreateMuxer().WithAutoReset();
    }

    public override void OnLoaded()
    {
        SettingsService
            .General.ObservePropertyChanged(x => x.ParallelLimit)
            .ObserveOnUIThreadDispatcher()
            .Subscribe(x => _downloadSemaphore.MaxCount = x)
            .AddTo(this);

        Progress
            .ObservePropertyChanged(x => x.Current, false)
            .ObserveOnUIThreadDispatcher()
            .Subscribe(_ => OnPropertyChanged(nameof(IsProgressIndeterminate)))
            .AddTo(this);
    }

    public override int Index => 1;
    public override string DisplayName => "Dashboard";
    public override LucideIconKind IconKind => LucideIconKind.House;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyPropertyChangedFor(nameof(IsProgressIndeterminate))]
    [NotifyCanExecuteChangedFor(nameof(ProcessQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowAuthSetupCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowSettingsCommand))]
    public override partial bool IsBusy { get; set; }

    public ProgressContainer<Percentage> Progress { get; } = new();

    public bool IsProgressIndeterminate => IsBusy && Progress.Current.Fraction is <= 0 or >= 1;

    public ObservableCollection<DownloadViewModel> Downloads { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessQueryCommand))]
    public partial string Query { get; set; } = string.Empty;

    public bool CanProcessQuery() => !Query.IsNullOrEmpty() || !Query.IsNullOrWhiteSpace();

    [RelayCommand(CanExecute = nameof(CanProcessQuery))]
    private async Task ProcessQueryAsync()
    {
        if (Query.IsNullOrWhiteSpace())
            return;

        await SetBusyAsync(
            async () =>
            {
                var progress = _progressMuxer.CreateInput(0.01);

                try
                {
                    using var resolver = new QueryResolver(SettingsService.Youtube.LastAuthCookies);

                    // Split queries by newlines
                    var queries = Query.Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    );

                    var queryResults = new List<QueryResult>();

                    foreach (var (i, query) in queries.Index())
                    {
                        try
                        {
                            queryResults.Add(await resolver.ResolveAsync(query));
                        }
                        // If it's not the only query in the list, don't interrupt the process
                        // and report the error via an async notification instead of a sync dialog.
                        // https://github.com/Tyrrrz/YoutubeDownloader/issues/563
                        catch (YoutubeExplodeException ex)
                            when (ex is VideoUnavailableException or PlaylistUnavailableException
                                && queries.Length > 1
                            )
                        {
                            ToastService.ShowExceptionToast(ex);
                        }

                        progress.Report(Percentage.FromFraction((i + 1.0) / queries.Length));
                    }

                    // Aggregate results
                    var queryResult = QueryResult.Aggregate(queryResults);

                    // Single video result
                    if (queryResult.Videos.Count == 1)
                    {
                        var video = queryResult.Videos.Single();

                        using var downloader = new VideoDownloader(
                            SettingsService.Youtube.LastAuthCookies
                        );

                        var downloadOptions = await downloader.GetDownloadOptionsAsync(
                            video.Id,
                            SettingsService.Youtube.ShouldInjectLanguageSpecificAudioStreams
                        );

                        var download = await DialogService.ShowDownloadSingleSetupDialogAsync(
                            video,
                            downloadOptions
                        );

                        if (download is null)
                            return;

                        EnqueueDownload(download);

                        Query = "";
                    }
                    // Multiple videos
                    else if (queryResult.Videos.Count > 1)
                    {
                        var downloads = await DialogService.ShowDownloadMultipleSetupDialogAsync(
                            queryResult.Title,
                            queryResult.Videos,
                            // Pre-select videos if they come from a single query and not from search
                            queryResult.Kind
                                is not QueryResultKind.Search
                                    and not QueryResultKind.Aggregate
                        );

                        if (downloads.IsNullOrEmpty())
                            return;

                        foreach (var download in downloads)
                            EnqueueDownload(download);

                        Query = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    DialogService.ShowErrorMessageBox(
                        "Error",
                        ex is YoutubeExplodeException ? ex.Message : ex.ToString()
                    );
                }
                finally
                {
                    progress.ReportCompletion();
                }
            },
            "Searching..."
        );
    }

    private async void EnqueueDownload(DownloadViewModel download, int position = 0)
    {
        Downloads.Insert(position, download);
        var progress = _progressMuxer.CreateInput();

        try
        {
            using var downloader = new VideoDownloader(SettingsService.Youtube.LastAuthCookies);
            var tagInjector = new MediaTagInjector();

            using var access = await _downloadSemaphore.AcquireAsync(download.CancellationToken);

            download.Status = DownloadStatus.Started;

            var downloadOption =
                download.DownloadOption
                ?? await downloader.GetBestDownloadOptionAsync(
                    download.Video!.Id,
                    download.DownloadPreference!,
                    SettingsService.Youtube.ShouldInjectLanguageSpecificAudioStreams,
                    download.CancellationToken
                );

            await downloader.DownloadVideoAsync(
                download.FilePath!,
                download.Video!,
                downloadOption,
                SettingsService.Youtube.ShouldInjectSubtitles,
                download.Progress.Merge(progress),
                download.CancellationToken
            );

            if (SettingsService.Youtube.ShouldInjectTags)
            {
                try
                {
                    await tagInjector.InjectTagsAsync(
                        download.FilePath!,
                        download.Video!,
                        download.CancellationToken
                    );
                }
                catch
                {
                    // Media tagging is not critical
                }
            }

            download.Status = DownloadStatus.Completed;
        }
        catch (Exception ex)
        {
            try
            {
                // Delete the incompletely downloaded file
                if (!string.IsNullOrWhiteSpace(download.FilePath))
                    File.Delete(download.FilePath);
            }
            catch
            {
                // Ignore
            }

            download.Status =
                ex is OperationCanceledException ? DownloadStatus.Canceled : DownloadStatus.Failed;

            // Short error message for YouTube-related errors, full for others
            download.ErrorMessage = ex is YoutubeExplodeException ? ex.Message : ex.ToString();
        }
        finally
        {
            progress.ReportCompletion();
            download.Dispose();
        }
    }

    private void RemoveDownload(DownloadViewModel download)
    {
        Downloads.Remove(download);
        download.CancelCommand.Execute(null);
        download.Dispose();
    }

    [RelayCommand]
    private async Task RemoveSuccessfulDownloadsAsync() { }

    [RelayCommand]
    private async Task RemoveInactiveDownloadsAsync() { }

    [RelayCommand]
    private void RestartDownload(DownloadViewModel download)
    {
        var position = Math.Max(0, Downloads.IndexOf(download));
        RemoveDownload(download);

        var newDownload = LazyServiceProvider.GetRequiredService<DownloadViewModel>();
        newDownload.Video = download.Video;
        newDownload.FilePath = download.FilePath;
        if (download.DownloadOption is not null)
        {
            newDownload.DownloadOption = download.DownloadOption;
        }
        else
        {
            newDownload.DownloadPreference = download.DownloadPreference;
        }

        EnqueueDownload(newDownload, position);
    }

    [RelayCommand]
    private async Task RestartFailedDownloadsAsync() { }

    [RelayCommand]
    private async Task CancelAllDownloadsAsync() { }

    [RelayCommand]
    private async Task ShowAuthSetupAsync() { }

    [RelayCommand]
    private async Task ShowSettingsAsync() { }
}
