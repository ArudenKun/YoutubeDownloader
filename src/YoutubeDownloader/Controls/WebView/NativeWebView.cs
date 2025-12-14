using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform;
using JetBrains.Annotations;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using R3;
using R3.ObservableEvents;
using Vanara.PInvoke;
using WinFormsControl = System.Windows.Forms.Control;
using WinFormsPanel = System.Windows.Forms.Panel;

namespace YoutubeDownloader.Controls.WebView;

[PublicAPI]
public class NativeWebView : NativeControlHost, IDisposable
{
    private const string EmptySource = "about:blank";

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private WebView2? _webView; // the actual WebView2 control
    private WinFormsControl? _hostPanel; // WinForms panel to host WebView2
    private IntPtr _parentHwnd; // Avalonia native HWND
    private IntPtr _prevParentWndProc = IntPtr.Zero; // original parent WndProc
    private bool _isSubclassed; // flag if parent is subclassed
    private GCHandle? _wndProcHandle; // keeps delegate alive for SetWindowLongPtr
    private bool _isInitialized;
    private IDisposable? _subscriptions;

    public event EventHandler<CoreWebView2InitializationCompletedEventArgs>? InitializationCompleted;
    public event EventHandler<CoreWebView2NavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<CoreWebView2NavigationStartingEventArgs>? NavigationStarting;

    // ReSharper disable once InconsistentNaming
    public event EventHandler<CoreWebView2DOMContentLoadedEventArgs>? DOMContentLoaded;

    public static readonly StyledProperty<CoreWebView2CreationProperties?> CreationPropertiesProperty =
        AvaloniaProperty.Register<NativeWebView, CoreWebView2CreationProperties?>(
            nameof(CreationProperties),
            defaultBindingMode: BindingMode.OneWay
        );

    public CoreWebView2CreationProperties? CreationProperties
    {
        get => GetValue(CreationPropertiesProperty);
        set => SetValue(CreationPropertiesProperty, value);
    }

    public static readonly StyledProperty<CoreWebView2> CoreWebView2Property =
        AvaloniaProperty.Register<NativeWebView, CoreWebView2>(
            nameof(CoreWebView2),
            defaultBindingMode: BindingMode.OneWayToSource
        );

    public CoreWebView2 CoreWebView2
    {
        get => GetValue(CoreWebView2Property);
        set => SetValue(CoreWebView2Property, value);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Guard: Do not run if CoreWebView2 is not yet ready.
        // The value is already stored in the Avalonia Property system,
        // so InitializeWebView will pick it up when it finishes.
        if (!_isInitialized || _webView == null)
            return;

        if (change.Property == SourceProperty)
        {
            var newUrl = change.GetNewValue<Uri?>();
            _webView?.Source = newUrl;
        }
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        _parentHwnd = parent.Handle;

        // create non-top-level host panel and fill parent
        _hostPanel = new WinFormsPanel { Dock = DockStyle.Fill };
        _hostPanel.CreateControl();

        // parent it to Avalonia HWND
        User32.SetParent(_hostPanel.Handle, _parentHwnd);

        var style = User32.GetWindowLongPtr(_hostPanel.Handle, User32.WindowLongFlags.GWL_STYLE);
        style |= (IntPtr)(User32.WindowStyles.WS_CHILD | User32.WindowStyles.WS_VISIBLE);
        User32.SetWindowLong(_hostPanel.Handle, User32.WindowLongFlags.GWL_STYLE, style);

        // match panel size to parent
        ResizeHostToParent();

        // subclass parent to track WM_SIZE for automatic resize
        SubclassParentWindow();

        // create WebView2 inside host panel
        _webView = new WebView2 { Dock = DockStyle.Fill };
        _hostPanel.Controls.Add(_webView);
        _webView.CreateControl();

        InitializeWebView();

        // return WebView2 HWND to Avalonia
        return new PlatformHandle(_webView.Handle, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        UnsubclassParentWindow(); // restore parent WndProc

        try
        {
            _webView?.Dispose();
            _hostPanel?.Dispose();
        }
        catch
        {
            // ignore exceptions during cleanup
        }

        _webView = null;
        _hostPanel = null;
        base.DestroyNativeControlCore(control);
    }

    private async void InitializeWebView()
    {
        try
        {
            if (CreationProperties is not null)
            {
                _webView?.CreationProperties = CreationProperties;
            }

            await _webView!.EnsureCoreWebView2Async();
            SetValue(CoreWebView2Property, _webView.CoreWebView2);
            _subscriptions = AddHandlers();

            _isInitialized = true;

            if (Source is not null)
            {
                _webView.Source = Source;
            }
            else
            {
                _webView.CoreWebView2.Navigate(EmptySource);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebView2 init error: {ex.Message}");
        }
    }

    private IDisposable AddHandlers() =>
        Disposable.Combine(
            _webView
                .Events()
                .CoreWebView2InitializationCompleted.Subscribe(x =>
                    InitializationCompleted?.Invoke(this, x)
                ),
            CoreWebView2
                .Events()
                .NavigationStarting.Subscribe(x => NavigationStarting?.Invoke(this, x)),
            CoreWebView2
                .Events()
                .NavigationCompleted.Subscribe(x => NavigationCompleted?.Invoke(this, x)),
            CoreWebView2.Events().DOMContentLoaded.Subscribe(x => DOMContentLoaded?.Invoke(this, x))
        );

    public void Dispose()
    {
        _subscriptions?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Parent subclass + resize

    // adjust host panel to fill Avalonia HWND
    private void ResizeHostToParent()
    {
        if (_hostPanel == null || _parentHwnd == IntPtr.Zero)
            return;

        if (User32.GetClientRect(_parentHwnd, out var rc))
        {
            int w = rc.Right - rc.Left;
            int h = rc.Bottom - rc.Top;
            User32.MoveWindow(_hostPanel.Handle, 0, 0, Math.Max(0, w), Math.Max(0, h), true);
        }
    }

    // subclass parent HWND to intercept size/move messages
    private void SubclassParentWindow()
    {
        if (_isSubclassed || _parentHwnd == IntPtr.Zero)
            return;

        WndProcDelegate newProc = ParentWndProc;
        _wndProcHandle = GCHandle.Alloc(newProc); // keep delegate alive
        IntPtr ptr = Marshal.GetFunctionPointerForDelegate(newProc);
        _prevParentWndProc = User32.SetWindowLong(
            _parentHwnd,
            User32.WindowLongFlags.GWL_WNDPROC,
            ptr
        );
        _isSubclassed = true;
    }

    // remove subclass and restore original WndProc
    private void UnsubclassParentWindow()
    {
        if (!_isSubclassed || _parentHwnd == IntPtr.Zero)
            return;

        User32.SetWindowLong(_parentHwnd, User32.WindowLongFlags.GWL_WNDPROC, _prevParentWndProc);
        _prevParentWndProc = IntPtr.Zero;

        if (_wndProcHandle is { IsAllocated: true })
            _wndProcHandle.Value.Free();

        _isSubclassed = false;
    }

    // intercept parent window messages to handle resizing
    private IntPtr ParentWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        const uint wmSize = 0x0005;
        const uint wmMove = 0x0003;
        const uint wmWindowposchanged = 0x0047;

        if (msg == wmSize || msg == wmMove || msg == wmWindowposchanged)
            ResizeHostToParent();

        return User32.CallWindowProc(_prevParentWndProc, hwnd, msg, wParam, lParam);
    }

    #endregion;
}
