using AutoInterfaceAttributes;
using JetBrains.Annotations;
using Volo.Abp.DependencyInjection;

namespace YoutubeDownloader.Services;

[AutoInterface]
[UsedImplicitly]
public sealed class DialogService : IDialogService, ISingletonDependency { }
