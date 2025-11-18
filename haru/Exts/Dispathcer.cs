using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

static class DispatcherQueueExtensions
{
    public static Task EnqueueAsync(this DispatcherQueue dq, Action action)
    {
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (dq.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }))
        {
            return tcs.Task;
        }
        else
        {
            tcs.SetException(new InvalidOperationException("TryEnqueue failed"));
            return tcs.Task;
        }
    }
}
