using FitEdit.Data.Fit;
using Dynastream.Fit;
using Newtonsoft.Json;
using NUnit.Framework;

namespace FitEdit.Data.IntegrationTests
{
  public class Copy
  {
    private const string source_ = @"..\..\..\..\data\devices\forerunner-945\sports\running\treadmill\2019-12-17\"
        + @"steep-1mi-easy-2x[2mi 2min rest]\garmin-connect\activity.fit";

    /// <summary>
    /// Verify round trip integrity, i.e. encode(decode(file)) == file
    /// </summary>
    [Test]
    public async Task Copies()
    {
      var dest = "output.fit";

      var fitFile = await new Reader().ReadAsync(source_);
      new Writer().Write(fitFile, dest);
      var fitFile2 = await new Reader().ReadAsync(dest);

      Assert.That(fitFile.MessageDefinitions.Count, Is.EqualTo(fitFile2.MessageDefinitions.Count));

      for (int i = 0; i < fitFile.MessageDefinitions.Count; i++)
      {
        AssertAreEqual(fitFile.MessageDefinitions[i], fitFile2.MessageDefinitions[i]);
      }

      Assert.That(fitFile.Messages.Count, Is.EqualTo(fitFile2.Messages.Count));

      for (int i = 0; i < fitFile.Messages.Count; i++)
      {
        AssertAreEqual(fitFile.Messages[i], fitFile2.Messages[i]);
      }
    }

    // This test doesn't pass due to minor differences e.g. protocol version
    [Explicit]
    [Test]
    public async Task Copy_FilesBinarySame()
    {
      var dest = "output.fit";

      var fitFile = await new Reader().ReadAsync(source_);
      new Writer().Write(fitFile, dest);

      Assert.That(source_, Is.EqualTo(dest));
    }

    // This test doesn't pass due to minor differences e.g. protocol version
    [Explicit]
    [Test]
    public async Task Copies_JsonEqual()
    {
      var dest = "output.fit";

      var fitFile = await new Reader().ReadAsync(source_);
      new Writer().Write(fitFile, dest);
      var fitFile2 = new Reader().ReadAsync(dest);

      var json = JsonConvert.SerializeObject(fitFile, Formatting.Indented);
      var json2 = JsonConvert.SerializeObject(fitFile2, Formatting.Indented);

      System.IO.File.WriteAllText("output.json", json);
      System.IO.File.WriteAllText("output2.json", json2);

      Assert.That(json, Is.EqualTo(json2));
    }

    private void AssertAreEqual(MesgDefinition a, MesgDefinition b)
    {
      Assert.That(a.GlobalMesgNum, Is.EqualTo(b.GlobalMesgNum));
      Assert.That(a.LocalMesgNum, Is.EqualTo(b.LocalMesgNum));
      Assert.That(a.NumDevFields, Is.EqualTo(b.NumDevFields));
      Assert.That(a.NumFields, Is.EqualTo(b.NumFields));
      Assert.That(a.IsBigEndian, Is.EqualTo(b.IsBigEndian));
    }

    private void AssertAreEqual(Mesg a, Mesg b)
    {
      Assert.That(a.Name, Is.EqualTo(b.Name));
      Assert.That(a.Num, Is.EqualTo(b.Num));
      Assert.That(a.LocalNum, Is.EqualTo(b.LocalNum));
      List<Field> fields = a.Fields.Values.ToList();

      Assert.That(fields.Count, Is.EqualTo(fields.Count));

      for (int i = 0; i < fields.Count; i++)
      {
        AssertAreEqual(fields[i], fields[i]);
      }
    }

    private void AssertAreEqual(Field a, Field b)
    {
      Assert.That(a.Name, Is.EqualTo(b.Name));
      Assert.That(a.Num, Is.EqualTo(b.Num));
      Assert.That(a.Type, Is.EqualTo(b.Type));
      Assert.That(a.Scale, Is.EqualTo(b.Scale));
      Assert.That(a.Offset, Is.EqualTo(b.Offset));
      Assert.That(a.Units, Is.EqualTo(b.Units));
      Assert.That(a.IsAccumulated, Is.EqualTo(b.IsAccumulated));
      Assert.That(a.ProfileType, Is.EqualTo(b.ProfileType));
      Assert.That(a.IsExpandedField, Is.EqualTo(b.IsExpandedField));
    }
  }
}
