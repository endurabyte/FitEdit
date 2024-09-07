using FitEdit.Data.Fit;

namespace FitEdit.Data.UnitTests.Fit.Writer;

public class WriteMethod
{
  [Fact]
  public void WritesFile()
  {
    var dest = "output.fit";

    var fitFile = new FitFile();
    new Data.Fit.Writer().Write(fitFile, dest);
    File.Exists(dest).Should().BeTrue();
  }
}