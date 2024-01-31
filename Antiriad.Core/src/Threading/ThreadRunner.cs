using Antiriad.Core.Log;

namespace Antiriad.Core.Threading;

/// <summary>
/// Simple, fast, auto start/stop Thread Pool
/// </summary>
public sealed class ThreadRunner
{
  /// <summary>
  /// Sets or gets the minimum Runners count to maintain available
  /// </summary>
  public static long MinimumCount = 5;

  /// <summary>
  /// Timeout for killing an inactive excedent Runner
  /// </summary>
  public static int InactivityTimeout = 10000;

  /// <summary>
  /// Maximum concurrent use of runners
  /// </summary>
  public static long RunningPeak = 0;
  private static long lastRunningPeak;

  /// <summary>
  /// Current concurrent use of runners
  /// </summary>
  public static long RunningCount = 0;

  private static readonly object LockRunners = new();
  private static readonly ThreadRunner Pool = new();

  private const int EnoughTime = 5000;

  private readonly Timer keeper;
  private readonly List<Runner> runners = new();

  private ThreadRunner()
  {
    AppDomain.CurrentDomain.ProcessExit += this.DoProcessExit;
    this.keeper = new Timer(this.KeepPool, null, 0, 1000);
  }

  private void DoProcessExit(object? sender, EventArgs e)
  {
    lock (LockRunners)
    {
      this.keeper.Change(Timeout.Infinite, 0);
      this.StopAll();
    }
  }

  private void KeepPool(object? state)
  {
    if (!Monitor.TryEnter(LockRunners, 1000)) return;
    List<Runner>? pending = null;

    try
    {
      if (RunningPeak != lastRunningPeak)
      {
        lastRunningPeak = RunningPeak;
        Trace.Debug($"ThreadRunner Minimum={MinimumCount} RunningPeak={RunningPeak} RunnerCount={this.runners.Count}");
      }

      var now = DateTime.Now;

      if (this.runners.Count > MinimumCount)
      {
        var list = this.runners.Where(i => !i.IsBusy && i.Termination < now).ToList();

        foreach (var item in list)
        {
          item.Terminate();
          this.runners.Remove(item);
        }
      }
      else
      {
        while (this.runners.Count < MinimumCount)
        {
          var runner = new Runner(false);
          this.runners.Add(runner);
          (pending ??= new List<Runner>()).Add(runner);
        }
      }
    }
    finally
    {
      Monitor.Exit(LockRunners);
    }

    pending?.ForEach(i => i.Start());
  }

  private void StopAll()
  {
    if (this.runners.Count == 0) return;
    this.runners.ForEach(i => i.Terminate());
    this.runners.ForEach(i => i.Join(EnoughTime));
    this.runners.Clear();
  }

  private bool Get(Action<object?> action, object? arg, ThreadPriority priority)
  {
    if (!Monitor.TryEnter(LockRunners, EnoughTime))
    {
      Trace.Error("ThreadRunner: cannot get runner");
      return false;
    }

    Runner runner;

    try
    {
      if ((runner = this.runners.Find(i => !i.IsBusy)!) != null)
        return runner.Wakeup(action, arg, priority);

      this.runners.Add(runner = new Runner(true));
    }
    finally
    {
      Monitor.Exit(LockRunners);
    }

    return runner.Start() && runner.Wakeup(action, arg, priority);
  }

  /// <summary>
  /// Get a Runner and executes <code>action</code>
  /// </summary>
  /// <param name="action">Method for running Runner action</param>
  /// <param name="arg">Argument that will be received in method proc</param>
  /// <param name="priority">Desired thread priority</param>
  /// <returns>True if a Runner is Spawned</returns>
  public static bool Spawn<T>(Action<T> action, T? arg, ThreadPriority priority = ThreadPriority.Normal)
  {
    if (action == null)
    {
      Trace.Error("ThreadRunner.Spawn: empty action");
      return false;
    }

    var isok = Pool.Get(o => action((T)o!), arg, priority);

    if (!isok)
      throw new Exception("ThreadRunner.Spawn: cannot run thread");

    return isok;
  }

  public static bool Spawn(Action<object> action, ThreadPriority priority = ThreadPriority.Normal)
  {
    return Spawn<object>(action, null, priority);
  }

  public static bool Spawn(Action action, ThreadPriority priority = ThreadPriority.Normal)
  {
    return Spawn<object>(o => action(), null, priority);
  }

  internal static void RunnerStart()
  {
    if (Interlocked.Increment(ref RunningCount) > RunningPeak)
      RunningPeak = RunningCount;
  }

  internal static void RunnerStop()
  {
    Interlocked.Decrement(ref RunningCount);
  }

  public static void EnsureMinimum(int count)
  {
    MinimumCount = count;
    Pool.WaitMinimum();
  }

  private void WaitMinimum()
  {
    while (this.runners.Count < MinimumCount) Thread.Yield();
  }
}
