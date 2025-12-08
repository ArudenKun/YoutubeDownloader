using System.Text.Json.Serialization;
using AutoInterfaceAttributes;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using R3;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Options;
using YoutubeDownloader.Utilities;

namespace YoutubeDownloader.Services;

[AutoInterface(Inheritance = [typeof(IDisposable)])]
[INotifyPropertyChanged]
public sealed partial class SettingsService : JsonFileBase, ISettingsService, ISingletonDependency
{
    private readonly CompositeDisposable _disposables = new();

    public SettingsService()
        : base(AppHelper.SettingsPath, SettingServiceJsonSerializerContext.Default)
    {
        this.WatchAllProperties().Debounce(1.Seconds()).Subscribe(_ => Save()).AddTo(_disposables);
    }

    [ObservableProperty]
    public partial bool AutoUpdate { get; set; }

    [ObservableProperty]
    public partial LoggingOptions Logging { get; set; } = new();

    // Required for AutoInterface to see the methods
    // ReSharper disable RedundantOverriddenMember
    public override void Reset()
    {
        base.Reset();
    }

    public override void Save()
    {
        base.Save();
    }

    public override bool Load()
    {
        return base.Load();
    }

    // ReSharper restore RedundantOverriddenMember

    public void Dispose() => _disposables.Dispose();

    [JsonSerializable(typeof(SettingsService))]
    private sealed partial class SettingServiceJsonSerializerContext : JsonSerializerContext;
}
