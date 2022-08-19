using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public static class WebElementExtensions
{
  /// <summary>
  /// This script injects an HTML input web element to receive a file.
  /// It simulates the dragenter, dragover, and drop events on the targeted element within the file set in the datatransfer object.
  /// https://sqa.stackexchange.com/a/22199
  /// </summary>
  private const string JS_DROP_FILE = "var target = arguments[0]," +
            "    offsetX = arguments[1]," +
            "    offsetY = arguments[2]," +
            "    document = target.ownerDocument || document," +
            "    window = document.defaultView || window;" +
            "" +
            "var input = document.createElement('INPUT');" +
            "input.type = 'file';" +
            "input.style.display = 'none';" +
            "input.onchange = function () {" +
            "  var rect = target.getBoundingClientRect()," +
            "      x = rect.left + (offsetX || (rect.width >> 1))," +
            "      y = rect.top + (offsetY || (rect.height >> 1))," +
            "      dataTransfer = { files: this.files };" +
            "" +
            "  ['dragenter', 'dragover', 'drop'].forEach(function (name) {" +
            "    var evt = document.createEvent('MouseEvent');" +
            "    evt.initMouseEvent(name, !0, !0, window, 0, 0, 0, x, y, !1, !1, !1, !1, 0, null);" +
            "    evt.dataTransfer = dataTransfer;" +
            "    target.dispatchEvent(evt);" +
            "  });" +
            "" +
            "  setTimeout(function () { document.body.removeChild(input); }, 25);" +
            "};" +
            "document.body.appendChild(input);" +
            "return input;";

  public static void DropFile(this IWebElement target, string path, int offsetX, int offsetY)
  {
    if (!File.Exists(path))
    {
      throw new WebDriverException("File not found: " + path);
    }

    IWebDriver driver = (target as WebElement)!.WrappedDriver;
    var js = (IJavaScriptExecutor)driver;

    IWebElement input = (WebElement)js.ExecuteScript(JS_DROP_FILE, target, offsetX, offsetY);
    input.SendKeys(path);
  }
}