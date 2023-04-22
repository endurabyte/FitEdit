using System.Runtime.InteropServices.JavaScript;

namespace Dauer.Ui.Services;

public class Storage
{
  public static async Task<Models.File?> OpenFileAsync()
  {
    if (!OperatingSystem.IsBrowser())
    {
      return null;
    }

    JSObject obj = await WebStorage.OpenFileAsync();
    string fileName = obj.GetPropertyAsString("name") ?? string.Empty;
    byte[] bytes = obj.GetPropertyAsByteArray("bytes") ?? Array.Empty<byte>();

    return new Models.File(fileName, bytes);
  }

  public static async Task SaveAsync(Models.File file)
  {
    await Task.Run(() =>
    {
      if (!OperatingSystem.IsBrowser())
      {
        return;
      }
      WebStorage.DownloadByteArray(file.Name, file.Bytes);
    });
  }
}
