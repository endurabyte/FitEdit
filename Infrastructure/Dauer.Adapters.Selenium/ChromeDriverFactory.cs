using FundLog.Model.Extensions;
using OpenQA.Selenium.Chrome;

namespace Dauer.Adapters.Selenium;

public class ChromeDriverFactory
{
  public ChromeDriver Create(bool randomize = false)
  {
    if (randomize)
    {
      ChromeDriverProcess.Randomize().Await();
    }

    ChromeDriverService cds = ChromeDriverService.CreateDefaultService();
    cds.HideCommandPromptWindow = true;

    var options = new ChromeOptions
    {
      //Proxy = new Proxy
      //{
      //  Kind = ProxyKind.Manual,
      //  IsAutoDetect = false,
      //  HttpProxy = "http://172.67.176.103:80",
      //  SslProxy = "http://204.150.232.240:7664"
      //}
    };

    options.AddArguments(
      // On a headless Raspberry Pi, 
      // adding either --window-size or --start-maximized prevents "element not interactable"
      // but adding both cause "OpenQA.Selenium.WebDriverException: The HTTP request to the remote WebDriver server for URL http://localhost:42239/session timed out after 60 seconds."
      // https://stackoverflow.com/questions/22322596
      "--window-size=1200,800",
      //"--start-maximized",

      // Note: Some bot detectors (e.g. Discover Card) notice --headless and block us
      // https://stackoverflow.com/questions/55364643
      "--headless",

      // Needed for headless on Linux without root
      //"--no-sandbox",

      @$"--user-data-dir=C:\Users\doug\AppData\Local\dauer\Chrome\User Data"
    //$"--user-data-dir=C:/Users/doug/AppData/Local/Google/Chrome/User Data"

    // Fake user agent to prevent fingerprinting
    //"--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36"
    //"--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:103.0) Gecko/20100101 Firefox/103.0"
    );

    var driver = new ChromeDriver(cds, options);

    // Conceal that we are using browser automation.
    // See https://intoli.com/blog/not-possible-to-block-chrome-headless/
    //     https://intoli.com/blog/making-chrome-headless-undetectable/
    // Test with https://intoli.com/blog/not-possible-to-block-chrome-headless/chrome-headless-test.html
    //           https://bot.sannysoft.com
    if (driver is ChromeDriver chrome)
    {
      // navigator.webdriver. Its presence is used for bot detection
      chrome.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument",
        new Dictionary<string, object>
        {
          ["source"] = @"const newProto = navigator.__proto__; delete newProto.webdriver; navigator.__proto__ = newProto;",
        });
    }

    return driver;
  }
}