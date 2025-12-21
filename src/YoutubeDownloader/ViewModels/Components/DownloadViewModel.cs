using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gress;
using R3;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Models;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.ViewModels.Components;

public sealed partial class DownloadViewModel : ViewModel, ITransientDependency
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public override void OnLoaded()
    {
        Progress
            .ObservePropertyChanged(x => x.Current, false)
            .ObserveOnUIThreadDispatcher()
            .Subscribe(_ => OnPropertyChanged(nameof(IsProgressIndeterminate)))
            .AddTo(this);
    }

    [ObservableProperty]
    public partial IVideo? Video { get; set; }

    [ObservableProperty]
    public partial VideoDownloadOption? DownloadOption { get; set; }

    [ObservableProperty]
    public partial VideoDownloadPreference? DownloadPreference { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCanceledOrFailed))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenFileCommand))]
    public partial DownloadStatus Status { get; set; } = DownloadStatus.Enqueued;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyErrorMessageCommand))]
    public partial string? ErrorMessage { get; set; }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public string? FileName => Path.GetFileName(FilePath);

    public ProgressContainer<Percentage> Progress { get; } = new();

    public bool IsProgressIndeterminate => Progress.Current.Fraction is <= 0 or >= 1;

    public bool IsCanceledOrFailed => Status is DownloadStatus.Canceled or DownloadStatus.Failed;

    private bool CanCancel => Status is DownloadStatus.Enqueued or DownloadStatus.Started;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        if (IsDisposed)
            return;

        _cancellationTokenSource.Cancel();
    }

    private bool CanShowFile() => Status == DownloadStatus.Completed;

    [RelayCommand(CanExecute = nameof(CanShowFile))]
    private async Task ShowFileAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            return;

        try
        {
            await Launcher.LaunchUriAsync(new Uri(Path.GetDirectoryName(FilePath)!));
        }
        catch (Exception ex)
        {
            DialogService.ShowErrorMessageBox("Error", ex.Message);
        }
    }

    private bool CanOpenFile() => Status == DownloadStatus.Completed;

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenFileAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            return;

        try
        {
            await Launcher.LaunchFileInfoAsync(new FileInfo(FilePath));
        }
        catch (Exception ex)
        {
            DialogService.ShowErrorMessageBox("Error", ex.Message);
        }
    }

    [RelayCommand]
    private async Task CopyErrorMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ErrorMessage))
            return;

        await Clipboard.SetTextAsync(ErrorMessage);
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        if (!disposing)
            return;

        _cancellationTokenSource.Dispose();

        base.Dispose(disposing);
    }
}
