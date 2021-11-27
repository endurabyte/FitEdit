using Dynastream.Fit;

namespace Dauer.Data.Fit
{
  public class Writer
  {
    public void Write(FitFile fitFile, string destination)
    {
      var encoder = new Encode(ProtocolVersion.V20);
      using var dest = new FileStream(destination, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

      encoder.Open(dest);

      foreach (var definition in fitFile.MessageDefinitions)
      {
        encoder.Write(definition);
      }

      foreach (var message in fitFile.Messages)
      {
        encoder.Write(message);
      }

      encoder.Close();
    }
  }
}