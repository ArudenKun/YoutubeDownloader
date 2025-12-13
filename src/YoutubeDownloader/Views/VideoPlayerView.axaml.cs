using Avalonia.Input;
using Avalonia.Interactivity;
using YoutubeDownloader.ViewModels;

namespace YoutubeDownloader.Views;

public sealed partial class VideoPlayerView : UserControl<VideoPlayerViewModel>
{
    public VideoPlayerView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        VideoView.PointerEntered += VideoViewOnPointerEntered;
        VideoView.PointerExited += VideoViewOnPointerExited;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        DataContext.StopCommand.Execute(null);
        VideoView.PointerEntered -= VideoViewOnPointerEntered;
        VideoView.PointerExited -= VideoViewOnPointerExited;
    }

    private void VideoViewOnPointerEntered(object? sender, PointerEventArgs e)
    {
        ControlsPanel.IsVisible = true;
    }

    private void VideoViewOnPointerExited(object? sender, PointerEventArgs e)
    {
        ControlsPanel.IsVisible = false;
    }
}
