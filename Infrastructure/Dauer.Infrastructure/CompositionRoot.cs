using Dauer.Services;

namespace Dauer.Infrastructure
{
  public interface ICompositionRoot
  {
    T Get<T>();
    void Register<T>(object service);
  }

  public class CompositionRoot : ICompositionRoot
  {
    private readonly Dictionary<Type, object> registrations_ = new();

    public CompositionRoot()
    {
      Register<IFitService>(new FitService());
    }

    public T Get<T>() => registrations_.TryGetValue(typeof(T), out object service) ? (T)service : default;

    public void Register<T>(object service) => registrations_[typeof(T)] = service;
  }
}
