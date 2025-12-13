using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using R3;
using R3.ObservableEvents;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Utilities;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.ViewModels;

public sealed partial class VideoPlayerViewModel : ViewModel, ITransientDependency
{
    private readonly LibVLC _libVlc = new();
    private readonly YoutubeClient _youtubeClient;

    private bool _isOpening;

    public VideoPlayerViewModel(YoutubeClient youtubeClient)
    {
        _youtubeClient = youtubeClient;

        MediaPlayer = new MediaPlayer(_libVlc);
        MediaPlayer
            .Events()
            .Opening.ObserveOnUIThreadDispatcher()
            .Subscribe(_ =>
            {
                _isOpening = true;
                PlayCommand.NotifyCanExecuteChanged();
            })
            .AddTo(this);
        MediaPlayer
            .Events()
            .Playing.ObserveOnUIThreadDispatcher()
            .Subscribe(_ =>
            {
                _isOpening = false;
                PlayCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
            })
            .AddTo(this);

        MediaPlayer.AddTo(this);
        _libVlc.AddTo(this);
    }

    public MediaPlayer MediaPlayer { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlayEnabled))]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    public partial string Source { get; set; } = string.Empty;

    public bool IsPlayEnabled =>
        !MediaPlayer.IsPlaying
        && !_isOpening
        && !string.IsNullOrWhiteSpace(Source)
        && VideoId.TryParse(Source) is not null;

    [RelayCommand(CanExecute = nameof(IsPlayEnabled))]
    private async Task PlayAsync()
    {
        if (Design.IsDesignMode)
            return;

        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(Source);
        var videoStreamInfo = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        Logger.LogInformation("Video: {Url}", videoStreamInfo.Url);
        Logger.LogInformation("Audio: {Url}", audioStreamInfo.Url);
        using var media = new Media(_libVlc, new Uri(videoStreamInfo.Url));
        media.AddOption($":input-slave={audioStreamInfo.Url}");
        MediaPlayer.Play(media);

        OnPropertyChanged(nameof(IsPlaying));
    }

    public bool IsPlaying => MediaPlayer.IsPlaying;

    [RelayCommand(CanExecute = nameof(IsPlaying))]
    private void Stop()
    {
        MediaPlayer.Stop();
        DispatchHelper.Invoke(() =>
        {
            PlayCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        });
    }
}
