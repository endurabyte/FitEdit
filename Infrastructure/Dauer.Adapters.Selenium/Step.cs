using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public abstract class Step
{
  public string Name { get; protected set; } = string.Empty;

  protected readonly IWebDriver driver_;

  public Step(IWebDriver driver)
  {
    driver_ = driver;
  }

  public void Close() => driver_.Dispose();
}
