using System.Security.Cryptography;
using System.Text.RegularExpressions;
using DynamicData;
using FitEdit.Data.Fit;
using FitEdit.Model;
using FitEdit.Model.Data;
using FitEdit.Model.Extensions;
using FitEdit.UnitTests.Shared;
using Microsoft.Extensions.Logging;

namespace FitEdit.Data.IntegrationTests.Writer;

using Writer = Fit.Writer;

public class WriteMethod
{
  /// <summary>
  /// Verify round trip integrity, i.e. encode(decode(file)) == file
  /// </summary>
  [Theory]
  [InlineData("../../../../TestData/2019-12-17-treadmill-run.fit")]
  [InlineData("../../../../TestData/15535326668_ACTIVITY.fit")]
  [InlineData("../../../../TestData/2024-09-05-groningen-paddling.fit")]
  [InlineData("../../../../TestData/2024-11-17-elemnt-roam.fit")]
  public async Task PreservesFitFile(string source)
  {
    var bytes = await File.ReadAllBytesAsync(source);

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

  [Theory]
  [InlineData("../../../../TestData/2019-12-17-treadmill-run.fit")]
  [InlineData("../../../../TestData/15535326668_ACTIVITY.fit")]
  [InlineData("../../../../TestData/2024-11-17-elemnt-roam.fit")]
  public async Task PreservesBytes(string source)
  {
    var bytes = await File.ReadAllBytesAsync(source);

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

  [Theory]
  [InlineData("../../../../TestData/2019-12-17-treadmill-run.fit")]
  [InlineData("../../../../TestData/15535326668_ACTIVITY.fit")]
  [InlineData("../../../../TestData/2024-11-17-elemnt-roam.fit")]
  public async Task SerializesToEqualJson(string source)
  {
    // Arrange
    var fitFile = await new Reader().ReadAsync(source);
    MemoryStream ms = new();
    new Writer().Write(fitFile, ms);
    ms.Position = 0;

    // Act
    var fitFile2 = await new Reader().ReadAsync(ms);

    var json = fitFile.ToJson().Sha256();
    var json2 = fitFile2.ToJson().Sha256();

    // Assert
    json.Should().Be(json2);
  }
}