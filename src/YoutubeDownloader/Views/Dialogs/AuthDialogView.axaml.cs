using Avalonia.Interactivity;
using Microsoft.Web.WebView2.Core;
using YoutubeDownloader.ViewModels.Dialogs;
using ZLinq;

namespace YoutubeDownloader.Views.Dialogs;

public partial class AuthDialogView : UserControl<AuthDialogViewModel>
{
    private const string HomePageUrl = "https://www.youtube.com";

    private static readonly string LoginPageUrl =
        $"https://accounts.google.com/ServiceLogin?continue={Uri.EscapeDataString(HomePageUrl)}";

    private static readonly Uri LoginPageUri = new(
        $"https://accounts.google.com/ServiceLogin?continue={Uri.EscapeDataString(HomePageUrl)}"
    );

    public AuthDialogView()
    {
        InitializeComponent();

        WebView.Loaded += WebViewOnLoaded;
        WebView.CoreWebView2InitializationCompleted += WebViewOnCoreWebView2InitializationCompleted;
        WebView.NavigationStarting += WebViewOnNavigationStarting;
    }

    private void NavigateToLoginPage() => WebView.Source = LoginPageUri;

    private void WebViewOnLoaded(object? sender, RoutedEventArgs e) => NavigateToLoginPage();

    private void WebViewOnCoreWebView2InitializationCompleted(
        object? sender,
        CoreWebView2InitializationCompletedEventArgs args
    )
    {
        if (!args.IsSuccess)
            return;

        var coreWebView2 = WebView?.CoreWebView2;
        if (coreWebView2 is null)
            return;

        coreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        coreWebView2.Settings.AreDevToolsEnabled = false;
        coreWebView2.Settings.IsGeneralAutofillEnabled = false;
        coreWebView2.Settings.IsPasswordAutosaveEnabled = false;
        coreWebView2.Settings.IsStatusBarEnabled = false;
        coreWebView2.Settings.IsSwipeNavigationEnabled = false;
    }

    private async void WebViewOnNavigationStarting(
        object? sender,
        CoreWebView2NavigationStartingEventArgs args
    )
    {
        var coreWebView2 = WebView?.CoreWebView2;
        if (coreWebView2 is null)
            return;

        // Reset existing browser cookies if the user is attempting to log in (again)
        if (string.Equals(args.Uri, LoginPageUrl, StringComparison.OrdinalIgnoreCase))
            coreWebView2.CookieManager.DeleteAllCookies();

        // Extract the cookies after being redirected to the home page (i.e. after logging in)
        if (args.Uri.StartsWith(HomePageUrl, StringComparison.OrdinalIgnoreCase))
        {
            var cookies = await coreWebView2.CookieManager.GetCookiesAsync(args.Uri);
            DataContext.Cookies = cookies
                .AsValueEnumerable()
                .Select(c => c.ToSystemNetCookie())
                .ToArray();
        }
    }
}
