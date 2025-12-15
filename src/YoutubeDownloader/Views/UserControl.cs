using Avalonia.Interactivity;
using R3;
using Volo.Abp.DependencyInjection;
using YoutubeDownloader.ViewModels;

namespace YoutubeDownloader.Views;

public abstract class UserControl<TViewModel> : UserControl, IView<TViewModel>, ITransientDependency
    where TViewModel : ViewModel
{
    protected CompositeDisposable Disposables { get; } = new();

    public new TViewModel DataContext
    {
        get =>
            base.DataContext as TViewModel
            ?? throw new InvalidCastException(
                $"DataContext is null or not of the expected type '{typeof(TViewModel).FullName}'."
            );
        set => base.DataContext = value;
    }

    public TViewModel ViewModel => DataContext;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        ViewModel.OnLoaded();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        ViewModel.OnUnloaded();
    }

    public void Dispose()
    {
        Disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
