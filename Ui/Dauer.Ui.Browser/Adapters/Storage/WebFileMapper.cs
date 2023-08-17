using System.Runtime.InteropServices.JavaScript;
using Dauer.Model;

namespace Dauer.Ui.Browser.Adapters.Storage;

public static class WebFileMapper
{
  public static FileReference? Map(this JSObject? obj)
  {
    if (!OperatingSystem.IsBrowser())
    {
      return null;
    }

    string fileName = obj?.GetPropertyAsString("name") ?? string.Empty;
    byte[] bytes = obj?.GetPropertyAsByteArray("bytes") ?? Array.Empty<byte>();

    return new FileReference(fileName, bytes);
  }
}
