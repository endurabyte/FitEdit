#nullable enable
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FitEdit.Model;
using FitEdit.Model.Extensions;
using Dynastream.Fit;
using AssemblyExtensions = FitEdit.Model.Extensions.AssemblyExtensions;
using DateTime = System.DateTime;

namespace FitEdit.Data.Fit;

public static partial class MesgExtensions
{ 
  private static readonly Assembly? fit_;

  static MesgExtensions()
  {
    if (!AssemblyExtensions.TryGetLoadedAssembly("FitEdit.Adapters.Fit", out var assembly))
    {
      return;
    }
    fit_ = assembly;
  }

  /// <summary>
  /// Return true if the given message occurs between the given DateTimes.
  /// If either DateTime is not specified, it is not considered.
  /// </summary>
  public static bool IsBetween(this Mesg mesg, DateTime after = default, DateTime before = default) => mesg switch
  {
    IDurationOfTime dur when (after == default || dur.GetStartTime().GetDateTime() > after)
                             && (before == default || dur.GetTimestamp().GetDateTime() <= before) => true,

    IInstantOfTime inst when (after == default || inst.GetTimestamp().GetDateTime() > after)
                             && (before == default || inst.GetTimestamp().GetDateTime() <= before) => true,

    _ => false,
  };
  
  public static void SetFieldValue(this Mesg mesg, string name, object? value, bool pretty)
  {
    try
    {
      if (pretty && TryUnprettifyField(mesg, name, value, out object? result))
      {
        value = result;
      }

      mesg.SetFieldValue(name, value);
    }
    catch (Exception e)
    {
      Log.Error(e);
    }
  }

  public static object? GetFieldValue(this Mesg mesg, string name, bool prettify)
  {
    object value = TryParseFieldNumber(name, out byte id)
    ? mesg.GetFieldValue(id)
    : mesg.GetFieldValue(name);

    return prettify ? PrettifyField(mesg, name, value) : value;
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
  public static bool TryConvertToEnum(Mesg mesg, string fieldName, string? s, out object? result)
  {
    result = null;
    if (s == null) { return false; }
    if (fit_ == null) { return false; }

    Field? field = mesg.GetField(fieldName);
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
  public static bool TryConvertStringToBytes(Mesg mesg, string fieldName, string? s, out object? result)
  {
    result = null;
    if (s == null) { return false; }

    Field? field = mesg.GetField(fieldName);
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
  public static bool TryConvertBytesToString(Mesg mesg, string fieldName, out string? result)
  {
    result = null;

    Field? field = mesg.GetField(fieldName);
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

  private static string PrependSourceType(Mesg mesg, string fieldName)
  {
    var sourceType = mesg.GetFieldValue(nameof(SourceType));
    string? stName = null;

    try
    {
      stName = Enum.GetName(typeof(SourceType), sourceType);
    }
    catch (Exception)
    {
      stName = null;
    }
    return stName == null ? fieldName : $"{stName}{fieldName}";
  }

  private static string MapFieldNameToTypeName(Mesg mesg, string fieldName, object? fieldValue)
  {
    string? mapped = MapFieldNameToTypeName(mesg.Name, fieldName, fieldValue);
    return string.Equals(mapped, fieldName) 

      // If no mapping occured, try again with data specific to this message
      ? mesg.Name switch
        {
          // Map DeviceType to e.g. LocalDeviceType, AntplusDeviceType, BleDeviceType
          nameof (MesgNum.DeviceInfo) when fieldName == nameof(DeviceInfoMesg.FieldDefNum.DeviceType) => PrependSourceType(mesg, fieldName),
          _ => fieldName,
        } 
      
      : mapped;
  }

  private static string MapFieldNameToTypeName(string mesgName, string fieldName, object? fieldValue) => mesgName switch
  {
    // Map Type => Activity
    nameof(MesgNum.Activity) when fieldName == nameof(ActivityMesg.FieldDefNum.Type) => nameof(Activity),
    nameof(MesgNum.FileId) when fieldName == nameof(FileIdMesg.FieldDefNum.Product) => nameof(GarminProduct),
    nameof(MesgNum.FileId) when fieldName == nameof(FileIdMesg.FieldDefNum.Type) => nameof(Dynastream.Fit.File),
    nameof(MesgNum.Event) when fieldName == nameof(EventMesg.FieldDefNum.Event) => nameof(Event),
    nameof(MesgNum.DeviceInfo) when fieldName == nameof(DeviceInfoMesg.FieldDefNum.Product) => nameof(GarminProduct),
    nameof(MesgNum.DeviceSettings) when fieldName == nameof(DeviceSettingsMesg.FieldDefNum.MountingSide) => nameof(Side),

    nameof(MesgNum.UserProfile) when fieldName.EndsWith("Setting") => nameof(DisplayMeasure),
    nameof(MesgNum.ZonesTarget) when fieldName == nameof(ZonesTargetMesg.FieldDefNum.HrCalcType) => nameof(HrZoneCalc),
    nameof(MesgNum.ZonesTarget) when fieldName == nameof(ZonesTargetMesg.FieldDefNum.PwrCalcType) => nameof(PwrZoneCalc),
    nameof(MesgNum.FieldDescription) when fieldName == nameof(FieldDescriptionMesg.FieldDefNum.FitBaseTypeId) => nameof(FitBaseType),
    nameof(MesgNum.FieldDescription) when fieldName == nameof(FieldDescriptionMesg.FieldDefNum.NativeMesgNum) => nameof(MesgNum),
    nameof(MesgNum.Lap) when fieldName == nameof(LapMesg.FieldDefNum.Event) => nameof(Event),
    _ => fieldName,
  };

  private static bool TryMapDateTimeToTimestamp(string? dtString, out uint timestamp)
  {
    timestamp = 0;
    if (dtString is null) { return false; }
    if (!System.DateTime.TryParse(dtString, out System.DateTime dt)) { return false; }

    timestamp = new Dynastream.Fit.DateTime(dt).GetTimeStamp();
    return true;
  }

  private static bool TryUnprettifyField(Mesg mesg, string name, object? value, out object? result)
  {
    name = MapFieldNameToTypeName(mesg, name, value);

    if (TryConvertToLiteral(name, value as string, out result))
    {
      return true;
    }

    if (TryConvertToEnum(mesg, name, value as string, out result))
    {
      return true;
    }

    if (TryConvertStringToBytes(mesg, name, value as string, out result))
    {
      return true;
    }

    if (name.Contains(nameof(RecordMesg.FieldDefNum.Timestamp)))
    {
      if (TryMapDateTimeToTimestamp(value as string, out uint timestamp))
      {
        result = timestamp;
        return true;
      }
    }

    if (mesg.Name == nameof(MesgNum.FileId))
    {
      if (name == nameof(FileIdMesg.FieldDefNum.TimeCreated))
      {
        if (TryMapDateTimeToTimestamp(value as string, out uint timestamp))
        {
          result = timestamp;
          return true;
        }
      }
    }

    if (mesg.Name == nameof(MesgNum.Lap))
    {
      if (name == nameof(LapMesg.FieldDefNum.StartTime))
      {
        if (TryMapDateTimeToTimestamp(value as string, out uint timestamp))
        {
          result = timestamp;
          return true;
        }
      }
    }

    if (mesg.Name == nameof(MesgNum.Session))
    {
      if (name == nameof(SessionMesg.FieldDefNum.StartTime))
      {
        if (TryMapDateTimeToTimestamp(value as string, out uint timestamp))
        {
          result = timestamp;
          return true;
        }
      }
    }

    if (name == "StartTime")
    {
      if (TryMapDateTimeToTimestamp(value as string, out uint timestamp))
      {
        result = timestamp;
        return true;
      }
    }

    if (name.Contains(nameof(RecordMesg.FieldDefNum.PositionLat)) 
     || name.Contains(nameof(RecordMesg.FieldDefNum.PositionLong)))
    {
      if (value is string s && GeospatialExtensions.TryGetCoordinate(s, out double d))
      {
        result = d.ToSemicircles();
        return true;
      }
    }

    return false;
  }

  private static object? PrettifyField(this Mesg mesg, string name, object? value)
  {
    if (value == null) { return value; }

    name = MapFieldNameToTypeName(mesg, name, value);

    if (name == nameof(HrMesg.FieldDefNum.FractionalTimestamp))
    {
      return value;
    }

    if (name.Contains(nameof(RecordMesg.FieldDefNum.Timestamp)))
    {
      uint uint32 = Convert.ToUInt32(value);
      return mesg.TimestampToDateTime(uint32).GetDateTime();
    }

    if (name.Contains(nameof(RecordMesg.FieldDefNum.PositionLat)))
    {
      return $"{((int)value).ToDegrees()}°";
    }

    if (name.Contains(nameof(RecordMesg.FieldDefNum.PositionLong)))
    {
      return $"{((int)value).ToDegrees()}°";
    }

    if (mesg.Name == nameof(MesgNum.FileId))
    {
      if (name == nameof(FileIdMesg.FieldDefNum.TimeCreated))
      {
        return mesg.TimestampToDateTime((uint)value).GetDateTime();
      }
    }

    if (mesg.Name == nameof(MesgNum.Lap))
    {
      if (name == nameof(LapMesg.FieldDefNum.StartTime))
      {
        return mesg.TimestampToDateTime((uint)value).GetDateTime();
      }
    }

    if (mesg.Name == nameof(MesgNum.Session))
    {
      if (name == nameof(SessionMesg.FieldDefNum.StartTime))
      {
        return mesg.TimestampToDateTime((uint)value).GetDateTime();
      }
    }

    if (name == "StartTime")
    {
      return mesg.TimestampToDateTime((uint)value).GetDateTime();
    }

    if (TryConvertBytesToString(mesg, name, out string? s))
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

  [GeneratedRegex("\\d+ $")]
  private static partial Regex fieldRegex();
}