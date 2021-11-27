using Dauer.Model;
using Dynastream.Fit;
using System;
using System.IO;

namespace Dauer.Data.Fit
{
  public class Reader
  {
    public FitFile Read(string source)
    {
      try
      {
        // Attempt to open .FIT file
        using var fitSource = new FileStream(source, FileMode.Open);

        var decoder = new Decode();
        var mesgBroadcaster = new MesgBroadcaster();

        // Connect the Broadcaster to our event (message) source (in this case the Decoder)
        decoder.MesgEvent += mesgBroadcaster.OnMesg;
        decoder.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;

        var fitFile = new FitFile();
        mesgBroadcaster.MesgEvent += (o, s) => fitFile.Messages.Add(s.mesg);
        mesgBroadcaster.MesgDefinitionEvent += (o, s) => fitFile.MessageDefinitions.Add(s.mesgDef);

        bool ok = decoder.IsFIT(fitSource);
        ok &= decoder.CheckIntegrity(fitSource);

        // Process the file
        if (ok)
        {
          decoder.Read(fitSource);
        }
        else
        {
          Log.Error($"Integrity Check Failed {source}");
          if (decoder.InvalidDataSize)
          {
            Log.Error("Invalid Size Detected, Attempting to decode...");
            decoder.Read(fitSource);
          }
          else
          {
            Log.Error("Attempting to decode by skipping the header...");
            decoder.Read(fitSource, DecodeMode.InvalidHeader);
          }
        }

        return fitFile;
      }
      catch (FitException ex)
      {
        Log.Error(ex.Message);
      }
      catch (Exception ex)
      {
        Log.Error(ex.Message);
      }

      return null;
    }
  }
}