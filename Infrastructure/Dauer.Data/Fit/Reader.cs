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
        Log.Info($"Opening {source}...");

        using var fitSource = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);

        var decoder = new Decode();

        var fitFile = new FitFile();

        decoder.MesgEvent += (o, s) =>
        {
          Log.Debug($"Found {nameof(Mesg)} \'{s.mesg.Name}\'. (Num, LocalNum) = ({s.mesg.Num}, {s.mesg.LocalNum}). Fields = {string.Join(", ", s.mesg.Fields.Select(field => $"({field.Num} \'{field.Name}\')"))}");
          var mesg = MessageFactory.Create(s.mesg); // Convert general Mesg to specific e.g. LapMesg

          fitFile.Messages.Add(mesg);
          fitFile.Events.Add(new MesgEventArgs(mesg));
        };

        decoder.MesgDefinitionEvent += (o, s) =>
        {
          Log.Debug($"Found {nameof(MesgDefinition)}. (GlobalMesgNum, LocalMesgNum) = ({s.mesgDef.GlobalMesgNum}, {s.mesgDef.LocalMesgNum}). Fields = {string.Join(", ", s.mesgDef.GetFields().Select(field => field.Num))}");
          fitFile.MessageDefinitions.Add(s.mesgDef);
          fitFile.Events.Add(s);
        };

        decoder.DeveloperFieldDescriptionEvent += (o, s) =>
        {
          Log.Debug($"Found {nameof(DeveloperFieldDescription)}. (ApplicationId, ApplicationVersion, FieldDefinitionNumber) = ({s.Description.ApplicationId}, {s.Description.ApplicationVersion}, {s.Description.FieldDefinitionNumber}");
          fitFile.Events.Add(s);
        };

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

        Log.Info($"Found {fitFile.Messages.Count} messages");
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