using Dauer.Data.Fit;
using NUnit.Framework;

namespace Dauer.UnitTests
{
    [TestFixture]
    public class EncodeMethod
    {

        [Test]
        public void WritesFile()
        {
            var dest = "output.fit";

            var fitFile = new FitFile();
            new FitEncoder().Encode(fitFile, dest);
            FileAssert.Exists(dest);
        }
    }
}