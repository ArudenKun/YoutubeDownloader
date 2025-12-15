using YoutubeDownloader.ViewModels;

namespace YoutubeDownloader.Views;

public interface IView<TViewModel> : IDisposable
    where TViewModel : ViewModel
{
    TViewModel ViewModel { get; }
    TViewModel DataContext { get; set; }
}
