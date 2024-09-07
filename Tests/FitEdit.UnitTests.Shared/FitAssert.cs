using Dynastream.Fit;
using FitEdit.Data.Fit;

namespace FitEdit.UnitTests.Shared;

public static class FitAssert
{
  public static void AreEqual(FitFile a, FitFile b)
  {
    a.MessageDefinitions.Count.Should().Be(b.MessageDefinitions.Count);

    foreach (var kvp in a.MessageDefinitions)
    {
      AreEqual(kvp.Value, b.MessageDefinitions[kvp.Key]);
    }

    foreach (var kvp in b.MessageDefinitions)
    {
      AreEqual(kvp.Value, a.MessageDefinitions[kvp.Key]);
    }

    a.Messages.Count.Should().Be(b.Messages.Count);

    for (int i = 0; i < a.Messages.Count; i++)
    {
      AreEqual(a.Messages[i], b.Messages[i]);
    }
  }

  public static void AreEqual(MesgDefinition a, MesgDefinition b)
  {
    a.GlobalMesgNum.Should().Be(b.GlobalMesgNum);
    a.LocalMesgNum.Should().Be(b.LocalMesgNum);
    a.NumDevFields.Should().Be(b.NumDevFields);
    a.NumFields.Should().Be(b.NumFields);
    a.IsBigEndian.Should().Be(b.IsBigEndian);
  }

  public static void AreEqual(Mesg a, Mesg b)
  {
    a.Name.Should().Be(b.Name);
    a.Num.Should().Be(b.Num);
    a.LocalNum.Should().Be(b.LocalNum);
    a.Fields.Count.Should().Be(b.Fields.Count);

    var fields = a.Fields.Values.ToList();

    for (int i = 0; i < fields.Count; i++)
    {
      AreEqual(fields[i], fields[i]);
    }
  }

  public static void AreEqual(Field a, Field b)
  {
    a.Name.Should().Be(b.Name);
    a.Num.Should().Be(b.Num);
    a.Type.Should().Be(b.Type);
    a.Scale.Should().Be(b.Scale);
    a.Offset.Should().Be(b.Offset);
    a.Units.Should().Be(b.Units);
    a.IsAccumulated.Should().Be(b.IsAccumulated);
    a.ProfileType.Should().Be(b.ProfileType);
    a.IsExpandedField.Should().Be(b.IsExpandedField);
  }
  
  /// <summary>
  /// Compare the stream to the expected byte array, 
  /// but skip the FIT headers, since they differ in protocol version and CRC
  /// </summary>
  public static void AreEquivalentExceptHeader(MemoryStream actual, byte[] expected)
  {
    actual.Position = 0;
    _ = new Header(actual);
    int headerLength = (int)actual.Position;

    var a2 = actual.ToArray().Skip(headerLength).ToArray();
    var b2 = expected.ToArray().Skip(headerLength).ToArray();

    a2.Should().BeEquivalentTo(b2);
  }
}
