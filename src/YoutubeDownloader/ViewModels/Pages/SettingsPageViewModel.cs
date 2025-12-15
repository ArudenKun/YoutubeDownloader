using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Utilities;
using YoutubeDownloader.ViewModels.Dialogs;
using YoutubeExplode;

namespace YoutubeDownloader.ViewModels.Pages;

public sealed partial class SettingsPageViewModel : PageViewModel, ISingletonDependency
{
    private readonly ISukiDialogManager _dialogManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly YoutubeClient _youtubeClient;

    public SettingsPageViewModel(
        ISukiDialogManager dialogManager,
        IServiceProvider serviceProvider,
        YoutubeClient youtubeClient
    )
    {
        _dialogManager = dialogManager;
        _serviceProvider = serviceProvider;
        _youtubeClient = youtubeClient;
    }

    public override int Index => int.MaxValue;
    public override string DisplayName => "Settings";
    public override LucideIconKind IconKind => LucideIconKind.Settings;

    public string UserDataFolder => AppHelper.DataDir;

    public CoreWebView2CreationProperties CreationProperties { get; } =
        new() { UserDataFolder = AppHelper.DataDir, EnabledDevTools = true };

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Uri Url { get; set; } = new("about:blank");

    [RelayCommand]
    private async Task ProcessSearchAsync()
    {
        Url = new Uri(Search);
        // var client = new HttpClient();
        // var request = new HttpRequestMessage();
        // request.Headers.Referrer = new Uri("https://www.youtube.com/watch?v=");
        //
        // var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(Search);
        // var videoStreamInfo = manifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
        // var audioStreamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        //
        // using var media = new Media(
        //     _libVlc,
        //     new Uri(videoStreamInfo.Url),
        //     [$":input-slave={audioStreamInfo.Url}"]
        // );
        // MediaPlayer.Play(media);
    }

    [RelayCommand]
    private void OpenAuthDialog()
    {
        var vm = _serviceProvider.GetRequiredService<AuthDialogViewModel>();
        var dialog = _dialogManager
            .CreateDialog()
            .WithViewModel(d =>
            {
                vm.SetDialog(d);
                return vm;
            });
        dialog.TryShow();
    }
}
