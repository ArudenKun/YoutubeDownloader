using System.Net;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Utilities;

namespace YoutubeDownloader.ViewModels.Dialogs;

public sealed partial class AuthDialogViewModel : DialogViewModel, ITransientDependency
{
    private const string HomePageUrl = "https://www.youtube.com";

    private static readonly string LoginPageUrl =
        $"https://accounts.google.com/ServiceLogin?continue={Uri.EscapeDataString(HomePageUrl)}";

    public CoreWebView2CreationProperties CreationProperties { get; } =
        new()
        {
            UserDataFolder = AppHelper.DataDir.CombinePath("webview"),
            EnabledDevTools = false,
        };

    [ObservableProperty]
    public partial string Url { get; set; } = HomePageUrl;

    public IReadOnlyList<Cookie> Cookies
    {
        get => SettingsService.LastAuthCookies;
        set => SettingsService.LastAuthCookies = value;
    }

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    public override void OnLoaded()
    {
        IsAuthenticated =
            Cookies.Any()
            &&
            // None of the '__SECURE' cookies should be expired
            Cookies
                .Where(c => c.Name.StartsWith("__SECURE", StringComparison.OrdinalIgnoreCase))
                .All(c => !c.Expired && c.Expires.ToUniversalTime() > DateTime.UtcNow);
    }

    [RelayCommand]
    private void Logout()
    {
        Cookies = [];
        Url = LoginPageUrl;
    }

    [RelayCommand]
    private void NavigateToLoginPage()
    {
        Url = LoginPageUrl;
    }

    public void SetDialog(ISukiDialog dialog)
    {
        Dialog = dialog;
    }
}
