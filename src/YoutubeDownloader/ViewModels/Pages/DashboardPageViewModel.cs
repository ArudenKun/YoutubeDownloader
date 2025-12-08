using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using WebViewControl;
using YoutubeDownloader.Core.Utilities;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace YoutubeDownloader.ViewModels.Pages;

public sealed partial class DashboardPageViewModel : PageViewModel, ITransientDependency
{
    private readonly YoutubeClient _youtubeClient;

    private WebView _webView = null!;

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

        _webView.LoadUrl("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ");
    }

    [RelayCommand]
    private void InitializeWebView(WebView webView)
    {
        _webView.BeforeResourceLoad += WebViewOnBeforeResourceLoad;
        _webView = webView;
    }

    private void WebViewOnBeforeResourceLoad(ResourceHandler handler)
    {
        if (
            handler.Url.StartsWith("https://www.youtube.com/embed")
            || handler.Url.StartsWith("https://www.youtube-nocookie.com/embed")
        )
        {
            Logger.LogInformation("BeforeResourceLoad {Method} {Url}", handler.Method, handler.Url);
            var request = new HttpRequestMessage(HttpMethod.Get, handler.Url);
            request.Headers.Referrer = new Uri("https://youtube.com/");
            var response = AsyncHelper.RunSync(async () => await Http.Client.SendAsync(request));
            var data = AsyncHelper.RunSync(async () =>
                await response.Content.ReadAsByteArrayAsync()
            );
            var stream = new MemoryStream(data);
            handler.RespondWith(stream);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _webView.BeforeResourceLoad -= WebViewOnBeforeResourceLoad;
        }

        base.Dispose(disposing);
    }
}
