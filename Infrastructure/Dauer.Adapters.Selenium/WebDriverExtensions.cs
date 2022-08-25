using Dauer.Model;
using Dauer.Model.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Text.RegularExpressions;

namespace Dauer.Adapters.Selenium;

public static class WebDriverExtensions
{
  public static T RunJs<T>(this IWebDriver driver, string script) => (T)((IJavaScriptExecutor)driver).ExecuteScript(script);

  public static bool TryWaitForUrl
  (
    this IWebDriver driver,
    Regex regex,
    RetryConfig config = default
  ) => Resilently.RetryAsync(() =>
  {
    return Task.FromResult(regex.IsMatch(driver.Url));
  }, config.WithDescription($"{nameof(TryWaitForUrl)}(\"{regex}\")")).Await();

  public static bool TryClick
  (
    this ISearchContext ctx,
    By by,
    RetryConfig config = default
  ) => ctx.RetryAsync(by, elem =>
  {
    elem.Click();
    return Task.CompletedTask;
  }, config.WithDescription($"{nameof(TryClick)}({by})")).Await();

  public static bool TryClick
  (
    this IWebElement elem,
    RetryConfig config = default
  ) => elem.RetryAsync(elem =>
  {
    elem.Click();
    return Task.FromResult(true);
  }, config.WithDescription($"{nameof(TryClick)}({elem.TagName})")).Await();

  public static bool TrySetText
  (
    this ISearchContext ctx,
    By by,
    string text,
    RetryConfig config = default
  ) => ctx.RetryAsync(by, elem =>
  {
    elem.Clear();
    elem.SendKeys(text);
    return Task.CompletedTask;
  }, config.WithDescription($"{nameof(TrySetText)}(\"{text}\")")).Await();

  public static bool TryGetText
  (
    this ISearchContext ctx,
    By by,
    out string text,
    RetryConfig config = default
  )
  {
    string tmp = null;

    bool ok = ctx.RetryAsync(by, elem =>
    {
      tmp = elem.Text;
      return Task.CompletedTask;
    }, config).Await();
    
    text = ok ? tmp : null;
    return ok;
  }

  public static async Task<bool> RetryAsync
  (
    this ISearchContext ctx,
    By by,
    Func<IWebElement, Task> action,
    RetryConfig config = default
  ) => await Resilently.RetryAsync(async () =>
  {
    if (ctx.TryFindElement(by, out IWebElement elem))
    {
      await action(elem).AnyContext();
      return true;
    }

    return false;

  }, config).AnyContext();

  public static async Task<bool> RetryAsync
  (
    this IWebElement elem,
    Func<IWebElement, Task<bool>> action,
    RetryConfig config = default
  ) => await Resilently.RetryAsync(async () => await action(elem).AnyContext(), config).AnyContext();

  public static bool TryFindElement(this ISearchContext ctx, By by, out IWebElement element)
  {
    try
    {
      element = ctx.FindElement(by);
      return element != null;
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
      ts = TimeSpan.FromSeconds(5);
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
      ts = TimeSpan.FromSeconds(5);
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
