using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace YoutubeDownloader.Extensions;

public static class DispatcherExtensions
{
    public static Task<T> PostAsync<T>(
        this IDispatcher dispatcher,
        Func<T> action,
        DispatcherPriority dispatcherPriority = default
    )
    {
        var tcs = new TaskCompletionSource<T>();
        dispatcher.Post(() => tcs.SetResult(action()), dispatcherPriority);
        return tcs.Task;
    }

    public static void WaitOnDispatcherFrame(this Task task, Dispatcher? dispatcher = null)
    {
        var frame = new DispatcherFrame();
        AggregateException? capturedException = null;

        task.ContinueWith(
            t =>
            {
                capturedException = t.Exception;
                frame.Continue = false; // 结束消息循环
            },
            TaskContinuationOptions.AttachedToParent
        );

        dispatcher ??= Dispatcher.UIThread;
        dispatcher.PushFrame(frame);

        if (capturedException != null)
        {
            throw capturedException;
        }
    }
}
