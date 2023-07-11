using System.IO.Compression;

namespace Dauer.Model;

public static class Zip
{
  public static List<BlobFile> Unzip(BlobFile file)
  {
    using var ms = new MemoryStream(file.Bytes);
    using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
    return archive.GetFiles();
  }

  private static List<BlobFile> GetFiles(this ZipArchive archive)
  {
    var files = new List<BlobFile>();

    try
    {
      foreach (ZipArchiveEntry entry in archive.Entries)
      {
        files.Add(new BlobFile
        {
          Name = entry.Name,
          Bytes = entry.ExtractFile(),
        });
      }
    }
    catch (Exception e)
    {
      Log.Error(e);
    }

    return files;
  }

  private static byte[] ExtractFile(this ZipArchiveEntry entry)
  {
    using var stream = entry.Open();
    using var memoryStream = new MemoryStream();
    stream.CopyTo(memoryStream);
    return memoryStream.ToArray();
  }
}
