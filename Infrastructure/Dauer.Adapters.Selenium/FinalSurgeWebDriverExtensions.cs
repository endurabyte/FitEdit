using Dauer.Model;
using Dauer.Model.Extensions;
using OpenQA.Selenium;
using System.Text.RegularExpressions;

namespace Dauer.Adapters.Selenium;

public static class FinalSurgeWebDriverExtensions
{
  public static async Task<bool> SignedInToFinalSurge(this IWebDriver driver, bool advise = false)
  {
    // If signed in, we get redirected to /workoutcalendar.
    // If not signed in, we get redirected to /login

    driver.Url = "https://www.finalsurge.com/login/";
    Thread.Sleep(4000);

    bool signedIn = await driver.TryWaitForUrl(new Regex("https://beta.finalsurge.com/workoutcalendar")).AnyContext();

    if (!signedIn && advise)
    {
      Log.Warn($"Not signed in. Try 'dauer login-finalsurge -u <username> -p <password>'");
    }

    return signedIn;

  }
}
