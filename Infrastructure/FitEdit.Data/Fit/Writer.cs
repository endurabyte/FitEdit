using FitEdit.Model;
using Dynastream.Fit;

namespace FitEdit.Data.Fit
{
  public class Writer
  {
    public void Write(FitFile fitFile, string destination)
    {
      if (fitFile == null)
      {
        return;
      }

      using var dest = new FileStream(destination, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
      Write(fitFile, dest);
      Log.Info($"Finished writing {destination}");
    }

    public void Write(FitFile fitFile, Stream dest)
    {
      var encoder = new Encode(ProtocolVersion.V20);
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

        try
        {
          write();
        }
        catch (Exception e)
        {
          Log.Error(e.Message);
        }
      }

      Log.Info($"Wrote {fitFile.Messages.Count} messages and {fitFile.MessageDefinitions.Count} definitions");
      encoder.Close();
    }
  }
}