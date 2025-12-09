using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Volo.Abp.DependencyInjection;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace YoutubeDownloader.ViewModels.Pages;

public sealed partial class DashboardPageViewModel : PageViewModel, ITransientDependency
{
    private readonly YoutubeClient _youtubeClient;

    public DashboardPageViewModel(YoutubeClient youtubeClient)
    {
        _youtubeClient = youtubeClient;
    }

    public override int Index => 1;
    public override string DisplayName => "Dashboard";
    public override LucideIconKind IconKind => LucideIconKind.House;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessSearchQueryCommand))]
    [NotifyPropertyChangedFor(nameof(IsProcessSearchQueryEnabled))]
    public partial string SearchQuery { get; set; } = string.Empty;

    public bool IsProcessSearchQueryEnabled =>
        !SearchQuery.IsNullOrEmpty() || !SearchQuery.IsNullOrWhiteSpace();

    [RelayCommand(CanExecute = nameof(IsProcessSearchQueryEnabled))]
    private async Task ProcessSearchQuery()
    {
        // var cookieManger = CefCookieManager.GetGlobal(null);
        // var visitor = new CookieVisitor();
        // cookieManger.VisitAllCookies(visitor);
        // var cookies = await visitor.GetCookiesAsync();
        var searchResult = await _youtubeClient.Search.GetVideosAsync(SearchQuery);
        if (searchResult.Count is 0)
        {
            ToastService.ShowToast(NotificationType.Warning, "Search", "No videos found");
            return;
        }

        foreach (var videoSearchResult in searchResult) { }
    }
}
