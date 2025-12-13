using System.Threading.Tasks;
using Avalonia.Metadata;
using R3.ObservableEvents;

[assembly: GenerateStaticEventObservables(typeof(TaskScheduler))]

[assembly: XmlnsDefinition(
    "https://github.com/arudenkun/YoutubeDownloader",
    "YoutubeDownloader.ViewModels"
)]
[assembly: XmlnsDefinition(
    "https://github.com/arudenkun/YoutubeDownloader",
    "YoutubeDownloader.ViewModels.Pages"
)]
[assembly: XmlnsDefinition(
    "https://github.com/arudenkun/YoutubeDownloader",
    "YoutubeDownloader.ViewModels.Dialogs"
)]
[assembly: XmlnsDefinition(
    "https://github.com/arudenkun/YoutubeDownloader",
    "YoutubeDownloader.Views"
)]
[assembly: XmlnsDefinition(
    "https://github.com/arudenkun/YoutubeDownloader",
    "YoutubeDownloader.Views.Pages"
)]
[assembly: XmlnsDefinition(
    "https://github.com/arudenkun/YoutubeDownloader",
    "YoutubeDownloader.Views.Dialogs"
)]
[assembly: XmlnsDefinition(
    "https://github.com/arudenkun/YoutubeDownloader",
    "YoutubeDownloader.Controls"
)]
[assembly: XmlnsPrefix("https://github.com/arudenkun/YoutubeDownloader", "yd")]
