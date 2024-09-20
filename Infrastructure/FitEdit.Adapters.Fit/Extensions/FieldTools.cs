namespace FitEdit.Adapters.Fit.Extensions;

using Dynastream.Fit;
using Dynastream.Utility;
using Fit = Dynastream.Fit.Fit;

public static class FieldTools
{
  public static void ReadFieldValue(
      FieldBase field,
      byte size,
      EndianBinaryReader mesgReader)
  {
    byte baseType = (byte)(field.Type & Fit.BaseTypeNumMask);
    // strings may be an array and are of variable length
    if (baseType == Fit.String)
    {
      byte[] bytes = mesgReader.ReadBytes(size);
      List<byte> utf8Bytes = new List<byte>();

      if (!Array.Exists(bytes, x => x != 0))
      {
        // Array has no non zero values, don't add any strings
        return;
      }

      for (int i = 0; i < size; i++)
      {
        byte b = bytes[i];
        utf8Bytes.Add(b);

        if (b == 0x00)
        {
          field.AddValue(utf8Bytes.ToArray());
          utf8Bytes.Clear();
        }
      }

      if (utf8Bytes.Count != 0)
      {
        // Add a Null Terminator
        //utf8Bytes.Add(0);
        field.AddValue(utf8Bytes.ToArray());
        utf8Bytes.Clear();
      }
    }
    else
    {
      int numElements = baseType < 0 || baseType >= Fit.BaseType.Length
        ? 1
        : size / Fit.BaseType[baseType].size;

      for (int i = 0; i < numElements; i++)
      {
        object value;
        bool invalid = TryReadValue(
            out value,
            field.Type,
            mesgReader,
            size);

        if (!invalid || numElements > 1)
        {
          field.SetRawValue(i, value);
        }
      }

      // Save raw bytes
      if (!FitConfig.CacheSourceData) { return; }

      mesgReader.BaseStream.Position -= Math.Min(mesgReader.BaseStream.Position, size);
      field.SourceData = mesgReader.ReadBytes(size);
    }
  }

  private static bool TryReadValue(
      out object value,
      byte type,
      EndianBinaryReader mesgReader,
      byte size)
  {
    bool invalid = true;
    byte baseTypeNum = (byte)(type & Fit.BaseTypeNumMask);
    switch (baseTypeNum)
    {
      case Fit.Enum:
      case Fit.Byte:
      case Fit.UInt8:
      case Fit.UInt8z:
        value = mesgReader.ReadByte();
        if ((byte)value != (byte)Fit.BaseType[baseTypeNum].invalidValue)
        {
          invalid = false;
        }
        break;

      case Fit.SInt8:
        value = mesgReader.ReadSByte();
        if ((sbyte)value != (sbyte)Fit.BaseType[baseTypeNum].invalidValue)
        {
          invalid = false;
        }
        break;

      case Fit.SInt16:
        value = mesgReader.ReadInt16();
        if ((short)value != (short)Fit.BaseType[baseTypeNum].invalidValue)
        {
          invalid = false;
        }
        break;

      case Fit.UInt16:
      case Fit.UInt16z:
        value = mesgReader.ReadUInt16();
        if ((ushort)value !=
            (ushort)Fit.BaseType[baseTypeNum].invalidValue)
        {
          invalid = false;
        }
        break;

      case Fit.SInt32:
        value = mesgReader.ReadInt32();
        if ((int)value != (int)Fit.BaseType[baseTypeNum].invalidValue)
        {
          invalid = false;
        }
        break;

      case Fit.UInt32:
      case Fit.UInt32z:
        value = mesgReader.ReadUInt32();
        if ((uint)value != (uint)Fit.BaseType[baseTypeNum].invalidValue)
        {
          invalid = false;
        }
        break;

      case Fit.SInt64:
        value = mesgReader.ReadInt64();
        if ((long)value != (long)Fit.BaseType[baseTypeNum].invalidValue)
        {
          invalid = false;
        }
        break;

      case Fit.UInt64:
      case Fit.UInt64z:
        value = mesgReader.ReadUInt64();
        if ((ulong)value != (ulong)Fit.BaseType[baseTypeNum].invalidValue)
        {
          invalid = false;
        }
        break;

      case Fit.Float32:
        value = mesgReader.ReadSingle();
        if (!float.IsNaN((float)value))
        {
          invalid = false;
        }
        break;

      case Fit.Float64:
        value = mesgReader.ReadDouble();
        if (!double.IsNaN((double)value))
        {
          invalid = false;
        }
        break;

      default:
        value = mesgReader.ReadBytes(size);
        break;
    }

    return invalid;
  }

  public static void WriteField(FieldBase field, byte expectedSize, BinaryWriter bw)
  {
    bool typeKnown = FitTypes.TypeMap.ContainsKey(field.Type);
    if (!typeKnown)
    {
      bw.Write(new byte[expectedSize]);
      return;
    }

    List<object> rawValues = Enumerable.Range(0, field.GetNumValues())
      .Select(field.GetRawValue)
      .ToList();

    PadField(expectedSize, field.Type, rawValues);

    var baseType = (byte)(field.Type & Fit.BaseTypeNumMask);

    foreach (var value in rawValues)
    {
      Write(bw, baseType, value);
    }
  }

  public static void Write(BinaryWriter bw, byte baseType, object value)
  {
    switch (baseType)
    {
      case Fit.Enum:
      case Fit.Byte:
      case Fit.UInt8:
      case Fit.UInt8z:
        bw.Write((byte)value);
        break;

      case Fit.SInt8:
        bw.Write((sbyte)value);
        break;

      case Fit.SInt16:
        bw.Write((short)value);
        break;

      case Fit.UInt16:
      case Fit.UInt16z:
        bw.Write((ushort)value);
        break;

      case Fit.SInt32:
        bw.Write((int)value);
        break;

      case Fit.UInt32:
      case Fit.UInt32z:
        bw.Write((uint)value);
        break;

      case Fit.SInt64:
        bw.Write((long)value);
        break;

      case Fit.UInt64:
      case Fit.UInt64z:
        bw.Write((ulong)value);
        break;

      case Fit.Float32:
        bw.Write((float)value);
        break;

      case Fit.Float64:
        bw.Write((double)value);
        break;

      case Fit.String:
        bw.Write((byte[])value);
        break;

      default:
        break;
    }
  }

  public static void PadField(int expectedSize, byte type, List<object> values)
  {
    int actualSize = FitTypes.GetFieldSize(type, values);

    while (actualSize < expectedSize)
    {
      object toAdd = type switch
      {
        Fit.String => FitEdit.Adapters.Fit.Extensions.FieldTools.PadString(expectedSize - actualSize, values),
        _ => FitTypes.GetInvalidValue(type),
      };

      values.Add(toAdd);
      actualSize = FitTypes.GetFieldSize(type, values);
    }
  }

  public static byte[] PadString(int padAmount, List<object> values)
  {
    byte[] str = values.SelectMany(v => v as byte[]).ToArray();
    byte[] padding = Enumerable.Repeat(Convert.ToByte(FitTypes.GetInvalidValue(Fit.String)), padAmount).ToArray();
    byte[] output = new byte[str.Length + padAmount];

    // Fill with str and padding
    Array.Copy(str, output, str.Length);
    Array.Copy(padding, 0, output, str.Length, padAmount);

    return output;
  }
}
