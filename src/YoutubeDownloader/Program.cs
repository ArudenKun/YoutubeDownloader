using Avalonia;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Velopack;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Hosting;
using YoutubeDownloader.Options;
using YoutubeDownloader.Utilities;
using Application = Avalonia.Application;

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
            Logger.Information("Starting Avalonia host.");
            VelopackApp.Build().SetArgs(args).SetLogger(VelopackLogger.Instance).Run();
            var builder = Host.CreateApplicationBuilder(args);
            await ConfigureAsync(builder);
            var app = builder.Build();
            await app.InitializeApplicationAsync();
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

    private static async Task ConfigureAsync(HostApplicationBuilder builder)
    {
        builder.Configuration.AddConfiguration(
            ConfigurationHelper.BuildConfiguration(
                new AbpConfigurationBuilderOptions { BasePath = AppHelper.DataDir }
            )
        );
        builder.Configuration.AddAppSettingsSecretsJson();
        builder.AddAvaloniaHosting<App>(appBuilder =>
            appBuilder
                .UsePlatformDetect()
                .UseR3(exception => Logger.Fatal(exception, "R3 Unhandled Exception"))
                .LogToTrace()
        );
        builder.AddAutofac();
        await builder.AddApplicationAsync<YoutubeDownloaderModule>();
        builder.Services.AddSingleton(sp => new LoggingLevelSwitch(
            sp.GetRequiredService<IOptions<LoggingOptions>>().Value.LogEventLevel
        ));
        builder.Services.AddSerilog(
            (sp, loggingConfiguration) =>
                loggingConfiguration
                    .MinimumLevel.ControlledBy(sp.GetRequiredService<LoggingLevelSwitch>())
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithDemystifiedStackTraces()
                    .WriteTo.Async(c =>
                        c.File(
                            AppHelper.LogsDir.CombinePath("log.txt"),
                            outputTemplate: LoggingOptions.Template,
                            fileSizeLimitBytes: sp.GetRequiredService<
                                IOptions<LoggingOptions>
                            >().Value.Size == 0
                                ? null
                                : (long)
                                    sp.GetRequiredService<IOptions<LoggingOptions>>()
                                        .Value.Size.Megabytes()
                                        .Bytes,
                            retainedFileTimeLimit: 30.Days(),
                            rollingInterval: RollingInterval.Day,
                            rollOnFileSizeLimit: true,
                            shared: true
                        )
                    )
                    .WriteTo.Async(c => c.Console(outputTemplate: LoggingOptions.Template))
        );
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    [UsedImplicitly]
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UsePlatformDetect().LogToTrace();
}
