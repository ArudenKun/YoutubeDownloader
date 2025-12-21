using YoutubeDownloader.ViewModels;

namespace YoutubeDownloader.Views;

public interface IView : IDisposable;

public interface IView<TViewModel> : IView
    where TViewModel : ViewModel
{
    TViewModel ViewModel { get; }
    TViewModel DataContext { get; set; }
}
