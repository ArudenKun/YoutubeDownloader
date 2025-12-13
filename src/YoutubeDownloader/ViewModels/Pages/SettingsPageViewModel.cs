using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using Volo.Abp.DependencyInjection;
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

        VideoPlayerViewModel = _serviceProvider.GetRequiredService<VideoPlayerViewModel>();
    }

    public override int Index => int.MaxValue;
    public override string DisplayName => "Settings";
    public override LucideIconKind IconKind => LucideIconKind.Settings;

    public VideoPlayerViewModel VideoPlayerViewModel { get; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [RelayCommand]
    private void ProcessSearch()
    {
        VideoPlayerViewModel.Source = Search;
    }

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
