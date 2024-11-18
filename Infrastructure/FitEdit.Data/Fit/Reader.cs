using System.Diagnostics;
using FitEdit.Model;
using Dynastream.Fit;
using FitEdit.Adapters.Fit.Extensions;

namespace FitEdit.Data.Fit;

public class Reader
{
  public async Task<List<FitFile>> ReadAsync(string source)
  {
    await using var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
    return await ReadAsync(stream);
  }

  public async Task<List<FitFile>> ReadAsync(Stream stream)
  {
    if (!TryGetDecoder(stream, out List<FitFile> fitFiles, out Decode decoder))
    {
      return [];
    }

    try
    {
      if (!decoder.CheckIntegrity(stream))
      {
        Log.Warn($"Integrity Check failed...");
        if (decoder.IsDataSizeInvalid)
        {
          Log.Warn("Invalid Size detected...");
        }

        Log.Warn("Attempting to read by skipping the header...");
        var result = await decoder.ReadAsync(stream, DecodeMode.InvalidHeader);
        if (result != DecodeResult.OkEndOfFile)
        {
          Log.Error($"Could not read FIT file by skipping the header");
          return null;
        }
      }

      {
        var result = await decoder.ReadAsync(stream);
        if (result != DecodeResult.OkEndOfFile)
        {
          Log.Error($"Could not read FIT file");
          return null;
        }
      }

      foreach (var fitFile in fitFiles)
      {
        Log.Info($"Found {fitFile.Messages.Count} messages and {fitFile.MessageDefinitions.Count} definitions");
        fitFile.ForwardfillEvents();
      }
      return fitFiles;
    }
    catch (Exception ex)
    {
      Log.Error(ex.Message);
      return null;
    }
  }

  /// <summary>
  /// Read the given number of FIT messages. Return false on error or end of stream.
  /// This gives single-threaded environments such as WASM a chance to update the UI,
  /// for example to show a progress bar or a message to the user.
  /// </summary>
  public static async Task<DecodeResult> ReadSomeAsync(Stream stream, Decode decoder, int messageCount = 1)
  {
    try
    {
      return await decoder.ReadAsync(stream, DecodeMode.Normal, messageCount);
    }
    catch (FitException e)
    {
      Log.Error($"{e}");
      return DecodeResult.ErrFitException; // Keep reading despite exception
    }
  }

  public bool TryGetDecoder(Stream stream, out List<FitFile> fits, out Decode decoder)
  {
    decoder = new Decode();
    fits = [];
    var fitsTemp = new List<FitFile>();
    var tmp = new FitFile();
    fitsTemp.Add(tmp);

    decoder.FitFileRead += () =>
    {
      Log.Debug($"Read FIT file with {tmp.Events.Count} messages");
      tmp = new FitFile();
    };

    decoder.MesgEvent += (o, s) =>
    {
      s.DebugLog();

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
      s.DebugLog();

      tmp.MessageDefinitions[s.mesgDef.GlobalMesgNum] = s.mesgDef;
      tmp.Events.Add(s);
    };

    decoder.DeveloperFieldDescriptionEvent += (o, s) =>
    {
      s.DebugLog();

      tmp.Events.Add(s);
    };

    bool ok = Decode.IsFIT(stream);
    ok &= decoder.CheckIntegrity(stream);

    if (!Decode.IsFIT(stream))
    {
      Log.Error($"File is not a FIT file");
      return false;
    }

    fits = fitsTemp;
    return true; // Ignore integrity check
  }
}

public static class FitLog
{ 
}