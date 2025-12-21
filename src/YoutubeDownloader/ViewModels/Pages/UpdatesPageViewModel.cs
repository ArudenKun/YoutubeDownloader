using Lucide.Avalonia;

namespace YoutubeDownloader.ViewModels.Pages;

public sealed class UpdatesPageViewModel : PageViewModel
{
    public UpdatesPageViewModel()
    {
        IsVisibleOnSideMenu = false;
    }

    public override int Index => int.MaxValue;
    public override string DisplayName => "Updates";
    public override LucideIconKind IconKind => LucideIconKind.CircleArrowUp;
}
