using Newtonsoft.Json;

namespace FitEdit.Data.UnitTests.Fit.Reader
{
  public class ReadMethod
  {
    private const string source_ = @"..\..\..\..\TestData\2019-12-17-treadmill-run.fit";

    [Fact]
    public async Task ReadsFile()
    {
      var fitFile = await new Data.Fit.Reader().ReadAsync(source_);
      fitFile.Should().NotBeNull(); 
    }

    [Fact]
    public async Task DumpsToJson()
    {
      var fitFile = await new Data.Fit.Reader().ReadAsync(source_);

      fitFile.Invoking(f =>
      {
        var json = JsonConvert.SerializeObject(f, Formatting.Indented);
        json.Should().NotBeNullOrEmpty();
      }).Should().NotThrow();
    }
  }
}