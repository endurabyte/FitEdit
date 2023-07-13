#nullable enable
using System.Text;
using System.Text.RegularExpressions;
using Dauer.Model;
using Dauer.Model.Extensions;
using Dynastream.Fit;

namespace Dauer.Data.Fit;

public partial class Message : HasProperties
{
  public Mesg Mesg { get; set; }
  public bool IsNamed => Mesg.Name != "unknown";
  private static System.Reflection.Assembly? fit_;

  public Message(Mesg mesg)
  {
    Mesg = mesg;
  }

  static Message()
  {
    if (!AssemblyExtensions.TryGetLoadedAssembly("Dauer.Adapters.Fit", out var assembly))
    {
      return;
    }
    fit_ = assembly;
  }

  public void SetValue(string name, object? value)
  {
    Mesg.SetFieldValue(name, value);
    NotifyPropertyChanged(nameof(Mesg));
  }

  public object? GetValue(string name, bool prettify)
  {
    object value = TryParseFieldNumber(name, out byte id)
    ? Mesg.GetFieldValue(id)
    : Mesg.GetFieldValue(name);

    return prettify ? PrettifyField(name, value) : value;
  }

  private object? PrettifyField(string name, object? value)
  {
    if (value == null) { return value; }

    if (Mesg.Name == nameof(MesgNum.FileId))
    {
      if (name == nameof(FileIdMesg.FieldDefNum.Type))
      {
        name = nameof(Dynastream.Fit.File);
      }

      if (name == nameof(FileIdMesg.FieldDefNum.Product))
      {
        name = nameof(GarminProduct);
      }
    }

    if (Mesg.Name == nameof(MesgNum.Event))
    {
      if (name == nameof(EventMesg.FieldDefNum.Event))
      {
        name = nameof(Dynastream.Fit.Event);
      }
    }

    if (Mesg.Name == nameof(MesgNum.DeviceInfo))
    {
      if (name == nameof(DeviceInfoMesg.FieldDefNum.Product))
      {
        name = nameof(GarminProduct);
      }

      if (name == nameof(DeviceInfoMesg.FieldDefNum.DeviceType))
      {
        // Map DeviceType to e.g. LocalDeviceType, AntplusDeviceType, BleDeviceType
        var sourceType = Mesg.GetFieldValue(nameof(Dynastream.Fit.SourceType));
        string? stName = Enum.GetName(typeof(Dynastream.Fit.SourceType), sourceType);
        if (stName != null)
        {
          name = $"{stName}{name}";
        }
      }
    }

    if (Mesg.Name == nameof(MesgNum.DeviceSettings))
    {
      if (name == nameof(DeviceSettingsMesg.FieldDefNum.MountingSide))
      {
        name = nameof(Dynastream.Fit.Side);
      }
    }

    if (Mesg.Name == nameof(MesgNum.UserProfile))
    {
      // Map DisplayMeasure
      // e.g. ElevSetting, WeightSetting, SpeedSetting, DistSetting,
      //      PositionSetting, TemperatureSEtting, HeightSetting, DepthSetting
      if (name.EndsWith("Setting"))
      {
        string? typeName = Enum.GetName(typeof(Dynastream.Fit.DisplayMeasure), value) ?? name;
        return typeName ?? value;
        // Message doesn't have a value for this field.
        // Can happen when "Hide unused fields" is unchecked
        // Example: Attempt to get "PowerSetting" field from a run activity
      }
    }

    if (Mesg.Name == nameof(MesgNum.ZonesTarget))
    {
      if (name == nameof(ZonesTargetMesg.FieldDefNum.PwrCalcType))
      {
        name = nameof(PwrZoneCalc);
      }
      if (name == nameof(ZonesTargetMesg.FieldDefNum.HrCalcType))
      {
        name = nameof(HrZoneCalc);
      }
    }

    if (Mesg.Name == nameof(MesgNum.DeveloperDataId))
    {
      if (name == nameof(DeveloperDataIdMesg.FieldDefNum.ApplicationId))
      {

      }
    }

    if (Mesg.Name == nameof(MesgNum.Sport))
    {
      if (name == nameof(SportMesg.FieldDefNum.Name))
      {

      }
    }

    if (Mesg.Name == nameof(MesgNum.FieldDescription))
    {
      if (name == nameof(FieldDescriptionMesg.FieldDefNum.FitBaseTypeId))
      {
        // Map FitBaseTypeId => FitBaseType
        name = nameof(FitBaseType);
      }

      if (name == nameof(FieldDescriptionMesg.FieldDefNum.NativeMesgNum))
      {
        name = nameof(MesgNum);
      }
    }

    if (Mesg.Name == nameof(MesgNum.Lap))
    {
      if (name == nameof(LapMesg.FieldDefNum.Event))
      {
        // Map Event => Event
        // No need; they have the same name.
        //name = nameof(Dynastream.Fit.Event); 
      }

      if (name == nameof(LapMesg.FieldDefNum.AvgPowerPosition))
      {

      }
    }

    if (Mesg.Name == nameof(MesgNum.Record))
    {
      if (name == nameof(RecordMesg.FieldDefNum.Timestamp))
      {
        return Mesg.TimestampToDateTime((uint)value).GetDateTime();
      }
      if (name == nameof(RecordMesg.FieldDefNum.PositionLat))
      {
        return $"{((int)value).ToDegrees()}°N";
      }
      if (name == nameof(RecordMesg.FieldDefNum.PositionLong))
      {
        return $"{((int)value).ToDegrees()}°W";
      }
    }

    if (Mesg.Name == nameof(MesgNum.Activity))
    {
      if (name == nameof(ActivityMesg.FieldDefNum.Type))
      {
        // Map Type => Activity
        name = nameof(Dynastream.Fit.Activity);
      }
    }

    // Handle strings
    // Strings are encoded as ASCII byte arrays
    Field? field = Mesg.GetField(name);
    if (field != null && field.Type == Dynastream.Fit.Fit.String)
    {
      var bytes = (byte[])field.GetValue();

      // Remove null terminator, it shows as the "glyph not found" character U+25A1 □ WHITE SQUARE
      int nullTerminator = Array.IndexOf(bytes, (byte)0);
      bytes = bytes[..nullTerminator];
      return Encoding.ASCII.GetString(bytes);
    }

    // Convert int-like values to int
    if (!value.TryGetInt(out int i))
    {
      return value;
    }

    // Try to map int-like values to enums and static literals
    if (fit_ == null || !fit_.TryFindIdentifier(name, i, out string? identifier))
    {
      return value;
    }

    return identifier;
  }

  /// <summary>
  /// Parse e.g. "Field 253" and return 253
  /// </summary>
  private static bool TryParseFieldNumber(string field, out byte id)
  {
    id = 0;
    var match = fieldRegex().Match(field);
    return match.Success && byte.TryParse(match.Value, out id);
  }

  [GeneratedRegex("\\d+$")]
  private static partial Regex fieldRegex();
}