using Dauer.Model;
using Dauer.Model.Web;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;

namespace Dauer.Adapters.Selenium;

public class GarminUploadStep : Step, IStep
{
  public string File { get; set; }

  public GarminUploadStep(IWebDriver driver) : base(driver) => Name = "Garmin Upload";

  public async Task<bool> Run()
  {
    if (!await SignedIn().AnyContext())
    {
      Log.Error($"Not signed in. Try dauer login-garmin -u <username> -p <password>");
      return false;
    }

    Log.Info($"Uploading {File}...");

    string url = "https://connect.garmin.com/modern/import-data";
    driver_.Url = url;

    // Wait for import-data form to appear.
    if (!WaitForElement(By.Id("import-data"), out IWebElement droparea))
    {
      Log.Error($"Could not find import-data element");
      return false;
    }

    string absolutePath = Path.GetFullPath(File);
    droparea.DropFile(absolutePath!, 0, 0);

    driver_.FindElement(By.Id("import-data-start")).Click();
    string progress = "";

    // This callback is called regularly, by default every 500ms.
    // Use the upload progress bar width percent as upload progress
    var callback = (IWebElement elem) =>
    {
      IWebElement dzUpload = driver_.FindElement(By.ClassName("dz-upload"));
      string style = dzUpload.GetAttribute("style");

      var regex = new Regex(@"(\d+.\d+)");
      var match = regex.Match(style);

      if (match.Success)
      { 
        if (progress != match.Captures[0].Value)
        {
          progress = match.Captures[0].Value;
          Log.Info($"Progress: {progress}%");
        }
      }

      // True to keep waiting
      return true;
    };

    if (!WaitForElement(By.ClassName("status-link"), out IWebElement statusSpan, TimeSpan.FromSeconds(30), callback: callback))
    {
      Log.Error($"Upload timed out");
      return false;
    }

    if (!TryFindElement(By.ClassName("detail-link"), out IWebElement viewDetailsLink))
    {
      Log.Error($"Could not find View Details link");
      return false;
    }

    if (TryFindElement(By.ClassName("dz-error-message"), out IWebElement elem))
    {
      Log.Error($"Error: {elem.Text}");
      return false;
    }

    viewDetailsLink.Click();

    bool ok = driver_.Url.Contains("https://connect.garmin.com/modern/activity/");

    if (ok)
    {
      string id = driver_.Url.Split('/').Last();
      Log.Info($"Uploaded activity {id} ({driver_.Url})");
    }

    return await Task.FromResult(ok);
  }
}
