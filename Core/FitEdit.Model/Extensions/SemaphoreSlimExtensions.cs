#nullable enable

namespace FitEdit.Model.Extensions;

public static class SemaphoreSlimExtensions
{
  /// <summary>
  /// Try to enter the semaphore. If it is busy, return. Else call the given Func.
  /// 
  /// <para/>
  /// This means the only call of the given Func can run at any given time, for each count of the given semaphore.
  /// </summary>
  public static async Task RunAtomically(this SemaphoreSlim sem, Func<Task> f, string callerName)
  {
    bool entered = await sem.WaitAsync(TimeSpan.Zero).AnyContext();

    if (!entered)
    {
      return;
    }

    try
    {
      await f();
    }
    catch (Exception e)
    {
      Log.Error($"Exception while running {callerName}: {e}");
    }
    finally
    {
      sem.Release();
    }
  }

  /// <summary>
  /// Wait up to the given timeout to enter the semaphore.
  /// If the semaphore times out, call the given Func anyway.
  /// 
  /// <para/>
  /// This means the call will wait any already-running calls to 
  /// finish (up to the timeout), instead of bailing, and then run.
  /// </summary>
  public static async Task RunAtomically(this SemaphoreSlim sem, Func<Task> f, string callerName, TimeSpan timeout)
  {
    bool entered = await sem.WaitAsync(timeout).AnyContext();

    try
    {
      await f();
    }
    catch (Exception e)
    {
      Log.Error($"Exception while running {callerName}: {e}");
    }
    finally
    {
      if (entered)
      {
        sem.Release();
      }
    }
  }

}
