using Dauer.Model;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

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

  protected Task<bool> SignedIn()
  {
    Log.Info($"Checking if we are signed in...");

    // Check if already logged in. If we aren't, we get redirected away from /modern.
    string url = "https://connect.garmin.com/modern/activities";
    driver_.Url = url;

    // Wait for "signed in" class to appear on root html element
    bool signedIn = TryFindElement(By.ClassName("signed-in"), out _);

    if (signedIn)
    {
      Log.Info($"  Already signed in");
    }
    else
    {
      Log.Info($"  Not signed in");
    }

    return Task.FromResult(signedIn);
  }

  protected T RunJs<T>(string script) => (T)((IJavaScriptExecutor)driver_).ExecuteScript(script);

  protected bool TryFindElement(By by, out IWebElement element)
  {
    try
    {
      element = driver_.FindElement(by);
      return element != null && element.Displayed;
    }
    catch (NoSuchElementException)
    {
      element = null;
      return false;
    }
  }

  protected bool WaitForElement(By by, out IWebElement element, TimeSpan ts = default, Func<IWebElement, bool> callback = null)
  {
    if (ts == default)
    {
      ts = TimeSpan.FromSeconds(10.00);
    }

    try
    {
      IWebElement elem = null;

      bool found = new WebDriverWait(driver_, ts)
        .Until(driver =>
        {
          return (callback?.Invoke(elem) ?? true) 
            && TryFindElement(by, out elem);
        });

      element = elem;
      return found;
    }
    catch (WebDriverTimeoutException)
    {
      element = null;
      return false;
    }
  }
}
