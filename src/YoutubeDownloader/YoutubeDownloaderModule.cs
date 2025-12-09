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

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        context.ServiceProvider.GetRequiredService<ISettingsService>().Load();
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        context.ServiceProvider.GetRequiredService<ISettingsService>().Save();
    }
}
