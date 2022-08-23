using Dauer.Model.Web;
using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public class GarminDeleteStep : Step, IStep
{
  public string ActivityId { get; set; }

  public GarminDeleteStep(IWebDriver driver) : base(driver)
  {
  }

  public Task<bool> Run()
  {
    driver_.Url = $"https://connect.garmin.com/modern/activity/{ActivityId}";

    if (!driver_.WaitForElement(By.CssSelector(".icon-gear"), out IWebElement gear))
    {
      return Task.FromResult(false);
    }

    gear.Click();

    if (!driver_.WaitForElement(By.Id("btn-delete"), out IWebElement deleteButton))
    {
      return Task.FromResult(false);
    }

    deleteButton.Click();

    if (!driver_.WaitForElement(By.PartialLinkText("Delete"), out IWebElement confirmDeleteButton))
    {
      return Task.FromResult(false);
    }

    confirmDeleteButton.Click();

    return Task.FromResult(true);
  }
}
