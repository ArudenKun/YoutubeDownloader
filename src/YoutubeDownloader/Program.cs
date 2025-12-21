using Avalonia;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Velopack;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Hosting;
using YoutubeDownloader.Options;
using YoutubeDownloader.Services;
using YoutubeDownloader.Utilities;

namespace YoutubeDownloader;

public static class Program
{
    private static ILogger Logger => Log.ForContext("SourceContext", nameof(YoutubeDownloader));

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.WithDemystifiedStackTraces()
            .WriteTo.Async(c =>
                c.File(
                    AppHelper.LogsDir.CombinePath("log.txt"),
                    outputTemplate: LoggingOptions.Template
                )
            )
            .WriteTo.Async(c => c.Console(outputTemplate: LoggingOptions.Template))
            .CreateBootstrapLogger();

        try
        {
            Logger.Information("Starting Avalonia Host");
            VelopackApp.Build().SetArgs(args).SetLogger(VelopackLogger.Instance).Run();
            var app = Host.CreateDefaultBuilder(args)
                .ConfigureLogging()
                .ConfigureConfiguration()
                .ConfigureAvalonia()
                .UseAutofac()
                .UseApplication<YoutubeDownloaderModule>()
                .UseConsoleLifetime()
                .Build();

            await app.InitializeAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Configure the loggers
    /// </summary>
    /// <param name="hostBuilder">IHostBuilder</param>
    /// <returns>IHostBuilder</returns>
    private static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder) =>
        hostBuilder
            .ConfigureServices(services =>
                services.AddSingleton(sp => new ObservableLoggingLevelSwitch(
                    sp.GetRequiredService<SettingsService>()
                ))
            )
            .UseSerilog(
                (_, sp, loggingConfiguration) =>
                    loggingConfiguration
                        .MinimumLevel.ControlledBy(
                            sp.GetRequiredService<ObservableLoggingLevelSwitch>()
                        )
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override(
                            "Microsoft.EntityFrameworkCore",
                            LogEventLevel.Warning
                        )
                        .Enrich.FromLogContext()
                        .Enrich.WithDemystifiedStackTraces()
                        .WriteTo.Async(c =>
                            c.File(
                                AppHelper.LogsDir.CombinePath("log.txt"),
                                outputTemplate: LoggingOptions.Template,
                                fileSizeLimitBytes: sp.GetRequiredService<SettingsService>().Logging.Size
                                == 0
                                    ? null
                                    : (long)
                                        sp.GetRequiredService<SettingsService>()
                                            .Logging.Size.Megabytes()
                                            .Bytes,
                                retainedFileTimeLimit: 30.Days(),
                                rollingInterval: RollingInterval.Day,
                                rollOnFileSizeLimit: true,
                                shared: true
                            )
                        )
                        .WriteTo.Async(c => c.Console(outputTemplate: LoggingOptions.Template))
            );

    /// <summary>
    /// Configure the configuration
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    private static IHostBuilder ConfigureConfiguration(this IHostBuilder hostBuilder) =>
        hostBuilder
            .ConfigureHostConfiguration(configHost =>
                configHost
                    .AddConfiguration(
                        ConfigurationHelper.BuildConfiguration(
                            new AbpConfigurationBuilderOptions { BasePath = AppHelper.DataDir }
                        )
                    )
                    .AddAppSettingsSecretsJson()
            )
            .ConfigureAppConfiguration(
                (_, configApp) =>
                    configApp
                        .AddConfiguration(
                            ConfigurationHelper.BuildConfiguration(
                                new AbpConfigurationBuilderOptions { BasePath = AppHelper.DataDir }
                            )
                        )
                        .AddAppSettingsSecretsJson()
            );

    private static IHostBuilder ConfigureAvalonia(this IHostBuilder hostBuilder) =>
        hostBuilder.UseAvaloniaHosting<App>(appBuilder =>
            appBuilder
                .UsePlatformDetect()
                .UseR3(exception => Logger.Fatal(exception, "R3 Unhandled Exception"))
                .LogToTrace()
        );

    // Avalonia configuration, don't remove; also used by visual designer.
    [UsedImplicitly]
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UsePlatformDetect().LogToTrace();
}
