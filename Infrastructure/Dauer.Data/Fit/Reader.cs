using Dauer.Model;
using Dynastream.Fit;

namespace Dauer.Data.Fit
{
  public class Reader
  {
    public FitFile Read(string source)
    {
      try
      {
        Log.Info($"Opening {source}");

        using var fitSource = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);

        var decoder = new Decode();

        var fitFile = new FitFile();
        decoder.MesgEvent += (o, s) => fitFile.Messages.Add(MessageFactory.Create(s.mesg));
        decoder.MesgDefinitionEvent += (o, s) => fitFile.MessageDefinitions.Add(s.mesgDef);

        bool ok = decoder.IsFIT(fitSource);
        ok &= decoder.CheckIntegrity(fitSource);

        if (!decoder.IsFIT(fitSource))
        {
          Log.Error($"Is not a FIT file: {source}");
          return null;
        }

        if (!decoder.CheckIntegrity(fitSource))
        {
          Log.Warn($"Integrity Check failed...");
          if (decoder.InvalidDataSize)
          {
            Log.Warn("Invalid Size detected...");
          }

          Log.Warn("Attempting to read by skipping the header...");
          if (!decoder.Read(fitSource, DecodeMode.InvalidHeader))
          {
            Log.Error($"Could not read {source} by skipping the header");
            return null;
          }
        }

        if (!decoder.Read(fitSource))
        {
          Log.Error($"Could not read {source}");
          return null;
        }

        return fitFile;

      }
      catch (Exception ex)
      {
        Log.Error(ex.Message);
        return null;
      }
    }
  }
}