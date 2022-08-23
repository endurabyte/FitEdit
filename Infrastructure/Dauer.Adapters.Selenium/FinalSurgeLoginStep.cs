using Dauer.Model;
using Dauer.Model.Web;
using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public class FinalSurgeLoginStep : Step, IStep
{
  public string Username { get; set; }
  public string Password { get; set; }
  public bool Force { get; set; }

  public FinalSurgeLoginStep(IWebDriver driver) : base(driver) => Name = "Final Surge Login";

  public Task<bool> Run()
  {
    if (!Force && driver_.SignedInToFinalSurge())
    {
      Log.Info($"  Already logged in. Use --force to login again.");
      return Task.FromResult(true);
    }

    bool ok = LogInWithUserPass() || driver_.SignedInToFinalSurge();

    if (!ok)
    {
      Log.Error("Could not log in");
    }
    else
    {
      Log.Info("Signed in");
    }

    return Task.FromResult(ok);
  }

  private bool LogInWithUserPass()
  {
    Log.Info($"Logging in in to Final Surge with user/pass...");

    driver_.Url = "https://www.finalsurge.com/login/";

    if (!driver_.WaitForElement(By.CssSelector("input[name=\"email\"]"), out IWebElement emailInput))
    {
      Log.Error($"Could not find email input");
      return false;
    }

    if (!driver_.WaitForElement(By.CssSelector("input[name=\"password\"]"), out IWebElement passwordInput))
    {
      Log.Error($"Could not find password input");
      return false;
    }

    if (!driver_.WaitForElement(By.CssSelector(".check-option__box"), out IWebElement rememberCheckbox))
    {
      Log.Error($"Could not find password input");
      return false;
    }

    // Wait for "Sign In" button to appear
    if (!driver_.WaitForElement(By.CssSelector("button[type=\"submit\"]"), out IWebElement signInButton))
    {
      Log.Error($"Could not find sign in button");
      return false;
    }

    emailInput.SendKeys(Username);
    passwordInput.SendKeys(Password);
    rememberCheckbox.Click();
    signInButton.Click();

    return true;
  }
}
