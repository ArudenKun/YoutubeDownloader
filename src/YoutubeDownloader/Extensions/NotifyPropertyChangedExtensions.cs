using System.ComponentModel;
using R3;

namespace YoutubeDownloader.Extensions;

public static class NotifyPropertyChangedExtensions
{
    extension<TOwner>(TOwner owner)
        where TOwner : INotifyPropertyChanged
    {
        public Observable<Unit> WatchAllProperties(bool deep = false)
        {
            var sources = new List<Observable<Unit>>();
            var ownerStream = Observable
                .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => (_, e) => h(e),
                    h => owner.PropertyChanged += h,
                    h => owner.PropertyChanged -= h
                )
                .Select(_ => Unit.Default);
            sources.Add(ownerStream);

            if (!deep)
                return sources.Merge();

            {
                var properties = typeof(TOwner).GetProperties();

                foreach (var property in properties)
                {
                    if (!property.CanRead || property.GetIndexParameters().Length is not 0)
                        continue;

                    try
                    {
                        var value = property.GetValue(owner);
                        if (value is INotifyPropertyChanged child)
                        {
                            var childStream = Observable
                                .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                                    h => (_, e) => h(e),
                                    h => child.PropertyChanged += h,
                                    h => child.PropertyChanged -= h
                                )
                                .Select(_ => Unit.Default);
                            sources.Add(childStream);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            return sources.Merge();
        }
    }
}
