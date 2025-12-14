using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace YoutubeDownloader.Hosting;

[DependsOn(typeof(AbpAutofacModule))]
public sealed class YoutubeDownloaderHostingModule : AbpModule;
