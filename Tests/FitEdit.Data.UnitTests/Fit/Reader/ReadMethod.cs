using System.Text.Json;
using FitEdit.Model.Data;

namespace FitEdit.Data.UnitTests.Fit.Reader;

public class ReadMethod
{
  [Theory]
  [InlineData("../../../../TestData/2019-12-17-treadmill-run.fit")]
  [InlineData("../../../../TestData/15535326668_ACTIVITY.fit")]
  [InlineData("../../../../TestData/2024-11-17-elemnt-roam.fit")]
  public async Task ReadsFile(string source)
  {
    var fitFile = await new Data.Fit.Reader().ReadAsync(source);
    fitFile.Should().NotBeNull(); 
  }

  [Theory]
  [InlineData("../../../../TestData/2019-12-17-treadmill-run.fit")]
  [InlineData("../../../../TestData/15535326668_ACTIVITY.fit")]
  [InlineData("../../../../TestData/2024-11-17-elemnt-roam.fit")]
  public async Task DumpsToJson(string source)
  {
    var fitFile = await new Data.Fit.Reader().ReadAsync(source);

    fitFile.Invoking(f =>
    {
      var json = f.ToJson();
      json.Should().NotBeNullOrEmpty();
    }).Should().NotThrow();
  }
}