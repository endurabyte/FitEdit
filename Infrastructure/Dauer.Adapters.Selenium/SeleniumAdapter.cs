using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.Web;
using Dauer.Services;
using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public class SeleniumAdapter : IBrowserAdapter
{
  private readonly IWebDriver driver_;
  private readonly IJavaScriptExecutor js_;

  private string UserAgent => $"{js_.ExecuteScript("return navigator.userAgent")}";

  public SeleniumAdapter(IWebDriver driver)
  {
    driver_ = driver;
    js_ = (IJavaScriptExecutor)driver;
    Log.Info($"User Agent: {UserAgent}");
  }

  public async Task<bool> Run(IStep step) => await step.Run().AnyContext();

  public async Task Close()
  {
    Log.Info("Closing and quitting driver");

    try
    {
      driver_.Close();
      driver_.Quit();
    }
    catch (Exception e)
    {
      Log.Error(e);
    }

    await Task.CompletedTask;
  }
}