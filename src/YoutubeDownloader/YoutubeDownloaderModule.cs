using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using YoutubeDownloader.Hosting;
using YoutubeDownloader.Services;
using YoutubeDownloader.Views;
using YoutubeExplode;

namespace YoutubeDownloader;

[DependsOn(typeof(YoutubeDownloaderHostingModule), typeof(AbpEventBusModule))]
public sealed class YoutubeDownloaderModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConventionalRegistrar(new ViewConventionalRegistrar());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISukiToastManager, SukiToastManager>();
        context.Services.AddSingleton<ISukiDialogManager, SukiDialogManager>();
        context.Services.AddSingleton<YoutubeClient>();
    }

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<TopLevel>(_ => App.TopLevel);
        context.Services.AddTransient<IClipboard>(sp =>
            sp.GetRequiredService<TopLevel>().Clipboard!
        );
        context.Services.AddTransient<IStorageProvider>(sp =>
            sp.GetRequiredService<TopLevel>().StorageProvider
        );
        context.Services.AddTransient<ILauncher>(sp => sp.GetRequiredService<TopLevel>().Launcher);
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        context.ServiceProvider.GetRequiredService<SettingsService>().Load();
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        context.ServiceProvider.GetRequiredService<SettingsService>().Save();
    }
}
