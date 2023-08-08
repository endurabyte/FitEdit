#nullable enable
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dauer.Model;
using Dauer.Model.Extensions;
using Dynastream.Fit;
using AssemblyExtensions = Dauer.Model.Extensions.AssemblyExtensions;

namespace Dauer.Data.Fit;

public partial class MessageWrapper : HasProperties
{
  public Mesg Mesg { get; set; }
  public bool IsNamed => Mesg.Name != "unknown";

  private static Assembly? fit_;

  public MessageWrapper(Mesg mesg)
  {
    Mesg = mesg;
  }

  static MessageWrapper()
  {
    if (!AssemblyExtensions.TryGetLoadedAssembly("Dauer.Adapters.Fit", out var assembly))
    {
      return;
    }
    fit_ = assembly;
  }

  public void SetValue(string name, object? value, bool pretty)
  {
    try
    {
      if (pretty && TryUnprettifyField(name, value, out object? result))
      {
        value = result;
      }

      Mesg.SetFieldValue(name, value);
      NotifyPropertyChanged(nameof(Mesg));
    }
    catch (Exception e)
    {
      Log.Error(e);
    }
  }

  public object? GetValue(string name, bool prettify)
  {
    object value = TryParseFieldNumber(name, out byte id)
    ? Mesg.GetFieldValue(id)
    : Mesg.GetFieldValue(name);

    return prettify ? PrettifyField(name, value) : value;
  }

  /// <summary>
  /// Return the named values if the field type is an enum or maps to public static literal fields.
  /// Return null if the field is not an enum or contains no public static literal fields.
  /// TODO make extension method on Field
  /// </summary>
  public static List<object?>? GetNamedValues(string mesgName, Field? field)
  {
    if (field == null) { return null; }

    object? value = field.GetValue();
    if (value == null) { return null; }

    // E.g. "byte" might be the backing type of an enum.
    Type backingType = value.GetType();

    if (backingType.IsEnum)
    {
      return backingType.GetEnumEntries().Values.Cast<object?>().ToList();
    }

    if (fit_ == null) { return null; }

    // e.g. field.Name == "Manufacturer" and i == 1 => identifier = "Garmin" 
    string name = MapFieldNameToTypeName(mesgName, field.Name, value);

    bool haveIdentifier = value.TryGetInt(out int i) && fit_.TryFindIdentifier(name, i, out string? _);
    if (!haveIdentifier) { return null; }

    if (!fit_.TryFindType(name, out Type? fieldType) || fieldType == null)
    {
      return null;
    }

    // If the int matched an identifier e.g. "Garmin" of Dynastream.Fit.Manufacturer (const ushort field)
    // or "Running" of Dynastream.Fit.Sport (enum backed by byte)

    bool targetIsEnum = fieldType.IsEnum;

    if (targetIsEnum)
    {
      return fieldType.GetEnumEntries().Values.Cast<object?>().ToList();
    }

    var literals = fieldType.GetLiterals().Values.Cast<object?>().ToList();
    return literals.Any() ? literals : null;
  }

  /// <summary>
  /// If the given field holds an enum, convert the given value to the enum type.
  /// Get the enum type of the field.
  /// </summary>
  public bool TryConvertToEnum(string fieldName, string? s, out object? result)
  {
    result = null;
    if (s == null) { return false; }
    if (fit_ == null) { return false; }

    Field? field = Mesg.GetField(fieldName);
    if (field == null) { return false; }

    return fit_.TryFindType($"{field.ProfileType}", out Type? t) 
      && t != null
      && t.IsAssignableTo(typeof(Enum))
      && Enum.TryParse(t, s, ignoreCase: true, out result);
  }

  /// <summary>
  /// Return true if the field is backed by a public static literal field.
  /// Works also for enum types.
  /// </summary>
  public static bool TryConvertToLiteral(string fieldName, string? s, out object? result)
  {
    result = null;
    if (s == null) { return false; }
    if (fit_ == null) { return false; }

    if (!fit_.TryFindType(fieldName, out Type? t) || t == null)
    {
      return false;
    }

    FieldInfo? fieldInfo = t.GetField(s, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public);

    if (fieldInfo == null || !fieldInfo.IsLiteral)
    {
      return false;
    }

    result = fieldInfo.GetRawConstantValue();
    return true;
  }

  /// <summary>
  /// If the field type is string, convert the given string to a byte array. Include a null terminator.
  /// </summary>
  public bool TryConvertStringToBytes(string fieldName, string? s, out object? result)
  {
    result = null;
    if (s == null) { return false; }

    Field? field = Mesg.GetField(fieldName);
    if (field == null) { return false; }

    if (field.Type != Dynastream.Fit.Fit.String)
    {
      return false;
    }

    // Strings are encoded as ASCII byte arrays
    // Add null terminator
    byte[] bytes = Encoding.ASCII.GetBytes(s);
    byte[] withNull = new byte[bytes.Length + 1];
    Array.Copy(bytes, withNull, bytes.Length);
    withNull[bytes.Length] = 0;
    result = bytes;
    return true;
  }

  /// <summary>
  /// If the field type is string, convert its backing byte array to a string. Remove the null terminator.
  /// </summary>
  public bool TryConvertBytesToString(string fieldName, out string? result)
  {
    result = null;

    Field? field = Mesg.GetField(fieldName);
    if (field == null) { return false; }

    if (field.Type != Dynastream.Fit.Fit.String)
    {
      return false;
    }

    var bytes = (byte[])field.GetValue();

    // Strings are encoded as ASCII byte arrays
    // Remove null terminator, it shows as the "glyph not found" character U+25A1 □ WHITE SQUARE
    int nullTerminator = Array.IndexOf(bytes, (byte)0);
    if (nullTerminator != -1)
    {
      bytes = bytes[..nullTerminator];
    }
    result = Encoding.ASCII.GetString(bytes);
    return true;
  }

  private static string MapFieldNameToTypeName(string mesgName, string fieldName, object? fieldValue)
  {
    return mesgName switch
    {
      nameof(MesgNum.FileId) when fieldName == nameof(FileIdMesg.FieldDefNum.Product) => nameof(GarminProduct),
      nameof(MesgNum.DeviceInfo) when fieldName == nameof(DeviceInfoMesg.FieldDefNum.Product) => nameof(GarminProduct),
      nameof(MesgNum.UserProfile) when fieldName.EndsWith("Setting") => nameof(Dynastream.Fit.DisplayMeasure),
      _ => fieldName,
    };
  }

  private bool TryUnprettifyField(string name, object? value, out object? result)
  {
    result = null;

    name = MapFieldNameToTypeName(Mesg.Name, name, value);

    if (TryConvertToLiteral(name, value as string, out result))
    {
      return true;
    }

    if (TryConvertToEnum(name, value as string, out result))
    {
      return true;
    }

    if (TryConvertStringToBytes(name, value as string, out result))
    {
      return true;
    }

    if (Mesg.Name == nameof(MesgNum.Record))
    {
      if (name == nameof(RecordMesg.FieldDefNum.Timestamp))
      {
        if (value != null)
        {
          if (value is string dtString)
          {
            uint timestamp = new Dynastream.Fit.DateTime(System.DateTime.Parse(dtString)).GetTimeStamp();
            result = timestamp;
            return true;
          }
        }
      }

      if (name == nameof(RecordMesg.FieldDefNum.PositionLat))
      {
        if (GeospatialExtensions.TryGetCoordinate(value as string, out double d))
        {
          result = d.ToSemicircles();
          return true;
        }
      }

      if (name == nameof(RecordMesg.FieldDefNum.PositionLong))
      {
        if (GeospatialExtensions.TryGetCoordinate(value as string, out double d))
        {
          result = d.ToSemicircles();
          return true;
        }
      }
    }

    // Manual interventions (just an example)
    if (Mesg.Name == nameof(MesgNum.UserProfile))
    {
      if (name == nameof(Gender) && value is string gender)
      {
        if (Enum.TryParse(gender, ignoreCase: true, out Gender g))
        {
          result = g;
          return true;
        }
      }
    }

    return false;
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

    if (TryConvertBytesToString(name, out string? s))
    {
      return s;
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