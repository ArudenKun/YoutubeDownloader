using System.Drawing;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform;
using JetBrains.Annotations;
using Microsoft.Web.WebView2.Core;
using R3;
using R3.ObservableEvents;
using Vanara.PInvoke;
using YoutubeDownloader.Extensions;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;

namespace YoutubeDownloader.Controls.WebView;

[PublicAPI]
public class NativeWebView : NativeControlHost
{
    private const string EmptySource = "about:blank";

    private CoreWebView2Controller? _controller;
    private InvisibleWindow? _invisibleWindow;
    private IDisposable? _subscriptions;
    private HWND _handle;

    public event EventHandler? WebViewCreated;
    public event EventHandler<CoreWebView2NavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<CoreWebView2NavigationStartingEventArgs>? NavigationStarting;

    // ReSharper disable once InconsistentNaming
    public event EventHandler<CoreWebView2DOMContentLoadedEventArgs>? DOMContentLoaded;

    public static readonly StyledProperty<string?> BrowserExecutableFolderProperty =
        AvaloniaProperty.Register<NativeWebView, string?>(
            nameof(BrowserExecutableFolder),
            defaultBindingMode: BindingMode.OneWay
        );

    public string? BrowserExecutableFolder
    {
        get => GetValue(BrowserExecutableFolderProperty);
        set => SetValue(BrowserExecutableFolderProperty, value);
    }

    public static readonly StyledProperty<string?> UserDataFolderProperty =
        AvaloniaProperty.Register<NativeWebView, string?>(
            nameof(UserDataFolder),
            defaultBindingMode: BindingMode.OneWay
        );

    public string? UserDataFolder
    {
        get => GetValue(UserDataFolderProperty);
        set => SetValue(UserDataFolderProperty, value);
    }

    public static readonly StyledProperty<CoreWebView2> CoreWebView2Property =
        AvaloniaProperty.Register<NativeWebView, CoreWebView2>(
            nameof(CoreWebView2),
            defaultBindingMode: BindingMode.OneWayToSource
        );

    public CoreWebView2 CoreWebView2
    {
        get => GetValue(CoreWebView2Property);
        private set => SetValue(CoreWebView2Property, value);
    }

    public bool CanGoBack => CoreWebView2.CanGoBack;

    public bool CanGoForward => CoreWebView2.CanGoForward;

    public static readonly StyledProperty<Uri?> SourceProperty = AvaloniaProperty.Register<
        NativeWebView,
        Uri?
    >(nameof(Source));

    public Uri? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public bool GoBack()
    {
        _controller?.CoreWebView2.GoBack();
        return true;
    }

    public bool GoForward()
    {
        _controller?.CoreWebView2.GoForward();
        return true;
    }

    public Task<string?> InvokeScript(string scriptName)
    {
        return _controller?.CoreWebView2?.ExecuteScriptAsync(scriptName)
            ?? Task.FromResult<string?>(null);
    }

    public void Navigate(Uri url)
    {
        _controller?.CoreWebView2?.Navigate(url.AbsolutePath);
    }

    public void NavigateToString(string text)
    {
        _controller?.CoreWebView2?.NavigateToString(text);
    }

    public bool Refresh()
    {
        _controller?.CoreWebView2?.Reload();
        return true;
    }

    public bool Stop()
    {
        _controller?.CoreWebView2?.Stop();
        return true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (_controller is null)
            return;

        if (change.Property == SourceProperty)
        {
            var newUrl = change.GetNewValue<Uri?>();
            CoreWebView2.Navigate(newUrl?.ToString());
        }
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        _handle = parent.Handle;
        if (_handle.IsNull)
        {
            _invisibleWindow ??= new InvisibleWindow();
            _handle = _invisibleWindow.Handle;
        }
        else
        {
            SetLayeredWindowAttributes(
                _handle,
                new COLORREF(1),
                0,
                LayeredWindowAttributes.LWA_COLORKEY
            );
        }

        if (_controller == null)
        {
            InitializeWebViewAsync().WaitOnDispatcherFrame();
        }
        else
        {
            _controller.ParentWindow = (IntPtr)_handle; // 重新设置父窗口
        }

        // return WebView2 HWND to Avalonia
        return new PlatformHandle(_controller!.ParentWindow, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        try
        {
            _invisibleWindow?.Dispose();
            _controller?.Close();
            _subscriptions?.Dispose();
        }
        catch
        {
            // ignore exceptions during cleanup
        }

        _invisibleWindow = null;
        _controller = null;
        base.DestroyNativeControlCore(control);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (_controller is null)
            return;
        var newSize = e.NewSize;
        _controller.BoundsMode = CoreWebView2BoundsMode.UseRasterizationScale;
        _controller.Bounds = new Rectangle(0, 0, (int)newSize.Width, (int)newSize.Height);
    }

    private async Task InitializeWebViewAsync()
    {
        var env = await CoreWebView2Environment.CreateAsync(
            BrowserExecutableFolder,
            UserDataFolder
        );
        _controller = await env.CreateCoreWebView2ControllerAsync((IntPtr)_handle);
        _controller.DefaultBackgroundColor = Color.Transparent;
        _controller.IsVisible = true;
        _controller.CoreWebView2.Navigate(Source is not null ? Source.AbsoluteUri : EmptySource);
        SetValue(CoreWebView2Property, _controller.CoreWebView2);
        WebViewCreated?.Invoke(this, EventArgs.Empty);
    }

    private IDisposable AddHandlers() =>
        Disposable.Combine(
            CoreWebView2
                .Events()
                .NavigationStarting.Subscribe(x => NavigationStarting?.Invoke(this, x)),
            CoreWebView2
                .Events()
                .NavigationCompleted.Subscribe(x => NavigationCompleted?.Invoke(this, x)),
            CoreWebView2.Events().DOMContentLoaded.Subscribe(x => DOMContentLoaded?.Invoke(this, x))
        );

    private class InvisibleWindow : IDisposable
    {
        private const string ClassName = "InvisibleWindow";

        public HWND Handle { get; private set; }

        public InvisibleWindow()
        {
            var wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = WndProc,
                hInstance = GetModuleHandle(),
                lpszClassName = ClassName,
            };

            RegisterClassEx(wndClass);

            Handle = CreateWindowEx(
                0,
                ClassName,
                ClassName,
                0,
                0,
                0,
                0,
                0,
                HWND.NULL,
                HMENU.NULL,
                wndClass.hInstance,
                IntPtr.Zero
            );
        }

        private static IntPtr WndProc(HWND hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (uint)WindowMessage.WM_DESTROY)
            {
                PostQuitMessage();
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            if (Handle.IsNull)
                return;

            DestroyWindow(Handle);
            Handle = HWND.NULL;
        }
    }
}
