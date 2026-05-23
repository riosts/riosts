using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using PixelWorldsInjector.Models;

namespace PixelWorldsInjector.Services;

/// <summary>
/// Polls running processes and fires <see cref="StatusChanged"/> when the set of
/// alive instance PIDs changes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ProcessMonitor : IDisposable
{
    private readonly Func<IEnumerable<Instance>> _getInstances;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;

    public event Action<Instance, bool>? StatusChanged;

    public ProcessMonitor(Func<IEnumerable<Instance>> getInstances)
    {
        _getInstances = getInstances;
        _loopTask = Task.Run(LoopAsync);
    }

    public bool IsAlive(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private async Task LoopAsync()
    {
        var previous = new Dictionary<string, bool>();
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                foreach (var instance in _getInstances().ToList())
                {
                    var alive = instance.RunningPid is int pid && IsAlive(pid);
                    var wasAlive = previous.TryGetValue(instance.Id, out var prev) && prev;
                    if (alive != wasAlive)
                    {
                        previous[instance.Id] = alive;
                        if (!alive)
                        {
                            instance.RunningPid = null;
                        }

                        try
                        {
                            StatusChanged?.Invoke(instance, alive);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn("StatusChanged handler threw", ex);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(2), _cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on dispose
        }
        catch (Exception ex)
        {
            Logger.Error("ProcessMonitor loop crashed", ex);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _loopTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // ignore
        }

        _cts.Dispose();
    }
}
