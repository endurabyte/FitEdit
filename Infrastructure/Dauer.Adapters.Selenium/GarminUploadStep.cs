using Dauer.Model;
using Dauer.Model.Web;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using System.Text.RegularExpressions;

namespace Dauer.Adapters.Selenium;

public class GarminUploadStep : Step, IStep
{
  public string File { get; set; }

  private string progress_ = "";

  public GarminUploadStep(IWebDriver driver) : base(driver) => Name = "Garmin Upload";

  public async Task<bool> Run()
  {
    Log.Info($"Uploading {File}...");

    driver_.Url = "https://connect.garmin.com/modern/import-data";

    // Wait for import-data form to appear.
    if (!driver_.WaitForElement(By.Id("import-data"), out IWebElement droparea))
    {
      Log.Error($"Could not access upload page");

      await driver_.SignedInToGarmin(advise: true).AnyContext();
      return false;
    }

    string absolutePath = Path.GetFullPath(File);
    droparea.DropFile(absolutePath, 0, 0);

    driver_.FindElement(By.Id("import-data-start")).Click();

    // Wait for upload to finish
    progress_ = "";
    if (!driver_.WaitForElement(By.ClassName("status-link"), out IWebElement statusSpan, TimeSpan.FromSeconds(30), callback: WatchProgress))
    {
      Log.Error($"Upload timed out");
      return false;
    }

    // Show success status message 
    if (driver_.TryFindElement(By.CssSelector(".dz-success-mark > .status-message"), out IWebElement successMsg))
    {
      if (!string.IsNullOrWhiteSpace(successMsg.Text))
      {
        Log.Info($"Success: {successMsg.Text}");
      }
    }

    // Show errors
    if (driver_.TryFindElement(By.ClassName("dz-error-message"), out IWebElement errMsg))
    {
      if (!string.IsNullOrWhiteSpace(errMsg.Text))
      {
        Log.Error($"Error: {errMsg.Text}");
        return false;
      }
    }

    // Navigate to activity
    if (!driver_.TryFindElement(By.ClassName("detail-link"), out IWebElement details))
    {
      Log.Error($"Could not find uploaded activity");
      return false;
    }

    details.Click();

    bool ok = driver_.Url.Contains("https://connect.garmin.com/modern/activity/");

    if (ok)
    {
      string id = driver_.Url.Split('/').Last();
      Log.Info($"Uploaded activity {id} ({driver_.Url})");
    }

    return await Task.FromResult(ok);
  }

  /// <summary>
  /// This callback is called regularly, by default every 500ms.
  /// Use the upload progress bar width percent as upload progress
  /// </summary>
  private bool WatchProgress(IWebElement elem)
  {
    IWebElement dzUpload = driver_.FindElement(By.ClassName("dz-upload"));
    string style = dzUpload.GetAttribute("style");

    var regex = new Regex(@"(\d+.\d+)");
    Match match = regex.Match(style);

    if (match.Success)
    {
      if (progress_ != match.Captures[0].Value)
      {
        progress_ = match.Captures[0].Value;
        Log.Info($"Progress: {progress_}%");
      }
    }

    // True to keep waiting
    return true;
  }
}
