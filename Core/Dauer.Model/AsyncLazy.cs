using System.Runtime.CompilerServices;

namespace Dauer.Model;

public class AsyncLazy<T>
{
  private readonly Lazy<Task<T>> instance_;
  public T Value => GetAwaiter().GetResult();

  public AsyncLazy(Func<T> factory)
  {
    instance_ = new Lazy<Task<T>>(() => Task.Run(factory));
  }

  public AsyncLazy(Func<Task<T>> factory)
  {
    instance_ = new Lazy<Task<T>>(() => Task.Run(factory));
  }

  public TaskAwaiter<T> GetAwaiter() => instance_.Value.GetAwaiter();
}