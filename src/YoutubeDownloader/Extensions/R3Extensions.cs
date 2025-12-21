using YoutubeDownloader.ViewModels;

namespace YoutubeDownloader.Extensions;

public static class R3Extensions
{
    public static TDisposable AddTo<TDisposable>(this TDisposable disposable, ViewModel viewModel)
        where TDisposable : IDisposable
    {
        viewModel.AddTo(disposable);
        return disposable;
    }
}
