using Avalonia;

namespace Dauer.Ui;

public interface IContainer
{
  T? Get<T>();
  IContainer Register<TService, TImpl>(TImpl impl) where TImpl : TService;
}

public class Container : IContainer
{
  private readonly Dictionary<Type, object> registrations_ = new();
  public IContainer Register<TService, TImpl>(TImpl impl) where TImpl : TService
  {
    registrations_[typeof(TService)] = impl!;
    return this;
  }

  public T? Get<T>() => registrations_.TryGetValue(typeof(T), out object? t) ? (T)t : default;
}

public class AvaloniaContainer : IContainer
{
  public T? Get<T>() => AvaloniaLocator.Current.GetService<T>();

  public IContainer Register<TService, TImpl>(TImpl impl) where TImpl : TService
  {
    AvaloniaLocator.CurrentMutable.Bind<TService>().ToFunc(() => impl);
    return this;
  }
}
