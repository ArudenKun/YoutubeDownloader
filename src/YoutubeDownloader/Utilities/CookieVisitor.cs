using Xilium.CefGlue;

namespace YoutubeDownloader.Utilities;

public sealed class CookieVisitor : CefCookieVisitor
{
    private readonly TaskCompletionSource<List<CefCookie>> _tcs = new();
    private readonly List<CefCookie> _cookies = [];

    protected override bool Visit(CefCookie cookie, int count, int total, out bool delete)
    {
        _cookies.Add(cookie);

        delete = false;
        return true;
    }

    public Task<List<CefCookie>> GetCookiesAsync() => _tcs.Task;

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_tcs.Task.IsCompleted)
        {
            _tcs.SetResult(_cookies);
        }

        base.Dispose(disposing);
    }
}
