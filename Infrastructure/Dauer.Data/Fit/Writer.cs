using Dauer.Model;
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

      // Preserve the original message order
      foreach (var message in fitFile.Events)
      {
        Action write = message switch
        {
          _ when message is MesgEventArgs args => () => encoder.Write(args.mesg),
          _ when message is MesgDefinitionEventArgs args => () => encoder.Write(args.mesgDef),
          _ => () => { },
        };

        write();
      }

      Log.Info($"Wrote {fitFile.Messages.Count} messages and {fitFile.MessageDefinitions.Count} definitions to {destination}");
      encoder.Close();
    }
  }
}