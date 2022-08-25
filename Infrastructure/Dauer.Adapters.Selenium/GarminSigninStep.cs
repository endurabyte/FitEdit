using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.Web;
using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public class GarminSigninStep : Step, IStep
{
  public string Username { get; set; }
  public string Password { get; set; }
  public bool Force { get; set; }

  public GarminSigninStep(IWebDriver driver) : base(driver) => Name = "Garmin Login";

  public async Task<bool> Run()
  {
    if (!Force && await driver_.SignedInToGarmin().AnyContext())
    {
      Log.Info($"  Already signed in. Use --force to login again.");
      return true;
    }

    await SignInWithUserPass().AnyContext();

    bool ok = await driver_.SignedInToGarmin().AnyContext();

    if (!ok)
    {
      Log.Error("Could not log in");
    }
    else
    {
      Log.Info("Signed in");
    }

    return ok;
  }

  private Task SignInWithUserPass()
  {
    Log.Info($"Signing in to Garmin Connect with user/pass...");

    driver_.Url = "https://connect.garmin.com/signin/";

    // Signin elements are in an iframe to sso.garmin.com
    if (!driver_.WaitForElement(By.Id("gauth-widget-frame-gauth-widget"), out IWebElement authFrame))
    {
      Log.Error($"Could not find sign in button");
      return Task.CompletedTask;
    }

    driver_.SwitchTo().Frame(authFrame);

    // Wait for "Sign In" button to appear
    if (!driver_.WaitForElement(By.Id("login-btn-signin"), out IWebElement signInButton))
    {
      Log.Error($"Could not find sign in button");
      return Task.CompletedTask;
    }

    driver_.FindElement(By.Id("username")).SendKeys(Username);
    driver_.FindElement(By.Id("password")).SendKeys(Password);
    driver_.FindElement(By.Id("login-remember-checkbox")).Click();
    signInButton.Click();

    // Wait for "signed in" class to appear on root html element
    if (!driver_.WaitForElement(By.ClassName("signed-in"), out _))
    {
      Log.Error($"Could not sign in");
    }

    return Task.CompletedTask;
  }
}