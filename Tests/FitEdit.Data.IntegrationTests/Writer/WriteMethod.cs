using FitEdit.Data.Fit;
using FitEdit.Model.Data;
using FitEdit.UnitTests.Shared;

namespace FitEdit.Data.IntegrationTests.Writer;

using Writer = Fit.Writer;

public class WriteMethod
{
  private const string source_ = @"..\..\..\..\TestData\15535326668_ACTIVITY.fit";

  /// <summary>
  /// Verify round trip integrity, i.e. encode(decode(file)) == file
  /// </summary>
  [Fact]
  public async Task PreservesFitFile()
  {
    var bytes = File.ReadAllBytes(source_);

    var ms1 = new MemoryStream(bytes);
    var ms2 = new MemoryStream();

    var fitFiles = await new Reader().ReadAsync(ms1);
    new Writer().Write(fitFiles, ms2);

    ms2.Position = 0;
    var fitFiles2 = await new Reader().ReadAsync(ms2);

    foreach (int i in Enumerable.Range(0, fitFiles.Count))
    {
      FitAssert.AreEqual(fitFiles[i], fitFiles2[i]);
    }
  }

  [Fact]
  public async Task PreservesBytes()
  {
    var bytes = File.ReadAllBytes(source_);

    var ms1 = new MemoryStream(bytes);

    var fitFiles = await new Reader().ReadAsync(ms1);

    // Assert each FIT file is equal to the original
    long byteIndex = 0;
    foreach (int i in Enumerable.Range(0, fitFiles.Count))
    {
      var ms2 = new MemoryStream();
      new Writer().Write(fitFiles[i], ms2);

      byte[] thisFileOnly = bytes.Skip((int)byteIndex).Take((int)ms2.Length).ToArray();
      FitAssert.AreEquivalentExceptHeader(ms2, thisFileOnly);

      byteIndex += ms2.Length;
    }
  }

  [Fact]
  public async Task SerializesToEqualJson()
  {
    // Arrange
    var fitFile = await new Reader().ReadAsync(source_);
    MemoryStream ms = new();
    new Writer().Write(fitFile, ms);
    ms.Position = 0;

    // Act
    var fitFile2 = await new Reader().ReadAsync(ms);

    var json = fitFile.ToPrettyJson();
    var json2 = fitFile2.ToPrettyJson();

    // Assert
    json.Should().Be(json2);
  }

}