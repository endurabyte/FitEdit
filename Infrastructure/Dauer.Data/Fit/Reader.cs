using Dauer.Model;
using Dynastream.Fit;

namespace Dauer.Data.Fit
{
  public class Reader
  {
    public async Task<FitFile> ReadAsync(string source)
    {
      Log.Info($"Opening {source}...");
      using var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
      return await ReadAsync(source, stream);
    }

    public async Task<FitFile> ReadAsync(string source, Stream stream)
    {
      if (!TryGetDecoder(source, stream, out FitFile fitFile, out Decode decoder))
      {
        return new FitFile();
      }

      try
      {
        if (!decoder.CheckIntegrity(stream))
        {
          Log.Warn($"Integrity Check failed...");
          if (decoder.InvalidDataSize)
          {
            Log.Warn("Invalid Size detected...");
          }

          Log.Warn("Attempting to read by skipping the header...");
          if (!await decoder.ReadAsync(stream, DecodeMode.InvalidHeader))
          {
            Log.Error($"Could not read {source} by skipping the header");
            return null;
          }
        }
        if (!await decoder.ReadAsync(stream))
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

    /// <summary>
    /// Read just one FIT message. Return false on error or end of stream.
    /// This gives single-threaded environments such as WASM a chance to update the UI,
    /// for example to show a progress bar or a message to the user.
    /// </summary>
    public async Task<bool> ReadOneAsync(Stream stream, Decode decoder, int messageCount = 1) => stream.Position switch
    {
      _ when stream.Position >= stream.Length => false,
      _ => await decoder.ReadAsync(stream, DecodeMode.Partial, messageCount)
    };

    public bool TryGetDecoder(string source, Stream stream, out FitFile fit, out Decode decoder)
    {
      decoder = new Decode();
      var tmp = new FitFile();

      decoder.MesgEvent += (o, s) =>
      {
        Log.Debug($"Found {nameof(Mesg)} \'{s.mesg.Name}\'. (Num, LocalNum) = ({s.mesg.Num}, {s.mesg.LocalNum}). Fields = {string.Join(", ", s.mesg.Fields.Select(field => $"({field.Num} \'{field.Name}\')"))}");
        var mesg = MessageFactory.Create(s.mesg); // Convert general Mesg to specific e.g. LapMesg

        tmp.Messages.Add(mesg);
        tmp.Events.Add(new MesgEventArgs(mesg));
      };

      decoder.MesgDefinitionEvent += (o, s) =>
      {
        Log.Debug($"Found {nameof(MesgDefinition)}. (GlobalMesgNum, LocalMesgNum) = ({s.mesgDef.GlobalMesgNum}, {s.mesgDef.LocalMesgNum}). Fields = {string.Join(", ", s.mesgDef.GetFields().Select(field => field.Num))}");
        tmp.MessageDefinitions.Add(s.mesgDef);
        tmp.Events.Add(s);
      };

      decoder.DeveloperFieldDescriptionEvent += (o, s) =>
      {
        Log.Debug($"Found {nameof(DeveloperFieldDescription)}. (ApplicationId, ApplicationVersion, FieldDefinitionNumber) = ({s.Description.ApplicationId}, {s.Description.ApplicationVersion}, {s.Description.FieldDefinitionNumber}");
        tmp.Events.Add(s);
      };

      bool ok = decoder.IsFIT(stream);
      ok &= decoder.CheckIntegrity(stream);

      if (!decoder.IsFIT(stream))
      {
        Log.Error($"Is not a FIT file: {source}");
        fit = null;
        return false;
      }

      fit = tmp;
      return true;
    }
  }
}