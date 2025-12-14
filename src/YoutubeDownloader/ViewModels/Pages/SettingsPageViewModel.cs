using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.WinForms;
using SukiUI.Dialogs;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Utilities;
using YoutubeDownloader.ViewModels.Dialogs;

namespace YoutubeDownloader.ViewModels.Pages;

public sealed partial class SettingsPageViewModel : PageViewModel, ISingletonDependency
{
    private readonly ISukiDialogManager _dialogManager;
    private readonly IServiceProvider _serviceProvider;

    public SettingsPageViewModel(ISukiDialogManager dialogManager, IServiceProvider serviceProvider)
    {
        _dialogManager = dialogManager;
        _serviceProvider = serviceProvider;
    }

    public override int Index => int.MaxValue;
    public override string DisplayName => "Settings";
    public override LucideIconKind IconKind => LucideIconKind.Settings;

    public string UserDataFolder => AppHelper.DataDir;

    [ObservableProperty]
    public partial string Search { get; set; } = "https://www.youtube.com/watch?v=hlM4zl3QHIY";

    public override void OnLoaded() { }

    [RelayCommand]
    private void ProcessSearch() { }

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
