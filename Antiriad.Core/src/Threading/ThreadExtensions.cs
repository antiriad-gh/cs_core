namespace Antiriad.Core.Threading;

using System.Collections.Generic;
using System.Threading;
using Antiriad.Core.Collections;

/// <summary>
/// Thread class extension methods.
/// </summary>
public static class ThreadExtensions
{
  /// <summary>
  /// Waits for all threads in the sequence to terminate using Thread.Join() on them.
  /// </summary>
  /// <param name="threads">
  /// The instance to which method applies.
  /// </param>
  public static void Join(this IEnumerable<Thread> threads)
  {
    foreach (var thread in threads)
    {
      thread.Join();
    }
  }

  /// <summary>
  /// Performs action in different threads simultaneously and returns when all thread finishes
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="list">List of items</param>
  /// <param name="action">Action to perform on each item</param>
  /// <param name="timeout">Optional timeout for complete operation</param>
  /// <returns>True if returns before reach timeout</returns>
  public static bool ParallelTask<T>(this IList<T> list, Action<T> action, int timeout = -1)
  {
    if (list == null)
      return false;

    var counter = list.Count;

    if (counter == 0)
      return false;

    if (counter == 1)
    {
      action(list[0]);
      return true;
    }

    var cd = new ManualResetEventSlim(false);

    list.ForEach(i =>
      ThreadRunner.Spawn(o =>
      {
        action(i);
        if (Interlocked.Decrement(ref counter) == 0) cd.Set();
      }));

    return cd.Wait(timeout);
  }

  /// <summary>
  /// Performs action in different threads simultaneously and returns when all thread finishes
  /// </summary>
  /// <typeparam name="TSource"></typeparam>
  /// <param name="list">List of items</param>
  /// <param name="func">Action to perform on each item</param>
  /// <param name="timeout">Optional timeout for complete operation</param>
  /// <returns>True if returns before reach timeout and all funcs return true</returns>
  public static bool ParallelAll<TSource>(this IList<TSource> list, Func<TSource, bool> func, int timeout = -1)
  {
    if (list == null)
      return false;

    var counter = list.Count;

    if (counter == 0)
      return false;

    if (counter == 1)
      return func(list[0]);

    var cd = new ManualResetEventSlim(false);
    var oks = counter;

    list.ForEach(i =>
      ThreadRunner.Spawn(o =>
      {
        if (func(i)) Interlocked.Decrement(ref oks);
        if (Interlocked.Decrement(ref counter) == 0) cd.Set();
      }));

    return cd.Wait(timeout) && oks == counter && oks == 0;
  }

  public static bool Spawn<T>(this EventHandler<T> evt, object sender, T args)
  {
    return evt != null && ThreadRunner.Spawn(o => evt.Invoke(sender, args));
  }
}
