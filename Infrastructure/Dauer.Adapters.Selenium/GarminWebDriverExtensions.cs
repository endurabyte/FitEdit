using Dauer.Model;
using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public static class GarminWebDriverExtensions
{
  public static Task<bool> SignedInToGarmin(this IWebDriver driver, bool advise = false)
  {
    Log.Info($"Checking if we are signed in...");

    // Check if already logged in. If we aren't, we get redirected away from /modern.
    driver.Url = "https://connect.garmin.com/modern";

    // Wait for "signed in" class to appear on root html element
    bool signedIn = driver.TryFindElement(By.ClassName("signed-in"), out _);

    if (!signedIn && advise)
    {
      Log.Warn($"Not signed in. Try 'dauer login-garmin -u <username> -p <password>'");
    }

    return Task.FromResult(signedIn);
  }
}
