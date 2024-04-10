using FitEdit.Data.Fit;
using NUnit.Framework;

namespace FitEdit.Data.UnitTests.Fit.Writer;

[TestFixture]
public class WriteMethod
{
  [Test]
  public void WritesFile()
  {
    var dest = "output.fit";

    var fitFile = new FitFile();
    new Data.Fit.Writer().Write(fitFile, dest);
    Assert.That(File.Exists(dest), Is.True);
  }
}