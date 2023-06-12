using Dauer.Data.Fit;
using Dynastream.Fit;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dauer.Data.IntegrationTests
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

      Assert.AreEqual(fitFile.MessageDefinitions.Count, fitFile2.MessageDefinitions.Count);

      for (int i = 0; i < fitFile.MessageDefinitions.Count; i++)
      {
        AssertAreEqual(fitFile.MessageDefinitions[i], fitFile2.MessageDefinitions[i]);
      }

      Assert.AreEqual(fitFile.Messages.Count, fitFile2.Messages.Count);

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

      FileAssert.AreEqual(source_, dest);
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

      Assert.AreEqual(json, json2);
    }

    private void AssertAreEqual(MesgDefinition a, MesgDefinition b)
    {
      Assert.AreEqual(a.GlobalMesgNum, b.GlobalMesgNum);
      Assert.AreEqual(a.LocalMesgNum, b.LocalMesgNum);
      Assert.AreEqual(a.NumDevFields, b.NumDevFields);
      Assert.AreEqual(a.NumFields, b.NumFields);
      Assert.AreEqual(a.IsBigEndian, b.IsBigEndian);
    }

    private void AssertAreEqual(Mesg a, Mesg b)
    {
      Assert.AreEqual(a.Name, b.Name);
      Assert.AreEqual(a.Num, b.Num);
      Assert.AreEqual(a.LocalNum, b.LocalNum);
      List<Field> fields = a.Fields.Values.ToList();

      Assert.AreEqual(fields.Count, fields.Count);

      for (int i = 0; i < fields.Count; i++)
      {
        AssertAreEqual(fields[i], fields[i]);
      }
    }

    private void AssertAreEqual(Field a, Field b)
    {
      Assert.AreEqual(a.Name, b.Name);
      Assert.AreEqual(a.Num, b.Num);
      Assert.AreEqual(a.Type, b.Type);
      Assert.AreEqual(a.Scale, b.Scale);
      Assert.AreEqual(a.Offset, b.Offset);
      Assert.AreEqual(a.Units, b.Units);
      Assert.AreEqual(a.IsAccumulated, b.IsAccumulated);
      Assert.AreEqual(a.ProfileType, b.ProfileType);
      Assert.AreEqual(a.IsExpandedField, b.IsExpandedField);
    }
  }
}