using Dauer.Services;
using Lamar;

namespace Dauer.Infrastructure;

public interface ICompositionRoot
{
  ServiceRegistry Registry { get; }
  T Get<T>();
}

public class CompositionRoot : ICompositionRoot
{
  public ServiceRegistry Registry { get; }
  private IContainer _container { get; }

  public CompositionRoot()
  {
    Registry = new ServiceRegistry();
    Registry.For<IFitService>().Use<FitService>();
  }

  public T Get<T>() => _container.GetInstance<T>();
}