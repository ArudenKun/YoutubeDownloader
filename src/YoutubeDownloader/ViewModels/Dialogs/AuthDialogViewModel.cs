using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Utilities;
using ZLinq;

namespace YoutubeDownloader.ViewModels.Dialogs;

public sealed partial class AuthDialogViewModel : DialogViewModel, ITransientDependency
{
    [ObservableProperty]
    public partial string CookiesOrCookiePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    public DateTime DateTimeText => DateTime.MinValue;

    public override void OnLoaded()
    {
        IsAuthenticated =
            SettingsService.LastAuthCookies.Any()
            &&
            // None of the '__SECURE' cookies should be expired
            SettingsService
                .LastAuthCookies.Where(c =>
                    c.Name.StartsWith("__SECURE", StringComparison.OrdinalIgnoreCase)
                )
                .All(c => !c.Expired && c.Expires.ToUniversalTime() > DateTime.UtcNow);
    }

    [RelayCommand]
    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(CookiesOrCookiePath))
            return;

        SettingsService.LastAuthCookies = File.Exists(CookiesOrCookiePath)
            ? NetscapeCookieParser
                .ParseFile(CookiesOrCookiePath)
                .AsValueEnumerable()
                .Select(c => c)
                .ToArray()
            : NetscapeCookieParser
                .Parse(CookiesOrCookiePath)
                .AsValueEnumerable()
                .Select(c => c)
                .ToArray();

        CloseDialog();
    }
}
