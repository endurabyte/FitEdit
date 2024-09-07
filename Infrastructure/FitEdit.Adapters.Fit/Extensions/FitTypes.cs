namespace FitEdit.Adapters.Fit.Extensions;

using Fit = Dynastream.Fit.Fit;

public static class FitTypes
{
  static FitTypes()
  {
    TypeMap = Dynastream.Fit.Fit.BaseType.ToDictionary(bt => bt.baseTypeField, bt => bt);
    TypeIndexMap = Dynastream.Fit.Fit.BaseType
      .Select((bt, i) => new { bt, i })
      .ToDictionary(pair => (byte)pair.i, pair => pair.bt);
  }

  /// <summary>
  /// Map base type to FitType.
  /// e.g. given 0x86 (== 134), return the FitType whose base type is 0x86.
  /// </summary>
  public static Dictionary<byte, Dynastream.Fit.Fit.FitType> TypeMap { get; set; }

  /// <summary>
  /// Map index into the BaseTypes array to FitType.
  /// e.g. given Fit.UInt32 (==0x06), return the FitType at index 6, i.e. whose base type is 0x86 (== 134).
  /// </summary>
  public static Dictionary<byte, Dynastream.Fit.Fit.FitType> TypeIndexMap { get; set; }

  public static object GetInvalidValue(byte type) => TypeMap[type].invalidValue;

  public static byte GetFieldSize(byte type, List<object> values)
  {
    byte size = 0;

    switch (type & Fit.BaseTypeNumMask)
    {
      case Fit.Enum:
      case Fit.SInt8:
      case Fit.UInt8:
      case Fit.SInt16:
      case Fit.UInt16:
      case Fit.SInt32:
      case Fit.UInt32:
      case Fit.Float32:
      case Fit.Float64:
      case Fit.UInt8z:
      case Fit.UInt16z:
      case Fit.UInt32z:
      case Fit.SInt64:
      case Fit.UInt64:
      case Fit.UInt64z:
      case Fit.Byte:
        size = (byte)(values.Count * Fit.BaseType[type & Fit.BaseTypeNumMask].size);
        break;

      case Fit.String:
        // Each string may be of differing length
        int len = 0; // use int since byte can overflow
        // The fit binary must also include a null terminator
        foreach (byte[] element in values)
        {
          len += element.Length;
        }
        size = len > 255 ? (byte)255 : (byte)len;
        break;

      default:
        break;
    }
    return size;
  }
}