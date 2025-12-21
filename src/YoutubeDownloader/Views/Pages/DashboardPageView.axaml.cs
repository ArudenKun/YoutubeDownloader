using Avalonia.Input;
using Avalonia.Interactivity;
using YoutubeDownloader.ViewModels.Pages;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;

namespace YoutubeDownloader.Views.Pages;

public partial class DashboardPageView : UserControl<DashboardPageViewModel>
{
    public DashboardPageView()
    {
        InitializeComponent();

        QueryTextBox.AddHandler(KeyDownEvent, OnKeyDownHandler, RoutingStrategies.Tunnel);
    }

    private void OnKeyDownHandler(object? sender, KeyEventArgs args)
    {
        // When pressing Enter without Shift, execute the default button command
        // instead of adding a new line.
        if (args.Key != Key.Enter || args.KeyModifiers == KeyModifiers.Shift)
            return;

        args.Handled = true;
        ProcessQueryButton.Command?.Execute(ProcessQueryButton.CommandParameter);
    }
}
