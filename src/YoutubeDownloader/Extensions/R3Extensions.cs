using Avalonia.Controls;
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

    /// <summary>
    /// Dispose self on target UserControl has been detached from logical tree.
    /// </summary>
    /// <param name="disposable"></param>
    /// <param name="control"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Self disposable</returns>
    public static T AddTo<T>(this T disposable, UserControl control)
        where T : IDisposable
    {
        control.DetachedFromLogicalTree += (_, _) => disposable.Dispose();
        return disposable;
    }

    /// <summary>
    /// Dispose self on target Window has been closed.
    /// </summary>
    /// <param name="disposable"></param>
    /// <param name="window"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Self disposable</returns>
    public static T AddTo<T>(this T disposable, Window window)
        where T : IDisposable
    {
        window.Closed += (_, _) => disposable.Dispose();
        return disposable;
    }
}
