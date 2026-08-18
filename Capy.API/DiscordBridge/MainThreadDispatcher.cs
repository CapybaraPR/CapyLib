using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MEC;

namespace Capy.API.DiscordBridge;

internal sealed class MainThreadDispatcher
{
    private readonly ConcurrentQueue<Action> _workQueue = new();
    private CoroutineHandle _coroutine;
    private int _running;

    public void Start()
    {
        if (Interlocked.Exchange(ref _running, 1) == 0)
        {
            _coroutine = Timing.RunCoroutine(DispatchLoop());
        }
    }

    public void Stop()
    {
        if (Interlocked.Exchange(ref _running, 0) == 1)
        {
            if (_coroutine.IsRunning)
                Timing.KillCoroutines(_coroutine);

            while (_workQueue.TryDequeue(out _)) { }
        }
    }

    public Task<T> InvokeAsync<T>(Func<T> function, int timeoutSeconds = 8)
    {
        var tcs = new TaskCompletionSource<T>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

        cts.Token.Register(() => tcs.TrySetException(new TimeoutException("Таймаут выполнения задачи на главном потоке сервера.")));

        _workQueue.Enqueue(() =>
        {
            if (tcs.Task.IsCompleted) return;

            try
            {
                T result = function();
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    public Task InvokeAsync(Action action, int timeoutSeconds = 8)
    {
        return InvokeAsync<object?>(() =>
        {
            action();
            return null;
        }, timeoutSeconds);
    }

    private IEnumerator<float> DispatchLoop()
    {
        while (_running == 1)
        {
            int processed = 0;
            while (_workQueue.TryDequeue(out var action) && processed < 32)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Exiled.API.Features.Log.Error($"[DiscordBridge.Dispatcher] Ошибка выполнения на главном потоке: {ex}");
                }

                processed++;
            }

            yield return Timing.WaitForOneFrame;
        }
    }
}
