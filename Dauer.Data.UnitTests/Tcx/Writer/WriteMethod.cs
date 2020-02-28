using NUnit.Framework;

namespace Dauer.Data.UnitTests.Tcx.Writer
{
    [TestFixture]
    public class WriteMethod
    {
        [Test]
        public void WritesString()
        {
            var db = Fixtures.GetTrainingCenterDatabase();
            string xml = Data.Tcx.Writer.Write(db);
        }
    }
}