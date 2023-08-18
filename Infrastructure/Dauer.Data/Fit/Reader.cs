using System.Diagnostics;
using Dauer.Adapters.Fit;
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
        fitFile.ForwardfillEvents();
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
    public async Task<bool> ReadOneAsync(Stream stream, Decode decoder, int messageCount = 1)
    {
      try
      {
        return stream.Position switch
        {
          _ when stream.Position >= stream.Length => false,
          _ => await decoder.ReadAsync(stream, DecodeMode.Partial, messageCount)
        };
      }
      catch (FitException e)
      {
        Log.Error($"{e}");
        return true; // Keep reading despite exception
      }
    }

    public bool TryGetDecoder(string source, Stream stream, out FitFile fit, out Decode decoder)
    {
      decoder = new Decode();
      var tmp = new FitFile();

      decoder.MesgEvent += (o, s) =>
      {
        if (Debugger.IsAttached)
        {
          Log.Debug($"Found {nameof(Mesg)} \'{s.mesg.Name}\'. (Num, LocalNum) = ({s.mesg.Num}, {s.mesg.LocalNum}).");
          Log.Debug($"  Fields:");
          Log.Debug($"    FieldNum\t Name\t Data:");
          Log.Debug($"    {string.Join("\n    ", s.mesg.Fields.Values
                            .Select(field => $"{field.Num}\t " +
                                             $"\'{field.Name}\'\t " +
                                             $"{string.Join(" ", field.SourceData?.Select(b => $"{b:X2}") ?? new List<string>())}"))}"
          );

          Log.Debug(s.PrintBytes());
        }

        var mesg = MessageFactory.Create(s.mesg);

        if (!tmp.MessagesByDefinition.ContainsKey(mesg.Num))
        {
          tmp.MessagesByDefinition[mesg.Num] = new List<Mesg>() { mesg };
        }
        else
        {
          tmp.MessagesByDefinition[mesg.Num].Add(mesg);
        }

        tmp.Events.Add(new MesgEventArgs { mesg = mesg });
      };

      decoder.MesgDefinitionEvent += (o, s) =>
      {
        if (Debugger.IsAttached)
        {
          Log.Debug($"Found {nameof(MesgDefinition)}. (GlobalMesgNum, LocalMesgNum) = ({s.mesgDef.GlobalMesgNum}, {s.mesgDef.LocalMesgNum}).");

          int fieldIndex = 0;
          Log.Debug($"  Field Definitions");
          Log.Debug($"    FieldIndex\t Num\t Size\t Type\t TypeName\t FieldName\t (Hex Values)");
          Log.Debug($"    {string.Join("\n    ", s.mesgDef.GetFields()
              .Select(fieldDef => $"{fieldIndex++}\t " +
                                  $"{fieldDef.Num}\t " +
                                  $"{fieldDef.Size}\t " +
                                  $"{fieldDef.Type}\t " +
                                  $"{(FitTypes.TypeMap.TryGetValue(fieldDef.Type, out var type) ? type.typeName : "Unknown Type")}\t " +
                                  $"\'{Profile.GetField(s.mesgDef.GlobalMesgNum, fieldDef.Num)?.Name ?? "Unknown Field"}\'\t " +
                                  $"({fieldDef.Num:X2} {fieldDef.Size:X2} {fieldDef.Type:X2})"))}");

          Log.Debug(s.PrintBytes());
        }
        tmp.MessageDefinitions[s.mesgDef.GlobalMesgNum] = s.mesgDef;
        tmp.Events.Add(s);
      };

      decoder.DeveloperFieldDescriptionEvent += (o, s) =>
      {
        Log.Debug($"Found {nameof(DeveloperFieldDescription)}. (ApplicationId, ApplicationVersion, FieldDefinitionNumber) = ({s.Description.ApplicationId}, {s.Description.ApplicationVersion}, {s.Description.FieldDefinitionNumber}");
        Log.Debug(s.PrintBytes());
        tmp.Events.Add(s);
      };

      bool ok = Decode.IsFIT(stream);
      ok &= decoder.CheckIntegrity(stream);

      if (!Decode.IsFIT(stream))
      {
        Log.Error($"Is not a FIT file: {source}");
        fit = null;
        return false;
      }

      fit = tmp;
      return true; // Ignore integrity check
    }
  }
}