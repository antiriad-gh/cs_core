using Antiriad.Core.Log;

namespace Antiriad.Core.Threading;

/// <summary>
/// Pooled Thread
/// </summary>
internal class Runner
{
  private readonly Thread thread;
  private readonly ManualResetEvent awake;
  private volatile bool terminated;
  private Action<object?>? action;
  private object? arg;
  private volatile bool running;

  internal Runner(bool reserved)
  {
    this.awake = new ManualResetEvent(false);
    this.thread = new Thread(this.Body) { IsBackground = true };
    this.running = reserved;
  }

  internal bool Start()
  {
    try
    {
      if (!this.thread.IsAlive) this.thread.Start();
      return true;
    }
    catch
    {
      return false;
    }
  }

  internal bool IsBusy { get { return this.running; } }
  internal DateTime Termination { get; private set; }

  internal void Terminate()
  {
    this.terminated = true;
    this.awake.Set();
  }

  internal void Join(int milliseconds)
  {
    try
    {
      if (this.thread.ManagedThreadId != Environment.CurrentManagedThreadId)
        this.thread.Join(milliseconds);
    }
    catch { }
  }

  internal bool Wakeup(Action<object?> action, object? arg, ThreadPriority priority = ThreadPriority.Normal)
  {
    this.running = true;
    this.action = action;
    this.arg = arg;

    if (priority != ThreadPriority.Normal)
      this.thread.Priority = priority;

    return this.awake.Set();
  }

  private void Body()
  {
    while (!this.terminated)
    {
      this.awake.WaitOne();

      if (this.terminated)
        break;

      this.awake.Reset();

      try
      {
        ThreadRunner.RunnerStart();

        if (this.action != null)
        {
          this.action(this.arg);
          this.action = null;
        }
        else
          Trace.Warning("Runner.Body: thread with no action assigned");
      }
      catch (Exception ex)
      {
        Trace.Exception(ex);
      }
      finally
      {
        ThreadRunner.RunnerStop();
      }

      this.running = false;
      this.thread.Priority = ThreadPriority.Normal;
      this.Termination = DateTime.Now.AddMilliseconds(ThreadRunner.InactivityTimeout);
    }
  }
}
