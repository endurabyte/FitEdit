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
        using var fitSource = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);

        var decoder = new Decode();

        var fitFile = new FitFile();
        decoder.MesgEvent += (o, s) => fitFile.Messages.Add(MessageFactory.Create(s.mesg));
        decoder.MesgDefinitionEvent += (o, s) => fitFile.MessageDefinitions.Add(s.mesgDef);

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