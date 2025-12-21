using Avalonia.Interactivity;
using R3;

namespace YoutubeDownloader.Extensions;

public static class ControlExtensions
{
    public delegate void RoutedEventHandler<in TEventArgs>(object sender, TEventArgs e)
        where TEventArgs : RoutedEventArgs;

    public static Observable<(object? Sender, TEventArgs EventArgs)> OnEvent<TEventArgs>(
        this Interactive target,
        RoutedEvent<TEventArgs> routedEvent,
        RoutingStrategies routingStrategies = RoutingStrategies.Bubble | RoutingStrategies.Direct,
        bool handledEventsToo = false
    )
        where TEventArgs : RoutedEventArgs =>
        Observable.FromEventHandler<TEventArgs>(
            add => target.AddHandler(routedEvent, add, routingStrategies, handledEventsToo),
            remove => target.RemoveHandler(routedEvent, remove)
        );
}
