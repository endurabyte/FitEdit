using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Dauer.Adapters.Selenium;

public static class WebDriverExtensions
{
  public static T RunJs<T>(this IWebDriver driver, string script) => (T)((IJavaScriptExecutor)driver).ExecuteScript(script);

  public static bool TryFindElement(this IWebDriver driver, By by, out IWebElement element)
  {
    try
    {
      element = driver.FindElement(by);
      return element != null /*&& element.Displayed*/;
    }
    catch (NoSuchElementException)
    {
      element = null;
      return false;
    }
  }

  public static bool WaitForClickable(this IWebDriver driver, By by, out IWebElement element, TimeSpan ts = default)
  {
    if (ts == default)
    {
      ts = TimeSpan.FromSeconds(2.00);
    }

    try
    {
      element = new WebDriverWait(driver, ts)
      .Until(ExpectedConditions.ElementToBeClickable(by));
      return true;
    }
    catch (WebDriverTimeoutException)
    {
      element = null;
      return false;
    }
  }

  public static bool WaitForElement(this IWebDriver driver, By by, out IWebElement element, TimeSpan ts = default, Func<IWebElement, bool> callback = null)
  {
    if (ts == default)
    {
      ts = TimeSpan.FromSeconds(2.00);
    }

    try
    {
      IWebElement elem = null;

      bool found = new WebDriverWait(driver, ts)
        .Until(driver =>
        {
          return (callback?.Invoke(elem) ?? true)
            && driver.TryFindElement(by, out elem);
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
