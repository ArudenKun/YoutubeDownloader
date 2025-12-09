using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using YoutubeDownloader.ViewModels.Dialogs;

namespace YoutubeDownloader.Views.Dialogs;

public partial class AuthDialogView : UserControl<AuthDialogViewModel>, IWebView2StorageService
{
    private const string HomePageUrl = "https://www.youtube.com";

    private static readonly string LoginPageUrl =
        $"https://accounts.google.com/ServiceLogin?continue={Uri.EscapeDataString(HomePageUrl)}";

    public AuthDialogView()
    {
        InitializeComponent();

        WebBrowser.CoreWebView2InitializationCompleted +=
            WebBrowserOnCoreWebView2InitializationCompleted;
        WebBrowser.NavigationStarting += WebBrowserOnNavigationStarting;

        WebBrowser.StorageService = this;
    }

    public required ILogger<AuthDialogView> Logger { get; init; }

    private void WebBrowserOnCoreWebView2InitializationCompleted(
        object? sender,
        CoreWebView2InitializationCompletedEventArgs e
    )
    {
        WebBrowser.CoreWebView2?.Settings.AreDefaultContextMenusEnabled = false;
        WebBrowser.CoreWebView2?.Settings.AreDevToolsEnabled = false;
        WebBrowser.CoreWebView2?.Settings.IsGeneralAutofillEnabled = false;
        WebBrowser.CoreWebView2?.Settings.IsPasswordAutosaveEnabled = false;
        WebBrowser.CoreWebView2?.Settings.IsStatusBarEnabled = false;
        WebBrowser.CoreWebView2?.Settings.IsSwipeNavigationEnabled = false;

        DataContext.NavigateToLoginPageCommand.Execute(null);
    }

    private async void WebBrowserOnNavigationStarting(
        object? sender,
        CoreWebView2NavigationStartingEventArgs args
    )
    {
        if (WebBrowser.CoreWebView2 is not { } coreWebView2)
            return;

        // Reset existing browser cookies if the user is attempting to log in (again)
        if (string.Equals(args.Uri, LoginPageUrl, StringComparison.OrdinalIgnoreCase))
            coreWebView2.CookieManager.DeleteAllCookies();

        // Extract the cookies after being redirected to the home page (i.e. after logging in)
        if (args.Uri.StartsWith(HomePageUrl, StringComparison.OrdinalIgnoreCase))
        {
            var cookies = await coreWebView2.CookieManager.GetCookiesAsync(args.Uri);
            DataContext.Cookies = cookies.Select(c => c.ToSystemNetCookie()).ToArray();
        }
    }

    public IEnumerable<
        KeyValuePair<(WebView2StorageItemType type, string key), WebView2StorageItemValue>
    >? GetStorages(string requestUri)
    {
        var now = DateTime.Now;

        var dict = new Dictionary<
            (WebView2StorageItemType type, string key),
            WebView2StorageItemValue
        >
        {
            { (WebView2StorageItemType.LocalStorage, "global_test"), 2 },
            { (WebView2StorageItemType.SessionStorage, "global_test_s"), 7.5 },
            { (WebView2StorageItemType.LocalStorage, "global_test_now"), now },
            {
                (WebView2StorageItemType.AllStorage, "global_test_now_str"),
                now.ToString("yyyy-MM-dd HH:mm:ss.fffffff")
            },
        };

        foreach (var it in dict)
        {
            yield return it;
        }
    }
}
