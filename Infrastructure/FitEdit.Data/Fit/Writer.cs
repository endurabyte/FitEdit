using FitEdit.Model;
using Dynastream.Fit;

namespace FitEdit.Data.Fit;

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
  }

  public void Write(IEnumerable<FitFile> files, Stream dest)
  {
    foreach (var fitFile in files)
    {
      var tmpStream = new MemoryStream();
      Write(fitFile, tmpStream);
      tmpStream.Position = 0;
      tmpStream.CopyTo(dest);
    }
  }

  public void Write(FitFile fitFile, Stream dest)
  {
    var encoder = new Encode(dest, ProtocolVersion.V20);

    try
    {
      // Preserve the original message order
      foreach (var message in fitFile.Events)
      {
        switch (message)
        {
          case MesgEventArgs mesgArgs:
            mesgArgs.DebugLog();
            encoder.Write(mesgArgs.mesg);
            break;
          case MesgDefinitionEventArgs mesgDefArgs:
            mesgDefArgs.DebugLog();
            encoder.Write(mesgDefArgs.mesgDef);
            break;
        }
      }
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
    }
    Log.Info($"Wrote {fitFile.Messages.Count} messages and {fitFile.MessageDefinitions.Count} definitions");
    encoder.Close();
  }
}