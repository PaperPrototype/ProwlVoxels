using System;
using System.Collections.Concurrent;
using System.Threading;
using Prowl.Runtime;

namespace Voxels;

/// <summary>
/// Small background job queue backed by dedicated worker threads.
/// Dispose() cancels the CancellationToken passed to running jobs and joins
/// every worker thread, so nothing keeps running after the owner (e.g. World)
/// is disposed. This matters in the editor: stopping play mode disposes the
/// scene but not the editor process itself, so any thread not explicitly
/// stopped here would keep running in the background indefinitely.
/// </summary>
public sealed class JobSystem : IDisposable
{
    private readonly BlockingCollection<Action<CancellationToken>> jobs = new();
    private readonly CancellationTokenSource cts = new();
    private readonly Thread[] workers;
    private bool disposed;

    public JobSystem(int workerCount = 0)
    {
        if (workerCount <= 0)
            workerCount = Math.Max(1, Environment.ProcessorCount - 1);

        workers = new Thread[workerCount];
        for (int i = 0; i < workers.Length; i++)
        {
            workers[i] = new Thread(WorkerLoop)
            {
                Name = $"JobSystem Worker {i}",
                IsBackground = true,
            };
            workers[i].Start();
        }
    }

    /// <summary>Queues a job to run on a worker thread. Ignored once disposed.</summary>
    public void Enqueue(Action<CancellationToken> job)
    {
        if (disposed) return;

        try
        {
            jobs.Add(job);
        }
        catch (InvalidOperationException)
        {
            // Add can race with CompleteAdding during Dispose; safe to drop.
        }
    }

    private void WorkerLoop()
    {
        var token = cts.Token;
        try
        {
            foreach (var job in jobs.GetConsumingEnumerable(token))
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    job(token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when a job observes cancellation mid-work.
                }
                catch (Exception e)
                {
                    Debug.LogError($"JobSystem job threw: {e}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the token cancels while a worker is idle-waiting.
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        cts.Cancel();
        jobs.CompleteAdding();

        foreach (var thread in workers)
            thread.Join();

        cts.Dispose();
        jobs.Dispose();
    }
}
