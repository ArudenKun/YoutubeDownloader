using CommunityToolkit.Mvvm.ComponentModel;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using YoutubeDownloader.Models.EventData;
using YoutubeDownloader.ViewModels.Pages;
using ZLinq;

namespace YoutubeDownloader.ViewModels;

public sealed partial class MainViewModel
    : ViewModel,
        ILocalEventHandler<ShowPageEventData>,
        ISingletonDependency
{
    private readonly Dictionary<Type, PageViewModel> _pageCache;

    public MainViewModel(IEnumerable<IPageViewModel> pageViewModels)
    {
        _pageCache = pageViewModels
            .AsValueEnumerable()
            .OrderBy(x => x.Index)
            .Cast<PageViewModel>()
            .ToDictionary(k => k.GetType(), v => v);
    }

    public ICollection<PageViewModel> Pages => _pageCache.Values;

    [ObservableProperty]
    public partial PageViewModel Page { get; set; }

    public Task HandleEventAsync(ShowPageEventData eventData)
    {
        if (_pageCache.TryGetValue(eventData.ViewModelType, out var cachedPage))
        {
            Page = cachedPage;
        }

        return Task.CompletedTask;
    }
}
