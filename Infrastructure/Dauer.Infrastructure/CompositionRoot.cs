using Dauer.Adapters.Selenium;
using Dauer.Model.Services;
using Dauer.Services;
using Lamar;
using OpenQA.Selenium;

namespace Dauer.Infrastructure;

public interface ICompositionRoot
{
  ServiceRegistry Registry { get; }
  T Get<T>();
}

public class CompositionRoot : ICompositionRoot
{
  public ServiceRegistry Registry { get; }
  private IContainer Container_ { get; }

  public CompositionRoot()
  {
    Registry = new ServiceRegistry();

    Registry.For<IWebDriver>().Use(new ChromeDriverFactory().Create());
    Registry.For<IFitService>().Use<FitService>();
    Registry.For<IBrowserAdapter>().Use<SeleniumAdapter>();
    Registry.For<IBrowserService>().Use<BrowserService>();
    Registry.For<FinalSurgeCalendar>().Use<FinalSurgeCalendar>();
    Registry.For<FinalSurgeCalendarSearch>().Use<FinalSurgeCalendarSearch>();
  }

  public T Get<T>() => Container_.GetInstance<T>();
}
