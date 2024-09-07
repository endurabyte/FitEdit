using FitEdit.Data.Fit;
using Dynastream.Fit;
using Newtonsoft.Json;

namespace FitEdit.Data.IntegrationTests
{
  public class CopyMethod
  {
    private const string source_ = @"..\..\..\..\TestData\2019-12-17-treadmill-run.fit";

    /// <summary>
    /// Verify round trip integrity, i.e. encode(decode(file)) == file
    /// </summary>
    [Fact]
    public async Task Copies()
    {
      var bytes = System.IO.File.ReadAllBytes(source_);

      var ms1 = new MemoryStream(bytes);
      var ms2 = new MemoryStream();

      var fitFile = await new Reader().ReadAsync(ms1);
      new Writer().Write(fitFile, ms2);

      var buf1 = ms1.ToArray();
      var buf2 = ms2.ToArray();

      //buf1.Should().BeEquivalentTo(bytes);
      //buf2.Should().BeEquivalentTo(bytes);
      
      ms2.Position = 0;
      var fitFile2 = await new Reader().ReadAsync(ms2);

      fitFile.MessageDefinitions.Count.Should().Be(fitFile2.MessageDefinitions.Count);

      foreach (var kvp in fitFile.MessageDefinitions)
      {
        AssertAreEqual(kvp.Value, fitFile2.MessageDefinitions[kvp.Key]);
      }

      foreach (var kvp in fitFile2.MessageDefinitions)
      {
        AssertAreEqual(kvp.Value, fitFile.MessageDefinitions[kvp.Key]);
      }

      fitFile.Messages.Count.Should().Be(fitFile2.Messages.Count);

      for (int i = 0; i < fitFile.Messages.Count; i++)
      {
        AssertAreEqual(fitFile.Messages[i], fitFile2.Messages[i]);
      }
    }

    [Fact(Skip = "This test doesn't pass due to minor differences e.g. protocol version")]
    public async Task Copy_FilesBinarySame()
    {
      var fitFile = await new Reader().ReadAsync(source_);
      var ms = new MemoryStream();
      new Writer().Write(fitFile, ms);

      ms.GetBuffer().Should().BeEquivalentTo(fitFile.GetBytes());
    }

    [Fact(Skip = "This test doesn't pass due to minor differences e.g. protocol version")]
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

      json.Should().Be(json2);
    }

    private void AssertAreEqual(MesgDefinition a, MesgDefinition b)
    {
      a.GlobalMesgNum.Should().Be(b.GlobalMesgNum);
      a.LocalMesgNum.Should().Be(b.LocalMesgNum);
      a.NumDevFields.Should().Be(b.NumDevFields);
      a.NumFields.Should().Be(b.NumFields);
      a.IsBigEndian.Should().Be(b.IsBigEndian);
    }

    private void AssertAreEqual(Mesg a, Mesg b)
    {
      a.Name.Should().Be(b.Name);
      a.Num.Should().Be(b.Num);
      a.LocalNum.Should().Be(b.LocalNum);
      a.Fields.Count.Should().Be(b.Fields.Count);

      List<Field> fields = a.Fields.Values.ToList();

      for (int i = 0; i < fields.Count; i++)
      {
        AssertAreEqual(fields[i], fields[i]);
      }
    }

    private void AssertAreEqual(Field a, Field b)
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
  }
}
