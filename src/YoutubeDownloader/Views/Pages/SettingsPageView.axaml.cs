using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Web.WebView2.Core;
using R3;
using R3.ObservableEvents;
using YoutubeDownloader.ViewModels.Pages;

namespace YoutubeDownloader.Views.Pages;

public partial class SettingsPageView : UserControl<SettingsPageViewModel>
{
    public SettingsPageView()
    {
        InitializeComponent();
        WebView2 = WebView;
    }

    public WebView2 WebView2 { get; }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        WebView2
            .Events()
            .CoreWebView2InitializationCompleted.Subscribe(_ =>
            {
                WebView2.CoreWebView2!.AddWebResourceRequestedFilter(
                    "https://www.youtube.com/embed/*",
                    CoreWebView2WebResourceContext.All
                );

                var coreWebView2 = WebView2.CoreWebView2;
                coreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                coreWebView2.Settings.AreDevToolsEnabled = false;
                coreWebView2.Settings.IsGeneralAutofillEnabled = false;
                coreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                coreWebView2.Settings.IsStatusBarEnabled = false;
                coreWebView2.Settings.IsSwipeNavigationEnabled = false;
            })
            .AddTo(Disposables);
        WebView2
            .Events()
            .WebResourceRequested.Subscribe(x =>
            {
                x.Request.Headers.SetHeader(
                    "Referer",
                    "https://github.com/Tyrrrz/YoutubeDownloader"
                );
            })
            .AddTo(Disposables);
    }
}
