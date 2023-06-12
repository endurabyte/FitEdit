using System.Runtime.InteropServices.JavaScript;

namespace Dauer.Ui.Adapters.Storage;

public static class WebFileMapper
{
  public static Models.File? Map(this JSObject? obj)
  {
    if (!OperatingSystem.IsBrowser())
    {
      return null;
    }

    string fileName = obj?.GetPropertyAsString("name") ?? string.Empty;
    byte[] bytes = obj?.GetPropertyAsByteArray("bytes") ?? Array.Empty<byte>();

    return new Models.File(fileName, bytes);
  }
}
