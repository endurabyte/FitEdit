namespace Dauer.Ui.Infra;

public interface IContainer
{
  T? Get<T>();
  IContainer Register<TService>(object impl);
}

public class Container : IContainer
{
  private readonly Dictionary<Type, object> registrations_ = new();
  public IContainer Register<TService>(object impl) 
  {
    registrations_[typeof(TService)] = impl!;
    return this;
  }

  public T? Get<T>() => registrations_.TryGetValue(typeof(T), out object? t) ? (T)t : default;
}