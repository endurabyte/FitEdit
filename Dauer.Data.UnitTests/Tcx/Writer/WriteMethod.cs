using Dauer.UnitTests.Shared;
using NUnit.Framework;

namespace Dauer.Data.UnitTests.Tcx.Writer
{
    [TestFixture]
    public class WriteMethod
    {
        [Test]
        public void WritesString()
        {
            var db = TcxFixtures.GetTrainingCenterDatabase();
            string xml = Data.Tcx.Writer.Write(db);
        }
    }
}