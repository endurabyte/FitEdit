using System.Runtime.InteropServices.JavaScript;

namespace Dauer.Ui.Infra.Adapters.Storage;

public static class WebFileMapper
{
  public static Model.File? Map(this JSObject? obj)
  {
    if (!OperatingSystem.IsBrowser())
    {
      return null;
    }

    string fileName = obj?.GetPropertyAsString("name") ?? string.Empty;
    byte[] bytes = obj?.GetPropertyAsByteArray("bytes") ?? Array.Empty<byte>();

    return new Model.File(fileName, bytes);
  }
}
